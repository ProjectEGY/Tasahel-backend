using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using PostexS.Models;
using PostexS.Models.ViewModels;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Nancy.Extensions;

namespace PostexS.Controllers
{
    public class PartialsController : Controller
    {
        private readonly IGeneric<Notification> _notifiaction; private readonly UserManager<ApplicationUser> _userManger;

        public PartialsController(IGeneric<Notification> notifiaction, UserManager<ApplicationUser> userManger)
        {
            _notifiaction = notifiaction;
            _userManger = userManger;
        }
        public ActionResult Header()
        {
            string name = "الأدمن";
            if (User.IsInRole("Client"))
            {
                name = "راسل";
            }
            else if (User.IsInRole("Admin"))
            {
                name = "الأدمن";
            }
            else if (User.IsInRole("Accountant"))
            {
                name = "المحاسب";
            }
            else if (User.IsInRole("HighAdmin"))
            {
                name = "مشرف مميز";
            }
            else if (User.IsInRole("LowAdmin"))
            {
                name = "مشرف عادي";
            }
            else if (User.IsInRole("TrustAdmin"))
            {
                name = "مساعد الأدمن";
            }
            HeaderVM headerVM = new HeaderVM()
            {
                Name = name,
            };


            var user = _userManger.GetUserId(User);
            var Notifications = _notifiaction.Get(x => x.UserId == user).OrderByDescending(d => d.CreateOn).ToList();
            headerVM.NotificationsNumber = Notifications.Count(d => !d.IsSeen && !d.IsDeleted);
            if (Notifications != null && Notifications.Count > 0)
            {
                foreach (var item in Notifications)
                {
                    headerVM.Notifications.Add(new NotificationVM()
                    {
                        Body = item.Body,
                        //NotificationType = item.NotificationType,
                        IsSeen = item.IsSeen,
                        Id = item.Id,
                        Title = item.Title,
                        //Link = item.NotificationLink
                    });
                }
            }
            return PartialView(headerVM);
        }
        public async Task<ActionResult> ClientSideMenu()
        {
            var userid = _userManger.GetUserId(User);
            var user = await _userManger.FindByIdAsync(userid);
            SideMenuVM sideMenuVM = new SideMenuVM()
            {
                Name = user.Name,
                //Name = "راسل",
                /*Complaints = db.Complaints.Count(w => w.IsDeleted == false && w.IsViewed == false)*/
            };
            return PartialView(sideMenuVM);
        }
        public async Task<ActionResult> DriverSideMenu()
        {
            var userid = _userManger.GetUserId(User);
            var user = await _userManger.FindByIdAsync(userid);
            SideMenuVM sideMenuVM = new SideMenuVM()
            {
                Name = user.Name,
                //  Name = "مندوب",
                /*Complaints = db.Complaints.Count(w => w.IsDeleted == false && w.IsViewed == false)*/
            };
            return PartialView(sideMenuVM);
        }
        public async Task<ActionResult> AdminSideMenu()
        {
            var userid = _userManger.GetUserId(User);
            var user = await _userManger.FindByIdAsync(userid);
            SideMenuVM sideMenuVM = new SideMenuVM()
            {
                Name = user.Name,
                //  Name = "الأدمن",
                /*Complaints = db.Complaints.Count(w => w.IsDeleted == false && w.IsViewed == false)*/
            };
            return PartialView(sideMenuVM);
        }
    }
}