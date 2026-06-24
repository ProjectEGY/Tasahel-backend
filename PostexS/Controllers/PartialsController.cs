using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            if (User.IsInRole("TrackingAdmin"))
            {
                name = "متابعه";
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
            headerVM.NotificationsNumber = _notifiaction.GetCount(x => x.UserId == user && !x.IsSeen && !x.IsDeleted);
            var recentNotifications = _notifiaction.GetAllAsIQueryable(
                filter: x => x.UserId == user && !x.IsDeleted,
                orderby: q => q.OrderByDescending(d => d.CreateOn),
                asNoTracking: true)
                .Take(20)
                .Select(item => new NotificationVM
                {
                    Body = item.Body,
                    IsSeen = item.IsSeen,
                    Id = item.Id,
                    Title = item.Title,
                })
                .ToList();
            headerVM.Notifications = recentNotifications;
            return PartialView(headerVM);
        }
        public async Task<ActionResult> ClientSideMenu()
        {
            var userid = _userManger.GetUserId(User);
            var userName = await _userManger.Users
                .Where(u => u.Id == userid)
                .Select(u => u.Name)
                .FirstOrDefaultAsync();
            SideMenuVM sideMenuVM = new SideMenuVM()
            {
                Name = userName ?? "راسل",
            };
            return PartialView(sideMenuVM);
        }
        public async Task<ActionResult> DriverSideMenu()
        {
            var userid = _userManger.GetUserId(User);
            var userName = await _userManger.Users
                .Where(u => u.Id == userid)
                .Select(u => u.Name)
                .FirstOrDefaultAsync();
            SideMenuVM sideMenuVM = new SideMenuVM()
            {
                Name = userName ?? "مندوب",
            };
            return PartialView(sideMenuVM);
        }
        public async Task<ActionResult> AdminSideMenu()
        {
            var userid = _userManger.GetUserId(User);
            var userName = await _userManger.Users
                .Where(u => u.Id == userid)
                .Select(u => u.Name)
                .FirstOrDefaultAsync();
            SideMenuVM sideMenuVM = new SideMenuVM()
            {
                Name = userName ?? "الأدمن",
            };
            return PartialView(sideMenuVM);
        }
        public async Task<ActionResult> TrackingAdminSideMenu()
        {
            var userid = _userManger.GetUserId(User);
            var userName = await _userManger.Users
                .Where(u => u.Id == userid)
                .Select(u => u.Name)
                .FirstOrDefaultAsync();
            SideMenuVM sideMenuVM = new SideMenuVM()
            {
                Name = userName ?? "متابعه",
            };
            return PartialView(sideMenuVM);
        }
    }
}