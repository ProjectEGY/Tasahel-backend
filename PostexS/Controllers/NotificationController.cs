using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PagedList.Core;
using PostexS.Helper;
using PostexS.Interfaces;
using PostexS.Models;
using PostexS.Models.Domain;
using PostexS.Models.Enums;
using PostexS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Controllers
{
    [Authorize(Roles = "Admin,HighAdmin,Accountant,Client,TrustAdmin")]
    public class NotificationController : Controller
    {
        private readonly IGeneric<Notification> _notification;
        private IGeneric<DeviceTokens> _pushNotification;
        private readonly IGeneric<ApplicationUser> _user;
        private readonly IGeneric<Branch> _branch;
        private readonly UserManager<ApplicationUser> _userManger;
        private readonly FirebaseMessagingService _firebaseService;
        private readonly IWebHostEnvironment _hostEnvironment;

        public NotificationController(IGeneric<Notification> notification, UserManager<ApplicationUser> userManger, IGeneric<ApplicationUser> user,
            IGeneric<DeviceTokens> pushNotification, FirebaseMessagingService firebaseService, IGeneric<Branch> branch,
            IWebHostEnvironment hostEnvironment)
        {
            _notification = notification;
            _pushNotification = pushNotification;
            _user = user;
            _userManger = userManger;
            _firebaseService = firebaseService;
            _branch = branch;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManger.GetUserAsync(User);
            var isHighAdmin = User.IsInRole("HighAdmin");

            if (isHighAdmin)
            {
                // HighAdmin يشوف فروعه بس
                var branchName = _branch.Get(x => x.Id == currentUser.BranchId && !x.IsDeleted).FirstOrDefault()?.Name;
                ViewBag.Branches = new SelectList(new[] { new { Id = currentUser.BranchId, Name = branchName } }, "Id", "Name", currentUser.BranchId);
                ViewBag.IsHighAdmin = true;
                ViewBag.CurrentBranchId = currentUser.BranchId;
            }
            else
            {
                var branches = _branch.Get(x => !x.IsDeleted).ToList();
                ViewBag.Branches = new SelectList(branches, "Id", "Name");
                ViewBag.IsHighAdmin = false;
                ViewBag.CurrentBranchId = 0;
            }

            return View();
        }

        /// <summary>
        /// AJAX endpoint للبحث عن المستخدمين بالاسم أو رقم الهاتف مع فلتر الفرع والنوع
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string term, long branchId, string userType)
        {
            try
            {
                var currentUser = await _userManger.GetUserAsync(User);
                var isHighAdmin = User.IsInRole("HighAdmin");

                UserType type = userType == "driver" ? UserType.Driver : UserType.Client;

                // استخدام UserManager.Users مباشرة بدل الـ Generic Repository
                var query = _userManger.Users
                    .Where(x => !x.IsDeleted && x.UserType == type);

                // HighAdmin يشوف فرعه بس
                if (isHighAdmin)
                {
                    query = query.Where(x => x.BranchId == currentUser.BranchId);
                }
                else if (branchId > 0)
                {
                    query = query.Where(x => x.BranchId == branchId);
                }

                // بحث بالاسم أو رقم الهاتف
                if (!string.IsNullOrEmpty(term))
                {
                    term = term.Trim();
                    query = query.Where(x =>
                        (x.Name != null && x.Name.Contains(term)) ||
                        (x.PhoneNumber != null && x.PhoneNumber.Contains(term)));
                }

                var users = query
                    .Select(x => new { id = x.Id, text = (x.Name ?? "") + " - " + (x.PhoneNumber ?? "") })
                    .Take(50)
                    .ToList();

                return Json(new { results = users });
            }
            catch (Exception ex)
            {
                return Json(new { results = new object[0], error = ex.Message });
            }
        }

        /// <summary>
        /// اختبار اتصال Firebase - يتحقق من صلاحية الـ credentials
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> TestFirebase()
        {
            var results = new Dictionary<string, object>();

            // اختبار Captain (Driver) Firebase
            try
            {
                // نبعت رسالة وهمية لتوكن مش موجود - لو رجع Unregistered يبقى الاتصال شغال
                var testMessage = new FirebaseAdmin.Messaging.Message()
                {
                    Token = "test_invalid_token_for_connectivity_check",
                    Notification = new FirebaseAdmin.Messaging.Notification()
                    {
                        Title = "Test",
                        Body = "Test"
                    }
                };
                await _firebaseService.CaptainMessaging.SendAsync(testMessage);
                results["captain"] = new { status = "OK", message = "متصل بنجاح" };
            }
            catch (FirebaseAdmin.Messaging.FirebaseMessagingException fex)
            {
                // لو رجع خطأ Firebase عادي (مثل UNREGISTERED أو INVALID_ARGUMENT) يبقى الاتصال شغال
                results["captain"] = new { status = "OK", message = $"الاتصال شغال - {fex.MessagingErrorCode}" };
            }
            catch (Exception ex)
            {
                // لو رجع خطأ credentials يبقى المفتاح باظ
                results["captain"] = new { status = "FAILED", message = $"❌ مفتاح المندوب باظ: {ex.Message}" };
            }

            // اختبار Customer (Sender) Firebase
            try
            {
                var testMessage = new FirebaseAdmin.Messaging.Message()
                {
                    Token = "test_invalid_token_for_connectivity_check",
                    Notification = new FirebaseAdmin.Messaging.Notification()
                    {
                        Title = "Test",
                        Body = "Test"
                    }
                };
                await _firebaseService.CustomerMessaging.SendAsync(testMessage);
                results["customer"] = new { status = "OK", message = "متصل بنجاح" };
            }
            catch (FirebaseAdmin.Messaging.FirebaseMessagingException fex)
            {
                results["customer"] = new { status = "OK", message = $"الاتصال شغال - {fex.MessagingErrorCode}" };
            }
            catch (Exception ex)
            {
                results["customer"] = new { status = "FAILED", message = $"❌ مفتاح الراسل باظ: {ex.Message}" };
            }

            return Json(results);
        }

        [HttpPost]
        public async Task<ActionResult> Create(List<string> Users, string Body, string Title, string targetType, IFormFile Image)
        {
            if (string.IsNullOrEmpty(Title) || string.IsNullOrEmpty(Body))
            {
                return Json(new { success = false, message = "العنوان والمحتوى مطلوبين" });
            }

            try
            {
                // رفع الصورة لو موجودة
                string imageUrl = null;
                if (Image != null && Image.Length > 0)
                {
                    var fileName = await MediaControl.Upload(Models.Enums.FilePath.Notifications, Image, _hostEnvironment);
                    imageUrl = $"{Request.Scheme}://{Request.Host}/Images/Notifications/{fileName}";
                }

                // تحديد الـ Firebase instance حسب نوع المستخدم
                var firebaseInstance = targetType == "driver"
                    ? _firebaseService.CaptainMessaging
                    : _firebaseService.CustomerMessaging;

                var send = new SendNotification(_pushNotification, _notification, firebaseInstance);
                int sentCount = 0;

                if (Users == null || Users.Count == 0)
                {
                    // إرسال للكل من نفس النوع
                    var currentUser = await _userManger.GetUserAsync(User);
                    var isHighAdmin = User.IsInRole("HighAdmin");
                    UserType type = targetType == "driver" ? UserType.Driver : UserType.Client;

                    var allUsers = _userManger.Users.Where(x => !x.IsDeleted && x.UserType == type);
                    if (isHighAdmin)
                    {
                        allUsers = allUsers.Where(x => x.BranchId == currentUser.BranchId);
                    }

                    foreach (var user in allUsers.ToList())
                    {
                        await send.SendToAllSpecificAndroidUserDevices(user.Id, Title, Body, Image: imageUrl, notificationType: "admin");
                        sentCount++;
                    }
                }
                else
                {
                    foreach (var item in Users)
                    {
                        await send.SendToAllSpecificAndroidUserDevices(item, Title, Body, Image: imageUrl, notificationType: "admin");
                        sentCount++;
                    }
                }

                var targetName = targetType == "driver" ? "مندوب" : "راسل";
                return Json(new { success = true, message = $"تم إرسال الإشعار بنجاح لعدد {sentCount} {targetName}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء الإرسال: " + ex.Message });
            }
        }

        public ActionResult All(int? page)
        {
            var user = _userManger.GetUserId(User);
            int pageSize = 40;
            int pageNumber = (page ?? 1);

            var totalCount = _notification.GetCount(x => x.UserId == user);
            int pageCount = (int)Math.Ceiling((double)totalCount / pageSize);

            var notifications = _notification
                .Get(x => x.UserId == user)
                .OrderByDescending(d => d.CreateOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var model = new NotificationPagedViewModel
            {
                Notifications = notifications,
                PageNumber = pageNumber,
                PageCount = pageCount
            };

            return View(model);
        }

        private async Task<ActionResult> MarkNotificationAsSeen(long Id)
        {
            var notify = await _notification.GetObj(x => x.Id == Id);
            if (notify != null)
            {
                notify.IsSeen = true;
                await _notification.Update(notify);
            }
            return RedirectToAction("ShowNotification", new { id = Id });
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> MarkAllNotificationAsSeenAsync(List<long> ids, string ReturnUrl)
        {
            foreach (var id in ids)
            {
                var notify = await _notification.GetObj(x => x.Id == id);
                if (notify != null)
                {
                    notify.IsSeen = true;
                    await _notification.Update(notify);
                }
            }
            return Redirect(ReturnUrl);
        }

        public async Task<ActionResult> ShowNotification(int id)
        {
            var notify = await _notification.GetObj(x => x.Id == id);
            await MarkNotificationAsSeen(id);
            return View(notify);
        }
    }
}
