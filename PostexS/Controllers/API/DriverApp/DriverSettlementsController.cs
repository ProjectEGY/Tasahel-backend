using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using PostexS.Models.Dtos;
using PostexS.Models.Enums;

namespace PostexS.Controllers.API
{
    /// <summary>
    /// تطبيق المندوب - التقفيلات (سجل التقفيلات، تفاصيل التقفيلة، في انتظار التقفيل)
    /// </summary>
    public class DriverSettlementsController : DriverBaseController
    {
        private readonly IGeneric<Order> _orders;
        private readonly IGeneric<Wallet> _wallets;

        public DriverSettlementsController(
            UserManager<ApplicationUser> userManager,
            IGeneric<ApplicationUser> user,
            IGeneric<Location> locations,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IGeneric<Order> orders,
            IGeneric<Wallet> wallets)
            : base(userManager, user, locations, configuration, httpClientFactory)
        {
            _orders = orders;
            _wallets = wallets;
        }

        #region سجل التقفيلات (التقفيلات المكتملة)

        /// <summary>
        /// سجل التقفيلات - قائمة التقفيلات اللي اتعملت للمندوب
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. يرجع قائمة التقفيلات المكتملة مع المبلغ وعدد الطلبات والتاريخ.
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

            var query = _wallets.GetAllAsIQueryable(
                filter: x => x.ActualUserId == user.Id && !x.IsDeleted && x.TransactionType == TransactionType.OrderFinished,
                orderby: q => q.OrderByDescending(w => w.CreateOn),
                IncludeProperties: "Orders");

            var totalCount = query.Count();

            var settlements = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var dtos = settlements.Select(s => new SenderSettlementDto
            {
                Id = s.Id,
                Amount = s.Amount,
                Date = s.CreateOn,
                OrderCount = s.Orders != null ? s.Orders.Count : 0,
                TransactionType = s.TransactionType,
                Note = s.Note,
            }).ToList();

            var response = new PaginatedResponse<SenderSettlementDto>(dtos, pageNumber, pageSize, totalCount);

            baseResponse.Data = response;
            return Ok(baseResponse);
        }

        /// <summary>
        /// تفاصيل تقفيلة - يرجع بيانات التقفيلة مع قائمة الطلبات اللي جواها
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. المندوب يقدر يشوف تقفيلاته بس - مش هيقدر يدخل على تقفيلة مندوب تاني.
        /// يرجع: بيانات التقفيلة + قائمة الطلبات (الكود، اسم المستلم، المحصل، العمولة، الحالة).
        /// </remarks>
        /// <param name="walletId">معرف التقفيلة (Wallet Id)</param>
        [HttpGet("Settlements/{walletId}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetSettlementDetails([FromRoute] long walletId)
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            // التحقق إن التقفيلة بتاعت المندوب الحالي
            var settlement = await _wallets.GetSingle(
                x => x.Id == walletId && x.ActualUserId == user.Id && !x.IsDeleted && x.TransactionType == TransactionType.OrderFinished);

            if (settlement == null)
            {
                baseResponse.ErrorCode = Errors.TheOrderNotExistOrDeleted;
                baseResponse.ErrorMessage = "التقفيلة غير موجودة";
                return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
            }

            // الطلبات المرتبطة بالتقفيلة - عن طريق WalletId أو ReturnedWalletId
            var orders = _orders.Get(x =>
                x.DeliveryId == user.Id &&
                (x.WalletId == walletId || x.ReturnedWalletId == walletId)
            ).ToList();

            var orderDtos = new List<DriverSettlementOrderDto>();
            foreach (var o in orders)
            {
                var sender = await _user.GetObj(x => x.Id == o.ClientId);
                orderDtos.Add(new DriverSettlementOrderDto
                {
                    Code = o.Code,
                    ClientName = o.ClientName,
                    ClientPhone = o.ClientPhone,
                    SenderName = sender?.Name ?? "-",
                    ArrivedCost = o.ArrivedCost,
                    DeliveryCost = o.DeliveryCost,
                    Cost = o.Cost,
                    TotalCost = o.TotalCost,
                    Status = o.Status,
                    StatusArabic = GetStatusInArabic(o.Status),
                });
            }

            baseResponse.Data = new
            {
                Settlement = new SenderSettlementDto
                {
                    Id = settlement.Id,
                    Amount = settlement.Amount,
                    Date = settlement.CreateOn,
                    OrderCount = orderDtos.Count,
                    TransactionType = settlement.TransactionType,
                    Note = settlement.Note,
                },
                Statistics = new
                {
                    TotalCollected = orders.Sum(o => o.ArrivedCost),
                    TotalDriverCommission = orders.Sum(o => o.DeliveryCost),
                    TotalToCompany = orders.Sum(o => o.ArrivedCost) - orders.Sum(o => o.DeliveryCost),
                    DeliveredCount = orders.Count(o => o.Status == OrderStatus.Delivered),
                    DeliveredWithEditPriceCount = orders.Count(o => o.Status == OrderStatus.Delivered_With_Edit_Price),
                    PartialDeliveredCount = orders.Count(o => o.Status == OrderStatus.PartialDelivered),
                    ReturnedCount = orders.Count(o => o.Status == OrderStatus.Returned),
                    ReturnedPaidDeliveryCount = orders.Count(o => o.Status == OrderStatus.Returned_And_Paid_DeliveryCost),
                    ReturnedOnSenderCount = orders.Count(o => o.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender),
                    PartialReturnedCount = orders.Count(o => o.Status == OrderStatus.PartialReturned),
                },
                Orders = orderDtos
            };

            return Ok(baseResponse);
        }

        #endregion

        #region في انتظار التقفيل (توصيل)

        /// <summary>
        /// طلبات في انتظار التقفيل (توصيل) - الطلبات اللي المندوب سلمها بس لسه ما اتقفلتش
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. يرجع الطلبات اللي حالتها: تم التوصيل، تم التوصيل مع تعديل السعر، تسليم جزئي، مؤجل.
        /// مع إحصائيات: عدد كل حالة، إجمالي المحصل، نسبة المندوب، المبلغ المطلوب تسليمه للشركة.
        /// </remarks>
        [HttpGet("PendingDeliveries")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> PendingDeliveries(
            [FromHeader(Name = "Latitude")] double? latitude,
            [FromHeader(Name = "Longitude")] double? longitude,
            int page = 1, int size = 15)
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            var allPending = _orders.Get(x =>
                x.DeliveryId == user.Id && !x.IsDeleted && !x.Finished
                && (x.Status == OrderStatus.Delivered
                    || x.Status == OrderStatus.Delivered_With_Edit_Price
                    || x.Status == OrderStatus.PartialDelivered
                    || x.Status == OrderStatus.Waiting)
            ).ToList();

            var stats = new
            {
                TotalOrders = allPending.Count,
                DeliveredCount = allPending.Count(o => o.Status == OrderStatus.Delivered),
                DeliveredWithEditPriceCount = allPending.Count(o => o.Status == OrderStatus.Delivered_With_Edit_Price),
                PartialDeliveredCount = allPending.Count(o => o.Status == OrderStatus.PartialDelivered),
                WaitingCount = allPending.Count(o => o.Status == OrderStatus.Waiting),
                TotalCollected = allPending.Sum(o => o.ArrivedCost),
                TotalDriverCommission = allPending.Sum(o => o.DeliveryCost),
                TotalToCompany = allPending.Sum(o => o.ArrivedCost) - allPending.Sum(o => o.DeliveryCost),
            };

            var totalCount = allPending.Count;
            var pagedOrders = allPending.OrderByDescending(x => x.LastUpdated ?? x.CreateOn)
                .Skip((page - 1) * size).Take(size).ToList();

            var dto = new List<OrderDto>();
            foreach (var item in pagedOrders)
            {
                var model = new OrderDto(item);
                var sender = await _user.GetObj(x => x.Id == item.ClientId);
                model.AgentName = sender?.Name ?? "-";
                model.SenderName = sender?.Name ?? "-";
                model.SenderNumber = sender?.PhoneNumber ?? "-1";
                model.SenderSecondaryPhone = sender?.SecondaryPhone;
                model.SenderWhatsappPhone = sender?.WhatsappPhone;
                dto.Add(model);
            }

            await UpdateLocationIfProvided(latitude, longitude, user);

            baseResponse.Data = new
            {
                Statistics = stats,
                Orders = new PaginatedResponse<OrderDto>(dto, page, size, totalCount)
            };
            return Ok(baseResponse);
        }

        #endregion

        #region مرتجعات في انتظار التقفيل

        /// <summary>
        /// مرتجعات في انتظار التقفيل - الطلبات المرتجعة اللي لسه ما اتقفلتش
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. يرجع الطلبات اللي حالتها: مرتجع كامل، مرتجع جزئي، مرتجع ودفع شحن، مرتجع وشحن على الراسل.
        /// مع إحصائيات: عدد كل حالة، إجمالي المحصل، نسبة المندوب.
        /// </remarks>
        [HttpGet("PendingReturns")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> PendingReturns(
            [FromHeader(Name = "Latitude")] double? latitude,
            [FromHeader(Name = "Longitude")] double? longitude,
            int page = 1, int size = 15)
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            var allPending = _orders.Get(x =>
                x.DeliveryId == user.Id && !x.IsDeleted && !x.ReturnedFinished
                && (x.Status == OrderStatus.Returned
                    || x.Status == OrderStatus.PartialReturned
                    || x.Status == OrderStatus.Returned_And_Paid_DeliveryCost
                    || x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender)
            ).ToList();

            var stats = new
            {
                TotalOrders = allPending.Count,
                ReturnedCount = allPending.Count(o => o.Status == OrderStatus.Returned),
                PartialReturnedCount = allPending.Count(o => o.Status == OrderStatus.PartialReturned),
                ReturnedPaidDeliveryCount = allPending.Count(o => o.Status == OrderStatus.Returned_And_Paid_DeliveryCost),
                ReturnedOnSenderCount = allPending.Count(o => o.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender),
                TotalCollected = allPending.Sum(o => o.ArrivedCost),
                TotalDriverCommission = allPending.Sum(o => o.DeliveryCost),
                TotalToCompany = allPending.Sum(o => o.ArrivedCost) - allPending.Sum(o => o.DeliveryCost),
            };

            var totalCount = allPending.Count;
            var pagedOrders = allPending.OrderByDescending(x => x.LastUpdated ?? x.CreateOn)
                .Skip((page - 1) * size).Take(size).ToList();

            var dto = new List<OrderDto>();
            foreach (var item in pagedOrders)
            {
                var model = new OrderDto(item);
                var sender = await _user.GetObj(x => x.Id == item.ClientId);
                model.AgentName = sender?.Name ?? "-";
                model.SenderName = sender?.Name ?? "-";
                model.SenderNumber = sender?.PhoneNumber ?? "-1";
                model.SenderSecondaryPhone = sender?.SecondaryPhone;
                model.SenderWhatsappPhone = sender?.WhatsappPhone;
                dto.Add(model);
            }

            await UpdateLocationIfProvided(latitude, longitude, user);

            baseResponse.Data = new
            {
                Statistics = stats,
                Orders = new PaginatedResponse<OrderDto>(dto, page, size, totalCount)
            };
            return Ok(baseResponse);
        }

        #endregion

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
