using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
using System.Drawing.Imaging;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ZXing;
using ZXing.Common;

namespace PostexS.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class SenderAppController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGeneric<ApplicationUser> _user;
        private readonly IGeneric<Order> _orders;
        private readonly IGeneric<OrderOperationHistory> _histories;
        private readonly IGeneric<Notification> _notifications;
        private readonly IGeneric<DeviceTokens> _deviceTokens;
        private readonly IGeneric<Wallet> _wallets;
        private readonly IGeneric<ContactUs> _contactUs;
        private readonly IGeneric<Branch> _branches;
        private readonly IGeneric<Location> _locations;
        private readonly ICRUD<Order> _CRUD;
        private readonly IConfiguration _configuration;
        private readonly BaseResponse _baseResponse;

        public SenderAppController(
            UserManager<ApplicationUser> userManager,
            IGeneric<ApplicationUser> user,
            IGeneric<Order> orders,
            IGeneric<OrderOperationHistory> histories,
            IGeneric<Notification> notifications,
            IGeneric<DeviceTokens> deviceTokens,
            IGeneric<Wallet> wallets,
            IGeneric<ContactUs> contactUs,
            IGeneric<Branch> branches,
            IGeneric<Location> locations,
            ICRUD<Order> CRUD,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _user = user;
            _orders = orders;
            _histories = histories;
            _notifications = notifications;
            _deviceTokens = deviceTokens;
            _wallets = wallets;
            _contactUs = contactUs;
            _branches = branches;
            _locations = locations;
            _CRUD = CRUD;
            _configuration = configuration;
            _baseResponse = new BaseResponse();
        }

        #region Private Helpers

        private async Task<string> GenerateToken(ApplicationUser user)
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

        private async Task<(ApplicationUser User, IActionResult ErrorResult)> GetCurrentSenderAsync()
        {
            var userId = User.Identity.Name;
            if (userId == null)
            {
                _baseResponse.ErrorCode = Errors.TheUserNotExistOrDeleted;
                _baseResponse.ErrorMessage = "User not found";
                return (null, StatusCode((int)HttpStatusCode.Unauthorized, _baseResponse));
            }

            var user = _user.Get(x => x.Id == userId).FirstOrDefault();
            if (user == null || user.IsDeleted)
            {
                _baseResponse.ErrorCode = Errors.TheUserNotExistOrDeleted;
                _baseResponse.ErrorMessage = "User not found or deleted";
                return (null, StatusCode((int)HttpStatusCode.NotFound, _baseResponse));
            }

            if (user.UserType != UserType.Client)
            {
                _baseResponse.ErrorCode = Errors.InvalidUserType;
                _baseResponse.ErrorMessage = "This endpoint is for sender accounts only";
                return (null, StatusCode((int)HttpStatusCode.Forbidden, _baseResponse));
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

        private List<OrderTimelineEntry> BuildTimeline(OrderOperationHistory history)
        {
            var timeline = new List<OrderTimelineEntry>();
            var defaultDate = default(DateTime);

            if (history.CreateDate != defaultDate)
                timeline.Add(new OrderTimelineEntry { Action = "Created", ActionArabic = "تم الإنشاء", Date = history.CreateDate });

            if (history.AcceptDate != defaultDate)
                timeline.Add(new OrderTimelineEntry { Action = "Accepted", ActionArabic = "تمت الموافقة", Date = history.AcceptDate });

            if (history.Assign_To_DriverDate != defaultDate)
                timeline.Add(new OrderTimelineEntry { Action = "Assigned to Driver", ActionArabic = "تم التعيين لمندوب", Date = history.Assign_To_DriverDate });

            if (history.EditDate != defaultDate)
                timeline.Add(new OrderTimelineEntry { Action = "Edited", ActionArabic = "تم التعديل", Date = history.EditDate });

            if (history.TransferDate != defaultDate)
                timeline.Add(new OrderTimelineEntry { Action = "Transferred", ActionArabic = "تم النقل", Date = history.TransferDate });

            if (history.AcceptTransferDate != defaultDate)
                timeline.Add(new OrderTimelineEntry { Action = "Transfer Accepted", ActionArabic = "تم قبول النقل", Date = history.AcceptTransferDate });

            if (history.FinishDate != defaultDate)
                timeline.Add(new OrderTimelineEntry { Action = "Finished", ActionArabic = "تم الانتهاء", Date = history.FinishDate });

            if (history.CompleteDate != defaultDate)
                timeline.Add(new OrderTimelineEntry { Action = "Completed", ActionArabic = "تمت التسوية", Date = history.CompleteDate });

            if (history.RejectDate != defaultDate)
                timeline.Add(new OrderTimelineEntry { Action = "Rejected", ActionArabic = "تم الرفض", Date = history.RejectDate });

            if (history.DeleteDate != defaultDate)
                timeline.Add(new OrderTimelineEntry { Action = "Deleted", ActionArabic = "تم الحذف", Date = history.DeleteDate });

            if (history.RestoreDate != defaultDate)
                timeline.Add(new OrderTimelineEntry { Action = "Restored", ActionArabic = "تمت الاستعادة", Date = history.RestoreDate });

            if (history.ReturnedFinishDate != defaultDate)
                timeline.Add(new OrderTimelineEntry { Action = "Return Finished", ActionArabic = "تم انتهاء المرتجع", Date = history.ReturnedFinishDate });

            if (history.ReturnedCompleteDate != defaultDate)
                timeline.Add(new OrderTimelineEntry { Action = "Return Completed", ActionArabic = "تمت تسوية المرتجع", Date = history.ReturnedCompleteDate });

            if (history.TransferReturnedDate != defaultDate)
                timeline.Add(new OrderTimelineEntry { Action = "Return Transferred", ActionArabic = "تم نقل المرتجع", Date = history.TransferReturnedDate });

            if (history.AcceptTransferReturnedDate != defaultDate)
                timeline.Add(new OrderTimelineEntry { Action = "Return Transfer Accepted", ActionArabic = "تم قبول نقل المرتجع", Date = history.AcceptTransferReturnedDate });

            timeline.Sort((a, b) => a.Date.CompareTo(b.Date));
            return timeline;
        }

        #endregion

        #region Authentication (AllowAnonymous)

        /// <summary>
        /// تسجيل دخول الراسل - يرسل رقم الهاتف وكلمة السر ويرجع بيانات البروفايل مع JWT Token
        /// </summary>
        /// <remarks>
        /// يتحقق ان الحساب من نوع راسل (Client) وانه مش في انتظار الموافقة.
        /// في حالة النجاح يرجع بيانات الراسل كاملة مع Token لاستخدامه في باقي الـ APIs.
        /// </remarks>
        /// <param name="model">رقم الهاتف وكلمة السر</param>
        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] SubmitLoginDto model)
        {
            if (!await _user.IsExist(x => x.PhoneNumber == model.Phone))
            {
                _baseResponse.ErrorCode = Errors.TheUsernameOrPasswordIsIncorrect;
                _baseResponse.ErrorMessage = "رقم الهاتف أو كلمة السر غير صحيحة";
                return StatusCode((int)HttpStatusCode.NotFound, _baseResponse);
            }

            var user = _user.Get(x => x.PhoneNumber == model.Phone).First();

            if (user == null || user.IsDeleted)
            {
                _baseResponse.ErrorCode = Errors.TheUserNotExistOrDeleted;
                _baseResponse.ErrorMessage = "المستخدم غير موجود أو محذوف";
                return StatusCode((int)HttpStatusCode.NotFound, _baseResponse);
            }

            if (user.UserType != UserType.Client)
            {
                _baseResponse.ErrorCode = Errors.InvalidUserType;
                _baseResponse.ErrorMessage = "هذا الحساب ليس حساب راسل";
                return StatusCode((int)HttpStatusCode.Forbidden, _baseResponse);
            }

            if (!user.IsApproved)      //  IsApproved  معناه انه متوافق عليه ,لانها معكوسه في التطبيق
            {
                _baseResponse.ErrorCode = Errors.UserNotApproved;
                _baseResponse.ErrorMessage = "الحساب في انتظار الموافقة";
                return StatusCode((int)HttpStatusCode.Forbidden, _baseResponse);
            }

            if (!await _userManager.CheckPasswordAsync(user, model.Password))
            {
                _baseResponse.ErrorCode = Errors.TheUsernameOrPasswordIsIncorrect;
                _baseResponse.ErrorMessage = "رقم الهاتف أو كلمة السر غير صحيحة";
                return StatusCode((int)HttpStatusCode.NotFound, _baseResponse);
            }

            var branch = _branches.Get(x => x.Id == user.BranchId).FirstOrDefault();
            var dto = new SenderAppProfileDto(user, branch);
            dto.Token = await GenerateToken(user);

            _baseResponse.Data = dto;
            return Ok(_baseResponse);
        }

        /// <summary>
        /// تسجيل حساب راسل جديد - إنشاء حساب جديد برقم الهاتف وكلمة السر والبيانات الشخصية
        /// </summary>
        /// <remarks>
        /// ينشئ حساب من نوع Client ويكون في انتظار موافقة الأدمن.
        /// لا يرجع Token - الراسل لازم يستنى الموافقة ويسجل دخول بعدها.
        /// الحقول المطلوبة: Name, Phone, Password, BranchId.
        /// الحقول الاختيارية: Email, WhatsappPhone, Address.
        /// </remarks>
        /// <param name="model">بيانات التسجيل</param>
        [HttpPost("Register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] SenderAppRegisterDto model)
        {
            if (model == null)
            {
                _baseResponse.ErrorCode = Errors.TheModelIsInvalid;
                _baseResponse.ErrorMessage = "البيانات مطلوبة";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            if (await _user.IsExist(x => x.PhoneNumber == model.Phone && !x.IsDeleted))
            {
                _baseResponse.ErrorCode = Errors.PhoneAlreadyRegistered;
                _baseResponse.ErrorMessage = "رقم الهاتف مسجل بالفعل";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            var user = new ApplicationUser
            {
                UserName = model.Phone,
                PhoneNumber = model.Phone,
                Name = model.Name,
                Email = model.Email,
                WhatsappPhone = model.WhatsappPhone,
                SecondaryPhone = model.SecondaryPhone,
                Address = model.Address,
                BranchId = model.BranchId,
                UserType = UserType.Client,
                IsApproved = false, // الحساب هيكون في انتظار موافقة الأدمن
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                _baseResponse.ErrorCode = Errors.SomeThingWentwrong;
                _baseResponse.ErrorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            _baseResponse.ErrorMessage = "تم إنشاء الحساب بنجاح وهو في انتظار موافقة الإدارة";
            return Ok(_baseResponse);
        }

        /// <summary>
        /// نسيت كلمة السر - إعادة تعيين كلمة السر لكلمة سر افتراضية (123456) باستخدام رقم الهاتف
        /// </summary>
        /// <remarks>
        /// يبحث عن حساب راسل برقم الهاتف المرسل ويعيد تعيين كلمة السر إلى 123456.
        /// الراسل يقدر يغير كلمة السر بعدين من خلال ChangePassword.
        /// </remarks>
        /// <param name="Phone">رقم هاتف الراسل المسجل</param>
        [HttpPut("ForgetPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgetPassword([FromQuery] string Phone)
        {
            if (string.IsNullOrEmpty(Phone))
            {
                _baseResponse.ErrorCode = Errors.TheModelIsInvalid;
                _baseResponse.ErrorMessage = "رقم الهاتف مطلوب";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            var user = _user.Get(x => x.PhoneNumber == Phone && !x.IsDeleted && x.UserType == UserType.Client).FirstOrDefault();
            if (user == null)
            {
                _baseResponse.ErrorCode = Errors.ThisPhoneNumberNotExist;
                _baseResponse.ErrorMessage = "رقم الهاتف غير مسجل";
                return StatusCode((int)HttpStatusCode.NotFound, _baseResponse);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, "123456");
            if (!result.Succeeded)
            {
                _baseResponse.ErrorCode = Errors.SomeThingWentwrong;
                _baseResponse.ErrorMessage = "حدث خطأ أثناء إعادة تعيين كلمة السر";
                return StatusCode((int)HttpStatusCode.InternalServerError, _baseResponse);
            }

            _baseResponse.Data = "تم إعادة تعيين كلمة السر بنجاح";
            return Ok(_baseResponse);
        }

        /// <summary>
        /// قائمة الفروع المتاحة - يرجع كل الفروع النشطة مع بياناتها (الاسم، العنوان، الهاتف، الواتساب، الإحداثيات)
        /// </summary>
        /// <remarks>
        /// يُستخدم في شاشة التسجيل لاختيار الفرع.
        /// لا يحتاج تسجيل دخول.
        /// </remarks>
        [HttpGet("Branches")]
        [AllowAnonymous]
        public IActionResult GetBranches()
        {
            var branches = _branches.Get(x => !x.IsDeleted).Select(b => new
            {
                b.Id,
                b.Name,
                b.Address,
                b.PhoneNumber,
                b.Whatsapp,
                b.Latitude,
                b.Longitude
            }).ToList();

            _baseResponse.Data = branches;
            return Ok(_baseResponse);
        }

        #endregion

        #region Profile (JWT Required)

        /// <summary>
        /// الحصول على البروفايل - يرجع بيانات الراسل الشخصية مع رصيد المحفظة واسم الفرع وإعدادات الإخفاء
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token في الـ Header.
        /// يرجع: Id, Name, Phone, Email, WhatsappPhone, Address, Wallet, BranchId, BranchName, HideSenderName, HideSenderPhone, HideSenderCode.
        /// </remarks>
        [HttpGet("Profile")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetProfile()
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            var branch = _branches.Get(x => x.Id == user.BranchId).FirstOrDefault();
            var dto = new SenderAppProfileDto(user, branch);

            _baseResponse.Data = dto;
            return Ok(_baseResponse);
        }

        /// <summary>
        /// تحديث البروفايل - تعديل البيانات الشخصية (الاسم، الإيميل، رقم الواتساب، العنوان)
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. كل الحقول اختيارية - ابعت الحقول اللي عايز تعدلها بس.
        /// يرجع بيانات البروفايل المحدثة.
        /// </remarks>
        /// <param name="dto">البيانات المراد تحديثها (كل الحقول اختيارية)</param>
        [HttpPut("Profile")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> UpdateProfile([FromBody] SenderAppUpdateProfileDto dto)
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            if (!string.IsNullOrEmpty(dto.Name))
                user.Name = dto.Name;
            if (!string.IsNullOrEmpty(dto.Email))
                user.Email = dto.Email;
            if (!string.IsNullOrEmpty(dto.WhatsappPhone))
                user.WhatsappPhone = dto.WhatsappPhone;
            if (!string.IsNullOrEmpty(dto.SecondaryPhone))
                user.SecondaryPhone = dto.SecondaryPhone;
            if (!string.IsNullOrEmpty(dto.Address))
                user.Address = dto.Address;

            await _user.Update(user);

            var branch = _branches.Get(x => x.Id == user.BranchId).FirstOrDefault();
            var profileDto = new SenderAppProfileDto(user, branch);

            _baseResponse.Data = profileDto;
            return Ok(_baseResponse);
        }

        /// <summary>
        /// تغيير كلمة السر - يغير كلمة السر القديمة لكلمة سر جديدة
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. لازم يبعت كلمة السر القديمة والجديدة.
        /// </remarks>
        /// <param name="dto">كلمة السر القديمة والجديدة</param>
        [HttpPut("ChangePassword")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            if (!await _userManager.CheckPasswordAsync(user, dto.OldPassword))
            {
                _baseResponse.ErrorCode = Errors.TheOldPasswordIsInCorrect;
                _baseResponse.ErrorMessage = "كلمة السر القديمة غير صحيحة";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
            if (!result.Succeeded)
            {
                _baseResponse.ErrorCode = Errors.SomeThingWentwrong;
                _baseResponse.ErrorMessage = "حدث خطأ أثناء تغيير كلمة السر";
                return StatusCode((int)HttpStatusCode.InternalServerError, _baseResponse);
            }

            _baseResponse.Data = "تم تغيير كلمة السر بنجاح";
            return Ok(_baseResponse);
        }

        /// <summary>
        /// حذف الحساب نهائياً - يقوم بحذف حساب المستخدم (Soft Delete) بعد التحقق من كلمة السر.
        /// مطلوب من Apple App Store و Google Play لأي تطبيق فيه إنشاء حسابات.
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token + كلمة السر الحالية للتأكيد.
        /// بعد الحذف:
        ///   - الحساب لن يقدر يسجل دخول مرة أخرى
        ///   - تُحذف الـ Device Tokens (لا يستقبل إشعارات)
        ///   - بياناته تظل في النظام للسجلات والمحاسبة (Soft Delete)
        ///   - الطلبات الجارية تظل سارية لحين معالجتها (لا تُلغى تلقائياً)
        /// </remarks>
        /// <param name="dto">DeleteAccountDto: Password (مطلوب) + Reason (اختياري)</param>
        [HttpDelete("Account")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountDto dto)
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            if (dto == null || string.IsNullOrEmpty(dto.Password))
            {
                _baseResponse.ErrorCode = Errors.SomeThingWentwrong;
                _baseResponse.ErrorMessage = "كلمة السر مطلوبة لتأكيد الحذف";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            // التحقق من كلمة السر للتأكد من هوية المستخدم
            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                _baseResponse.ErrorCode = Errors.TheOldPasswordIsInCorrect;
                _baseResponse.ErrorMessage = "كلمة السر غير صحيحة";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
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

                // إبطال إمكانية الدخول مستقبلاً
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;

                // سجّل سبب الحذف في الـ logs (لو متبعت) — مش بنخزن في user لأنه مش عنده Note
                if (!string.IsNullOrWhiteSpace(dto.Reason))
                {
                    System.Diagnostics.Debug.WriteLine($"[SenderApp] Account {user.Id} deleted. Reason: {dto.Reason.Trim()}");
                }

                await _user.Update(user);

                _baseResponse.Data = "تم حذف الحساب نهائياً. لن تقدر تسجل دخول مرة أخرى بهذه البيانات.";
                return Ok(_baseResponse);
            }
            catch (Exception ex)
            {
                _baseResponse.ErrorCode = Errors.SomeThingWentwrong;
                _baseResponse.ErrorMessage = "حدث خطأ أثناء حذف الحساب: " + ex.Message;
                return StatusCode((int)HttpStatusCode.InternalServerError, _baseResponse);
            }
        }

        #endregion

        #region Orders (JWT Required)

        /// <summary>
        /// إنشاء شحنة جديدة - إضافة طرد جديد مع بيانات المستلم والتكلفة
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. الشحنة بتتنشئ بحالة Placed و Pending=true (في انتظار موافقة الأدمن).
        /// الحقول المطلوبة: ClientName, ClientPhone.
        /// الحقول الاختيارية: ClientCity, Address, ClientCode, Cost, DeliveryFees, Notes.
        /// يرجع بيانات الشحنة مع الكود والباركود.
        /// </remarks>
        /// <param name="model">بيانات الشحنة (اسم المستلم، رقم الهاتف، العنوان، التكلفة...)</param>
        [HttpPost("Orders")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> CreateOrder([FromBody] OrderVM model)
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            if (model == null)
            {
                _baseResponse.ErrorCode = Errors.TheModelIsInvalid;
                _baseResponse.ErrorMessage = "بيانات الشحنة مطلوبة";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            if (string.IsNullOrWhiteSpace(model.ClientName))
            {
                _baseResponse.ErrorCode = Errors.TheModelIsInvalid;
                _baseResponse.ErrorMessage = "اسم المستلم مطلوب";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            if (string.IsNullOrWhiteSpace(model.ClientPhone))
            {
                _baseResponse.ErrorCode = Errors.TheModelIsInvalid;
                _baseResponse.ErrorMessage = "رقم هاتف المستلم مطلوب";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            string generalNote = user.OrdersGeneralNote != null ? user.OrdersGeneralNote + " - " : "";

            var order = new Order
            {
                Address = model.Address,
                AddressCity = model.ClientCity,
                ClientName = model.ClientName,
                ClientCode = model.ClientCode,
                ClientPhone = model.ClientPhone,
                ClientSecondaryPhone = model.ClientSecondaryPhone,
                Cost = model.Cost,
                DeliveryFees = model.DeliveryFees,
                Notes = generalNote + model.Notes,
                ClientId = user.Id,
                Pending = true,
                TotalCost = model.Cost + model.DeliveryFees,
                Status = OrderStatus.Placed,
                BranchId = user.BranchId,
            };

            if (!await _orders.Add(order))
            {
                _baseResponse.ErrorCode = Errors.SomeThingWentwrong;
                _baseResponse.ErrorMessage = "حدث خطأ أثناء إنشاء الشحنة";
                return StatusCode((int)HttpStatusCode.InternalServerError, _baseResponse);
            }

            var history = new OrderOperationHistory
            {
                OrderId = order.Id,
                Create_UserId = user.Id,
                CreateDate = order.CreateOn,
            };

            if (!await _histories.Add(history))
            {
                _baseResponse.ErrorCode = Errors.SomeThingWentwrong;
                _baseResponse.ErrorMessage = "حدث خطأ أثناء إنشاء الشحنة";
                return StatusCode((int)HttpStatusCode.InternalServerError, _baseResponse);
            }

            order.OrderOperationHistoryId = history.Id;
            order.Code = "Tas" + order.Id.ToString();
            order.BarcodeImage = GetBarcode(order.Code);

            if (!await _orders.Update(order))
            {
                _baseResponse.ErrorCode = Errors.SomeThingWentwrong;
                _baseResponse.ErrorMessage = "حدث خطأ أثناء إنشاء الشحنة";
                return StatusCode((int)HttpStatusCode.InternalServerError, _baseResponse);
            }

            await _CRUD.Update(order.Id);

            var orderDto = new SenderOrderDto(order, null, null);
            _baseResponse.Data = orderDto;
            return Ok(_baseResponse);
        }

        /// <summary>
        /// قائمة الشحنات - عرض كل شحنات الراسل مع إمكانية البحث والتصفية والتقسيم لصفحات
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. يرجع الشحنات مرتبة من الأحدث للأقدم.
        /// البحث بيشتغل على: اسم المستلم، كود الشحنة، كود العميل، رقم هاتف المستلم.
        /// التصفية: حسب الحالة (status) وتاريخ الإنشاء (from/to).
        /// الحد الأقصى لحجم الصفحة: 50.
        /// </remarks>
        /// <param name="search">نص البحث (اختياري) - بيبحث في اسم المستلم وكود الشحنة وكود العميل ورقم الهاتف</param>
        /// <param name="status">حالة الشحنة للتصفية (اختياري) - مثل: Placed, Assigned, Delivered, Returned...</param>
        /// <param name="from">تاريخ البداية للتصفية (اختياري)</param>
        /// <param name="to">تاريخ النهاية للتصفية (اختياري)</param>
        /// <param name="pageNumber">رقم الصفحة (افتراضي: 1)</param>
        /// <param name="pageSize">حجم الصفحة (افتراضي: 10، الحد الأقصى: 50)</param>
        [HttpGet("Orders")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetOrders(
            [FromQuery] string search = null,
            [FromQuery] OrderStatus? status = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            var query = _orders.GetAllAsIQueryable(
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
                query = query.Where(x => x.Status == status.Value);

            if (from.HasValue)
                query = query.Where(x => x.CreateOn >= from.Value);

            if (to.HasValue)
                query = query.Where(x => x.CreateOn <= to.Value.AddDays(1));

            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var orders = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var trackableStatuses = new[] { OrderStatus.Assigned, OrderStatus.Waiting };

            var orderDtos = orders.Select(o => new SenderOrderDto(
                o,
                o.Delivery?.Name,
                o.Delivery?.PhoneNumber,
                isTrackable: o.Delivery != null
                    && o.Delivery.Tracking
                    && trackableStatuses.Contains(o.Status)
            )).ToList();

            var response = new PaginatedResponse<SenderOrderDto>(orderDtos, pageNumber, pageSize, totalCount);

            _baseResponse.Data = response;
            return Ok(_baseResponse);
        }

        /// <summary>
        /// مواقع المناديب الحالية - يرجع الإحداثيات الحالية لكل مندوب عنده شحنات حالية (Assigned أو Waiting) مع التتبع مفعّل
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. بيرجع قائمة بكل المناديب اللي عندهم شحنات حالية للراسل،
        /// مع موقع كل مندوب وعدد الشحنات اللي معاه وأكوادها.
        /// مفيد لعرض كل المناديب على الخريطة مرة واحدة.
        /// </remarks>
        [HttpGet("DriversLocations")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> DriversLocations()
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            var trackableStatuses = new[] { OrderStatus.Assigned, OrderStatus.Waiting };

            var activeOrders = _orders.GetAllAsIQueryable(
                filter: x => x.ClientId == user.Id
                    && !x.IsDeleted
                    && trackableStatuses.Contains(x.Status)
                    && x.DeliveryId != null,
                IncludeProperties: "Delivery")
                .ToList();

            var driversData = activeOrders
                .Where(o => o.Delivery != null && o.Delivery.Tracking)
                .GroupBy(o => o.DeliveryId)
                .Select(g =>
                {
                    var driver = g.First().Delivery;
                    return new
                    {
                        DriverName = driver.Name,
                        Latitude = driver.Latitude,
                        Longitude = driver.Longitude,
                        OrdersCount = g.Count(),
                        Orders = g.Select(o => new
                        {
                            OrderCode = o.Code,
                            ReceiverName = o.ClientName,
                            Status = o.Status.ToString()
                        })
                    };
                }).ToList();

            _baseResponse.Data = driversData;
            return Ok(_baseResponse);
        }

        /// <summary>
        /// تفاصيل شحنة - يرجع بيانات الشحنة كاملة مع الـ Timeline (سجل العمليات) واسم المندوب
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. يرجع كل بيانات الشحنة + Timeline مرتب زمنياً يوضح كل عملية تمت على الشحنة
        /// (إنشاء، تعيين مندوب، تسليم، إرجاع... إلخ) مع التاريخ.
        /// </remarks>
        /// <param name="code">كود الشحنة (مثال: Tas123)</param>
        [HttpGet("Orders/{code}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetOrderByCode([FromRoute] string code)
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            if (string.IsNullOrWhiteSpace(code))
            {
                _baseResponse.ErrorCode = Errors.TheModelIsInvalid;
                _baseResponse.ErrorMessage = "كود الشحنة مطلوب";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            var order = await _orders.GetSingle(
                x => x.Code == code && x.ClientId == user.Id && !x.IsDeleted,
                includeProperties: "Delivery,OrderOperationHistory");

            if (order == null)
            {
                _baseResponse.ErrorCode = Errors.TheOrderNotExistOrDeleted;
                _baseResponse.ErrorMessage = "الشحنة غير موجودة";
                return StatusCode((int)HttpStatusCode.NotFound, _baseResponse);
            }

            var dto = new SenderAppOrderDetailDto(order, user, order.Delivery?.Name, order.Delivery?.PhoneNumber);

            if (order.OrderOperationHistory != null)
                dto.Timeline = BuildTimeline(order.OrderOperationHistory);

            _baseResponse.Data = dto;
            return Ok(_baseResponse);
        }

        /// <summary>
        /// تتبع المندوب - يرجع كل نقاط تحرك المندوب من وقت استلام الشحنة لحد دلوقتي
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. التتبع متاح فقط لو:
        /// 1. الشحنة معينة لمندوب
        /// 2. المندوب مفعّل عليه التتبع من الأدمن
        /// 3. الشحنة في حالة (Assigned أو Waiting) - يعني لسه مع المندوب
        ///
        /// بيرجع كل الإحداثيات من وقت ما المندوب استلم الشحنة (Assign_To_DriverDate) لحد الوقت الحالي،
        /// بالإضافة لموقع المندوب الحالي.
        /// </remarks>
        /// <param name="code">كود الشحنة (مثال: Tas123)</param>
        [HttpGet("Orders/{code}/TrackDriver")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> TrackDriver([FromRoute] string code)
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            if (string.IsNullOrWhiteSpace(code))
            {
                _baseResponse.ErrorCode = Errors.TheModelIsInvalid;
                _baseResponse.ErrorMessage = "كود الشحنة مطلوب";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            var order = await _orders.GetSingle(
                x => x.Code == code && x.ClientId == user.Id && !x.IsDeleted,
                includeProperties: "Delivery,OrderOperationHistory");

            if (order == null)
            {
                _baseResponse.ErrorCode = Errors.TheOrderNotExistOrDeleted;
                _baseResponse.ErrorMessage = "الشحنة غير موجودة";
                return StatusCode((int)HttpStatusCode.NotFound, _baseResponse);
            }

            var trackableStatuses = new[] { OrderStatus.Assigned, OrderStatus.Waiting };

            if (order.Delivery == null || !trackableStatuses.Contains(order.Status))
            {
                _baseResponse.ErrorCode = Errors.TheOrderNotExistOrDeleted;
                _baseResponse.ErrorMessage = "التتبع غير متاح - الشحنة ليست مع المندوب حالياً";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            if (!order.Delivery.Tracking)
            {
                _baseResponse.ErrorCode = Errors.TheOrderNotExistOrDeleted;
                _baseResponse.ErrorMessage = "التتبع غير مفعّل لهذا المندوب";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            // جلب تاريخ تعيين المندوب على الشحنة
            var assignedDate = order.OrderOperationHistory?.Assign_To_DriverDate ?? order.CreateOn;

            // جلب كل نقاط التتبع من وقت الاستلام لحد دلوقتي
            var locations = _locations.Get(x =>
                x.DeliveryId == order.DeliveryId
                && x.CreateOn >= assignedDate
                && !x.IsDeleted)
                .OrderByDescending(x => x.CreateOn)
                .Select(x => new LocationDto(x))
                .ToList();

            _baseResponse.Data = new
            {
                DriverName = order.Delivery.Name,
                CurrentLatitude = order.Delivery.Latitude,
                CurrentLongitude = order.Delivery.Longitude,
                OrderCode = order.Code,
                OrderStatus = order.Status.ToString(),
                AssignedDate = assignedDate,
                Locations = locations
            };
            return Ok(_baseResponse);
        }

        /// <summary>
        /// تعديل شحنة - تعديل بيانات الشحنة (متاح فقط قبل موافقة الأدمن - الحالة: Placed و Pending)
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. التعديل متاح فقط لو الشحنة لسه في حالة Placed و Pending=true (قبل ما الأدمن يوافق).
        /// كل الحقول اختيارية - ابعت الحقول اللي عايز تعدلها بس.
        /// الحقول القابلة للتعديل: ClientName, ClientPhone, ClientCode, Address, ClientCity, Notes, Cost, DeliveryFees.
        /// </remarks>
        /// <param name="code">كود الشحنة (مثال: Tas123)</param>
        /// <param name="dto">البيانات المراد تعديلها (كل الحقول اختيارية)</param>
        [HttpPut("Orders/{code}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> EditOrder([FromRoute] string code, [FromBody] SenderAppEditOrderDto dto)
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            var order = await _orders.GetSingle(
                x => x.Code == code && x.ClientId == user.Id && !x.IsDeleted,
                includeProperties: "OrderOperationHistory");

            if (order == null)
            {
                _baseResponse.ErrorCode = Errors.TheOrderNotExistOrDeleted;
                _baseResponse.ErrorMessage = "الشحنة غير موجودة";
                return StatusCode((int)HttpStatusCode.NotFound, _baseResponse);
            }

            if (order.Status != OrderStatus.Placed || !order.Pending)
            {
                _baseResponse.ErrorCode = Errors.OrderCannotBeEdited;
                _baseResponse.ErrorMessage = "لا يمكن تعديل الشحنة بعد موافقة الأدمن";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            if (!string.IsNullOrEmpty(dto.ClientName))
                order.ClientName = dto.ClientName;
            if (!string.IsNullOrEmpty(dto.ClientPhone))
                order.ClientPhone = dto.ClientPhone;
            if (!string.IsNullOrEmpty(dto.ClientSecondaryPhone))
                order.ClientSecondaryPhone = dto.ClientSecondaryPhone;
            if (!string.IsNullOrEmpty(dto.ClientCode))
                order.ClientCode = dto.ClientCode;
            if (!string.IsNullOrEmpty(dto.Address))
                order.Address = dto.Address;
            if (!string.IsNullOrEmpty(dto.ClientCity))
                order.AddressCity = dto.ClientCity;
            if (!string.IsNullOrEmpty(dto.Notes))
                order.Notes = dto.Notes;
            if (dto.Cost.HasValue)
                order.Cost = dto.Cost.Value;
            if (dto.DeliveryFees.HasValue)
                order.DeliveryFees = dto.DeliveryFees.Value;

            order.TotalCost = order.Cost + order.DeliveryFees;
            order.LastUpdated = DateTime.UtcNow;
            order.IsModified = true;
            order.ModifiedOn = DateTime.UtcNow;

            if (order.OrderOperationHistory != null)
            {
                order.OrderOperationHistory.Edit_UserId = user.Id;
                order.OrderOperationHistory.EditDate = DateTime.UtcNow;
                await _histories.Update(order.OrderOperationHistory);
            }

            await _orders.Update(order);

            var orderDto = new SenderOrderDto(order, null, null);
            _baseResponse.Data = orderDto;
            return Ok(_baseResponse);
        }

        /// <summary>
        /// إلغاء شحنة - حذف الشحنة نهائياً (متاح فقط قبل موافقة الأدمن - الحالة: Placed و Pending)
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. الإلغاء متاح فقط لو الشحنة لسه في حالة Placed و Pending=true.
        /// بعد موافقة الأدمن لا يمكن إلغاء الشحنة من التطبيق.
        /// </remarks>
        /// <param name="code">كود الشحنة المراد إلغائها (مثال: Tas123)</param>
        [HttpDelete("Orders/{code}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> CancelOrder([FromRoute] string code)
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            var order = await _orders.GetSingle(
                x => x.Code == code && x.ClientId == user.Id && !x.IsDeleted,
                includeProperties: "OrderOperationHistory");

            if (order == null)
            {
                _baseResponse.ErrorCode = Errors.TheOrderNotExistOrDeleted;
                _baseResponse.ErrorMessage = "الشحنة غير موجودة";
                return StatusCode((int)HttpStatusCode.NotFound, _baseResponse);
            }

            if (order.Status != OrderStatus.Placed || !order.Pending)
            {
                _baseResponse.ErrorCode = Errors.OrderCannotBeCancelled;
                _baseResponse.ErrorMessage = "لا يمكن إلغاء الشحنة بعد موافقة الأدمن";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            order.IsDeleted = true;
            order.DeletedOn = DateTime.UtcNow;

            if (order.OrderOperationHistory != null)
            {
                order.OrderOperationHistory.Delete_UserId = user.Id;
                order.OrderOperationHistory.DeleteDate = DateTime.UtcNow;
                await _histories.Update(order.OrderOperationHistory);
            }

            await _orders.Update(order);

            _baseResponse.Data = "تم إلغاء الشحنة بنجاح";
            return Ok(_baseResponse);
        }

        /// <summary>
        /// بيانات البوليصة - يرجع كل بيانات الشحنة اللازمة لطباعة البوليصة مع الباركود وبيانات الراسل والفرع
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. يرجع: كود الشحنة، بيانات المستلم، التكلفة، الباركود (Base64)،
        /// بيانات الراسل (مع مراعاة إعدادات الإخفاء HideSenderName/Phone/Code)،
        /// بيانات الفرع، واسم المندوب.
        /// </remarks>
        /// <param name="code">كود الشحنة (مثال: Tas123)</param>
        [HttpGet("Orders/{code}/Receipt")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetOrderReceipt([FromRoute] string code)
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            var order = await _orders.GetSingle(
                x => x.Code == code && x.ClientId == user.Id && !x.IsDeleted,
                includeProperties: "Delivery,Branch");

            if (order == null)
            {
                _baseResponse.ErrorCode = Errors.TheOrderNotExistOrDeleted;
                _baseResponse.ErrorMessage = "الشحنة غير موجودة";
                return StatusCode((int)HttpStatusCode.NotFound, _baseResponse);
            }

            _baseResponse.Data = new
            {
                OrderCode = order.Code,
                ReceiverName = order.ClientName,
                ReceiverPhone = order.ClientPhone,
                ReceiverCode = order.ClientCode,
                City = order.AddressCity,
                Address = order.Address,
                Cost = order.Cost,
                DeliveryFees = order.DeliveryFees,
                TotalCost = order.TotalCost,
                Notes = order.Notes,
                Status = order.Status,
                StatusArabic = GetStatusInArabic(order.Status),
                CreatedOn = order.CreateOn,
                BarcodeImageBase64 = order.BarcodeImage != null ? Convert.ToBase64String(order.BarcodeImage) : null,
                SenderName = user.HideSenderName ? null : user.Name,
                SenderPhone = user.HideSenderPhone ? null : user.PhoneNumber,
                SenderCode = user.HideSenderCode ? null : user.Id,
                BranchName = order.Branch?.Name,
                BranchAddress = order.Branch?.Address,
                BranchPhone = order.Branch?.PhoneNumber,
                DeliveryAgentName = order.Delivery?.Name,
                DeliveryAgentPhone = order.Delivery?.PhoneNumber,
            };

            return Ok(_baseResponse);
        }

        #endregion

        #region Financial (JWT Required)

        /// <summary>
        /// رصيد المحفظة - يرجع الرصيد الحالي لمحفظة الراسل
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. يرجع كائن فيه Balance (رصيد المحفظة الحالي).
        /// </remarks>
        [HttpGet("Wallet")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetWalletBalance()
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            _baseResponse.Data = new { Balance = user.Wallet };
            return Ok(_baseResponse);
        }

        /// <summary>
        /// ملخص مالي - يرجع إحصائيات مالية شاملة (رصيد المحفظة، إجمالي التكاليف، عدد الشحنات حسب الحالة)
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. يرجع: WalletBalance, TotalOrdersCost, TotalDeliveryFees, TotalCollected,
        /// TotalOrders, DeliveredOrders, ReturnedOrders, PendingOrders.
        /// يمكن تصفية الإحصائيات حسب تاريخ البداية والنهاية.
        /// </remarks>
        /// <param name="from">تاريخ البداية للتصفية (اختياري)</param>
        /// <param name="to">تاريخ النهاية للتصفية (اختياري)</param>
        [HttpGet("Wallet/Summary")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetFinancialSummary(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            var query = _orders.GetAllAsIQueryable(
                filter: x => x.ClientId == user.Id && !x.IsDeleted);

            if (from.HasValue)
                query = query.Where(x => x.CreateOn >= from.Value);
            if (to.HasValue)
                query = query.Where(x => x.CreateOn <= to.Value.AddDays(1));

            var orders = query.ToList();

            var deliveredStatuses = new[]
            {
                OrderStatus.Delivered, OrderStatus.Finished, OrderStatus.Completed,
                OrderStatus.PartialDelivered, OrderStatus.Delivered_With_Edit_Price
            };
            var returnedStatuses = new[]
            {
                OrderStatus.Returned, OrderStatus.PartialReturned,
                OrderStatus.Returned_And_Paid_DeliveryCost, OrderStatus.Returned_And_DeliveryCost_On_Sender
            };
            var pendingStatuses = new[] { OrderStatus.Placed, OrderStatus.Assigned, OrderStatus.Waiting };

            var summary = new SenderFinancialSummaryDto
            {
                WalletBalance = user.Wallet,
                TotalOrdersCost = orders.Sum(o => o.Cost),
                TotalDeliveryFees = orders.Sum(o => o.DeliveryFees),
                TotalCollected = orders.Where(o => deliveredStatuses.Contains(o.Status)).Sum(o => o.ArrivedCost),
                TotalOrders = orders.Count,
                DeliveredOrders = orders.Count(o => deliveredStatuses.Contains(o.Status)),
                ReturnedOrders = orders.Count(o => returnedStatuses.Contains(o.Status)),
                PendingOrders = orders.Count(o => pendingStatuses.Contains(o.Status)),
            };

            _baseResponse.Data = summary;
            return Ok(_baseResponse);
        }

        /// <summary>
        /// سجل الحركات المالية - يرجع كل حركات المحفظة (إيداع، سحب، تسوية...) مع تقسيم لصفحات
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. كل حركة فيها: نوع العملية، المبلغ، رصيد المحفظة بعد العملية، ملاحظات، رقم الطلب، التاريخ.
        /// مرتبة من الأحدث للأقدم. الحد الأقصى لحجم الصفحة: 50.
        /// </remarks>
        /// <param name="pageNumber">رقم الصفحة (افتراضي: 1)</param>
        /// <param name="pageSize">حجم الصفحة (افتراضي: 20، الحد الأقصى: 50)</param>
        [HttpGet("Wallet/Transactions")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetWalletTransactions(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 50) pageSize = 50;

            var query = _wallets.GetAllAsIQueryable(
                filter: x => (x.ActualUserId == user.Id || x.UserId == user.Id) && !x.IsDeleted,
                orderby: q => q.OrderByDescending(w => w.CreateOn));

            var totalCount = query.Count();

            var transactions = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var dtos = transactions.Select(t => new SenderWalletTransactionDto
            {
                Id = t.Id,
                TransactionType = t.TransactionType,
                Amount = t.Amount,
                WalletBalanceAfter = t.UserWalletLast,
                Note = t.Note,
                OrderNumber = t.OrderNumber,
                Date = t.CreateOn,
            }).ToList();

            var response = new PaginatedResponse<SenderWalletTransactionDto>(dtos, pageNumber, pageSize, totalCount);

            _baseResponse.Data = response;
            return Ok(_baseResponse);
        }

        #endregion

        #region Settlements (JWT Required)

        /// <summary>
        /// قائمة تسويات الطلبات - يرجع كل التسويات اللي تمت على طلبات الراسل المكتملة مع تقسيم لصفحات
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. كل تسوية فيها: المبلغ، التاريخ، عدد الطلبات المشمولة، ملاحظات.
        /// التسوية = سجل محفظة من نوع OrderComplete مرتبط بطلبات مكتملة.
        /// </remarks>
        /// <param name="pageNumber">رقم الصفحة (افتراضي: 1)</param>
        /// <param name="pageSize">حجم الصفحة (افتراضي: 20، الحد الأقصى: 50)</param>
        [HttpGet("Settlements/Orders")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetOrderSettlements(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 50) pageSize = 50;

            var query = _wallets.GetAllAsIQueryable(
                filter: x => x.ActualUserId == user.Id && !x.IsDeleted && x.TransactionType == TransactionType.OrderComplete,
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

            _baseResponse.Data = response;
            return Ok(_baseResponse);
        }

        /// <summary>
        /// تفاصيل تسوية طلبات - يرجع بيانات التسوية مع قائمة الشحنات المشمولة فيها (الكود، اسم المستلم، التكاليف، الحالة)
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. يرجع بيانات التسوية + قائمة الطلبات المرتبطة بيها.
        /// كل طلب فيه: Code, ClientName, ClientPhone, ArrivedCost, DeliveryCost, ClientCost, Cost, TotalCost, Status.
        /// </remarks>
        /// <param name="walletId">معرف التسوية (Wallet Id)</param>
        [HttpGet("Settlements/Orders/{walletId}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetOrderSettlementDetails([FromRoute] long walletId)
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            var settlement = await _wallets.GetSingle(
                x => x.Id == walletId && x.ActualUserId == user.Id && !x.IsDeleted && x.TransactionType == TransactionType.OrderComplete);

            if (settlement == null)
            {
                _baseResponse.ErrorCode = Errors.TheOrderNotExistOrDeleted;
                _baseResponse.ErrorMessage = "التسوية غير موجودة";
                return StatusCode((int)HttpStatusCode.NotFound, _baseResponse);
            }

            var orders = _orders.Get(x => x.CompletedId == walletId && x.ClientId == user.Id).ToList();

            var orderDtos = orders.Select(o => new SenderSettlementOrderDto
            {
                Code = o.Code,
                ClientName = o.ClientName,
                ClientPhone = o.ClientPhone,
                ClientSecondaryPhone = o.ClientSecondaryPhone,
                ArrivedCost = o.ArrivedCost,
                DeliveryCost = o.DeliveryCost,
                ClientCost = o.ClientCost,
                Cost = o.Cost,
                TotalCost = o.TotalCost,
                Status = o.Status,
                StatusArabic = GetStatusInArabic(o.Status),
            }).ToList();

            _baseResponse.Data = new
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
                Orders = orderDtos
            };

            return Ok(_baseResponse);
        }

        /// <summary>
        /// قائمة تسويات المرتجعات - يرجع كل التسويات اللي تمت على الطلبات المرتجعة مع تقسيم لصفحات
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. كل تسوية فيها: المبلغ، التاريخ، عدد الطلبات المرتجعة المشمولة، ملاحظات.
        /// التسوية = سجل محفظة من نوع OrderReturnedComplete مرتبط بطلبات مرتجعة.
        /// </remarks>
        /// <param name="pageNumber">رقم الصفحة (افتراضي: 1)</param>
        /// <param name="pageSize">حجم الصفحة (افتراضي: 20، الحد الأقصى: 50)</param>
        [HttpGet("Settlements/Returns")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetReturnSettlements(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 50) pageSize = 50;

            var query = _wallets.GetAllAsIQueryable(
                filter: x => x.ActualUserId == user.Id && !x.IsDeleted && x.TransactionType == TransactionType.OrderReturnedComplete,
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

            _baseResponse.Data = response;
            return Ok(_baseResponse);
        }

        /// <summary>
        /// تفاصيل تسوية مرتجعات - يرجع بيانات التسوية مع قائمة الشحنات المرتجعة المشمولة فيها
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. يرجع بيانات التسوية + قائمة الطلبات المرتجعة المرتبطة بيها.
        /// كل طلب فيه: Code, ClientName, ClientPhone, ArrivedCost, DeliveryCost, ClientCost, Cost, TotalCost, Status.
        /// </remarks>
        /// <param name="walletId">معرف التسوية (Wallet Id)</param>
        [HttpGet("Settlements/Returns/{walletId}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetReturnSettlementDetails([FromRoute] long walletId)
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            var settlement = await _wallets.GetSingle(
                x => x.Id == walletId && x.ActualUserId == user.Id && !x.IsDeleted && x.TransactionType == TransactionType.OrderReturnedComplete);

            if (settlement == null)
            {
                _baseResponse.ErrorCode = Errors.TheOrderNotExistOrDeleted;
                _baseResponse.ErrorMessage = "التسوية غير موجودة";
                return StatusCode((int)HttpStatusCode.NotFound, _baseResponse);
            }

            // Get orders linked to this return settlement
            var orders = _orders.Get(x =>
                x.ClientId == user.Id &&
                (x.ReturnedCompletedId == walletId || x.CompletedId == walletId)
            ).ToList();

            var orderDtos = orders.Select(o => new SenderSettlementOrderDto
            {
                Code = o.Code,
                ClientName = o.ClientName,
                ClientPhone = o.ClientPhone,
                ClientSecondaryPhone = o.ClientSecondaryPhone,
                ArrivedCost = o.ArrivedCost,
                DeliveryCost = o.DeliveryCost,
                ClientCost = o.ClientCost,
                Cost = o.Cost,
                TotalCost = o.TotalCost,
                Status = o.Status,
                StatusArabic = GetStatusInArabic(o.Status),
            }).ToList();

            _baseResponse.Data = new
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
                Orders = orderDtos
            };

            return Ok(_baseResponse);
        }

        #endregion

        #region Notifications (JWT Required)

        /// <summary>
        /// تسجيل رمز الإشعارات - حفظ Firebase Push Token لاستقبال الإشعارات على الجهاز
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. يُستدعى عند فتح التطبيق أو عند تحديث الـ Push Token.
        /// لو التوكن موجود قبل كده بيتم حذفه وتسجيله من جديد للمستخدم الحالي.
        /// </remarks>
        /// <param name="pushToken">رمز Firebase Push Token</param>
        [HttpPost("Notifications/PushToken")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> PushToken([FromQuery] string pushToken)
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            if (string.IsNullOrEmpty(pushToken))
            {
                _baseResponse.ErrorCode = Errors.TheModelIsInvalid;
                _baseResponse.ErrorMessage = "رمز الإشعارات مطلوب";
                return StatusCode((int)HttpStatusCode.BadRequest, _baseResponse);
            }

            if (await _deviceTokens.IsExist(x => x.Token == pushToken && !x.IsDeleted))
            {
                var existingToken = _deviceTokens.Get(x => x.Token == pushToken && !x.IsDeleted).First();
                existingToken.IsDeleted = true;
                await _deviceTokens.Update(existingToken);
            }

            await _deviceTokens.Add(new DeviceTokens { Token = pushToken, UserId = user.Id });

            _baseResponse.Data = "تم تسجيل رمز الإشعارات بنجاح";
            return Ok(_baseResponse);
        }

        /// <summary>
        /// قائمة الإشعارات - يرجع كل إشعارات الراسل مع تقسيم لصفحات ويحدد الإشعارات كمقروءة تلقائياً
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. كل إشعار فيه: Id, Title, Body, IsSeen, Date.
        /// الإشعارات اللي بتترجع بيتم تحديثها تلقائياً كمقروءة (IsSeen=true).
        /// مرتبة من الأحدث للأقدم. الحد الأقصى لحجم الصفحة: 50.
        /// </remarks>
        /// <param name="pageNumber">رقم الصفحة (افتراضي: 1)</param>
        /// <param name="pageSize">حجم الصفحة (افتراضي: 20، الحد الأقصى: 50)</param>
        [HttpGet("Notifications")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 50) pageSize = 50;

            var query = _notifications.GetAllAsIQueryable(
                filter: x => x.UserId == user.Id && !x.IsDeleted,
                orderby: q => q.OrderByDescending(n => n.Id));

            var totalCount = query.Count();

            var notifications = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var dtos = notifications.Select(n => new
            {
                n.Id,
                n.Title,
                n.Body,
                n.IsSeen,
                Date = n.CreateOn,
            }).ToList();

            // Mark as seen
            foreach (var notification in notifications.Where(n => !n.IsSeen))
            {
                notification.IsSeen = true;
                await _notifications.Update(notification);
            }

            var response = new PaginatedResponse<object>(dtos.Cast<object>(), pageNumber, pageSize, totalCount);

            _baseResponse.Data = response;
            return Ok(_baseResponse);
        }

        /// <summary>
        /// عدد الإشعارات غير المقروءة - يرجع عدد الإشعارات اللي لسه ما اتفتحتش (للبادج في التطبيق)
        /// </summary>
        /// <remarks>
        /// يحتاج JWT Token. يرجع كائن فيه Count (عدد الإشعارات الغير مقروءة).
        /// يُستخدم لعرض رقم البادج على أيقونة الإشعارات في التطبيق.
        /// </remarks>
        [HttpGet("Notifications/UnseenCount")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> UnseenNotificationCount()
        {
            var (user, errorResult) = await GetCurrentSenderAsync();
            if (errorResult != null) return errorResult;

            var count = _notifications.Get(x => !x.IsDeleted && x.UserId == user.Id && !x.IsSeen).Count();

            _baseResponse.Data = new { Count = count };
            return Ok(_baseResponse);
        }

        #endregion

        #region Support (AllowAnonymous)

        /// <summary>
        /// بيانات التواصل - يرجع لينكات السوشيال ميديا وبيانات كل الفروع (العنوان، الهاتف، الواتساب، الإحداثيات)
        /// </summary>
        /// <remarks>
        /// لا يحتاج تسجيل دخول. يرجع: FaceBook, Twitter, Instgram + قائمة الفروع ببياناتها الكاملة.
        /// يُستخدم في شاشة "تواصل معنا" في التطبيق.
        /// </remarks>
        [HttpGet("ContactUs")]
        [AllowAnonymous]
        public async Task<IActionResult> GetContactUs()
        {
            var contactUs = await _contactUs.GetObj(x => !x.IsDeleted);
            var branches = _branches.Get(x => !x.IsDeleted).ToList();

            _baseResponse.Data = new
            {
                FaceBook = contactUs?.FaceBook,
                Twitter = contactUs?.Twitter,
                Instgram = contactUs?.Instgram,
                Branches = branches.Select(b => new
                {
                    b.Id,
                    b.Name,
                    b.Address,
                    b.PhoneNumber,
                    b.Whatsapp,
                    b.Latitude,
                    b.Longitude
                })
            };

            return Ok(_baseResponse);
        }

        #endregion
    }
}
