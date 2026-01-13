using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Controllers
{
    [Authorize(Roles = "Admin,TrustAdmin")]
    public class WhatsAppBotCloudSettingsController : Controller
    {
        private readonly IWhatsAppBotCloudService _whatsAppBotCloudService;
        private readonly UserManager<ApplicationUser> _userManager;

        public WhatsAppBotCloudSettingsController(
            IWhatsAppBotCloudService whatsAppBotCloudService,
            UserManager<ApplicationUser> userManager)
        {
            _whatsAppBotCloudService = whatsAppBotCloudService;
            _userManager = userManager;
        }

        // GET: Settings page
        public async Task<IActionResult> Index()
        {
            var settings = await _whatsAppBotCloudService.GetSettingsAsync();
            return View(settings);
        }

        // POST: Save settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(WhatsAppBotCloudSettings model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            var result = await _whatsAppBotCloudService.UpdateSettingsAsync(model, userId);

            if (result)
            {
                TempData["Success"] = "تم حفظ الاعدادات بنجاح";
            }
            else
            {
                TempData["Error"] = "حدث خطأ اثناء حفظ الاعدادات";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Get Groups (no phone number needed - gets all groups)
        [HttpPost]
        public async Task<IActionResult> GetGroups(string phoneNumber = null)
        {
            // Note: phoneNumber parameter kept for backward compatibility but not used
            // WhatsApp Bot Cloud API gets all groups without needing phone number
            var result = await _whatsAppBotCloudService.GetGroupsAsync();

            return Json(new
            {
                success = result.Success,
                groups = result.Groups.Select(g => new { id = g.GroupId, name = g.GroupName, description = g.Description }),
                message = result.Success
                    ? $"تم جلب {result.Groups.Count} جروب بنجاح"
                    : $"فشل جلب الجروبات: {result.ErrorMessage}",
                response = result.ResponseBody,
                statusCode = result.StatusCode
            });
        }

        // POST: Send test message to phone number
        [HttpPost]
        public async Task<IActionResult> SendTestMessage(string phoneNumber, string message)
        {
            var settings = await _whatsAppBotCloudService.GetSettingsAsync();
            if (!settings.IsActive)
            {
                return Json(new { success = false, message = "خدمة الارسال متوقفة. قم بتفعيلها أولاً من الاعدادات" });
            }

            if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(message))
            {
                return Json(new { success = false, message = "برجاء ادخال رقم الهاتف والرسالة" });
            }

            var result = await _whatsAppBotCloudService.SendMessageAsync(phoneNumber, message);

            return Json(new
            {
                success = result.Success,
                message = result.Success ? "تم ارسال الرسالة بنجاح" : $"فشل ارسال الرسالة: {result.ErrorMessage}",
                response = result.ResponseBody,
                statusCode = result.StatusCode,
                duration = result.DurationMs
            });
        }

        // POST: Send test message to group
        [HttpPost]
        public async Task<IActionResult> SendTestGroupMessage(string groupId, string message)
        {
            var settings = await _whatsAppBotCloudService.GetSettingsAsync();
            if (!settings.IsActive)
            {
                return Json(new { success = false, message = "خدمة الارسال متوقفة. قم بتفعيلها أولاً من الاعدادات" });
            }

            if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(message))
            {
                return Json(new { success = false, message = "برجاء إدخال معرف الجروب والرسالة" });
            }

            var result = await _whatsAppBotCloudService.SendGroupMessageAsync(groupId, message);

            return Json(new
            {
                success = result.Success,
                message = result.Success ? "تم ارسال الرسالة للجروب بنجاح" : $"فشل ارسال الرسالة: {result.ErrorMessage}",
                response = result.ResponseBody,
                statusCode = result.StatusCode,
                duration = result.DurationMs
            });
        }
    }
}
