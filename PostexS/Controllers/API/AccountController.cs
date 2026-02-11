using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using PostexS.Helper;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using PostexS.Models.Enums;
using PostexS.Models.ViewModels;
using PostexS.Models.Dtos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Linq.Expressions;
using PostexS.Services;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.IO;
using System.Globalization;

namespace PostexS.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGeneric<ApplicationUser> _user;
        private readonly BaseResponse baseResponse;
        private readonly IConfiguration _configuration;
        private readonly IGeneric<Order> _orders;
        private readonly IGeneric<Location> _locations;
        private readonly IGeneric<OrderNotes> _orderNotes;
        private readonly IGeneric<OrderOperationHistory> _histories;
        private readonly IHttpClientFactory _httpClientFactory;
        public AccountController(UserManager<ApplicationUser> userManager, IGeneric<ApplicationUser> user
            , IConfiguration configuration, IGeneric<Location> locations, IGeneric<Order> orders,
            IHttpClientFactory httpClientFactory, IGeneric<OrderNotes> orderNotes,
            IGeneric<OrderOperationHistory> histories)
        {
            _userManager = userManager;
            _user = user;
            baseResponse = new BaseResponse();
            _configuration = configuration;
            _orders = orders;
            _locations = locations;
            _httpClientFactory = httpClientFactory;
            _orderNotes = orderNotes;
            _histories = histories;
        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login(SubmitLoginDto model, [FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude, string lang = "en")
        {
            if (!await _user.IsExist(x => x.PhoneNumber == model.Phone))
            {
                baseResponse.ErrorCode = Errors.TheUsernameOrPasswordIsIncorrect;
                return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
            }
            var user = _user.Get(x => x.PhoneNumber == model.Phone).First();
            if (user != null && !user.IsDeleted && await _userManager.CheckPasswordAsync(user, model.Password))
            {


                if (latitude.HasValue && longitude.HasValue)
                {
                    UpdateUserLocation locationdto = new UpdateUserLocation()
                    {
                        Longitude = longitude.Value,
                        Latitude = latitude.Value,
                    };
                    await UpdateLocationMethod(locationdto, user);
                }
                var dto = new LoginDto(user);
                dto.Token = await GenerateToken(user);
                baseResponse.Data = dto;
                return Ok(baseResponse);
            }
            baseResponse.ErrorCode = Errors.TheUsernameOrPasswordIsIncorrect;
            return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
        }
        [NonAction]
        public async Task<string> GenerateToken(ApplicationUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Id),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddYears(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        [HttpGet("GetProfile")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetProfile([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude, string lang = "en")
        {
            var userId = User.Identity.Name;
            if (userId != null)
            {
                var user = _user.Get(x => x.Id == userId).First();
                var dto = new LoginDto(user);
                if (latitude.HasValue && longitude.HasValue)
                {
                    UpdateUserLocation locationdto = new UpdateUserLocation()
                    {
                        Longitude = longitude.Value,
                        Latitude = latitude.Value,
                    };
                    await UpdateLocationMethod(locationdto, user);
                }
                baseResponse.Data = dto;
                return Ok(baseResponse);
            }
            else
            {
                baseResponse.ErrorCode = Errors.TheUserNotExistOrDeleted;
                return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
            }
        }
        [HttpGet("GetTracking")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetTracking([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude, string lang = "en")
        {
            var userId = User.Identity.Name;
            if (userId != null)
            {
                var user = _user.Get(x => x.Id == userId).First();

                if (latitude.HasValue && longitude.HasValue)
                {
                    UpdateUserLocation locationdto = new UpdateUserLocation()
                    {
                        Longitude = longitude.Value,
                        Latitude = latitude.Value,
                    };
                    await UpdateLocationMethod(locationdto, user);
                }
                baseResponse.Data = user.Tracking;
                return Ok(baseResponse);
            }
            else
            {
                baseResponse.ErrorCode = Errors.TheUserNotExistOrDeleted;
                return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
            }
        }
        [HttpPut("UpdateProfile")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> UpdateProfile([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude, UpdateUserDto dto, string lang = "en")
        {
            var userId = User.Identity.Name;
            if (userId != null)
            {
                var user = _user.Get(x => x.Id == userId).First();

                if (!string.IsNullOrEmpty(dto.UserName))
                    user.Name = dto.UserName;
                if (!string.IsNullOrEmpty(dto.Email))
                    user.Email = dto.Email;
                if (!string.IsNullOrEmpty(dto.WhatsappPhone))
                    user.WhatsappPhone = dto.WhatsappPhone;
                if (!string.IsNullOrEmpty(dto.Phone))
                    user.PhoneNumber = dto.Phone;

                await _user.Update(user);
                var userdto = _user.Get(x => x.Id == userId).First();
                var response = new LoginDto(userdto);
                if (latitude.HasValue && longitude.HasValue)
                {
                    UpdateUserLocation locationdto = new UpdateUserLocation()
                    {
                        Longitude = longitude.Value,
                        Latitude = latitude.Value,
                    };
                    await UpdateLocationMethod(locationdto, user);
                }
                baseResponse.Data = response;
                return Ok(baseResponse);
            }
            else
            {
                baseResponse.ErrorCode = Errors.TheUserNotExistOrDeleted;
                return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
            }
        }
        //[HttpPut("UpdateLocation")]
        //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        //public async Task<IActionResult> UpdateLocation(UpdateUserLocation dto, string lang = "en")
        //{
        //    var userId = User.Identity.Name;
        //    if (userId != null)
        //    {
        //        var user = _user.Get(x => x.Id == userId).First();

        //        if (dto.Longitude.HasValue && dto.Latitude.HasValue)
        //        {
        //            await UpdateLocationMethod(dto, user);
        //        }
        //        var userdto = _user.Get(x => x.Id == userId).First();
        //        var response = new LoginDto(userdto);
        //        baseResponse.Data = response;

        //        return Ok(baseResponse);
        //    }
        //    else
        //    {
        //        baseResponse.ErrorCode = Errors.TheUserNotExistOrDeleted;
        //        return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
        //    }
        //}

        [HttpPut("ChangePassword")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ChangePassword([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude, ChangePasswordDto model)
        {
            var userid = User.Identity.Name;
            if (userid != null)
            {
                var user = _user.Get(x => x.Id == userid).First();
                var isOldCorrect = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, model.OldPassword);
                if (isOldCorrect == PasswordVerificationResult.Failed)
                {
                    baseResponse.ErrorCode = Errors.TheOldPasswordIsInCorrect;
                    return StatusCode((int)HttpStatusCode.BadRequest, baseResponse);
                }
                user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, model.NewPassword);
                await _user.Update(user);
                if (latitude.HasValue && longitude.HasValue)
                {
                    UpdateUserLocation locationdto = new UpdateUserLocation()
                    {
                        Longitude = longitude.Value,
                        Latitude = latitude.Value,
                    };
                    await UpdateLocationMethod(locationdto, user);
                }
                return Ok(baseResponse);
            }
            baseResponse.ErrorCode = Errors.TheUserNotExistOrDeleted;
            return StatusCode((int)HttpStatusCode.NotFound, baseResponse);

        }
        [HttpPut("ForgetPassword")]
        public async Task<IActionResult> ForgetPassword(string Phone)
        {
            if (!await _user.IsExist(x => x.PhoneNumber == Phone))
            {
                baseResponse.ErrorCode = Errors.ThisPhoneNumberNotExist;
                return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
            }
            var user = _user.Get(x => x.PhoneNumber == Phone).First();
            var password = RandomGenerator.GenerateNumber(100000, 999999);
            var passwordHashe = _userManager.PasswordHasher.HashPassword(user, "123456");
            user.PasswordHash = passwordHashe;
            await _user.Update(user);
            //  await SMS.SendMessage("02", Phone, $"أهلاً وسهلاً بكم فى تطبيق صنايعى ترند الباسورد الجديد هو {password.ToString()}");
            return Ok(baseResponse);
        }
        [HttpGet("CurrentOrders")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> CurrentOrders([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude, string lang = "ar", int page = 1, int size = 15)
        {
            //int size = 15;
            var userid = User.Identity.Name;
            var user = await _userManager.FindByIdAsync(userid);
            var orders = new List<Order>();
            if (user != null)
            {
                if (user.UserType == UserType.Driver)
                {
                    orders = _orders.Get(x => x.DeliveryId == userid &&
                            (x.Status == OrderStatus.Assigned || x.Status == OrderStatus.Waiting)
                            && !x.IsDeleted).OrderBy(x => x.Id).Skip((page - 1) * size).Take(size).ToList();
                }
                else if (user.UserType == UserType.Client)
                {
                    orders = _orders.Get(x => x.ClientId == userid &&
                            (x.Status == OrderStatus.Assigned || x.Status == OrderStatus.Waiting
                            || x.Status == OrderStatus.Placed)
                            && !x.IsDeleted).OrderBy(x => x.Id).Skip((page - 1) * size).Take(size).ToList();
                }
            }
            string dilvertNotFound = "لم يتم تحديد السائق";
            var dto = new List<OrderDto>();
            foreach (var item in orders)
            {
                var model = new OrderDto(item);
                var sender = (await _user.GetObj(x => x.Id == item.ClientId));
                if (user.UserType == UserType.Driver)
                {
                    model.AgentName = (await _user.GetObj(x => x.Id == item.ClientId)).Name;
                    model.SenderName = sender.Name;
                    model.SenderNumber = sender.PhoneNumber;
                }
                else
                {
                    var delivery = await _user.GetObj(x => x.Id == item.DeliveryId);
                    model.AgentName = delivery?.Name ?? dilvertNotFound;
                    model.SenderName = delivery?.Name ?? dilvertNotFound;
                    model.SenderNumber = delivery?.PhoneNumber ?? "-1";
                }

                dto.Add(model);
            }
            if (latitude.HasValue && longitude.HasValue)
            {
                UpdateUserLocation locationdto = new UpdateUserLocation()
                {
                    Longitude = longitude.Value,
                    Latitude = latitude.Value,
                };
                await UpdateLocationMethod(locationdto, user);
            }
            baseResponse.Data = dto;
            return Ok(baseResponse);
        }
        [HttpGet("SearchOrders")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> SearchOrders([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude, string Search, string lang = "ar", int page = 1)
        {
            int size = 15;
            var userid = User.Identity.Name;
            var user = await _userManager.FindByIdAsync(userid);
            var orders = new List<Order>();
            if (user != null)
            {
                if (user.UserType == UserType.Driver)
                {
                    orders = _orders.Get(x => x.DeliveryId == userid &&
                            (x.Status == OrderStatus.Assigned || x.Status == OrderStatus.Waiting)
                            && !x.IsDeleted)/*.Skip((page - 1) * size).Take(size)*/.ToList();

                }
                else if (user.UserType == UserType.Client)
                {
                    orders = _orders.Get(x => x.ClientId == userid &&
                            (x.Status == OrderStatus.Assigned || x.Status == OrderStatus.Waiting
                            || x.Status == OrderStatus.Placed)
                            && !x.IsDeleted)/*.Skip((page - 1) * size).Take(size)*/.ToList();
                }
                if (!string.IsNullOrEmpty(Search))
                {
                    Search = Search.ToLower();
                    orders = orders.Where(x =>
                x.ClientPhone.ToLower().Contains(Search) ||
                x.Code.ToLower().Contains(Search)).ToList();
                }
            }
            string dilvertNotFound = "لم يتم تحديد السائق";
            var dto = new List<OrderDto>();
            foreach (var item in orders)
            {
                var model = new OrderDto(item);
                var sender = (await _user.GetObj(x => x.Id == item.ClientId));
                if (user.UserType == UserType.Driver)
                {
                    model.AgentName = (await _user.GetObj(x => x.Id == item.ClientId)).Name;
                    model.SenderName = sender.Name;
                    model.SenderNumber = sender.PhoneNumber;
                }
                else
                {
                    var delivery = await _user.GetObj(x => x.Id == item.DeliveryId);
                    model.AgentName = delivery?.Name ?? dilvertNotFound;
                    model.SenderName = delivery?.Name ?? dilvertNotFound;
                    model.SenderNumber = delivery?.PhoneNumber ?? "-1";
                }

                dto.Add(model);
            }
            if (latitude.HasValue && longitude.HasValue)
            {
                UpdateUserLocation locationdto = new UpdateUserLocation()
                {
                    Longitude = longitude.Value,
                    Latitude = latitude.Value,
                };
                await UpdateLocationMethod(locationdto, user);
            }
            baseResponse.Data = dto;
            return Ok(baseResponse);
        }
        [HttpGet("Statistics")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Statistics([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude)
        {
            var userid = User.Identity.Name;
            var user = await _userManager.FindByIdAsync(userid);
            StatisticsDto dto = new StatisticsDto();
            dto.Name = user.Name;
            if (user != null)
            {
                if (user.UserType == UserType.Driver)
                {
                    //عدد الطلبات الحاليه
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

            }

            if (latitude.HasValue && longitude.HasValue)
            {
                UpdateUserLocation locationdto = new UpdateUserLocation()
                {
                    Longitude = longitude.Value,
                    Latitude = latitude.Value,
                };
                await UpdateLocationMethod(locationdto, user);
            }
            baseResponse.Data = dto;
            return Ok(baseResponse);
        }

        #region Driver Statistics & Reports APIs

        /// <summary>
        /// إحصائيات المندوب الشاملة - جميع الطلبات مع النسب المئوية
        /// </summary>
        [HttpGet("DriverStatistics")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> DriverStatistics([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude)
        {
            var userid = User.Identity.Name;
            var user = await _userManager.FindByIdAsync(userid);
            if (user == null || user.IsDeleted)
            {
                baseResponse.ErrorCode = Errors.TheUserNotExistOrDeleted;
                return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
            }

            var dto = new DriverStatisticsDto();
            dto.Name = user.Name;

            if (user.UserType == UserType.Driver)
            {
                // عدد الطلبات الحالية (Assigned + Waiting)
                dto.CurrentOrdersCount = _orders.Get(x => x.DeliveryId == userid &&
                        (x.Status == OrderStatus.Assigned || x.Status == OrderStatus.Waiting)
                        && !x.IsDeleted).Count();

                // جميع الطلبات ما عدا PartialReturned
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

                // حساب النسب المئوية
                if (dto.AllOrdersCount > 0)
                {
                    dto.DeliveredPercentage = Math.Round((double)dto.DeliveredCount / dto.AllOrdersCount * 100, 1);
                    dto.PartialDeliveredPercentage = Math.Round((double)dto.PartialDeliveredCount / dto.AllOrdersCount * 100, 1);
                    dto.ReturnedPercentage = Math.Round((double)dto.ReturnedCount / dto.AllOrdersCount * 100, 1);
                }
            }

            if (latitude.HasValue && longitude.HasValue)
            {
                UpdateUserLocation locationdto = new UpdateUserLocation()
                {
                    Longitude = longitude.Value,
                    Latitude = latitude.Value,
                };
                await UpdateLocationMethod(locationdto, user);
            }
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
            var userid = User.Identity.Name;
            var user = await _userManager.FindByIdAsync(userid);
            if (user == null || user.IsDeleted)
            {
                baseResponse.ErrorCode = Errors.TheUserNotExistOrDeleted;
                return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
            }

            var dto = new DriverCurrentStatisticsDto();
            dto.Name = user.Name;

            if (user.UserType == UserType.Driver)
            {
                // عدد الطلبات الحالية (Assigned + Waiting)
                dto.CurrentOrdersCount = _orders.Get(x => x.DeliveryId == userid &&
                        (x.Status == OrderStatus.Assigned || x.Status == OrderStatus.Waiting)
                        && !x.IsDeleted).Count();

                // الطلبات الغير منتهية
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

                // حساب النسب المئوية
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

            if (latitude.HasValue && longitude.HasValue)
            {
                UpdateUserLocation locationdto = new UpdateUserLocation()
                {
                    Longitude = longitude.Value,
                    Latitude = latitude.Value,
                };
                await UpdateLocationMethod(locationdto, user);
            }
            baseResponse.Data = dto;
            return Ok(baseResponse);
        }

        /// <summary>
        /// تقرير المندوب بفلتر تاريخ
        /// </summary>
        [HttpGet("DriverReport")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> DriverReport([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude,
            string from = null, string to = null, int page = 1, int size = 50)
        {
            var userid = User.Identity.Name;
            var user = await _userManager.FindByIdAsync(userid);
            if (user == null || user.IsDeleted)
            {
                baseResponse.ErrorCode = Errors.TheUserNotExistOrDeleted;
                return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
            }

            var dto = new DriverReportDto();
            dto.Name = user.Name;

            if (user.UserType == UserType.Driver)
            {
                // تحديد نطاق التاريخ
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
                        toDate = parsedTo.AddDays(1).ToUniversalTime(); // نهاية اليوم
                }

                dto.FromDate = from ?? "الكل";
                dto.ToDate = to ?? "الكل";

                // جلب طلبات المندوب في الفترة المحددة
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

                // حساب النسب المئوية
                if (dto.TotalOrders > 0)
                {
                    dto.DeliveredPercentage = Math.Round((double)dto.DeliveredCount / dto.TotalOrders * 100, 1);
                    dto.ReturnedPercentage = Math.Round((double)dto.ReturnedCount / dto.TotalOrders * 100, 1);
                    dto.PartialDeliveredPercentage = Math.Round((double)dto.PartialDeliveredCount / dto.TotalOrders * 100, 1);
                }

                // الطلبات مع pagination
                var pagedOrders = orders.OrderByDescending(x => x.CreateOn).Skip((page - 1) * size).Take(size).ToList();
                foreach (var order in pagedOrders)
                {
                    var orderDto = new OrderDto(order);
                    var sender = await _user.GetObj(x => x.Id == order.ClientId);
                    orderDto.AgentName = sender?.Name ?? "-";
                    orderDto.SenderName = sender?.Name ?? "-";
                    orderDto.SenderNumber = sender?.PhoneNumber ?? "-";
                    dto.Orders.Add(orderDto);
                }
            }

            if (latitude.HasValue && longitude.HasValue)
            {
                UpdateUserLocation locationdto = new UpdateUserLocation()
                {
                    Longitude = longitude.Value,
                    Latitude = latitude.Value,
                };
                await UpdateLocationMethod(locationdto, user);
            }
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
            var userid = User.Identity.Name;
            var user = await _userManager.FindByIdAsync(userid);
            if (user == null || user.IsDeleted)
            {
                baseResponse.ErrorCode = Errors.TheUserNotExistOrDeleted;
                return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
            }

            var ordersQuery = _orders.Get(x =>
                x.DeliveryId == userid && !x.IsDeleted
                && x.Status != OrderStatus.Assigned
                && x.Status != OrderStatus.Placed
            );

            // فلتر حسب الحالة
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

            if (latitude.HasValue && longitude.HasValue)
            {
                UpdateUserLocation locationdto = new UpdateUserLocation()
                {
                    Longitude = longitude.Value,
                    Latitude = latitude.Value,
                };
                await UpdateLocationMethod(locationdto, user);
            }

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
            var userid = User.Identity.Name;
            var user = await _userManager.FindByIdAsync(userid);
            if (user == null || user.IsDeleted)
            {
                baseResponse.ErrorCode = Errors.TheUserNotExistOrDeleted;
                return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
            }

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

            if (latitude.HasValue && longitude.HasValue)
            {
                UpdateUserLocation locationdto = new UpdateUserLocation()
                {
                    Longitude = longitude.Value,
                    Latitude = latitude.Value,
                };
                await UpdateLocationMethod(locationdto, user);
            }

            baseResponse.Data = dto;
            return Ok(baseResponse);
        }

        /// <summary>
        /// الملخص المالي للمندوب
        /// </summary>
        [HttpGet("FinancialSummary")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> FinancialSummary([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude)
        {
            var userid = User.Identity.Name;
            var user = await _userManager.FindByIdAsync(userid);
            if (user == null || user.IsDeleted)
            {
                baseResponse.ErrorCode = Errors.TheUserNotExistOrDeleted;
                return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
            }

            var dto = new DriverFinancialSummaryDto();
            dto.Name = user.Name;

            if (user.UserType == UserType.Driver)
            {
                // الطلبات الغير منتهية (المعلقة - المطلوب تسليمها)
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

                // الطلبات المنتهية (تم تسويتها)
                var finishedOrders = _orders.Get(x =>
                    x.DeliveryId == userid && !x.IsDeleted && x.Finished
                    && x.Status != OrderStatus.PartialReturned
                ).ToList();

                dto.FinishedOrdersCount = finishedOrders.Count;
                dto.FinishedCollected = finishedOrders.Sum(x => x.ArrivedCost);
                dto.FinishedDriverCommission = finishedOrders.Sum(x => x.DeliveryCost);
                dto.FinishedDelivered = dto.FinishedCollected - dto.FinishedDriverCommission;

                // إجمالي
                dto.TotalOrdersCount = dto.PendingOrdersCount + dto.FinishedOrdersCount;
                dto.TotalCollected = dto.PendingCollected + dto.FinishedCollected;
                dto.TotalDriverCommission = dto.PendingDriverCommission + dto.FinishedDriverCommission;
                dto.TotalDeliveredToCompany = dto.PendingToDeliver + dto.FinishedDelivered;
            }

            if (latitude.HasValue && longitude.HasValue)
            {
                UpdateUserLocation locationdto = new UpdateUserLocation()
                {
                    Longitude = longitude.Value,
                    Latitude = latitude.Value,
                };
                await UpdateLocationMethod(locationdto, user);
            }

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

        #endregion

        [HttpGet("Images")]
        public IActionResult Images()
        {
            List<string> images = new List<string>();
            images.Add("wwwroot/Content/Images/Products/6e0295f2-4777-4dcd-a52c-cea23755b6fe.png");
            images.Add("wwwroot/Content/Images/Products/841cf1bd-618f-4675-af71-36a763db4abc.jpg");
            images.Add("wwwroot/Content/Images/Products/8b69bb74-7bb4-4099-b780-1730ccf0fa18.jpg");
            images.Add("wwwroot/Content/Images/Products/8dfebb44-abb8-48e1-931b-1c393697ca18.jpg");
            images.Add("wwwroot/Content/Images/Products/a8d99dd1-a037-468c-b320-165aa8d500dd.png");
            images.Add("wwwroot/Content/Images/Products/e82b946f-8f6b-417f-b69b-a616389ed4b4.png");


            var baseResponse = new { Data = images };
            return Ok(baseResponse);
        }
        private async Task<bool> UpdateLocationMethod(UpdateUserLocation dto, ApplicationUser user)
        {
            if (user.Tracking)
            {
                if (dto.Longitude.HasValue)
                    user.Longitude = dto.Longitude;

                if (dto.Latitude.HasValue)
                    user.Latitude = dto.Latitude;
                var address = await GetAddressFromCoordinatesAsync(dto.Latitude.Value, dto.Longitude.Value);

                await _user.Update(user);

                //add to locations list 
                // Define the Egypt time zone
                TimeZoneInfo egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");

                // Get the current UTC time
                DateTime utcNow = DateTime.UtcNow;

                // Convert the UTC time to Egypt time
                DateTime egyptTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, egyptTimeZone);

                Location location = new Location()
                {
                    DeliveryId = user.Id,
                    Longitude = dto.Longitude,
                    Latitude = dto.Latitude,
                    CreateOn = egyptTime,
                    Address = address,
                };
                await _locations.Add(location);
            }
            return true;
        }
        private async Task<string> GetAddressFromCoordinatesAsync(double latitude, double longitude)
        {
            string apiKey = "AIzaSyDR45xVCCyl8qLGg7tnQZcsAm4DGrhypFY";  // Replace with your actual API key
            string language = "ar";  // Arabic language code

            string url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude},{longitude}&key={apiKey}&language={language}";

            var client = _httpClientFactory.CreateClient();
            using var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(responseBody);

            // Check if the status returned is OK
            string status = json["status"]?.ToString();
            if (status != "OK")
            {
                return "Address not found";
            }

            // Retrieve the formatted address in Arabic if available
            var results = json["results"];
            if (results == null || results.Count() == 0)
            {
                return "Address not found";
            }


            // If no detailed address found, return the first formatted_address
            string fallbackAddress = results[1]["formatted_address"]?.ToString();
            return fallbackAddress ?? "Address not found";
        }

    }
}
