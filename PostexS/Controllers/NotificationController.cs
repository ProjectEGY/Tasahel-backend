using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PagedList.Core;
using PostexS.Helper;
using PostexS.Interfaces;
using PostexS.Migrations;
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
        private readonly UserManager<ApplicationUser> _userManger;
        public NotificationController(IGeneric<Notification> notification, UserManager<ApplicationUser> userManger, IGeneric<ApplicationUser> user,
            IGeneric<DeviceTokens> pushNotification)
        {
            _notification = notification;
            _pushNotification = pushNotification;
            _user = user;
            _userManger = userManger;
        }
        public IActionResult Index()
        {
            var Clients = _user.Get(x => !x.IsDeleted && x.UserType == UserType.Client).ToList();
            ViewBag.Clients = new SelectList(Clients, "Id", "Name");
            var Driver = _user.Get(x => !x.IsDeleted && x.UserType == UserType.Driver).ToList();
            ViewBag.Driver = new SelectList(Driver, "Id", "Name");
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> Create(List<string> Users, string Body, string Title)
        {
            if (string.IsNullOrEmpty(Title) || string.IsNullOrEmpty(Body))
            {
                TempData["SentError"] = false;
                return RedirectToAction(nameof(Index));
            }
            var send = new SendNotification(_pushNotification, _notification);
            if (Users.Count == 0)
            {
                await send.SendToAllSpecificAndroidUserDevices("", Title, Body, true);
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

            // Get total count of notifications for the user
            var totalCount = _notification
                .GetCount(x => x.UserId == user);

            // Calculate total pages
            int pageCount = (int)Math.Ceiling((double)totalCount / pageSize);

            // Get the notifications for the current page
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
            // return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
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
            // return Json(new { Success = true }, JsonRequestBehavior.AllowGet);
        }
        public async Task<ActionResult> ShowNotification(int id)
        {
            var notify = await _notification.GetObj(x => x.Id == id);
            await MarkNotificationAsSeen(id);
            return View(notify);
        }
    }
}
