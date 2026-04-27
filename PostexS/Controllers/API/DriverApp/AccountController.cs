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
using PostexS.Helper;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using PostexS.Models.Enums;
using PostexS.Models.ViewModels;
using PostexS.Models.Dtos;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace PostexS.Controllers.API
{
    /// <summary>
    /// تطبيق المندوب - الحساب والبروفايل (تسجيل الدخول، البروفايل، كلمة السر)
    /// </summary>
    public class AccountController : DriverBaseController
    {
        private readonly IGeneric<DeviceTokens> _deviceTokens;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            IGeneric<ApplicationUser> user,
            IGeneric<Location> locations,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IGeneric<DeviceTokens> deviceTokens)
            : base(userManager, user, locations, configuration, httpClientFactory)
        {
            _deviceTokens = deviceTokens;
        }

        /// <summary>
        /// تسجيل دخول المندوب
        /// </summary>
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
                // منع الراسل من تسجيل الدخول في تطبيق المندوب
                if (user.UserType == UserType.Client)
                {
                    baseResponse.ErrorCode = Errors.InvalidUserType;
                    baseResponse.ErrorMessage = "هذا الحساب خاص بالراسل، استخدم تطبيق الراسل لتسجيل الدخول";
                    return StatusCode((int)HttpStatusCode.Forbidden, baseResponse);
                }

                await UpdateLocationIfProvided(latitude, longitude, user);
                var dto = new LoginDto(user);
                dto.Token = await GenerateToken(user);
                baseResponse.Data = dto;
                return Ok(baseResponse);
            }
            baseResponse.ErrorCode = Errors.TheUsernameOrPasswordIsIncorrect;
            return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
        }

        /// <summary>
        /// نسيت كلمة السر - إعادة التعيين لكلمة سر افتراضية
        /// </summary>
        [HttpPut("ForgetPassword")]
        public async Task<IActionResult> ForgetPassword(string Phone)
        {
            if (!await _user.IsExist(x => x.PhoneNumber == Phone))
            {
                baseResponse.ErrorCode = Errors.ThisPhoneNumberNotExist;
                return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
            }
            var user = _user.Get(x => x.PhoneNumber == Phone).First();

            // منع الراسل من استخدام نسيت كلمة السر في تطبيق المندوب
            if (user.UserType == UserType.Client)
            {
                baseResponse.ErrorCode = Errors.InvalidUserType;
                baseResponse.ErrorMessage = "هذا الحساب خاص بالراسل، استخدم تطبيق الراسل";
                return StatusCode((int)HttpStatusCode.Forbidden, baseResponse);
            }

            var passwordHashe = _userManager.PasswordHasher.HashPassword(user, "123456");
            user.PasswordHash = passwordHashe;
            await _user.Update(user);
            return Ok(baseResponse);
        }

        /// <summary>
        /// الحصول على بيانات البروفايل
        /// </summary>
        [HttpGet("GetProfile")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetProfile([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude, string lang = "en")
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            var dto = new LoginDto(user);
            await UpdateLocationIfProvided(latitude, longitude, user);
            baseResponse.Data = dto;
            return Ok(baseResponse);
        }

        /// <summary>
        /// الحصول على حالة التتبع وسجل نقاط التتبع مع فلتر بالتاريخ
        /// </summary>
        [HttpGet("GetTracking")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetTracking(
            DateTime? from,
            DateTime? to,
            string lang = "en")
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            TimeZoneInfo egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            DateTime egyptNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            DateTime filterFrom = from ?? egyptNow.Date;
            DateTime filterTo = to ?? egyptNow.Date.AddDays(1);

            var locations = _locations.GetAllAsIQueryable(
                x => !x.IsDeleted && x.DeliveryId == user.Id
                    && x.CreateOn >= filterFrom && x.CreateOn < filterTo,
                orderby: q => q.OrderByDescending(x => x.CreateOn))
                .Select(x => new LocationDto(x))
                .ToList();

            baseResponse.Data = new
            {
                isTracking = user.Tracking,
                currentLatitude = user.Latitude,
                currentLongitude = user.Longitude,
                locations = locations
            };
            return Ok(baseResponse);
        }

        /// <summary>
        /// تحديث موقع المندوب
        /// </summary>
        [HttpPut("UpdateLocation")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> UpdateLocation([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude, string lang = "en")
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            if (!latitude.HasValue || !longitude.HasValue)
            {
                baseResponse.ErrorCode = Errors.TheModelIsInvalid;
                return StatusCode((int)HttpStatusCode.BadRequest, baseResponse);
            }

            await UpdateLocationIfProvided(latitude, longitude, user);
            baseResponse.Data = new
            {
                latitude = user.Latitude,
                longitude = user.Longitude
            };
            return Ok(baseResponse);
        }

        /// <summary>
        /// تحديث بيانات البروفايل
        /// </summary>
        [HttpPut("UpdateProfile")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> UpdateProfile(/*[FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude,*/ UpdateUserDto dto, string lang = "en")
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            if (!string.IsNullOrEmpty(dto.UserName))
                user.Name = dto.UserName;
            if (!string.IsNullOrEmpty(dto.Email))
                user.Email = dto.Email;
            if (!string.IsNullOrEmpty(dto.WhatsappPhone))
                user.WhatsappPhone = dto.WhatsappPhone;
            if (!string.IsNullOrEmpty(dto.Phone))
                user.PhoneNumber = dto.Phone;

            await _user.Update(user);
            var userdto = _user.Get(x => x.Id == user.Id).First();
            var response = new LoginDto(userdto);
            //await UpdateLocationIfProvided(latitude, longitude, user);
            baseResponse.Data = response;
            return Ok(baseResponse);
        }

        /// <summary>
        /// تغيير كلمة السر
        /// </summary>
        [HttpPut("ChangePassword")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ChangePassword([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude, ChangePasswordDto model)
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            var isOldCorrect = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, model.OldPassword);
            if (isOldCorrect == PasswordVerificationResult.Failed)
            {
                baseResponse.ErrorCode = Errors.TheOldPasswordIsInCorrect;
                return StatusCode((int)HttpStatusCode.BadRequest, baseResponse);
            }
            user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, model.NewPassword);
            await _user.Update(user);
            await UpdateLocationIfProvided(latitude, longitude, user);
            return Ok(baseResponse);
        }

        /// <summary>
        /// حذف الحساب نهائياً - مطلوب من Apple App Store و Google Play
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token + كلمة السر الحالية للتأكيد.
        /// بعد الحذف:
        ///   - الحساب لن يقدر يسجل دخول مرة أخرى
        ///   - تُحذف الـ Device Tokens (لا يستقبل إشعارات)
        ///   - بياناته تظل في النظام للسجلات والمحاسبة (Soft Delete)
        ///   - الطلبات الجارية تظل سارية لحين معالجتها
        /// </remarks>
        /// <param name="dto">DeleteAccountDto: Password (مطلوب) + Reason (اختياري)</param>
        [HttpDelete("Account")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> DeleteAccount(DeleteAccountDto dto)
        {
            var (user, errorResult) = await GetCurrentDriverAsync();
            if (errorResult != null) return errorResult;

            if (dto == null || string.IsNullOrEmpty(dto.Password))
            {
                baseResponse.ErrorCode = Errors.SomeThingWentwrong;
                baseResponse.ErrorMessage = "كلمة السر مطلوبة لتأكيد الحذف";
                return StatusCode((int)HttpStatusCode.BadRequest, baseResponse);
            }

            var isPasswordCorrect = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (isPasswordCorrect == PasswordVerificationResult.Failed)
            {
                baseResponse.ErrorCode = Errors.TheOldPasswordIsInCorrect;
                baseResponse.ErrorMessage = "كلمة السر غير صحيحة";
                return StatusCode((int)HttpStatusCode.BadRequest, baseResponse);
            }

            try
            {
                // حذف الـ Device Tokens (إيقاف الإشعارات)
                var tokens = _deviceTokens.Get(t => t.UserId == user.Id).ToList();
                foreach (var t in tokens)
                {
                    t.IsDeleted = true;
                    t.DeletedOn = DateTime.UtcNow;
                    await _deviceTokens.Update(t);
                }

                // Soft delete للحساب
                user.IsDeleted = true;
                user.Tracking = false; // إيقاف تتبع الموقع

                // إبطال إمكانية الدخول مستقبلاً
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;

                // سجّل سبب الحذف في الـ logs لو متبعت
                if (!string.IsNullOrWhiteSpace(dto.Reason))
                {
                    System.Diagnostics.Debug.WriteLine($"[DriverApp] Account {user.Id} deleted. Reason: {dto.Reason.Trim()}");
                }

                await _user.Update(user);

                baseResponse.Data = "تم حذف الحساب نهائياً. لن تقدر تسجل دخول مرة أخرى بهذه البيانات.";
                return Ok(baseResponse);
            }
            catch (Exception ex)
            {
                baseResponse.ErrorCode = Errors.SomeThingWentwrong;
                baseResponse.ErrorMessage = "حدث خطأ أثناء حذف الحساب: " + ex.Message;
                return StatusCode((int)HttpStatusCode.InternalServerError, baseResponse);
            }
        }

        /// <summary>
        /// صور العرض
        /// </summary>
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
    }
}
