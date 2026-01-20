using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using PostexS.Models.Dtos;
using PostexS.Models.Enums;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ZXing;
using ZXing.Common;

namespace PostexS.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class SenderController : ControllerBase
    {
        private readonly IGeneric<Order> _order;
        private readonly IGeneric<OrderOperationHistory> _histories;
        private readonly IGeneric<ApplicationUser> _user;
        private readonly ICRUD<Order> _CRUD;
        private readonly BaseResponse _baseResponse;

        public SenderController(
            IGeneric<Order> order,
            IGeneric<OrderOperationHistory> histories,
            IGeneric<ApplicationUser> user,
            ICRUD<Order> CRUD)
        {
            _order = order;
            _histories = histories;
            _user = user;
            _CRUD = CRUD;
            _baseResponse = new BaseResponse();
        }

        #region Private Helper Methods

        /// <summary>
        /// Validates API keys from Headers (X-Public-Key, X-Private-Key)
        /// </summary>
        private async Task<(ApplicationUser User, IActionResult ErrorResult)> ValidateApiKeysFromHeadersAsync()
        {
            // Get keys from headers
            var publicKey = Request.Headers["X-Public-Key"].FirstOrDefault();
            var privateKey = Request.Headers["X-Private-Key"].FirstOrDefault();

            if (string.IsNullOrEmpty(publicKey))
            {
                _baseResponse.ErrorCode = Errors.PublicKeyIsRequired;
                _baseResponse.ErrorMessage = "X-Public-Key header is required";
                return (null, StatusCode((int)HttpStatusCode.BadRequest, _baseResponse));
            }

            if (string.IsNullOrEmpty(privateKey))
            {
                _baseResponse.ErrorCode = Errors.PrivateKeyIsRequired;
                _baseResponse.ErrorMessage = "X-Private-Key header is required";
                return (null, StatusCode((int)HttpStatusCode.BadRequest, _baseResponse));
            }

            var user = await _user.GetObj(x =>
                x.PublicKey == publicKey &&
                x.PrivateKey == privateKey &&
                !x.IsDeleted);

            if (user == null)
            {
                _baseResponse.ErrorCode = Errors.PrivateKeyIsWrongOrPublicKeyIsWrong;
                _baseResponse.ErrorMessage = "Invalid API credentials";
                return (null, StatusCode((int)HttpStatusCode.Unauthorized, _baseResponse));
            }

            return (user, null);
        }

        private static string GetStatusInArabic(OrderStatus status)
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

        private static byte[] GetBarcode(string code)
        {
            var barcodeWriter = new BarcodeWriter
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Height = 50,
                    Width = 175
                }
            };
            using var barcodeBitmap = barcodeWriter.Write(code);
            using var ms = new MemoryStream();
            barcodeBitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }

        #endregion

        #region API Endpoints

        /// <summary>
        /// Create a new order
        /// إنشاء طلب جديد
        /// Headers Required: X-Public-Key, X-Private-Key
        /// </summary>
        [HttpPost("orders")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateOrder([FromBody] OrderVM model)
        {
            var (user, errorResult) = await ValidateApiKeysFromHeadersAsync();
            if (errorResult != null) return errorResult;

            if (model == null)
            {
                _baseResponse.ErrorCode = Errors.TheModelIsInvalid;
                _baseResponse.ErrorMessage = "Order data is required";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            if (string.IsNullOrWhiteSpace(model.ClientName))
            {
                _baseResponse.ErrorCode = Errors.TheModelIsInvalid;
                _baseResponse.ErrorMessage = "Client name is required";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            if (string.IsNullOrWhiteSpace(model.ClientPhone))
            {
                _baseResponse.ErrorCode = Errors.TheModelIsInvalid;
                _baseResponse.ErrorMessage = "Client phone is required";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            string GeneralNote = user.OrdersGeneralNote != null ? user.OrdersGeneralNote + " - " : "";

            Order order = new Order()
            {
                Address = model.Address,
                AddressCity = model.ClientCity,
                ClientName = model.ClientName,
                ClientCode = model.ClientCode,
                ClientPhone = model.ClientPhone,
                Cost = model.Cost,
                DeliveryFees = model.DeliveryFees,
                Notes = GeneralNote + model.Notes,
                ClientId = user.Id,
                Pending = true,
                TotalCost = model.Cost + model.DeliveryFees,
                Status = OrderStatus.Placed,
                BranchId = user.BranchId,
            };

            if (!await _order.Add(order))
            {
                _baseResponse.ErrorMessage = "Something went wrong, please try again later";
                _baseResponse.ErrorCode = Errors.SomeThingWentwrong;
                return StatusCode((int)HttpStatusCode.InternalServerError, _baseResponse);
            }

            OrderOperationHistory history = new OrderOperationHistory()
            {
                OrderId = order.Id,
                Create_UserId = user.Id,
                CreateDate = order.CreateOn,
            };

            if (!await _histories.Add(history))
            {
                _baseResponse.ErrorMessage = "Something went wrong, please try again later";
                _baseResponse.ErrorCode = Errors.SomeThingWentwrong;
                return StatusCode((int)HttpStatusCode.InternalServerError, _baseResponse);
            }

            order.OrderOperationHistoryId = history.Id;
            order.Code = "Tas" + order.Id.ToString();
            order.BarcodeImage = GetBarcode(order.Code);

            if (!await _order.Update(order))
            {
                _baseResponse.ErrorMessage = "Something went wrong, please try again later";
                _baseResponse.ErrorCode = Errors.SomeThingWentwrong;
                return StatusCode((int)HttpStatusCode.InternalServerError, _baseResponse);
            }

            await _CRUD.Update(order.Id);

            var orderDto = new SenderOrderDto(order, null, null);
            _baseResponse.Data = orderDto;

            return Ok(_baseResponse);
        }

        /// <summary>
        /// Search orders by receiver name or order code with pagination
        /// البحث في الطلبات باسم المستلم أو كود الطلب
        /// Headers Required: X-Public-Key, X-Private-Key
        /// </summary>
        [HttpGet("orders")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchOrders(
            [FromQuery] string search = null,
            [FromQuery] OrderStatus? status = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var (user, errorResult) = await ValidateApiKeysFromHeadersAsync();
            if (errorResult != null) return errorResult;

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            var query = _order.GetAllAsIQueryable(
                filter: x => x.ClientId == user.Id && !x.IsDeleted,
                orderby: q => q.OrderByDescending(o => o.CreateOn),
                IncludeProperties: "Delivery");

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(x =>
                    (x.ClientName != null && x.ClientName.ToLower().Contains(search)) ||
                    (x.Code != null && x.Code.ToLower().Contains(search)) ||
                    (x.ClientCode != null && x.ClientCode.ToLower().Contains(search)) ||
                    (x.ClientPhone != null && x.ClientPhone.Contains(search)));
            }

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var orders = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var orderDtos = orders.Select(o => new SenderOrderDto(
                o,
                o.Delivery?.Name,
                o.Delivery?.PhoneNumber
            )).ToList();

            var response = new PaginatedResponse<SenderOrderDto>
            {
                Items = orderDtos,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber < totalPages
            };

            _baseResponse.Data = response;
            return Ok(_baseResponse);
        }

        /// <summary>
        /// Get single order by Code
        /// الحصول على تفاصيل طلب واحد بالكود
        /// Headers Required: X-Public-Key, X-Private-Key
        /// </summary>
        [HttpGet("orders/{code}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetOrderByCode([FromRoute] string code)
        {
            var (user, errorResult) = await ValidateApiKeysFromHeadersAsync();
            if (errorResult != null) return errorResult;

            if (string.IsNullOrWhiteSpace(code))
            {
                _baseResponse.ErrorCode = Errors.TheModelIsInvalid;
                _baseResponse.ErrorMessage = "Order code is required";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            var order = await _order.GetSingle(
                x => x.Code == code && x.ClientId == user.Id && !x.IsDeleted,
                includeProperties: "Delivery");

            if (order == null)
            {
                _baseResponse.ErrorCode = Errors.TheOrderNotExistOrDeleted;
                _baseResponse.ErrorMessage = "Order not found";
                return StatusCode((int)HttpStatusCode.NotFound, _baseResponse);
            }

            var orderDto = new SenderOrderDto(
                order,
                order.Delivery?.Name,
                order.Delivery?.PhoneNumber);

            _baseResponse.Data = orderDto;
            return Ok(_baseResponse);
        }

        /// <summary>
        /// Get status of multiple orders (bulk status check)
        /// الحصول على حالة عدة طلبات دفعة واحدة
        /// Headers Required: X-Public-Key, X-Private-Key
        /// </summary>
        [HttpPost("orders/status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBulkOrderStatus([FromBody] BulkStatusRequest request)
        {
            var (user, errorResult) = await ValidateApiKeysFromHeadersAsync();
            if (errorResult != null) return errorResult;

            var hasCodes = request?.OrderCodes != null && request.OrderCodes.Any();

            if (!hasCodes)
            {
                _baseResponse.ErrorCode = Errors.TheModelIsInvalid;
                _baseResponse.ErrorMessage = "OrderCodes array is required";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            var totalRequested = request.OrderCodes?.Count ?? 0;
            if (totalRequested > 100)
            {
                _baseResponse.ErrorCode = Errors.TheModelIsInvalid;
                _baseResponse.ErrorMessage = "Maximum 100 orders per request";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            var query = _order.GetAllAsIQueryable(
                filter: x => x.ClientId == user.Id && !x.IsDeleted);

            query = query.Where(x => request.OrderCodes.Contains(x.Code));

            var orders = query.ToList();

            var statusList = orders.Select(o => new OrderStatusDto(
                o.Code,
                o.Status,
                o.LastUpdated
            )).ToList();

            _baseResponse.Data = new
            {
                Found = statusList.Count,
                Requested = totalRequested,
                Orders = statusList
            };

            return Ok(_baseResponse);
        }

        #endregion
    }
}
