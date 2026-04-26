using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using PostexS.Interfaces;
using PostexS.Models.Data;
using PostexS.Models.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace PostexS.Controllers
{
    [Authorize(Roles = "Admin,TrustAdmin")]
    public class WhaStackSessionsController : Controller
    {
        private readonly IWhaStackService _service;
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public WhaStackSessionsController(
            IWhaStackService service,
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory)
        {
            _service = service;
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var instances = await _service.GetSessionInstancesAsync();
            return View(instances);
        }

        public IActionResult Create() => View(new WhaStackSessionInstance());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WhaStackSessionInstance model)
        {
            if (!ModelState.IsValid) return View(model);

            var ok = await _service.AddSessionInstanceAsync(model);
            if (ok) TempData["Success"] = "تمت إضافة الجلسة بنجاح";
            else TempData["Error"] = "فشل إضافة الجلسة";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(long id)
        {
            var instance = await _service.GetSessionInstanceByIdAsync(id);
            if (instance == null) return NotFound();
            return View(instance);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(WhaStackSessionInstance model)
        {
            if (!ModelState.IsValid) return View(model);

            var ok = await _service.UpdateSessionInstanceAsync(model);
            if (ok) TempData["Success"] = "تم حفظ التعديلات بنجاح";
            else TempData["Error"] = "فشل تعديل الجلسة";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var ok = await _service.DeleteSessionInstanceAsync(id);
            if (ok) TempData["Success"] = "تم حذف الجلسة بنجاح";
            else TempData["Error"] = "فشل حذف الجلسة";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(long id)
        {
            var ok = await _service.ToggleSessionInstanceActiveAsync(id);
            if (ok) TempData["Success"] = "تم تحديث حالة الجلسة";
            else TempData["Error"] = "فشل تحديث الحالة";
            return RedirectToAction(nameof(Index));
        }

        // -------- JSON endpoints (used inline from WapilotSettings page) --------

        [HttpGet]
        public async Task<IActionResult> ListJson()
        {
            var instances = await _service.GetSessionInstancesAsync();
            return Json(new
            {
                success = true,
                items = instances.Select(i => new
                {
                    id = i.Id,
                    displayName = i.DisplayName,
                    phoneNumber = i.PhoneNumber,
                    sessionId = i.SessionId,
                    isActive = i.IsActive,
                    lastUsedAt = i.LastUsedAt,
                    consecutiveFailures = i.ConsecutiveFailures,
                    totalSentSuccess = i.TotalSentSuccess,
                    totalSentFailed = i.TotalSentFailed
                })
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateJson(string displayName, string phoneNumber, string sessionId, bool isActive = true)
        {
            if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(sessionId))
                return Json(new { success = false, message = "اسم الجلسة و Session ID مطلوبان" });

            var ok = await _service.AddSessionInstanceAsync(new WhaStackSessionInstance
            {
                DisplayName = displayName.Trim(),
                PhoneNumber = phoneNumber?.Trim(),
                SessionId = sessionId.Trim(),
                IsActive = isActive
            });

            return Json(new { success = ok, message = ok ? "تمت إضافة الجلسة بنجاح" : "فشل إضافة الجلسة" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateJson(long id, string displayName, string phoneNumber, string sessionId, bool isActive)
        {
            var existing = await _service.GetSessionInstanceByIdAsync(id);
            if (existing == null) return Json(new { success = false, message = "الجلسة غير موجودة" });

            existing.DisplayName = displayName?.Trim();
            existing.PhoneNumber = phoneNumber?.Trim();
            existing.SessionId = sessionId?.Trim();
            existing.IsActive = isActive;

            var ok = await _service.UpdateSessionInstanceAsync(existing);
            return Json(new { success = ok, message = ok ? "تم حفظ التعديلات" : "فشل التعديل" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteJson(long id)
        {
            var ok = await _service.DeleteSessionInstanceAsync(id);
            return Json(new { success = ok, message = ok ? "تم الحذف" : "فشل الحذف" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActiveJson(long id)
        {
            var ok = await _service.ToggleSessionInstanceActiveAsync(id);
            return Json(new { success = ok, message = ok ? "تم تحديث الحالة" : "فشل التحديث" });
        }

        // -------- Per-session test send (used in Index test panel) --------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTestFromSession(long sessionInstanceId, string phoneNumber, string message, bool isGroup = false)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(message))
                return Json(new { success = false, message = "برجاء إدخال الرقم/الجروب والرسالة" });

            var instance = await _service.GetSessionInstanceByIdAsync(sessionInstanceId);
            if (instance == null) return Json(new { success = false, message = "الجلسة غير موجودة" });
            if (!instance.IsActive) return Json(new { success = false, message = "الجلسة معطلة" });

            var settings = await _service.GetSettingsAsync();
            if (string.IsNullOrEmpty(settings.BaseUrl) || string.IsNullOrEmpty(settings.ApiKey))
                return Json(new { success = false, message = "إعدادات WhaStack الأساسية غير مكتملة" });

            var formattedTarget = isGroup ? phoneNumber : phoneNumber.TrimStart('+').Replace(" ", "").Replace("-", "");
            var url = $"{settings.BaseUrl.TrimEnd('/')}{(isGroup ? "/groups/send" : "/messages/send")}";

            object body = isGroup
                ? (object)new { session_id = instance.SessionId, group_id = formattedTarget, message }
                : (object)new { session_id = instance.SessionId, to = formattedTarget, message };

            var sw = Stopwatch.StartNew();
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
                using var content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
                using var response = await client.PostAsync(url, content);
                sw.Stop();
                var resBody = await response.Content.ReadAsStringAsync();

                // Persist stats on the chosen instance
                instance.LastUsedAt = DateTime.UtcNow;
                if (response.IsSuccessStatusCode) { instance.TotalSentSuccess++; instance.ConsecutiveFailures = 0; }
                else { instance.TotalSentFailed++; instance.ConsecutiveFailures++; instance.LastFailureAt = DateTime.UtcNow; }
                instance.IsModified = true;
                instance.ModifiedOn = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = response.IsSuccessStatusCode,
                    message = response.IsSuccessStatusCode
                        ? $"تم الإرسال بنجاح من \"{instance.DisplayName}\""
                        : $"فشل الإرسال (HTTP {(int)response.StatusCode}): {resBody}",
                    duration = sw.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                sw.Stop();
                return Json(new { success = false, message = $"خطأ: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetGroupsForSession(long sessionInstanceId)
        {
            var instance = await _service.GetSessionInstanceByIdAsync(sessionInstanceId);
            if (instance == null) return Json(new { success = false, message = "الجلسة غير موجودة" });

            var settings = await _service.GetSettingsAsync();
            if (string.IsNullOrEmpty(settings.BaseUrl) || string.IsNullOrEmpty(settings.ApiKey))
                return Json(new { success = false, message = "إعدادات WhaStack الأساسية غير مكتملة" });

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
                var url = $"{settings.BaseUrl.TrimEnd('/')}/groups?session_id={Uri.EscapeDataString(instance.SessionId)}";
                using var response = await client.GetAsync(url);
                var resBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return Json(new { success = false, message = $"فشل جلب الجروبات: HTTP {(int)response.StatusCode}" });

                var groups = new System.Collections.Generic.List<object>();
                try
                {
                    var json = JsonDocument.Parse(resBody);
                    JsonElement? arr = null;
                    if (json.RootElement.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Array) arr = d;
                    else if (json.RootElement.TryGetProperty("groups", out var g) && g.ValueKind == JsonValueKind.Array) arr = g;
                    else if (json.RootElement.ValueKind == JsonValueKind.Array) arr = json.RootElement;

                    if (arr.HasValue)
                    {
                        foreach (var grp in arr.Value.EnumerateArray())
                        {
                            string id = null, name = null;
                            if (grp.TryGetProperty("id", out var idEl)) id = idEl.GetString();
                            else if (grp.TryGetProperty("group_id", out var gidEl)) id = gidEl.GetString();
                            if (grp.TryGetProperty("name", out var nameEl)) name = nameEl.GetString();
                            else if (grp.TryGetProperty("subject", out var subEl)) name = subEl.GetString();
                            if (!string.IsNullOrEmpty(id))
                                groups.Add(new { id, name = string.IsNullOrEmpty(name) ? id : name });
                        }
                    }
                }
                catch { }

                return Json(new { success = true, groups, count = groups.Count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"خطأ: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendTest(string phoneNumber, string message)
        {
            if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(message))
                return Json(new { success = false, message = "برجاء إدخال رقم الهاتف والرسالة" });

            var result = await _service.SendMessageAsync(phoneNumber, message);
            return Json(new
            {
                success = result.Success,
                message = result.Success
                    ? $"تم الإرسال عبر الجلسة \"{result.UsedSessionName}\" بعد {result.AttemptsCount} محاولة"
                    : $"فشل: {result.ErrorMessage}",
                attempts = result.AttemptsCount,
                usedSession = result.UsedSessionName,
                duration = result.DurationMs
            });
        }
    }
}
