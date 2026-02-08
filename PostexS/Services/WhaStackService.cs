using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PostexS.Interfaces;
using PostexS.Models.Data;
using PostexS.Models.Domain;
using System;
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

        #region Message Sending

        public async Task<WhaStackSendResult> SendMessageAsync(string phoneNumber, string message)
        {
            var settings = await GetSettingsAsync();
            var stopwatch = Stopwatch.StartNew();
            var result = new WhaStackSendResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/messages/send";

                // Format phone number (remove + if present and any spaces)
                var formattedPhone = phoneNumber.TrimStart('+').Replace(" ", "").Replace("-", "");

                _logger.LogInformation("WhaStack: Sending message to {Phone} via {Url}", formattedPhone, url);

                var jsonBody = new
                {
                    session_id = settings.SessionId,
                    to = formattedPhone,
                    message = message
                };

                using var jsonContent = new StringContent(
                    JsonSerializer.Serialize(jsonBody),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

                using var response = await client.PostAsync(url, jsonContent);
                stopwatch.Stop();

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;
                result.DurationMs = stopwatch.ElapsedMilliseconds;

                _logger.LogInformation("WhaStack API response: Status={StatusCode}, Body={Body}", result.StatusCode, result.ResponseBody);

                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"HTTP {result.StatusCode}: {result.ResponseBody}";
                    _logger.LogWarning("WhaStack message failed: {Error}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                _logger.LogError(ex, "Error sending WhaStack message to {Phone}", phoneNumber);
            }

            return result;
        }

        public async Task<WhaStackSendResult> SendGroupMessageAsync(string groupId, string message)
        {
            var settings = await GetSettingsAsync();
            var stopwatch = Stopwatch.StartNew();
            var result = new WhaStackSendResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/groups/send";

                _logger.LogInformation("WhaStack: Sending message to group {GroupId} via {Url}", groupId, url);

                var jsonBody = new
                {
                    session_id = settings.SessionId,
                    group_id = groupId,
                    message = message
                };

                using var jsonContent = new StringContent(
                    JsonSerializer.Serialize(jsonBody),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

                using var response = await client.PostAsync(url, jsonContent);
                stopwatch.Stop();

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;
                result.DurationMs = stopwatch.ElapsedMilliseconds;

                _logger.LogInformation("WhaStack API response: Status={StatusCode}, Body={Body}", result.StatusCode, result.ResponseBody);

                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"HTTP {result.StatusCode}: {result.ResponseBody}";
                    _logger.LogWarning("WhaStack group message failed: {Error}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                _logger.LogError(ex, "Error sending WhaStack message to group {GroupId}", groupId);
            }

            return result;
        }

        public async Task<WhaStackSendResult> SendImageAsync(string phoneNumber, string mediaUrl, string caption = null)
        {
            var settings = await GetSettingsAsync();
            var stopwatch = Stopwatch.StartNew();
            var result = new WhaStackSendResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/messages/send-image";

                var formattedPhone = phoneNumber.TrimStart('+').Replace(" ", "").Replace("-", "");

                _logger.LogInformation("WhaStack: Sending image to {Phone} via {Url}", formattedPhone, url);

                var jsonBody = new
                {
                    session_id = settings.SessionId,
                    to = formattedPhone,
                    media_url = mediaUrl,
                    caption = caption ?? ""
                };

                using var jsonContent = new StringContent(
                    JsonSerializer.Serialize(jsonBody),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

                using var response = await client.PostAsync(url, jsonContent);
                stopwatch.Stop();

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;
                result.DurationMs = stopwatch.ElapsedMilliseconds;

                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"HTTP {result.StatusCode}: {result.ResponseBody}";
                    _logger.LogWarning("WhaStack send image failed: {Error}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                _logger.LogError(ex, "Error sending WhaStack image to {Phone}", phoneNumber);
            }

            return result;
        }

        public async Task<WhaStackSendResult> SendDocumentAsync(string phoneNumber, string mediaUrl, string fileName, string mimetype)
        {
            var settings = await GetSettingsAsync();
            var stopwatch = Stopwatch.StartNew();
            var result = new WhaStackSendResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/messages/send-document";

                var formattedPhone = phoneNumber.TrimStart('+').Replace(" ", "").Replace("-", "");

                _logger.LogInformation("WhaStack: Sending document to {Phone} via {Url}", formattedPhone, url);

                var jsonBody = new
                {
                    session_id = settings.SessionId,
                    to = formattedPhone,
                    media_url = mediaUrl,
                    file_name = fileName,
                    mimetype = mimetype
                };

                using var jsonContent = new StringContent(
                    JsonSerializer.Serialize(jsonBody),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

                using var response = await client.PostAsync(url, jsonContent);
                stopwatch.Stop();

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;
                result.DurationMs = stopwatch.ElapsedMilliseconds;

                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"HTTP {result.StatusCode}: {result.ResponseBody}";
                    _logger.LogWarning("WhaStack send document failed: {Error}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                _logger.LogError(ex, "Error sending WhaStack document to {Phone}", phoneNumber);
            }

            return result;
        }

        public async Task<WhaStackSendResult> SendGroupImageAsync(string groupId, string mediaUrl, string caption = null)
        {
            var settings = await GetSettingsAsync();
            var stopwatch = Stopwatch.StartNew();
            var result = new WhaStackSendResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/groups/send-image";

                _logger.LogInformation("WhaStack: Sending image to group {GroupId} via {Url}", groupId, url);

                var jsonBody = new
                {
                    session_id = settings.SessionId,
                    group_id = groupId,
                    media_url = mediaUrl,
                    caption = caption ?? ""
                };

                using var jsonContent = new StringContent(
                    JsonSerializer.Serialize(jsonBody),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

                using var response = await client.PostAsync(url, jsonContent);
                stopwatch.Stop();

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;
                result.DurationMs = stopwatch.ElapsedMilliseconds;

                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"HTTP {result.StatusCode}: {result.ResponseBody}";
                    _logger.LogWarning("WhaStack send group image failed: {Error}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                _logger.LogError(ex, "Error sending WhaStack image to group {GroupId}", groupId);
            }

            return result;
        }

        public async Task<WhaStackSendResult> SendGroupDocumentAsync(string groupId, string mediaUrl, string fileName)
        {
            var settings = await GetSettingsAsync();
            var stopwatch = Stopwatch.StartNew();
            var result = new WhaStackSendResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/groups/send-document";

                _logger.LogInformation("WhaStack: Sending document to group {GroupId} via {Url}", groupId, url);

                var jsonBody = new
                {
                    session_id = settings.SessionId,
                    group_id = groupId,
                    media_url = mediaUrl,
                    file_name = fileName
                };

                using var jsonContent = new StringContent(
                    JsonSerializer.Serialize(jsonBody),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

                using var response = await client.PostAsync(url, jsonContent);
                stopwatch.Stop();

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;
                result.DurationMs = stopwatch.ElapsedMilliseconds;

                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"HTTP {result.StatusCode}: {result.ResponseBody}";
                    _logger.LogWarning("WhaStack send group document failed: {Error}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                _logger.LogError(ex, "Error sending WhaStack document to group {GroupId}", groupId);
            }

            return result;
        }

        #endregion

        #region Group Management

        public async Task<WhaStackGetGroupsResult> GetGroupsAsync()
        {
            var settings = await GetSettingsAsync();
            var result = new WhaStackGetGroupsResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/groups?session_id={Uri.EscapeDataString(settings.SessionId)}";

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

                        // Try to parse groups from response
                        JsonElement? groupsElement = null;

                        // Try "data" property first
                        if (jsonResponse.RootElement.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
                        {
                            groupsElement = dataElement;
                        }
                        // Try "groups" property
                        else if (jsonResponse.RootElement.TryGetProperty("groups", out var groupsProp) && groupsProp.ValueKind == JsonValueKind.Array)
                        {
                            groupsElement = groupsProp;
                        }
                        // Response is directly an array
                        else if (jsonResponse.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            groupsElement = jsonResponse.RootElement;
                        }

                        if (groupsElement.HasValue)
                        {
                            foreach (var group in groupsElement.Value.EnumerateArray())
                            {
                                var groupInfo = new WhatsAppGroupInfo();

                                // Get Group ID
                                if (group.TryGetProperty("id", out var idElement))
                                {
                                    groupInfo.GroupId = idElement.GetString();
                                }
                                else if (group.TryGetProperty("group_id", out var groupIdElement))
                                {
                                    groupInfo.GroupId = groupIdElement.GetString();
                                }

                                // Get Group Name
                                if (group.TryGetProperty("name", out var nameElement))
                                {
                                    groupInfo.GroupName = nameElement.GetString();
                                }
                                else if (group.TryGetProperty("subject", out var subjectElement))
                                {
                                    groupInfo.GroupName = subjectElement.GetString();
                                }

                                // Get Description
                                if (group.TryGetProperty("description", out var descElement))
                                {
                                    groupInfo.Description = descElement.GetString();
                                }
                                else if (group.TryGetProperty("desc", out var descProp))
                                {
                                    groupInfo.Description = descProp.GetString();
                                }

                                if (!string.IsNullOrEmpty(groupInfo.GroupId))
                                {
                                    result.Groups.Add(groupInfo);
                                }
                            }
                        }

                        if (result.Groups.Count == 0)
                        {
                            result.ErrorMessage = "لم يتم العثور على جروبات في الاستجابة";
                            _logger.LogWarning("WhaStack: No groups found in response. Response body: {Response}", result.ResponseBody);
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning(parseEx, "WhaStack: Could not parse groups from response: {Response}", result.ResponseBody);
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

        #region Sessions

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
                _logger.LogInformation("WhaStack: Getting sessions via {Url}, ApiKey length={Length}, starts={Start}",
                    url, apiKey?.Length ?? 0, apiKey?.Length > 4 ? apiKey.Substring(0, 4) + "..." : "EMPTY");

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

                        // Try "data" property first
                        if (jsonResponse.RootElement.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
                        {
                            sessionsElement = dataElement;
                        }
                        // Try "sessions" property
                        else if (jsonResponse.RootElement.TryGetProperty("sessions", out var sessionsProp) && sessionsProp.ValueKind == JsonValueKind.Array)
                        {
                            sessionsElement = sessionsProp;
                        }
                        // Response is directly an array
                        else if (jsonResponse.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            sessionsElement = jsonResponse.RootElement;
                        }

                        if (sessionsElement.HasValue)
                        {
                            foreach (var session in sessionsElement.Value.EnumerateArray())
                            {
                                var sessionInfo = new WhaStackSessionInfo();

                                // Get Session ID
                                if (session.TryGetProperty("session_id", out var sidElement))
                                    sessionInfo.SessionId = sidElement.GetString();
                                else if (session.TryGetProperty("id", out var idElement))
                                    sessionInfo.SessionId = idElement.GetString();

                                // Get Name
                                if (session.TryGetProperty("name", out var nameElement))
                                    sessionInfo.Name = nameElement.GetString();
                                else if (session.TryGetProperty("session_name", out var snameElement))
                                    sessionInfo.Name = snameElement.GetString();

                                // Get Status
                                if (session.TryGetProperty("status", out var statusElement))
                                    sessionInfo.Status = statusElement.GetString();
                                else if (session.TryGetProperty("state", out var stateElement))
                                    sessionInfo.Status = stateElement.GetString();

                                // Get Phone Number
                                if (session.TryGetProperty("phone", out var phoneElement))
                                    sessionInfo.PhoneNumber = phoneElement.GetString();
                                else if (session.TryGetProperty("phone_number", out var pnElement))
                                    sessionInfo.PhoneNumber = pnElement.GetString();
                                else if (session.TryGetProperty("number", out var numElement))
                                    sessionInfo.PhoneNumber = numElement.GetString();

                                if (!string.IsNullOrEmpty(sessionInfo.SessionId))
                                {
                                    result.Sessions.Add(sessionInfo);
                                }
                            }
                        }

                        if (result.Sessions.Count == 0)
                        {
                            result.ErrorMessage = "لم يتم العثور على جلسات في الاستجابة";
                            _logger.LogWarning("WhaStack: No sessions found in response. Response body: {Response}", result.ResponseBody);
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning(parseEx, "WhaStack: Could not parse sessions from response: {Response}", result.ResponseBody);
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

                _logger.LogInformation("WhaStack: Getting quota via {Url}", url);

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
                        {
                            result.TotalQuota = totalElement.GetInt32();
                        }
                        if (jsonResponse.RootElement.TryGetProperty("remaining", out var remainingElement))
                        {
                            result.RemainingQuota = remainingElement.GetInt32();
                        }
                        // Try alternative property names
                        if (result.RemainingQuota == null && jsonResponse.RootElement.TryGetProperty("quota", out var quotaElement))
                        {
                            result.RemainingQuota = quotaElement.GetInt32();
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning(parseEx, "WhaStack: Could not parse quota from response: {Response}", result.ResponseBody);
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
    }

}
