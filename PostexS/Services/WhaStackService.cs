using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PostexS.Interfaces;
using PostexS.Models.Data;
using PostexS.Models.Domain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace PostexS.Services
{
    public class WhaStackService : IWhaStackService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WhaStackService> _logger;

        public WhaStackService(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<WhaStackService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        #region Settings Management

        public async Task<WhaStackSettings> GetSettingsAsync()
        {
            var settings = await _context.WhaStackSettings
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            return settings ?? new WhaStackSettings();
        }

        public async Task<bool> UpdateSettingsAsync(WhaStackSettings settings, string updatedBy)
        {
            var existing = await _context.WhaStackSettings
                .Where(s => !s.IsDeleted)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.BaseUrl = settings.BaseUrl;
                existing.SessionId = settings.SessionId;
                existing.ApiKey = settings.ApiKey;
                existing.MessageIntervalSeconds = settings.MessageIntervalSeconds;
                existing.IsActive = settings.IsActive;
                existing.LastUpdatedBy = updatedBy;
                existing.LastUpdatedAt = DateTime.UtcNow;
                existing.ModifiedOn = DateTime.UtcNow;
                existing.IsModified = true;

                _context.WhaStackSettings.Update(existing);
            }
            else
            {
                settings.LastUpdatedBy = updatedBy;
                settings.LastUpdatedAt = DateTime.UtcNow;
                settings.CreateOn = DateTime.UtcNow;
                await _context.WhaStackSettings.AddAsync(settings);
            }

            return await _context.SaveChangesAsync() > 0;
        }

        #endregion

        #region Session Instances Management (Round-Robin)

        public async Task<List<WhaStackSessionInstance>> GetSessionInstancesAsync()
        {
            await EnsureSeedFromLegacySettingsAsync();
            return await _context.WhaStackSessionInstances
                .Where(i => !i.IsDeleted)
                .OrderBy(i => i.Id)
                .ToListAsync();
        }

        public async Task<WhaStackSessionInstance> GetSessionInstanceByIdAsync(long id)
        {
            return await _context.WhaStackSessionInstances
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
        }

        public async Task<bool> AddSessionInstanceAsync(WhaStackSessionInstance instance)
        {
            instance.CreateOn = DateTime.UtcNow;
            await _context.WhaStackSessionInstances.AddAsync(instance);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateSessionInstanceAsync(WhaStackSessionInstance instance)
        {
            var existing = await _context.WhaStackSessionInstances
                .FirstOrDefaultAsync(i => i.Id == instance.Id && !i.IsDeleted);
            if (existing == null) return false;

            existing.DisplayName = instance.DisplayName;
            existing.PhoneNumber = instance.PhoneNumber;
            existing.SessionId = instance.SessionId;
            existing.IsActive = instance.IsActive;
            existing.ModifiedOn = DateTime.UtcNow;
            existing.IsModified = true;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteSessionInstanceAsync(long id)
        {
            var instance = await _context.WhaStackSessionInstances
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
            if (instance == null) return false;

            // Try to delete the remote session in WhaStack first; don't block local cleanup if it fails
            if (!string.IsNullOrEmpty(instance.SessionId))
            {
                try
                {
                    var remote = await DeleteRemoteSessionAsync(instance.SessionId);
                    if (!remote.Success)
                    {
                        _logger.LogWarning("Local delete will proceed despite remote delete failure for session {Id} ({SessionId}): {Error}",
                            instance.Id, instance.SessionId, remote.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Remote delete threw for session {Id}; proceeding with local delete", instance.Id);
                }
            }

            instance.IsDeleted = true;
            instance.DeletedOn = DateTime.UtcNow;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> ToggleSessionInstanceActiveAsync(long id)
        {
            var instance = await _context.WhaStackSessionInstances
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);
            if (instance == null) return false;

            instance.IsActive = !instance.IsActive;
            instance.ModifiedOn = DateTime.UtcNow;
            instance.IsModified = true;
            return await _context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// Auto-create the first session instance from legacy WhaStackSettings.SessionId
        /// to preserve the currently-running configuration on first access.
        /// </summary>
        private async Task EnsureSeedFromLegacySettingsAsync()
        {
            var any = await _context.WhaStackSessionInstances.AnyAsync(i => !i.IsDeleted);
            if (any) return;

            var legacy = await _context.WhaStackSettings
                .Where(s => !s.IsDeleted && !string.IsNullOrEmpty(s.SessionId))
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();
            if (legacy == null) return;

            var seeded = new WhaStackSessionInstance
            {
                DisplayName = "الجلسة الأساسية",
                SessionId = legacy.SessionId,
                IsActive = legacy.IsActive,
                CreateOn = DateTime.UtcNow
            };
            _context.WhaStackSessionInstances.Add(seeded);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Auto-seeded first WhaStack session instance from legacy settings");
        }

        /// <summary>
        /// Returns active sessions ordered for round-robin balancing:
        /// fewest successful sends in last 24h first, tie-break by oldest LastUsedAt.
        /// </summary>
        private async Task<List<WhaStackSessionInstance>> GetActiveSessionsOrderedForSendingAsync()
        {
            await EnsureSeedFromLegacySettingsAsync();

            var instances = await _context.WhaStackSessionInstances
                .Where(i => !i.IsDeleted && i.IsActive)
                .ToListAsync();
            if (instances.Count == 0) return instances;

            var since = DateTime.UtcNow.AddHours(-24);
            var ids = instances.Select(i => i.Id).ToList();
            var counts = await _context.WhatsAppMessageLogs
                .Where(l => l.WhaStackSessionInstanceId.HasValue
                            && ids.Contains(l.WhaStackSessionInstanceId.Value)
                            && l.IsSuccess
                            && l.RequestedAt >= since)
                .GroupBy(l => l.WhaStackSessionInstanceId.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync();

            var countMap = counts.ToDictionary(x => x.Id, x => x.Count);

            return instances
                .OrderBy(i => countMap.TryGetValue(i.Id, out var c) ? c : 0)
                .ThenBy(i => i.LastUsedAt ?? DateTime.MinValue)
                .ToList();
        }

        #endregion

        #region Message Sending (with rotation)

        public async Task<WhaStackSendResult> SendMessageAsync(string phoneNumber, string message)
        {
            var formattedPhone = (phoneNumber ?? "").TrimStart('+').Replace(" ", "").Replace("-", "");
            return await SendWithRotationAsync(
                endpoint: "/messages/send",
                buildBody: sid => new { session_id = sid, to = formattedPhone, message },
                target: formattedPhone);
        }

        public async Task<WhaStackSendResult> SendGroupMessageAsync(string groupId, string message)
        {
            return await SendWithRotationAsync(
                endpoint: "/groups/send",
                buildBody: sid => new { session_id = sid, group_id = groupId, message },
                target: groupId);
        }

        public async Task<WhaStackSendResult> SendImageAsync(string phoneNumber, string mediaUrl, string caption = null)
        {
            var formattedPhone = (phoneNumber ?? "").TrimStart('+').Replace(" ", "").Replace("-", "");
            return await SendWithRotationAsync(
                endpoint: "/messages/send-image",
                buildBody: sid => new { session_id = sid, to = formattedPhone, media_url = mediaUrl, caption = caption ?? "" },
                target: formattedPhone);
        }

        public async Task<WhaStackSendResult> SendDocumentAsync(string phoneNumber, string mediaUrl, string fileName, string mimetype)
        {
            var formattedPhone = (phoneNumber ?? "").TrimStart('+').Replace(" ", "").Replace("-", "");
            return await SendWithRotationAsync(
                endpoint: "/messages/send-document",
                buildBody: sid => new { session_id = sid, to = formattedPhone, media_url = mediaUrl, file_name = fileName, mimetype },
                target: formattedPhone);
        }

        public async Task<WhaStackSendResult> SendGroupImageAsync(string groupId, string mediaUrl, string caption = null)
        {
            return await SendWithRotationAsync(
                endpoint: "/groups/send-image",
                buildBody: sid => new { session_id = sid, group_id = groupId, media_url = mediaUrl, caption = caption ?? "" },
                target: groupId);
        }

        public async Task<WhaStackSendResult> SendGroupDocumentAsync(string groupId, string mediaUrl, string fileName)
        {
            return await SendWithRotationAsync(
                endpoint: "/groups/send-document",
                buildBody: sid => new { session_id = sid, group_id = groupId, media_url = mediaUrl, file_name = fileName },
                target: groupId);
        }

        /// <summary>
        /// Core rotation routine: tries each active session in priority order,
        /// retrying on failure with the next session until one succeeds or all are exhausted.
        /// </summary>
        private async Task<WhaStackSendResult> SendWithRotationAsync(string endpoint, Func<string, object> buildBody, string target)
        {
            var settings = await GetSettingsAsync();
            var result = new WhaStackSendResult();
            var sessions = await GetActiveSessionsOrderedForSendingAsync();

            // Fallback to legacy settings session if no instances are configured/active
            if (sessions.Count == 0)
            {
                if (string.IsNullOrEmpty(settings.SessionId))
                {
                    result.Success = false;
                    result.ErrorMessage = "لا توجد جلسات واتساب مفعلة في النظام";
                    return result;
                }
                sessions.Add(new WhaStackSessionInstance
                {
                    Id = 0,
                    DisplayName = "Legacy Session",
                    SessionId = settings.SessionId,
                    IsActive = true
                });
            }

            if (string.IsNullOrEmpty(settings.BaseUrl) || string.IsNullOrEmpty(settings.ApiKey))
            {
                result.Success = false;
                result.ErrorMessage = "إعدادات WhaStack الأساسية غير مكتملة (BaseUrl / ApiKey)";
                return result;
            }

            var url = $"{settings.BaseUrl.TrimEnd('/')}{endpoint}";

            foreach (var session in sessions)
            {
                result.AttemptsCount++;
                result.UsedSessionInstanceId = session.Id == 0 ? (long?)null : session.Id;
                result.UsedSessionName = session.DisplayName;

                var attempt = await TrySendOnceAsync(url, buildBody(session.SessionId), settings.ApiKey);

                // Persist usage stats only for real session instances (not the legacy fallback)
                if (session.Id != 0)
                {
                    session.LastUsedAt = DateTime.UtcNow;
                    if (attempt.Success)
                    {
                        session.ConsecutiveFailures = 0;
                        session.TotalSentSuccess++;
                    }
                    else
                    {
                        session.ConsecutiveFailures++;
                        session.LastFailureAt = DateTime.UtcNow;
                        session.TotalSentFailed++;
                    }
                    session.ModifiedOn = DateTime.UtcNow;
                    session.IsModified = true;
                    try { await _context.SaveChangesAsync(); }
                    catch (Exception saveEx) { _logger.LogWarning(saveEx, "Failed to update WhaStack session stats for {Id}", session.Id); }
                }

                result.Success = attempt.Success;
                result.StatusCode = attempt.StatusCode;
                result.ResponseBody = attempt.ResponseBody;
                result.ErrorMessage = attempt.ErrorMessage;
                result.DurationMs = attempt.DurationMs;

                if (attempt.Success)
                {
                    _logger.LogInformation("WhaStack: sent to {Target} via session {Name} ({Id}) on attempt {Attempt}",
                        target, session.DisplayName, session.Id, result.AttemptsCount);
                    return result;
                }

                var attemptErr = $"#{result.AttemptsCount} [{session.DisplayName}]: {attempt.ErrorMessage}";
                result.AttemptErrors.Add(attemptErr);
                _logger.LogWarning("WhaStack: send attempt failed via session {Name} ({Id}): {Error}. Trying next.",
                    session.DisplayName, session.Id, attempt.ErrorMessage);
            }

            result.Success = false;
            result.ErrorMessage = $"فشل الإرسال عبر كل الجلسات ({result.AttemptsCount} محاولة): {string.Join(" | ", result.AttemptErrors)}";
            _logger.LogError("WhaStack: All {Count} sessions failed to send to {Target}", result.AttemptsCount, target);
            return result;
        }

        private async Task<WhaStackSendResult> TrySendOnceAsync(string url, object body, string apiKey)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new WhaStackSendResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                using var jsonContent = new StringContent(
                    JsonSerializer.Serialize(body),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                using var response = await client.PostAsync(url, jsonContent);
                stopwatch.Stop();

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                result.Success = response.IsSuccessStatusCode;

                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"HTTP {result.StatusCode}: {result.ResponseBody}";
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.DurationMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        #endregion

        #region Group Management

        public async Task<WhaStackGetGroupsResult> GetGroupsAsync()
        {
            var settings = await GetSettingsAsync();
            var result = new WhaStackGetGroupsResult();

            // Use first active session if available, else fallback to settings
            var sessions = await GetActiveSessionsOrderedForSendingAsync();
            string sessionId = sessions.Count > 0 ? sessions[0].SessionId : settings.SessionId;

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/groups?session_id={Uri.EscapeDataString(sessionId ?? "")}";

                _logger.LogInformation("WhaStack: Getting groups via {Url}", url);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

                using var response = await client.GetAsync(url);

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var jsonResponse = JsonDocument.Parse(result.ResponseBody);

                        JsonElement? groupsElement = null;
                        if (jsonResponse.RootElement.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
                            groupsElement = dataElement;
                        else if (jsonResponse.RootElement.TryGetProperty("groups", out var groupsProp) && groupsProp.ValueKind == JsonValueKind.Array)
                            groupsElement = groupsProp;
                        else if (jsonResponse.RootElement.ValueKind == JsonValueKind.Array)
                            groupsElement = jsonResponse.RootElement;

                        if (groupsElement.HasValue)
                        {
                            foreach (var group in groupsElement.Value.EnumerateArray())
                            {
                                var groupInfo = new WhatsAppGroupInfo();

                                if (group.TryGetProperty("id", out var idElement))
                                    groupInfo.GroupId = idElement.GetString();
                                else if (group.TryGetProperty("group_id", out var groupIdElement))
                                    groupInfo.GroupId = groupIdElement.GetString();

                                if (group.TryGetProperty("name", out var nameElement))
                                    groupInfo.GroupName = nameElement.GetString();
                                else if (group.TryGetProperty("subject", out var subjectElement))
                                    groupInfo.GroupName = subjectElement.GetString();

                                if (group.TryGetProperty("description", out var descElement))
                                    groupInfo.Description = descElement.GetString();
                                else if (group.TryGetProperty("desc", out var descProp))
                                    groupInfo.Description = descProp.GetString();

                                if (!string.IsNullOrEmpty(groupInfo.GroupId))
                                    result.Groups.Add(groupInfo);
                            }
                        }

                        if (result.Groups.Count == 0)
                        {
                            result.ErrorMessage = "لم يتم العثور على جروبات في الاستجابة";
                            _logger.LogWarning("WhaStack: No groups found. Response: {Response}", result.ResponseBody);
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning(parseEx, "WhaStack: Could not parse groups");
                        result.Success = false;
                        result.ErrorMessage = "فشل في تحليل استجابة الـ API";
                    }
                }
                else
                {
                    result.ErrorMessage = $"HTTP {result.StatusCode}: {result.ResponseBody}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error getting WhaStack groups");
            }

            return result;
        }

        #endregion

        #region Sessions (remote API)

        public async Task<WhaStackSessionsResult> GetSessionsAsync()
        {
            var settings = await GetSettingsAsync();
            var result = new WhaStackSessionsResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/sessions";

                var apiKey = settings.ApiKey?.Trim();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                using var response = await client.GetAsync(url);

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var jsonResponse = JsonDocument.Parse(result.ResponseBody);
                        JsonElement? sessionsElement = null;

                        if (jsonResponse.RootElement.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
                            sessionsElement = dataElement;
                        else if (jsonResponse.RootElement.TryGetProperty("sessions", out var sessionsProp) && sessionsProp.ValueKind == JsonValueKind.Array)
                            sessionsElement = sessionsProp;
                        else if (jsonResponse.RootElement.ValueKind == JsonValueKind.Array)
                            sessionsElement = jsonResponse.RootElement;

                        if (sessionsElement.HasValue)
                        {
                            foreach (var session in sessionsElement.Value.EnumerateArray())
                            {
                                var sessionInfo = new WhaStackSessionInfo();

                                if (session.TryGetProperty("session_id", out var sidElement))
                                    sessionInfo.SessionId = sidElement.GetString();
                                else if (session.TryGetProperty("id", out var idElement))
                                    sessionInfo.SessionId = idElement.GetString();

                                if (session.TryGetProperty("name", out var nameElement))
                                    sessionInfo.Name = nameElement.GetString();
                                else if (session.TryGetProperty("session_name", out var snameElement))
                                    sessionInfo.Name = snameElement.GetString();

                                if (session.TryGetProperty("status", out var statusElement))
                                    sessionInfo.Status = statusElement.GetString();
                                else if (session.TryGetProperty("state", out var stateElement))
                                    sessionInfo.Status = stateElement.GetString();

                                if (session.TryGetProperty("phone", out var phoneElement))
                                    sessionInfo.PhoneNumber = phoneElement.GetString();
                                else if (session.TryGetProperty("phone_number", out var pnElement))
                                    sessionInfo.PhoneNumber = pnElement.GetString();
                                else if (session.TryGetProperty("number", out var numElement))
                                    sessionInfo.PhoneNumber = numElement.GetString();

                                if (!string.IsNullOrEmpty(sessionInfo.SessionId))
                                    result.Sessions.Add(sessionInfo);
                            }
                        }

                        if (result.Sessions.Count == 0)
                        {
                            result.ErrorMessage = "لم يتم العثور على جلسات في الاستجابة";
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning(parseEx, "WhaStack: Could not parse sessions");
                        result.Success = false;
                        result.ErrorMessage = "فشل في تحليل استجابة الـ API";
                    }
                }
                else
                {
                    result.ErrorMessage = $"HTTP {result.StatusCode}: {result.ResponseBody}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error getting WhaStack sessions");
            }

            return result;
        }

        #endregion

        #region Quota

        public async Task<WhaStackQuotaResult> GetQuotaAsync()
        {
            var settings = await GetSettingsAsync();
            var result = new WhaStackQuotaResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/messages/quota";

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

                using var response = await client.GetAsync(url);

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var jsonResponse = JsonDocument.Parse(result.ResponseBody);

                        if (jsonResponse.RootElement.TryGetProperty("total", out var totalElement))
                            result.TotalQuota = totalElement.GetInt32();
                        if (jsonResponse.RootElement.TryGetProperty("remaining", out var remainingElement))
                            result.RemainingQuota = remainingElement.GetInt32();
                        if (result.RemainingQuota == null && jsonResponse.RootElement.TryGetProperty("quota", out var quotaElement))
                            result.RemainingQuota = quotaElement.GetInt32();
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning(parseEx, "WhaStack: Could not parse quota");
                    }
                }
                else
                {
                    result.ErrorMessage = $"HTTP {result.StatusCode}: {result.ResponseBody}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error getting WhaStack quota");
            }

            return result;
        }

        #endregion

        #region Session Management (QR Code Flow)

        public async Task<WhaStackCreateSessionResult> CreateSessionAsync(string name)
        {
            var settings = await GetSettingsAsync();
            var result = new WhaStackCreateSessionResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/sessions";

                var jsonBody = new { name };

                using var jsonContent = new StringContent(
                    JsonSerializer.Serialize(jsonBody),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

                using var response = await client.PostAsync(url, jsonContent);

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var jsonResponse = JsonDocument.Parse(result.ResponseBody);

                        if (jsonResponse.RootElement.TryGetProperty("data", out var dataElement))
                        {
                            if (dataElement.TryGetProperty("session_id", out var sidElement))
                                result.SessionId = sidElement.GetString();
                            else if (dataElement.TryGetProperty("id", out var idElement))
                                result.SessionId = idElement.GetString();
                        }
                        else if (jsonResponse.RootElement.TryGetProperty("session_id", out var sidElement))
                            result.SessionId = sidElement.GetString();
                        else if (jsonResponse.RootElement.TryGetProperty("id", out var idElement))
                            result.SessionId = idElement.GetString();
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning(parseEx, "WhaStack: Could not parse create session response");
                    }
                }
                else
                {
                    result.ErrorMessage = $"HTTP {result.StatusCode}: {result.ResponseBody}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error creating WhaStack session");
            }

            return result;
        }

        public async Task<WhaStackQrResult> GetSessionQrAsync(string sessionId)
        {
            var settings = await GetSettingsAsync();
            var result = new WhaStackQrResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/sessions/{Uri.EscapeDataString(sessionId)}/qr";

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

                using var response = await client.GetAsync(url);

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

                        if (contentType.StartsWith("image/"))
                        {
                            var imageBytes = await response.Content.ReadAsByteArrayAsync();
                            result.QrCode = $"data:{contentType};base64,{Convert.ToBase64String(imageBytes)}";
                        }
                        else
                        {
                            var jsonResponse = JsonDocument.Parse(result.ResponseBody);

                            if (jsonResponse.RootElement.TryGetProperty("data", out var dataElement))
                            {
                                if (dataElement.TryGetProperty("qr", out var qrElement))
                                    result.QrCode = qrElement.GetString();
                                else if (dataElement.TryGetProperty("qr_code", out var qrcElement))
                                    result.QrCode = qrcElement.GetString();
                                else if (dataElement.TryGetProperty("image", out var imgElement))
                                    result.QrCode = imgElement.GetString();
                            }
                            else if (jsonResponse.RootElement.TryGetProperty("qr", out var qrElement))
                                result.QrCode = qrElement.GetString();
                            else if (jsonResponse.RootElement.TryGetProperty("qr_code", out var qrcElement))
                                result.QrCode = qrcElement.GetString();
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning(parseEx, "WhaStack: Could not parse QR response");
                    }
                }
                else
                {
                    result.ErrorMessage = $"HTTP {result.StatusCode}: {result.ResponseBody}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error getting WhaStack QR for session {SessionId}", sessionId);
            }

            return result;
        }

        public async Task<WhaStackStatusResult> GetSessionStatusAsync(string sessionId)
        {
            var settings = await GetSettingsAsync();
            var result = new WhaStackStatusResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/sessions/{Uri.EscapeDataString(sessionId)}/status";

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

                using var response = await client.GetAsync(url);

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var jsonResponse = JsonDocument.Parse(result.ResponseBody);
                        JsonElement root = jsonResponse.RootElement;

                        if (root.TryGetProperty("data", out var dataElement))
                            root = dataElement;

                        if (root.TryGetProperty("status", out var statusElement))
                            result.Status = statusElement.GetString();
                        else if (root.TryGetProperty("state", out var stateElement))
                            result.Status = stateElement.GetString();

                        if (root.TryGetProperty("phone", out var phoneElement))
                            result.PhoneNumber = phoneElement.GetString();
                        else if (root.TryGetProperty("phone_number", out var pnElement))
                            result.PhoneNumber = pnElement.GetString();
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning(parseEx, "WhaStack: Could not parse status response");
                    }
                }
                else
                {
                    result.ErrorMessage = $"HTTP {result.StatusCode}: {result.ResponseBody}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error getting WhaStack session status for {SessionId}", sessionId);
            }

            return result;
        }

        public async Task<WhaStackSendResult> DeleteRemoteSessionAsync(string sessionId)
        {
            var settings = await GetSettingsAsync();
            var result = new WhaStackSendResult();

            if (string.IsNullOrEmpty(settings.BaseUrl) || string.IsNullOrEmpty(settings.ApiKey))
            {
                result.Success = false;
                result.ErrorMessage = "إعدادات WhaStack غير مكتملة";
                return result;
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/sessions/{Uri.EscapeDataString(sessionId)}";

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

                using var response = await client.DeleteAsync(url);
                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                // 404 means already gone — treat as success for cleanup purposes
                result.Success = response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound;

                if (!result.Success)
                {
                    result.ErrorMessage = $"HTTP {result.StatusCode}: {result.ResponseBody}";
                    _logger.LogWarning("WhaStack delete session failed for {SessionId}: {Error}", sessionId, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error deleting WhaStack session {SessionId}", sessionId);
            }

            return result;
        }

        public async Task<WhaStackSendResult> ReconnectSessionAsync(string sessionId)
        {
            var settings = await GetSettingsAsync();
            var result = new WhaStackSendResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/sessions/{Uri.EscapeDataString(sessionId)}/reconnect";

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

                using var response = await client.PostAsync(url, null);

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;

                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"HTTP {result.StatusCode}: {result.ResponseBody}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error reconnecting WhaStack session {SessionId}", sessionId);
            }

            return result;
        }

        #endregion
    }
}
