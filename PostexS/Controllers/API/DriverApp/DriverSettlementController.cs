using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using PostexS.Models.Dtos;
using PostexS.Models.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace PostexS.Controllers.API
{
    /// <summary>
    /// تطبيق المندوب - سجل التقفيلات
    /// </summary>
    public class DriverSettlementController : DriverBaseController
    {
        private readonly IGeneric<Wallet> _wallets;
        private readonly IGeneric<Order> _orders;

        public DriverSettlementController(
            UserManager<ApplicationUser> userManager,
            IGeneric<ApplicationUser> user,
            IGeneric<Location> locations,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IGeneric<Wallet> wallets,
            IGeneric<Order> orders)
            : base(userManager, user, locations, configuration, httpClientFactory)
        {
            _wallets = wallets;
            _orders = orders;
        }

        /// <summary>
        /// قائمة التقفيلات - يرجع كل التقفيلات مع نسبة المندوب والإحصائيات وملخص الطلبات
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. كل تقفيلة فيها: المبلغ، إجمالي نسبة المندوب، إحصائيات حسب الحالة، وملخص الطلبات (الكود، اسم المستلم، المحصل، النسبة، الحالة).
        /// مرتبة من الأحدث للأقدم. الحد الأقصى لحجم الصفحة: 50.
        /// </remarks>
        [HttpGet("Settlements")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetSettlements(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 50) pageSize = 50;

            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");

            var query = _wallets.GetAllAsIQueryable(
                filter: x => x.ActualUserId == user.Id && !x.IsDeleted && x.TransactionType == TransactionType.OrderFinished,
                orderby: q => q.OrderByDescending(w => w.CreateOn),
                IncludeProperties: "Orders");

            var totalCount = query.Count();

            var settlements = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // جلب أسماء اللي قفلوا التقفيلات
            var closerIds = settlements.Select(s => s.UserId).Distinct().ToList();
            var closers = _user.Get(x => closerIds.Contains(x.Id)).ToList();

            var dtos = settlements.Select(s =>
            {
                var orders = s.Orders?.ToList() ?? new List<Order>();
                var totalCollected = orders.Sum(o => o.ArrivedCost);
                var totalDriverCommission = orders.Sum(o => o.DeliveryCost);
                var closer = closers.FirstOrDefault(c => c.Id == s.UserId);

                return new DriverSettlementDetailsDto
                {
                    Id = s.Id,
                    Amount = s.Amount,
                    TotalDriverCommission = totalDriverCommission,
                    TotalCollected = totalCollected,
                    TotalToCompany = totalCollected - totalDriverCommission,
                    Date = s.CreateOn,
                    OrderCount = orders.Count,
                    SettledBy = closer?.Name ?? "-",
                    SettledAt = TimeZoneInfo.ConvertTimeFromUtc(s.CreateOn, egyptTimeZone).ToString("yyyy-MM-dd hh:mm tt"),
                    Note = s.Note,
                    DeliveredCount = orders.Count(o => o.Status == OrderStatus.Delivered),
                    DeliveredWithEditPriceCount = orders.Count(o => o.Status == OrderStatus.Delivered_With_Edit_Price),
                    PartialDeliveredCount = orders.Count(o => o.Status == OrderStatus.PartialDelivered),
                    ReturnedCount = orders.Count(o => o.Status == OrderStatus.Returned),
                    ReturnedPaidDeliveryCount = orders.Count(o => o.Status == OrderStatus.Returned_And_Paid_DeliveryCost),
                    ReturnedOnSenderCount = orders.Count(o => o.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender),
                    RejectedCount = orders.Count(o => o.Status == OrderStatus.Rejected),
                    OrdersSummary = orders.Select(o => new DriverSettlementOrderSummaryDto
                    {
                        Code = o.Code,
                        ClientName = o.ClientName,
                        ArrivedCost = o.ArrivedCost,
                        DriverCommission = o.DeliveryCost,
                        Status = o.Status,
                        StatusArabic = GetStatusInArabic(o.Status),
                    }).ToList(),
                };
            }).ToList();

            var response = new PaginatedResponse<DriverSettlementDetailsDto>(dtos, pageNumber, pageSize, totalCount);

            baseResponse.Data = response;
            return Ok(baseResponse);
        }

        /// <summary>
        /// تفاصيل تقفيلة - يرجع بيانات التقفيلة الكاملة مع كل طلب ونسبة المندوب فيه
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. يرجع بيانات التقفيلة + قائمة الطلبات المرتبطة بيها.
        /// كل طلب فيه: الكود، اسم المستلم، رقمه، اسم الراسل، المبلغ المحصل، نسبة المندوب، الحالة.
        /// التقفيلة فيها: إجمالي نسبة المندوب، إجمالي المحصل، المبلغ المسلم للشركة، إحصائيات حسب الحالة.
        /// </remarks>
        [HttpGet("Settlements/{walletId}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetSettlementDetails([FromRoute] long walletId)
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            var settlement = await _wallets.GetSingle(
                x => x.Id == walletId && x.ActualUserId == user.Id && !x.IsDeleted && x.TransactionType == TransactionType.OrderFinished);

            if (settlement == null)
            {
                baseResponse.ErrorCode = Errors.TheOrderNotExistOrDeleted;
                baseResponse.ErrorMessage = "التقفيلة غير موجودة";
                return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
            }

            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");

            var orders = _orders.Get(x => x.WalletId == walletId && x.DeliveryId == user.Id).ToList();

            var senderIds = orders.Select(o => o.ClientId).Distinct().ToList();
            var senders = _user.Get(x => senderIds.Contains(x.Id)).ToList();

            // مين اللي قفل التقفيلة
            var closer = await _user.GetObj(x => x.Id == settlement.UserId);

            var orderDtos = orders.Select(o =>
            {
                var sender = senders.FirstOrDefault(s => s.Id == o.ClientId);
                return new DriverSettlementOrderDto
                {
                    Code = o.Code,
                    ClientName = o.ClientName,
                    ClientPhone = o.ClientPhone,
                    SenderName = sender?.Name ?? "-",
                    ArrivedCost = o.ArrivedCost,
                    DeliveryCost = o.DeliveryCost,
                    DriverCommission = o.DeliveryCost,
                    Cost = o.Cost,
                    TotalCost = o.TotalCost,
                    Status = o.Status,
                    StatusArabic = GetStatusInArabic(o.Status),
                };
            }).ToList();

            var totalCollected = orders.Sum(o => o.ArrivedCost);
            var totalDriverCommission = orders.Sum(o => o.DeliveryCost);

            baseResponse.Data = new
            {
                Settlement = new DriverSettlementDetailsDto
                {
                    Id = settlement.Id,
                    Amount = settlement.Amount,
                    TotalDriverCommission = totalDriverCommission,
                    TotalCollected = totalCollected,
                    TotalToCompany = totalCollected - totalDriverCommission,
                    Date = settlement.CreateOn,
                    OrderCount = orderDtos.Count,
                    SettledBy = closer?.Name ?? "-",
                    SettledAt = TimeZoneInfo.ConvertTimeFromUtc(settlement.CreateOn, egyptTimeZone).ToString("yyyy-MM-dd hh:mm tt"),
                    Note = settlement.Note,
                    DeliveredCount = orders.Count(o => o.Status == OrderStatus.Delivered),
                    DeliveredWithEditPriceCount = orders.Count(o => o.Status == OrderStatus.Delivered_With_Edit_Price),
                    PartialDeliveredCount = orders.Count(o => o.Status == OrderStatus.PartialDelivered),
                    ReturnedCount = orders.Count(o => o.Status == OrderStatus.Returned),
                    ReturnedPaidDeliveryCount = orders.Count(o => o.Status == OrderStatus.Returned_And_Paid_DeliveryCost),
                    ReturnedOnSenderCount = orders.Count(o => o.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender),
                    RejectedCount = orders.Count(o => o.Status == OrderStatus.Rejected),
                },
                Orders = orderDtos
            };

            return Ok(baseResponse);
        }

        private string GetStatusInArabic(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Placed => "جديد",
                OrderStatus.Assigned => "جارى التوصيل",
                OrderStatus.Delivered => "تم التوصيل",
                OrderStatus.Waiting => "مؤجل",
                OrderStatus.Rejected => "مرفوض",
                OrderStatus.Finished => "منتهي",
                OrderStatus.Completed => "تم تسويته",
                OrderStatus.PartialDelivered => "تم التوصيل جزئي",
                OrderStatus.Returned => "مرتجع كامل",
                OrderStatus.PartialReturned => "مرتجع جزئي",
                OrderStatus.Delivered_With_Edit_Price => "تم التوصيل مع تعديل السعر",
                OrderStatus.Returned_And_Paid_DeliveryCost => "مرتجع ودفع شحن",
                OrderStatus.Returned_And_DeliveryCost_On_Sender => "مرتجع وشحن على الراسل",
                _ => status.ToString()
            };
        }
    }
}
