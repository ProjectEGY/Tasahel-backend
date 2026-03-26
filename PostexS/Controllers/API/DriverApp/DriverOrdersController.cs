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
    /// تطبيق المندوب - الطلبات (الطلبات الحالية، البحث، الأرشيف، تفاصيل الطلب)
    /// </summary>
    public class DriverOrdersController : DriverBaseController
    {
        private readonly IGeneric<Order> _orders;
        private readonly IGeneric<OrderNotes> _orderNotes;
        private readonly IGeneric<OrderOperationHistory> _histories;

        public DriverOrdersController(
            UserManager<ApplicationUser> userManager,
            IGeneric<ApplicationUser> user,
            IGeneric<Location> locations,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IGeneric<Order> orders,
            IGeneric<OrderNotes> orderNotes,
            IGeneric<OrderOperationHistory> histories)
            : base(userManager, user, locations, configuration, httpClientFactory)
        {
            _orders = orders;
            _orderNotes = orderNotes;
            _histories = histories;
        }

        /// <summary>
        /// الطلبات الحالية للمندوب (المعينة والمؤجلة)
        /// </summary>
        [HttpGet("CurrentOrders")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> CurrentOrders([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude, string lang = "ar", int page = 1, int size = 15)
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            var userid = user.Id;
            var orders = new List<Order>();
            if (user.UserType == UserType.Driver)
            {
                orders = _orders.Get(x => x.DeliveryId == userid &&
                        (x.Status == OrderStatus.Assigned || x.Status == OrderStatus.Waiting)
                        && !x.IsDeleted).OrderBy(x => x.Id).Skip((page - 1) * size).Take(size).ToList();
            }

            string dilvertNotFound = "لم يتم تحديد السائق";
            var dto = new List<OrderDto>();
            foreach (var item in orders)
            {
                var model = new OrderDto(item);
                var sender = (await _user.GetObj(x => x.Id == item.ClientId));
                model.AgentName = sender?.Name ?? dilvertNotFound;
                model.SenderName = sender?.Name ?? dilvertNotFound;
                model.SenderNumber = sender?.PhoneNumber ?? "-1";
                dto.Add(model);
            }
            await UpdateLocationIfProvided(latitude, longitude, user);
            baseResponse.Data = dto;
            return Ok(baseResponse);
        }

        /// <summary>
        /// البحث في الطلبات الحالية للمندوب
        /// </summary>
        [HttpGet("SearchOrders")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> SearchOrders([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude, string Search, string lang = "ar", int page = 1)
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            var userid = user.Id;
            var orders = new List<Order>();
            if (user.UserType == UserType.Driver)
            {
                orders = _orders.Get(x => x.DeliveryId == userid &&
                        (x.Status == OrderStatus.Assigned || x.Status == OrderStatus.Waiting)
                        && !x.IsDeleted).ToList();
            }
            if (!string.IsNullOrEmpty(Search))
            {
                Search = Search.ToLower();
                orders = orders.Where(x =>
                    x.ClientPhone.ToLower().Contains(Search) ||
                    x.Code.ToLower().Contains(Search)).ToList();
            }

            string dilvertNotFound = "لم يتم تحديد السائق";
            var dto = new List<OrderDto>();
            foreach (var item in orders)
            {
                var model = new OrderDto(item);
                var sender = (await _user.GetObj(x => x.Id == item.ClientId));
                model.AgentName = sender?.Name ?? dilvertNotFound;
                model.SenderName = sender?.Name ?? dilvertNotFound;
                model.SenderNumber = sender?.PhoneNumber ?? "-1";
                dto.Add(model);
            }
            await UpdateLocationIfProvided(latitude, longitude, user);
            baseResponse.Data = dto;
            return Ok(baseResponse);
        }

        /// <summary>
        /// أرشيف الطلبات المنتهية للمندوب مع فلتر حسب الحالة
        /// </summary>
        [HttpGet("OrderHistory")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> OrderHistory([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude,
            int? status = null, int page = 1, int size = 15)
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            var userid = user.Id;

            var ordersQuery = _orders.Get(x =>
                x.DeliveryId == userid && !x.IsDeleted
                && x.Status != OrderStatus.Assigned
                && x.Status != OrderStatus.Placed
            );

            if (status.HasValue)
            {
                var orderStatus = (OrderStatus)status.Value;
                ordersQuery = ordersQuery.Where(x => x.Status == orderStatus);
            }

            var totalCount = ordersQuery.Count();
            var orders = ordersQuery.OrderByDescending(x => x.CreateOn)
                .Skip((page - 1) * size).Take(size).ToList();

            string driverNotFound = "لم يتم تحديد السائق";
            var dto = new List<OrderDto>();
            foreach (var item in orders)
            {
                var model = new OrderDto(item);
                var sender = await _user.GetObj(x => x.Id == item.ClientId);
                model.AgentName = sender?.Name ?? driverNotFound;
                model.SenderName = sender?.Name ?? driverNotFound;
                model.SenderNumber = sender?.PhoneNumber ?? "-1";
                dto.Add(model);
            }

            await UpdateLocationIfProvided(latitude, longitude, user);

            baseResponse.Data = new PaginatedResponse<OrderDto>(dto, page, size, totalCount);
            return Ok(baseResponse);
        }

        /// <summary>
        /// تفاصيل طلب واحد مع الملاحظات وتاريخ العمليات
        /// </summary>
        [HttpGet("OrderDetails/{orderId}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> OrderDetails([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude,
            long orderId)
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            var userid = user.Id;

            var order = _orders.Get(x => x.Id == orderId && !x.IsDeleted &&
                (x.DeliveryId == userid || x.ClientId == userid)).FirstOrDefault();

            if (order == null)
            {
                baseResponse.ErrorCode = Errors.TheOrderNotExistOrDeleted;
                return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
            }

            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");

            var dto = new OrderDetailsDto
            {
                Id = order.Id,
                Code = order.Code,
                ClientName = order.ClientName,
                ClientPhone = order.ClientPhone,
                ClientCode = order.ClientCode,
                Address = order.Address,
                AddressCity = order.AddressCity,
                Notes = order.Notes,
                Cost = order.Cost,
                DeliveryFees = order.DeliveryFees,
                TotalCost = order.TotalCost,
                ArrivedCost = order.ArrivedCost,
                DeliveryCost = order.DeliveryCost,
                ReturnedCost = order.ReturnedCost,
                Status = GetStatusInArabic((OrderStatus)order.Status),
                StatusCode = (int)order.Status,
                Finished = order.Finished,
                ReturnedImage = order.Returned_Image,
                CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(order.CreateOn, egyptTimeZone).ToString("yyyy-MM-dd hh:mm tt"),
                LastUpdated = order.LastUpdated.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(order.LastUpdated.Value, egyptTimeZone).ToString("yyyy-MM-dd hh:mm tt") : null,
            };

            // بيانات الراسل
            var sender = await _user.GetObj(x => x.Id == order.ClientId);
            if (sender != null)
            {
                dto.SenderName = sender.Name;
                dto.SenderPhone = sender.PhoneNumber;
            }

            // الملاحظات
            var notes = _orderNotes.Get(x => x.OrderId == orderId && !x.IsDeleted)
                .OrderByDescending(x => x.CreateOn).ToList();
            foreach (var note in notes)
            {
                var noteUser = await _user.GetObj(x => x.Id == note.UserId);
                dto.OrderNotes.Add(new OrderNoteDto
                {
                    Content = note.Content,
                    UserName = noteUser?.Name ?? "-",
                    Date = TimeZoneInfo.ConvertTimeFromUtc(note.CreateOn, egyptTimeZone).ToString("yyyy-MM-dd hh:mm tt")
                });
            }

            // تاريخ العمليات (Timeline)
            if (order.OrderOperationHistoryId.HasValue)
            {
                var history = _histories.Get(x => x.Id == order.OrderOperationHistoryId.Value).FirstOrDefault();
                if (history != null)
                {
                    dto.Timeline = new OrderHistoryTimelineDto();

                    if (!string.IsNullOrEmpty(history.Create_UserId))
                    {
                        var createUser = await _user.GetObj(x => x.Id == history.Create_UserId);
                        dto.Timeline.CreatedDate = history.CreateDate != DateTime.MinValue ? TimeZoneInfo.ConvertTimeFromUtc(history.CreateDate, egyptTimeZone).ToString("yyyy-MM-dd hh:mm tt") : null;
                        dto.Timeline.CreatedBy = createUser?.Name;
                    }
                    if (!string.IsNullOrEmpty(history.Assign_To_Driver_UserId))
                    {
                        var assignUser = await _user.GetObj(x => x.Id == history.Assign_To_Driver_UserId);
                        dto.Timeline.AssignedToDriverDate = history.Assign_To_DriverDate != DateTime.MinValue ? TimeZoneInfo.ConvertTimeFromUtc(history.Assign_To_DriverDate, egyptTimeZone).ToString("yyyy-MM-dd hh:mm tt") : null;
                        dto.Timeline.AssignedBy = assignUser?.Name;
                    }
                    if (!string.IsNullOrEmpty(history.Finish_UserId))
                    {
                        var finishUser = await _user.GetObj(x => x.Id == history.Finish_UserId);
                        dto.Timeline.FinishDate = history.FinishDate != DateTime.MinValue ? TimeZoneInfo.ConvertTimeFromUtc(history.FinishDate, egyptTimeZone).ToString("yyyy-MM-dd hh:mm tt") : null;
                        dto.Timeline.FinishedBy = finishUser?.Name;
                    }
                    if (!string.IsNullOrEmpty(history.Complete_UserId))
                    {
                        var completeUser = await _user.GetObj(x => x.Id == history.Complete_UserId);
                        dto.Timeline.CompleteDate = history.CompleteDate != DateTime.MinValue ? TimeZoneInfo.ConvertTimeFromUtc(history.CompleteDate, egyptTimeZone).ToString("yyyy-MM-dd hh:mm tt") : null;
                        dto.Timeline.CompletedBy = completeUser?.Name;
                    }
                }
            }

            await UpdateLocationIfProvided(latitude, longitude, user);

            baseResponse.Data = dto;
            return Ok(baseResponse);
        }

        /// <summary>
        /// تفاصيل طلب عن طريق الكود (QR Code Scan)
        /// </summary>
        [HttpGet("OrderDetailsByCode/{code}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> OrderDetailsByCode([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude,
            string code)
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            if (string.IsNullOrWhiteSpace(code))
            {
                baseResponse.ErrorCode = Errors.TheModelIsInvalid;
                return StatusCode((int)HttpStatusCode.BadRequest, baseResponse);
            }

            var userid = user.Id;

            var order = _orders.Get(x => x.Code == code && !x.IsDeleted &&
                (x.DeliveryId == userid || x.ClientId == userid)).FirstOrDefault();

            if (order == null)
            {
                baseResponse.ErrorCode = Errors.TheOrderNotExistOrDeleted;
                return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
            }

            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");

            var dto = new OrderDetailsDto
            {
                Id = order.Id,
                Code = order.Code,
                ClientName = order.ClientName,
                ClientPhone = order.ClientPhone,
                ClientCode = order.ClientCode,
                Address = order.Address,
                AddressCity = order.AddressCity,
                Notes = order.Notes,
                Cost = order.Cost,
                DeliveryFees = order.DeliveryFees,
                TotalCost = order.TotalCost,
                ArrivedCost = order.ArrivedCost,
                DeliveryCost = order.DeliveryCost,
                ReturnedCost = order.ReturnedCost,
                Status = GetStatusInArabic((OrderStatus)order.Status),
                StatusCode = (int)order.Status,
                Finished = order.Finished,
                ReturnedImage = order.Returned_Image,
                CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(order.CreateOn, egyptTimeZone).ToString("yyyy-MM-dd hh:mm tt"),
                LastUpdated = order.LastUpdated.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(order.LastUpdated.Value, egyptTimeZone).ToString("yyyy-MM-dd hh:mm tt") : null,
            };

            // بيانات الراسل
            var sender = await _user.GetObj(x => x.Id == order.ClientId);
            if (sender != null)
            {
                dto.SenderName = sender.Name;
                dto.SenderPhone = sender.PhoneNumber;
            }

            // الملاحظات
            var notes = _orderNotes.Get(x => x.OrderId == order.Id && !x.IsDeleted)
                .OrderByDescending(x => x.CreateOn).ToList();
            foreach (var note in notes)
            {
                var noteUser = await _user.GetObj(x => x.Id == note.UserId);
                dto.OrderNotes.Add(new OrderNoteDto
                {
                    Content = note.Content,
                    UserName = noteUser?.Name ?? "-",
                    Date = TimeZoneInfo.ConvertTimeFromUtc(note.CreateOn, egyptTimeZone).ToString("yyyy-MM-dd hh:mm tt")
                });
            }

            // تاريخ العمليات (Timeline)
            if (order.OrderOperationHistoryId.HasValue)
            {
                var history = _histories.Get(x => x.Id == order.OrderOperationHistoryId.Value).FirstOrDefault();
                if (history != null)
                {
                    dto.Timeline = new OrderHistoryTimelineDto();

                    if (!string.IsNullOrEmpty(history.Create_UserId))
                    {
                        var createUser = await _user.GetObj(x => x.Id == history.Create_UserId);
                        dto.Timeline.CreatedDate = history.CreateDate != DateTime.MinValue ? TimeZoneInfo.ConvertTimeFromUtc(history.CreateDate, egyptTimeZone).ToString("yyyy-MM-dd hh:mm tt") : null;
                        dto.Timeline.CreatedBy = createUser?.Name;
                    }
                    if (!string.IsNullOrEmpty(history.Assign_To_Driver_UserId))
                    {
                        var assignUser = await _user.GetObj(x => x.Id == history.Assign_To_Driver_UserId);
                        dto.Timeline.AssignedToDriverDate = history.Assign_To_DriverDate != DateTime.MinValue ? TimeZoneInfo.ConvertTimeFromUtc(history.Assign_To_DriverDate, egyptTimeZone).ToString("yyyy-MM-dd hh:mm tt") : null;
                        dto.Timeline.AssignedBy = assignUser?.Name;
                    }
                    if (!string.IsNullOrEmpty(history.Finish_UserId))
                    {
                        var finishUser = await _user.GetObj(x => x.Id == history.Finish_UserId);
                        dto.Timeline.FinishDate = history.FinishDate != DateTime.MinValue ? TimeZoneInfo.ConvertTimeFromUtc(history.FinishDate, egyptTimeZone).ToString("yyyy-MM-dd hh:mm tt") : null;
                        dto.Timeline.FinishedBy = finishUser?.Name;
                    }
                    if (!string.IsNullOrEmpty(history.Complete_UserId))
                    {
                        var completeUser = await _user.GetObj(x => x.Id == history.Complete_UserId);
                        dto.Timeline.CompleteDate = history.CompleteDate != DateTime.MinValue ? TimeZoneInfo.ConvertTimeFromUtc(history.CompleteDate, egyptTimeZone).ToString("yyyy-MM-dd hh:mm tt") : null;
                        dto.Timeline.CompletedBy = completeUser?.Name;
                    }
                }
            }

            await UpdateLocationIfProvided(latitude, longitude, user);

            baseResponse.Data = dto;
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
