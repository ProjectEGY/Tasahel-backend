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

        // الفترة بين كل لقطة لوكيشن وللي بعدها للسائق الواحد.
        // مينفعش نحفظ كل نقطة بتجي من التطبيق - ده بيملي الجدول ويبطّأ السيستم كله.
        protected static readonly TimeSpan LocationSnapshotInterval = TimeSpan.FromMinutes(5);

        protected async Task<bool> UpdateLocationMethod(UpdateUserLocation dto, ApplicationUser user)
        {
            if (!user.Tracking) return true;
            if (!dto.Longitude.HasValue || !dto.Latitude.HasValue) return true;

            TimeZoneInfo egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            DateTime egyptNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            var lastSnapshotAt = _locations.GetAllAsIQueryable(
                    x => !x.IsDeleted && x.DeliveryId == user.Id,
                    orderby: q => q.OrderByDescending(x => x.CreateOn),
                    asNoTracking: true)
                .Select(x => (DateTime?)x.CreateOn)
                .FirstOrDefault();

            if (lastSnapshotAt.HasValue && (egyptNow - lastSnapshotAt.Value) < LocationSnapshotInterval)
            {
                return true;
            }

            user.Longitude = dto.Longitude;
            user.Latitude = dto.Latitude;

            await _user.Update(user);

            Location location = new Location()
            {
                DeliveryId = user.Id,
                Longitude = dto.Longitude,
                Latitude = dto.Latitude,
                CreateOn = egyptNow,
            };
            await _locations.Add(location);

            return true;
        }
    }
}
