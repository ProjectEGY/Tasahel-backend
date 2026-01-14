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
        public AccountController(UserManager<ApplicationUser> userManager, IGeneric<ApplicationUser> user
            , IConfiguration configuration, IGeneric<Location> locations, IGeneric<Order> orders)
        {
            _userManager = userManager;
            _user = user;
            baseResponse = new BaseResponse();
            _configuration = configuration;
            _orders = orders;
            _locations = locations;
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
            var user = _userManager.FindByIdAsync(userid).Result;
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
            var user = _userManager.FindByIdAsync(userid).Result;
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
            var user = _userManager.FindByIdAsync(userid).Result;
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

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
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
}
