using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IWhaStackService _whaStackService;
        private readonly IWhatsAppProviderService _providerService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IGeneric<Order> _orders;

        public WapilotSettingsController(
            IWapilotService wapilotService,
            IWhatsAppBotCloudService whatsAppBotCloudService,
            IWhaStackService whaStackService,
            IWhatsAppProviderService providerService,
            UserManager<ApplicationUser> userManager,
            IServiceScopeFactory serviceScopeFactory,
            IGeneric<Order> orders)
        {
            _wapilotService = wapilotService;
            _whatsAppBotCloudService = whatsAppBotCloudService;
            _whaStackService = whaStackService;
            _providerService = providerService;
            _userManager = userManager;
            _serviceScopeFactory = serviceScopeFactory;
            _orders = orders;
        }

        // GET: Settings page
        public async Task<IActionResult> Index()
        {
            var wapilotSettings = await _wapilotService.GetSettingsAsync();
            var whatsAppBotCloudSettings = await _whatsAppBotCloudService.GetSettingsAsync();
            var whaStackSettings = await _whaStackService.GetSettingsAsync();
            var stats = await _wapilotService.GetQueueStatisticsAsync();
            var providerSettings = await _providerService.GetProviderSettingsAsync();

            var viewModel = new WhatsAppSettingsViewModel
            {
                WapilotSettings = wapilotSettings,
                WhatsAppBotCloudSettings = whatsAppBotCloudSettings,
                WhaStackSettings = whaStackSettings,
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

            // Clear ModelState and validate only the specific provider settings
            ModelState.Clear();

            if (providerType == "Wapilot")
            {
                // Validate only Wapilot settings
                if (string.IsNullOrWhiteSpace(viewModel.WapilotSettings?.InstanceId))
                {
                    ModelState.AddModelError("WapilotSettings.InstanceId", "The Instance ID field is required.");
                }
                if (string.IsNullOrWhiteSpace(viewModel.WapilotSettings?.ApiToken))
                {
                    ModelState.AddModelError("WapilotSettings.ApiToken", "The API Token field is required.");
                }

                if (!ModelState.IsValid)
                {
                    var stats = await _wapilotService.GetQueueStatisticsAsync();
                    var providerSettings = await _providerService.GetProviderSettingsAsync();
                    viewModel.WhatsAppBotCloudSettings = await _whatsAppBotCloudService.GetSettingsAsync();
                    viewModel.Statistics = stats;
                    viewModel.ProviderSettings = providerSettings;
                    viewModel.ActiveProvider = providerSettings.ActiveProvider;
                    return View(viewModel);
                }

                result = await _wapilotService.UpdateSettingsAsync(viewModel.WapilotSettings, userId);
            }
            else if (providerType == "WhatsAppBotCloud")
            {
                // Validate only WhatsAppBotCloud settings
                if (string.IsNullOrWhiteSpace(viewModel.WhatsAppBotCloudSettings?.InstanceId))
                {
                    ModelState.AddModelError("WhatsAppBotCloudSettings.InstanceId", "The Instance ID field is required.");
                }
                if (string.IsNullOrWhiteSpace(viewModel.WhatsAppBotCloudSettings?.AccessToken))
                {
                    ModelState.AddModelError("WhatsAppBotCloudSettings.AccessToken", "The Access Token field is required.");
                }

                if (!ModelState.IsValid)
                {
                    var stats = await _wapilotService.GetQueueStatisticsAsync();
                    var providerSettings = await _providerService.GetProviderSettingsAsync();
                    viewModel.WapilotSettings = await _wapilotService.GetSettingsAsync();
                    viewModel.Statistics = stats;
                    viewModel.ProviderSettings = providerSettings;
                    viewModel.ActiveProvider = providerSettings.ActiveProvider;
                    return View(viewModel);
                }

                result = await _whatsAppBotCloudService.UpdateSettingsAsync(viewModel.WhatsAppBotCloudSettings, userId);
            }
            else if (providerType == "WhaStack")
            {
                // Validate only WhaStack settings
                if (string.IsNullOrWhiteSpace(viewModel.WhaStackSettings?.SessionId))
                {
                    ModelState.AddModelError("WhaStackSettings.SessionId", "The Session ID field is required.");
                }
                if (string.IsNullOrWhiteSpace(viewModel.WhaStackSettings?.ApiKey))
                {
                    ModelState.AddModelError("WhaStackSettings.ApiKey", "The API Key field is required.");
                }

                if (!ModelState.IsValid)
                {
                    var stats = await _wapilotService.GetQueueStatisticsAsync();
                    var providerSettings = await _providerService.GetProviderSettingsAsync();
                    viewModel.WapilotSettings = await _wapilotService.GetSettingsAsync();
                    viewModel.WhatsAppBotCloudSettings = await _whatsAppBotCloudService.GetSettingsAsync();
                    viewModel.Statistics = stats;
                    viewModel.ProviderSettings = providerSettings;
                    viewModel.ActiveProvider = providerSettings.ActiveProvider;
                    return View(viewModel);
                }

                result = await _whaStackService.UpdateSettingsAsync(viewModel.WhaStackSettings, userId);
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

        // POST: Test Background WhatsApp Send to Phone (simulates background task pattern)
        [HttpPost]
        public IActionResult TestBackgroundSendToPhone(string phoneNumber, string message)
        {
            if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(message))
            {
                return Json(new { success = false, message = "برجاء إدخال رقم الهاتف والرسالة" });
            }

            // Fire-and-forget with scoped services (same pattern as order completion)
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var botCloudService = scope.ServiceProvider.GetRequiredService<IWhatsAppBotCloudService>();

                    // Send test message using scoped service (WhatsApp Bot Cloud)
                    await botCloudService.SendMessageAsync(phoneNumber, message);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Test Background WhatsApp Error: {ex.Message}");
                }
            });

            return Json(new { success = true, message = "تم إرسال الرسالة في الخلفية - تحقق من السجلات" });
        }

        // POST: Test Background WhatsApp Send to Group (simulates background task pattern)
        [HttpPost]
        public IActionResult TestBackgroundSendToGroup(string groupId, string message)
        {
            if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(message))
            {
                return Json(new { success = false, message = "برجاء إدخال معرف الجروب والرسالة" });
            }

            if (!groupId.EndsWith("@g.us"))
            {
                return Json(new { success = false, message = "معرف الجروب يجب أن ينتهي بـ @g.us" });
            }

            // Fire-and-forget with scoped services (same pattern as order completion)
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var botCloudService = scope.ServiceProvider.GetRequiredService<IWhatsAppBotCloudService>();

                    // Send message to group using scoped service (WhatsApp Bot Cloud)
                    await botCloudService.SendGroupMessageAsync(groupId, message);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Test Background WhatsApp Group Error: {ex.Message}");
                }
            });

            return Json(new { success = true, message = "تم إرسال الرسالة للجروب في الخلفية - تحقق من السجلات" });
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

        #region WhaStack Actions

        // POST: Send test message via WhaStack
        [HttpPost]
        public async Task<IActionResult> SendTestMessageWhaStack(string phoneNumber, string message)
        {
            var settings = await _whaStackService.GetSettingsAsync();
            if (!settings.IsActive)
            {
                return Json(new { success = false, message = "خدمة الارسال متوقفة. قم بتفعيلها أولاً من الاعدادات" });
            }

            if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(message))
            {
                return Json(new { success = false, message = "برجاء ادخال رقم الهاتف والرسالة" });
            }

            var result = await _whaStackService.SendMessageAsync(phoneNumber, message);

            return Json(new
            {
                success = result.Success,
                message = result.Success ? "تم ارسال الرسالة بنجاح" : $"فشل ارسال الرسالة: {result.ErrorMessage}",
                response = result.ResponseBody,
                statusCode = result.StatusCode,
                duration = result.DurationMs
            });
        }

        // POST: Send test group message via WhaStack
        [HttpPost]
        public async Task<IActionResult> SendTestGroupMessageWhaStack(string groupId, string message)
        {
            var settings = await _whaStackService.GetSettingsAsync();
            if (!settings.IsActive)
            {
                return Json(new { success = false, message = "خدمة الارسال متوقفة. قم بتفعيلها أولاً من الاعدادات" });
            }

            if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(message))
            {
                return Json(new { success = false, message = "برجاء إدخال معرف الجروب والرسالة" });
            }

            var result = await _whaStackService.SendGroupMessageAsync(groupId, message);
            return Json(new
            {
                success = result.Success,
                message = result.Success ? "تم ارسال الرسالة للجروب بنجاح" : $"فشل ارسال الرسالة: {result.ErrorMessage}",
                response = result.ResponseBody,
                statusCode = result.StatusCode,
                duration = result.DurationMs
            });
        }

        // POST: Get Groups for WhaStack
        [HttpPost]
        public async Task<IActionResult> GetGroupsForWhaStack()
        {
            var result = await _whaStackService.GetGroupsAsync();

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

        // POST: Get WhaStack Sessions
        [HttpPost]
        public async Task<IActionResult> GetWhaStackSessions()
        {
            var result = await _whaStackService.GetSessionsAsync();

            return Json(new
            {
                success = result.Success,
                sessions = result.Sessions.Select(s => new
                {
                    sessionId = s.SessionId,
                    name = s.Name,
                    status = s.Status,
                    phoneNumber = s.PhoneNumber
                }),
                message = result.Success
                    ? $"تم جلب {result.Sessions.Count} جلسة بنجاح"
                    : $"فشل جلب الجلسات: {result.ErrorMessage}",
                response = result.ResponseBody,
                statusCode = result.StatusCode
            });
        }

        // POST: Get WhaStack Quota
        [HttpPost]
        public async Task<IActionResult> GetWhaStackQuota()
        {
            var result = await _whaStackService.GetQuotaAsync();

            return Json(new
            {
                success = result.Success,
                totalQuota = result.TotalQuota,
                remainingQuota = result.RemainingQuota,
                message = result.Success
                    ? $"الكوتا المتبقية: {result.RemainingQuota ?? 0} / {result.TotalQuota ?? 0}"
                    : $"فشل جلب الكوتا: {result.ErrorMessage}",
                response = result.ResponseBody,
                statusCode = result.StatusCode
            });
        }

        #endregion
    }
}
