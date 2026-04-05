using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using PostexS.Models.Enums;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Controllers
{
    public class DeleteAccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGeneric<ApplicationUser> _user;

        public DeleteAccountController(
            UserManager<ApplicationUser> userManager,
            IGeneric<ApplicationUser> user)
        {
            _userManager = userManager;
            _user = user;
        }

        [HttpGet]
        public IActionResult Driver()
        {
            ViewBag.AppType = "driver";
            ViewBag.AppName = "تطبيق المندوب";
            ViewBag.AccentColor = "#2e7d32";
            ViewBag.AccentBg = "#e8f5e9";
            return View("Delete");
        }

        [HttpGet]
        public IActionResult Sender()
        {
            ViewBag.AppType = "sender";
            ViewBag.AppName = "تطبيق الراسل";
            ViewBag.AccentColor = "#1565c0";
            ViewBag.AccentBg = "#e3f2fd";
            return View("Delete");
        }

        [HttpPost]
        public async Task<IActionResult> Confirm(string phone, string password, string appType)
        {
            if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(password))
                return Json(new { success = false, message = "برجاء إدخال رقم الهاتف وكلمة المرور" });

            var user = _user.Get(x => x.PhoneNumber == phone).FirstOrDefault();

            if (user == null || user.IsDeleted)
                return Json(new { success = false, message = "رقم الهاتف أو كلمة المرور غير صحيحة" });

            if (appType == "driver" && user.UserType == UserType.Client)
                return Json(new { success = false, message = "رقم الهاتف أو كلمة المرور غير صحيحة" });

            if (appType == "sender" && user.UserType != UserType.Client)
                return Json(new { success = false, message = "رقم الهاتف أو كلمة المرور غير صحيحة" });

            if (!await _userManager.CheckPasswordAsync(user, password))
                return Json(new { success = false, message = "رقم الهاتف أو كلمة المرور غير صحيحة" });

            user.IsDeleted = true;
            await _user.Update(user);

            return Json(new { success = true, message = "تم حذف حسابك بنجاح" });
        }
    }
}
