using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// تطبيق المندوب - الإحصائيات والتقارير والملخص المالي
    /// </summary>
    public class DriverStatisticsController : DriverBaseController
    {
        private readonly IGeneric<Order> _orders;

        public DriverStatisticsController(
            UserManager<ApplicationUser> userManager,
            IGeneric<ApplicationUser> user,
            IGeneric<Location> locations,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IGeneric<Order> orders)
            : base(userManager, user, locations, configuration, httpClientFactory)
        {
            _orders = orders;
        }

        /// <summary>
        /// إحصائيات المندوب الأساسية
        /// </summary>
        [HttpGet("Statistics")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Statistics([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude)
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            var userid = user.Id;
            StatisticsDto dto = new StatisticsDto();
            dto.Name = user.Name;
            if (user.UserType == UserType.Driver)
            {
                dto.CurrentOrdersCount = _orders.Get(x => x.DeliveryId == userid &&
                        (x.Status == OrderStatus.Assigned || x.Status == OrderStatus.Waiting)
                        && !x.IsDeleted).Count();
                var orders = _orders.Get(x =>
                    (x.Status == OrderStatus.Delivered
                    || (x.Status == OrderStatus.Waiting && x.DeliveryId != null)
                    || (x.Status == OrderStatus.Rejected)
                    || (x.Status == OrderStatus.PartialDelivered)
                    || (x.Status == OrderStatus.Returned)
                    ) && !x.Finished && !x.IsDeleted
                    && x.DeliveryId == userid).ToList();

                dto.ReturnedCount = orders.Count(x => x.Status == OrderStatus.Returned);
                dto.PartialDeliveredCount = orders.Count(x => x.Status == OrderStatus.PartialDelivered);
                dto.DeliveredCount = orders.Count(x => x.Status == OrderStatus.Delivered);
                dto.WaitingCount = orders.Count(x => x.Status == OrderStatus.Waiting);
                dto.RejectedCount = orders.Count(x => x.Status == OrderStatus.Rejected);
                dto.AllOrdersCount = dto.CurrentOrdersCount + orders.Count();

                var OrdersMoney = orders.Where(x => x.Status != OrderStatus.PartialReturned).Sum(x => x.ArrivedCost);
                dto.OrdersMoney = OrdersMoney;
                var DriverMoney = orders.Where(x => x.Status != OrderStatus.PartialReturned).Sum(x => x.DeliveryCost);
                dto.DriverMoney = DriverMoney;
                dto.SystemMoney = OrdersMoney - DriverMoney;
            }

            await UpdateLocationIfProvided(latitude, longitude, user);
            baseResponse.Data = dto;
            return Ok(baseResponse);
        }

        /// <summary>
        /// إحصائيات المندوب الشاملة - جميع الطلبات مع النسب المئوية
        /// </summary>
        [HttpGet("DriverStatistics")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> DriverStatistics([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude)
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            var userid = user.Id;

            var dto = new DriverStatisticsDto();
            dto.Name = user.Name;

            if (user.UserType == UserType.Driver)
            {
                dto.CurrentOrdersCount = _orders.Get(x => x.DeliveryId == userid &&
                        (x.Status == OrderStatus.Assigned || x.Status == OrderStatus.Waiting)
                        && !x.IsDeleted).Count();

                var orders = _orders.Get(x =>
                    (x.Status != OrderStatus.PartialReturned) && !x.IsDeleted
                    && x.DeliveryId == userid).ToList();

                dto.ReturnedCount = orders.Count(x => x.Status == OrderStatus.Returned);
                dto.PartialDeliveredCount = orders.Count(x => x.Status == OrderStatus.PartialDelivered);
                dto.PartialReturned_ReceivedCount = orders.Count(x => x.Status == OrderStatus.PartialReturned && x.Finished);
                dto.Returned_ReceivedCount = orders.Count(x => x.Status == OrderStatus.Returned && x.Finished);
                dto.DeliveredCount = orders.Count(x => x.Status == OrderStatus.Delivered);
                dto.DeliveredFinishedCount = orders.Count(x => x.Status == OrderStatus.Delivered && x.Finished);
                dto.WaitingCount = orders.Count(x => x.Status == OrderStatus.Waiting);
                dto.RejectedCount = orders.Count(x => x.Status == OrderStatus.Rejected);
                dto.AllOrdersCount = orders.Count();

                var OrdersMoney = orders.Where(x => x.Status != OrderStatus.PartialReturned).Sum(x => x.ArrivedCost);
                dto.OrdersMoney = OrdersMoney;
                var DriverMoney = orders.Where(x => x.Status != OrderStatus.PartialReturned).Sum(x => x.DeliveryCost);
                dto.DriverMoney = DriverMoney;
                dto.SystemMoney = OrdersMoney - DriverMoney;

                if (dto.AllOrdersCount > 0)
                {
                    dto.DeliveredPercentage = Math.Round((double)dto.DeliveredCount / dto.AllOrdersCount * 100, 1);
                    dto.PartialDeliveredPercentage = Math.Round((double)dto.PartialDeliveredCount / dto.AllOrdersCount * 100, 1);
                    dto.ReturnedPercentage = Math.Round((double)dto.ReturnedCount / dto.AllOrdersCount * 100, 1);
                }
            }

            await UpdateLocationIfProvided(latitude, longitude, user);
            baseResponse.Data = dto;
            return Ok(baseResponse);
        }

        /// <summary>
        /// إحصائيات المندوب الحالية - الطلبات الغير منتهية فقط مع النسب
        /// </summary>
        [HttpGet("CurrentStatistics")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> CurrentStatistics([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude)
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            var userid = user.Id;

            var dto = new DriverCurrentStatisticsDto();
            dto.Name = user.Name;

            if (user.UserType == UserType.Driver)
            {
                dto.CurrentOrdersCount = _orders.Get(x => x.DeliveryId == userid &&
                        (x.Status == OrderStatus.Assigned || x.Status == OrderStatus.Waiting)
                        && !x.IsDeleted).Count();

                var orders = _orders.Get(x =>
                    (x.Status == OrderStatus.Delivered
                    || x.Status == OrderStatus.Waiting
                    || x.Status == OrderStatus.Rejected
                    || x.Status == OrderStatus.PartialDelivered
                    || x.Status == OrderStatus.Returned
                    || x.Status == OrderStatus.Assigned
                    ) && !x.Finished && !x.IsDeleted
                    && x.DeliveryId == userid).ToList();

                dto.ReturnedCount = orders.Count(x => x.Status == OrderStatus.Returned);
                dto.PartialDeliveredCount = orders.Count(x => x.Status == OrderStatus.PartialDelivered);
                dto.DeliveredCount = orders.Count(x => x.Status == OrderStatus.Delivered);
                dto.WaitingCount = orders.Count(x => x.Status == OrderStatus.Waiting);
                dto.RejectedCount = orders.Count(x => x.Status == OrderStatus.Rejected);
                dto.AssignedCount = orders.Count(x => x.Status == OrderStatus.Assigned);
                dto.AllOrdersCount = orders.Count();

                var OrdersMoney = orders.Where(x => x.Status != OrderStatus.PartialReturned).Sum(x => x.ArrivedCost);
                dto.OrdersMoney = OrdersMoney;
                var DriverMoney = orders.Where(x => x.Status != OrderStatus.PartialReturned).Sum(x => x.DeliveryCost);
                dto.DriverMoney = DriverMoney;
                dto.SystemMoney = OrdersMoney - DriverMoney;

                if (dto.AllOrdersCount > 0)
                {
                    dto.DeliveredPercentage = Math.Round((double)dto.DeliveredCount / dto.AllOrdersCount * 100, 1);
                    dto.ReturnedPercentage = Math.Round((double)dto.ReturnedCount / dto.AllOrdersCount * 100, 1);
                    dto.PartialDeliveredPercentage = Math.Round((double)dto.PartialDeliveredCount / dto.AllOrdersCount * 100, 1);
                    dto.RejectedPercentage = Math.Round((double)dto.RejectedCount / dto.AllOrdersCount * 100, 1);
                    dto.WaitingPercentage = Math.Round((double)dto.WaitingCount / dto.AllOrdersCount * 100, 1);
                    dto.AssignedPercentage = Math.Round((double)dto.AssignedCount / dto.AllOrdersCount * 100, 1);
                }
            }

            await UpdateLocationIfProvided(latitude, longitude, user);
            baseResponse.Data = dto;
            return Ok(baseResponse);
        }

        /// <summary>
        /// تقرير المندوب بفلتر تاريخ مع تفاصيل الطلبات
        /// </summary>
        [HttpGet("DriverReport")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> DriverReport([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude,
            string from = null, string to = null, int page = 1, int size = 50)
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            var userid = user.Id;

            var dto = new DriverReportDto();
            dto.Name = user.Name;

            if (user.UserType == UserType.Driver)
            {
                DateTime fromDate = DateTime.MinValue;
                DateTime toDate = DateTime.MaxValue;

                if (!string.IsNullOrEmpty(from))
                {
                    if (DateTime.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedFrom))
                        fromDate = parsedFrom.ToUniversalTime();
                }
                if (!string.IsNullOrEmpty(to))
                {
                    if (DateTime.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTo))
                        toDate = parsedTo.AddDays(1).ToUniversalTime();
                }

                dto.FromDate = from ?? "الكل";
                dto.ToDate = to ?? "الكل";

                var orders = _orders.Get(x =>
                    x.DeliveryId == userid && !x.IsDeleted
                    && x.Status != OrderStatus.PartialReturned
                    && x.CreateOn >= fromDate && x.CreateOn < toDate
                ).ToList();

                dto.TotalOrders = orders.Count;
                dto.DeliveredCount = orders.Count(x => x.Status == OrderStatus.Delivered || x.Status == OrderStatus.Delivered_With_Edit_Price);
                dto.ReturnedCount = orders.Count(x => x.Status == OrderStatus.Returned || x.Status == OrderStatus.Returned_And_Paid_DeliveryCost || x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender);
                dto.PartialDeliveredCount = orders.Count(x => x.Status == OrderStatus.PartialDelivered);
                dto.RejectedCount = orders.Count(x => x.Status == OrderStatus.Rejected);
                dto.WaitingCount = orders.Count(x => x.Status == OrderStatus.Waiting);

                dto.TotalCollected = orders.Sum(x => x.ArrivedCost);
                dto.DriverCommission = orders.Sum(x => x.DeliveryCost);
                dto.SystemMoney = dto.TotalCollected - dto.DriverCommission;

                if (dto.TotalOrders > 0)
                {
                    dto.DeliveredPercentage = Math.Round((double)dto.DeliveredCount / dto.TotalOrders * 100, 1);
                    dto.ReturnedPercentage = Math.Round((double)dto.ReturnedCount / dto.TotalOrders * 100, 1);
                    dto.PartialDeliveredPercentage = Math.Round((double)dto.PartialDeliveredCount / dto.TotalOrders * 100, 1);
                }

                var pagedOrders = orders.OrderByDescending(x => x.CreateOn).Skip((page - 1) * size).Take(size).ToList();
                foreach (var order in pagedOrders)
                {
                    var orderDto = new OrderDto(order);
                    var sender = await _user.GetObj(x => x.Id == order.ClientId);
                    orderDto.AgentName = sender?.Name ?? "-";
                    orderDto.SenderName = sender?.Name ?? "-";
                    orderDto.SenderNumber = sender?.PhoneNumber ?? "-";
                    orderDto.SenderSecondaryPhone = sender?.SecondaryPhone;
                    orderDto.SenderWhatsappPhone = sender?.WhatsappPhone;
                    dto.Orders.Add(orderDto);
                }
            }

            await UpdateLocationIfProvided(latitude, longitude, user);
            baseResponse.Data = dto;
            return Ok(baseResponse);
        }

        /// <summary>
        /// الملخص المالي للمندوب (الطلبات المعلقة والمنتهية وإجمالي التحصيل)
        /// </summary>
        [HttpGet("FinancialSummary")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> FinancialSummary([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude)
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            var userid = user.Id;

            var dto = new DriverFinancialSummaryDto();
            dto.Name = user.Name;

            if (user.UserType == UserType.Driver)
            {
                var pendingOrders = _orders.Get(x =>
                    x.DeliveryId == userid && !x.IsDeleted && !x.Finished
                    && x.Status != OrderStatus.PartialReturned
                    && x.Status != OrderStatus.Assigned
                    && x.Status != OrderStatus.Waiting
                    && x.Status != OrderStatus.Placed
                ).ToList();

                dto.PendingOrdersCount = pendingOrders.Count;
                dto.PendingCollected = pendingOrders.Sum(x => x.ArrivedCost);
                dto.PendingDriverCommission = pendingOrders.Sum(x => x.DeliveryCost);
                dto.PendingToDeliver = dto.PendingCollected - dto.PendingDriverCommission;

                var finishedOrders = _orders.Get(x =>
                    x.DeliveryId == userid && !x.IsDeleted && x.Finished
                    && x.Status != OrderStatus.PartialReturned
                ).ToList();

                dto.FinishedOrdersCount = finishedOrders.Count;
                dto.FinishedCollected = finishedOrders.Sum(x => x.ArrivedCost);
                dto.FinishedDriverCommission = finishedOrders.Sum(x => x.DeliveryCost);
                dto.FinishedDelivered = dto.FinishedCollected - dto.FinishedDriverCommission;

                dto.TotalOrdersCount = dto.PendingOrdersCount + dto.FinishedOrdersCount;
                dto.TotalCollected = dto.PendingCollected + dto.FinishedCollected;
                dto.TotalDriverCommission = dto.PendingDriverCommission + dto.FinishedDriverCommission;
                dto.TotalDeliveredToCompany = dto.PendingToDeliver + dto.FinishedDelivered;
            }

            await UpdateLocationIfProvided(latitude, longitude, user);
            baseResponse.Data = dto;
            return Ok(baseResponse);
        }
    }
}
