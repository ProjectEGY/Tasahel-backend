using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using PostexS.Models.Enums;
using PostexS.Models.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Controllers
{
    [Authorize(Roles = "Admin,TrustAdmin")]
    public class WapilotSettingsController : Controller
    {
        private readonly IWapilotService _wapilotService;
        private readonly IWhatsAppBotCloudService _whatsAppBotCloudService;
        private readonly IWhatsAppProviderService _providerService;
        private readonly UserManager<ApplicationUser> _userManager;

        public WapilotSettingsController(
            IWapilotService wapilotService,
            IWhatsAppBotCloudService whatsAppBotCloudService,
            IWhatsAppProviderService providerService,
            UserManager<ApplicationUser> userManager)
        {
            _wapilotService = wapilotService;
            _whatsAppBotCloudService = whatsAppBotCloudService;
            _providerService = providerService;
            _userManager = userManager;
        }

        // GET: Settings page
        public async Task<IActionResult> Index()
        {
            var wapilotSettings = await _wapilotService.GetSettingsAsync();
            var whatsAppBotCloudSettings = await _whatsAppBotCloudService.GetSettingsAsync();
            var stats = await _wapilotService.GetQueueStatisticsAsync();
            var providerSettings = await _providerService.GetProviderSettingsAsync();

            var viewModel = new WhatsAppSettingsViewModel
            {
                WapilotSettings = wapilotSettings,
                WhatsAppBotCloudSettings = whatsAppBotCloudSettings,
                ProviderSettings = providerSettings,
                Statistics = stats,
                ActiveProvider = providerSettings.ActiveProvider
            };

            return View(viewModel);
        }

        // POST: Save settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(WhatsAppSettingsViewModel viewModel, string providerType)
        {
            var userId = _userManager.GetUserId(User);
            bool result = false;

            if (providerType == "Wapilot")
            {
                if (!TryValidateModel(viewModel.WapilotSettings))
                {
                    var stats = await _wapilotService.GetQueueStatisticsAsync();
                    var providerSettings = await _providerService.GetProviderSettingsAsync();
                    viewModel.Statistics = stats;
                    viewModel.ProviderSettings = providerSettings;
                    viewModel.ActiveProvider = providerSettings.ActiveProvider;
                    return View(viewModel);
                }

                result = await _wapilotService.UpdateSettingsAsync(viewModel.WapilotSettings, userId);
            }
            else if (providerType == "WhatsAppBotCloud")
            {
                if (!TryValidateModel(viewModel.WhatsAppBotCloudSettings))
                {
                    var stats = await _wapilotService.GetQueueStatisticsAsync();
                    var providerSettings = await _providerService.GetProviderSettingsAsync();
                    viewModel.Statistics = stats;
                    viewModel.ProviderSettings = providerSettings;
                    viewModel.ActiveProvider = providerSettings.ActiveProvider;
                    return View(viewModel);
                }

                result = await _whatsAppBotCloudService.UpdateSettingsAsync(viewModel.WhatsAppBotCloudSettings, userId);
            }

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

        // POST: Test message - sends message directly to phone number
        [HttpPost]
        public async Task<IActionResult> SendTestMessage(string phoneNumber, string message)
        {
            var settings = await _wapilotService.GetSettingsAsync();
            if (!settings.IsActive)
            {
                return Json(new { success = false, message = "خدمة الارسال متوقفة. قم بتفعيلها أولاً من الاعدادات" });
            }

            if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(message))
            {
                return Json(new { success = false, message = "برجاء ادخال رقم الهاتف والرسالة" });
            }

            var result = await _wapilotService.SendTestMessageAsync(phoneNumber, message);

            return Json(new
            {
                success = result.Success,
                message = result.Success ? "تم ارسال الرسالة بنجاح" : $"فشل ارسال الرسالة: {result.ErrorMessage}",
                response = result.ResponseBody,
                statusCode = result.StatusCode,
                duration = result.DurationMs
            });
        }

        // POST: Create group and send message
        [HttpPost]
        public async Task<IActionResult> SendTestToGroup(string phoneNumber, string message)
        {
            var settings = await _wapilotService.GetSettingsAsync();
            if (!settings.IsActive)
            {
                return Json(new { success = false, message = "خدمة الارسال متوقفة. قم بتفعيلها أولاً من الاعدادات" });
            }

            if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(message))
            {
                return Json(new { success = false, message = "برجاء ادخال رقم الهاتف والرسالة" });
            }

            // First, create a group with the phone number
            var groupName = $"تسهيل اكسبريس - تجربة";
            var createResult = await _wapilotService.CreateGroupAsync(groupName, phoneNumber);

            if (!createResult.Success)
            {
                return Json(new
                {
                    success = false,
                    message = $"فشل انشاء الجروب: {createResult.ErrorMessage}",
                    response = createResult.ResponseBody,
                    statusCode = createResult.StatusCode
                });
            }

            if (string.IsNullOrEmpty(createResult.GroupId))
            {
                return Json(new
                {
                    success = false,
                    message = "تم انشاء الجروب لكن لم يتم الحصول على Group ID",
                    response = createResult.ResponseBody,
                    statusCode = createResult.StatusCode
                });
            }

            // Now send the message to the created group
            var sendResult = await _wapilotService.SendMessageAsync(message, createResult.GroupId);

            return Json(new
            {
                success = sendResult.Success,
                message = sendResult.Success
                    ? $"تم انشاء الجروب وارسال الرسالة بنجاح. Group ID: {createResult.GroupId}"
                    : $"تم انشاء الجروب لكن فشل ارسال الرسالة: {sendResult.ErrorMessage}",
                groupId = createResult.GroupId,
                response = sendResult.ResponseBody,
                statusCode = sendResult.StatusCode,
                duration = sendResult.DurationMs
            });
        }

        // GET: Queue view
        public async Task<IActionResult> Queue(int page = 1, string status = "pending")
        {
            const int pageSize = 50;
            var viewModel = new WhatsAppQueueViewModel();

            if (status == "sent")
            {
                viewModel.QueueItems = await _wapilotService.GetSentQueueAsync(page, pageSize);
                viewModel.TotalPages = (int)Math.Ceiling(await _wapilotService.GetSentCountAsync() / (double)pageSize);
            }
            else if (status == "failed")
            {
                viewModel.QueueItems = await _wapilotService.GetFailedQueueAsync(page, pageSize);
                viewModel.TotalPages = (int)Math.Ceiling(await _wapilotService.GetFailedCountAsync() / (double)pageSize);
            }
            else
            {
                viewModel.QueueItems = await _wapilotService.GetPendingQueueAsync(page, pageSize);
                viewModel.TotalPages = (int)Math.Ceiling(await _wapilotService.GetPendingCountAsync() / (double)pageSize);
                status = "pending";
            }

            viewModel.Statistics = await _wapilotService.GetQueueStatisticsAsync();
            viewModel.CurrentPage = page;
            viewModel.Status = status;

            return View(viewModel);
        }

        // POST: Send test message to group by ID
        [HttpPost]
        public async Task<IActionResult> SendTestToGroupById(string groupId, string message)
        {
            var settings = await _wapilotService.GetSettingsAsync();
            if (!settings.IsActive)
            {
                return Json(new { success = false, message = "خدمة الارسال متوقفة. قم بتفعيلها أولاً من الاعدادات" });
            }

            if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(message))
            {
                return Json(new { success = false, message = "برجاء إدخال معرف الجروب والرسالة" });
            }

            var result = await _wapilotService.SendMessageAsync(message, groupId);

            return Json(new
            {
                success = result.Success,
                message = result.Success ? "تم ارسال الرسالة للجروب بنجاح" : $"فشل ارسال الرسالة: {result.ErrorMessage}",
                response = result.ResponseBody,
                statusCode = result.StatusCode,
                duration = result.DurationMs
            });
        }

        // POST: Get Chat ID by Phone Number
        [HttpPost]
        public async Task<IActionResult> GetChatIdByPhone(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return Json(new { success = false, message = "برجاء إدخال رقم الهاتف" });
            }

            var result = await _wapilotService.GetChatIdByPhoneAsync(phoneNumber);

            return Json(new
            {
                success = result.Success,
                chatId = result.ChatId,
                isGroup = result.IsGroup,
                chatName = result.ChatName,
                message = result.Success
                    ? $"تم جلب Chat ID بنجاح{(result.IsGroup ? " (جروب)" : " (محادثة فردية)")}{(string.IsNullOrEmpty(result.ChatName) ? "" : $" - {result.ChatName}")}"
                    : $"فشل جلب Chat ID: {result.ErrorMessage}",
                response = result.ResponseBody,
                statusCode = result.StatusCode
            });
        }

        // POST: Get Chat ID by LID
        [HttpPost]
        public async Task<IActionResult> GetChatIdByLid(string lid)
        {
            if (string.IsNullOrEmpty(lid))
            {
                return Json(new { success = false, message = "برجاء إدخال LID" });
            }

            var result = await _wapilotService.GetChatIdByLidAsync(lid);

            return Json(new
            {
                success = result.Success,
                chatId = result.ChatId,
                isGroup = result.IsGroup,
                chatName = result.ChatName,
                message = result.Success
                    ? $"تم جلب Chat ID بنجاح{(result.IsGroup ? " (جروب)" : " (محادثة فردية)")}{(string.IsNullOrEmpty(result.ChatName) ? "" : $" - {result.ChatName}")}"
                    : $"فشل جلب Chat ID: {result.ErrorMessage}",
                response = result.ResponseBody,
                statusCode = result.StatusCode
            });
        }

        // GET: Logs view
        public async Task<IActionResult> Logs(int page = 1, int days = 30)
        {
            const int pageSize = 50;
            var logs = await _wapilotService.GetLogsAsync(days, page, pageSize);
            var totalCount = await _wapilotService.GetLogsCountAsync(days);

            var viewModel = new WhatsAppLogsViewModel
            {
                Logs = logs,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Days = days
            };

            return View(viewModel);
        }

        // POST: Update Active Provider
        [HttpPost]
        public async Task<IActionResult> UpdateProvider(int provider)
        {
            var userId = _userManager.GetUserId(User);
            var providerEnum = (PostexS.Models.Enums.WhatsAppProvider)provider;
            var result = await _providerService.UpdateProviderAsync(providerEnum, userId);

            return Json(new
            {
                success = result,
                message = result ? "تم تحديث المزود النشط بنجاح" : "حدث خطأ أثناء تحديث المزود"
            });
        }

        // POST: Get Groups for WhatsApp Bot Cloud
        [HttpPost]
        public async Task<IActionResult> GetGroupsForWhatsAppBotCloud()
        {
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

        // POST: Send test message via WhatsApp Bot Cloud
        [HttpPost]
        public async Task<IActionResult> SendTestMessageWhatsAppBotCloud(string phoneNumber, string message)
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

        // POST: Send test group message via WhatsApp Bot Cloud
        [HttpPost]
        public async Task<IActionResult> SendTestGroupMessageWhatsAppBotCloud(string groupId, string message)
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
