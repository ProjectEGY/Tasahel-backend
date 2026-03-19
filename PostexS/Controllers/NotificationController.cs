using Microsoft.AspNetCore.Authorization;
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

        public NotificationController(IGeneric<Notification> notification, UserManager<ApplicationUser> userManger, IGeneric<ApplicationUser> user,
            IGeneric<DeviceTokens> pushNotification, FirebaseMessagingService firebaseService, IGeneric<Branch> branch)
        {
            _notification = notification;
            _pushNotification = pushNotification;
            _user = user;
            _userManger = userManger;
            _firebaseService = firebaseService;
            _branch = branch;
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
            var currentUser = await _userManger.GetUserAsync(User);
            var isHighAdmin = User.IsInRole("HighAdmin");

            UserType type = userType == "driver" ? UserType.Driver : UserType.Client;

            var query = _user.Get(x => !x.IsDeleted && x.UserType == type);

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
                query = query.Where(x => x.Name.Contains(term) || x.PhoneNumber.Contains(term));
            }

            var users = query.Select(x => new { id = x.Id, text = x.Name + " - " + x.PhoneNumber }).Take(50).ToList();
            return Json(new { results = users });
        }

        [HttpPost]
        public async Task<ActionResult> Create(List<string> Users, string Body, string Title, string targetType)
        {
            if (string.IsNullOrEmpty(Title) || string.IsNullOrEmpty(Body))
            {
                TempData["SentError"] = true;
                return RedirectToAction(nameof(Index));
            }

            // تحديد الـ Firebase instance حسب نوع المستخدم
            var firebaseInstance = targetType == "driver"
                ? _firebaseService.CaptainMessaging
                : _firebaseService.CustomerMessaging;

            var send = new SendNotification(_pushNotification, _notification, firebaseInstance);

            if (Users == null || Users.Count == 0)
            {
                // إرسال للكل من نفس النوع
                var currentUser = await _userManger.GetUserAsync(User);
                var isHighAdmin = User.IsInRole("HighAdmin");
                UserType type = targetType == "driver" ? UserType.Driver : UserType.Client;

                var allUsers = _user.Get(x => !x.IsDeleted && x.UserType == type);
                if (isHighAdmin)
                {
                    allUsers = allUsers.Where(x => x.BranchId == currentUser.BranchId);
                }

                foreach (var user in allUsers.ToList())
                {
                    await send.SendToAllSpecificAndroidUserDevices(user.Id, Title, Body);
                }
            }
            else
            {
                foreach (var item in Users)
                {
                    await send.SendToAllSpecificAndroidUserDevices(item, Title, Body);
                }
            }

            TempData["SentSuccess"] = true;
            return RedirectToAction(nameof(Index));
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
