using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using PostexS.Models.Dtos;
using PostexS.Models.Enums;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PostexS.Controllers.API
{
    /// <summary>
    /// Base controller لتطبيق المندوب - يحتوي على الكود المشترك بين كل controllers المندوب
    /// </summary>
    [Route("api/Account")]
    [ApiController]
    public abstract class DriverBaseController : ControllerBase
    {
        protected readonly UserManager<ApplicationUser> _userManager;
        protected readonly IGeneric<ApplicationUser> _user;
        protected readonly IGeneric<Location> _locations;
        protected readonly IConfiguration _configuration;
        protected readonly IHttpClientFactory _httpClientFactory;
        protected readonly BaseResponse baseResponse;

        protected DriverBaseController(
            UserManager<ApplicationUser> userManager,
            IGeneric<ApplicationUser> user,
            IGeneric<Location> locations,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _userManager = userManager;
            _user = user;
            _locations = locations;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            baseResponse = new BaseResponse();
        }

        /// <summary>
        /// التحقق من المستخدم الحالي وأنه مندوب (ليس راسل)
        /// </summary>
        protected async Task<(ApplicationUser User, IActionResult ErrorResult)> GetCurrentDriverAsync()
        {
            var userId = User.Identity.Name;
            if (string.IsNullOrEmpty(userId))
            {
                baseResponse.ErrorCode = Errors.TheUserNotExistOrDeleted;
                baseResponse.ErrorMessage = "المستخدم غير موجود";
                return (null, StatusCode((int)HttpStatusCode.Unauthorized, baseResponse));
            }

            var user = _user.Get(x => x.Id == userId).FirstOrDefault();
            if (user == null || user.IsDeleted)
            {
                baseResponse.ErrorCode = Errors.TheUserNotExistOrDeleted;
                baseResponse.ErrorMessage = "المستخدم غير موجود أو محذوف";
                return (null, StatusCode((int)HttpStatusCode.NotFound, baseResponse));
            }

            if (user.UserType == UserType.Client)
            {
                baseResponse.ErrorCode = Errors.InvalidUserType;
                baseResponse.ErrorMessage = "هذا الحساب خاص بالراسل، استخدم تطبيق الراسل";
                return (null, StatusCode((int)HttpStatusCode.Forbidden, baseResponse));
            }

            return (user, null);
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

        protected async Task UpdateLocationIfProvided(double? latitude, double? longitude, ApplicationUser user)
        {
            if (latitude.HasValue && longitude.HasValue)
            {
                await UpdateLocationMethod(new UpdateUserLocation
                {
                    Longitude = longitude.Value,
                    Latitude = latitude.Value,
                }, user);
            }
        }

        protected async Task<bool> UpdateLocationMethod(UpdateUserLocation dto, ApplicationUser user)
        {
            if (user.Tracking)
            {
                if (dto.Longitude.HasValue)
                    user.Longitude = dto.Longitude;

                if (dto.Latitude.HasValue)
                    user.Latitude = dto.Latitude;
                var address = await GetAddressFromCoordinatesAsync(dto.Latitude.Value, dto.Longitude.Value);

                await _user.Update(user);

                TimeZoneInfo egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                DateTime utcNow = DateTime.UtcNow;
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

        protected async Task<string> GetAddressFromCoordinatesAsync(double latitude, double longitude)
        {
            string apiKey = "AIzaSyDR45xVCCyl8qLGg7tnQZcsAm4DGrhypFY";
            string language = "ar";
            string url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude},{longitude}&key={apiKey}&language={language}";

            var client = _httpClientFactory.CreateClient();
            using var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(responseBody);

            string status = json["status"]?.ToString();
            if (status != "OK")
                return "Address not found";

            var results = json["results"];
            if (results == null || results.Count() == 0)
                return "Address not found";

            string fallbackAddress = results[1]["formatted_address"]?.ToString();
            return fallbackAddress ?? "Address not found";
        }
    }
}
