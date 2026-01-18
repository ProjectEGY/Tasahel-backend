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
using System.Text.Json;
using System.Threading.Tasks;

namespace PostexS.Services
{
    public class WhatsAppBotCloudService : IWhatsAppBotCloudService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WhatsAppBotCloudService> _logger;

        public WhatsAppBotCloudService(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<WhatsAppBotCloudService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        #region Settings Management

        public async Task<WhatsAppBotCloudSettings> GetSettingsAsync()
        {
            var settings = await _context.WhatsAppBotCloudSettings
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            return settings ?? new WhatsAppBotCloudSettings();
        }

        public async Task<bool> UpdateSettingsAsync(WhatsAppBotCloudSettings settings, string updatedBy)
        {
            var existing = await _context.WhatsAppBotCloudSettings
                .Where(s => !s.IsDeleted)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.BaseUrl = settings.BaseUrl;
                existing.InstanceId = settings.InstanceId;
                existing.AccessToken = settings.AccessToken;
                existing.MessageIntervalSeconds = settings.MessageIntervalSeconds;
                existing.IsActive = settings.IsActive;
                existing.LastUpdatedBy = updatedBy;
                existing.LastUpdatedAt = DateTime.UtcNow;
                existing.ModifiedOn = DateTime.UtcNow;
                existing.IsModified = true;

                _context.WhatsAppBotCloudSettings.Update(existing);
            }
            else
            {
                settings.LastUpdatedBy = updatedBy;
                settings.LastUpdatedAt = DateTime.UtcNow;
                settings.CreateOn = DateTime.UtcNow;
                await _context.WhatsAppBotCloudSettings.AddAsync(settings);
            }

            return await _context.SaveChangesAsync() > 0;
        }

        #endregion

        #region Group Management

        public async Task<WhatsAppBotCloudGetGroupsResult> GetGroupsAsync()
        {
            var settings = await GetSettingsAsync();
            var result = new WhatsAppBotCloudGetGroupsResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                // API endpoint: GET /get_groups?instance_id=XXX&access_token=XXX
                var url = $"{baseUrl}/get_groups?instance_id={Uri.EscapeDataString(settings.InstanceId)}&access_token={Uri.EscapeDataString(settings.AccessToken)}";

                _logger.LogInformation("Getting groups via {Url}", url);

                using var response = await client.GetAsync(url);

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var jsonResponse = JsonDocument.Parse(result.ResponseBody);

                        // Check response status first
                        if (jsonResponse.RootElement.TryGetProperty("status", out var statusElement))
                        {
                            var status = statusElement.GetString();
                            if (status != "success")
                            {
                                result.Success = false;
                                if (jsonResponse.RootElement.TryGetProperty("message", out var msgElement))
                                {
                                    result.ErrorMessage = msgElement.GetString();
                                }
                                return result;
                            }
                        }

                        // Parse groups array - API returns data in "data" property
                        JsonElement? groupsElement = null;
                        
                        // Try "data" property first (WhatsApp Bot Cloud format)
                        if (jsonResponse.RootElement.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
                        {
                            groupsElement = dataElement;
                        }
                        // Try "groups" property (alternative format)
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
                            _logger.LogWarning("No groups found in response. Response body: {Response}", result.ResponseBody);
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning(parseEx, "Could not parse groups from response: {Response}", result.ResponseBody);
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
                _logger.LogError(ex, "Error getting groups");
            }

            return result;
        }

        public async Task<WhatsAppBotCloudSendResult> SendGroupMessageAsync(string groupId, string message)
        {
            var settings = await GetSettingsAsync();
            var stopwatch = Stopwatch.StartNew();
            var result = new WhatsAppBotCloudSendResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                // API endpoint: POST /send_group
                var url = $"{baseUrl}/send_group";

                _logger.LogInformation("Sending WhatsApp message to group {GroupId} via {Url}", groupId, url);

                // JSON body according to API documentation
                var jsonBody = new
                {
                    group_id = groupId,
                    type = "text",
                    message = message,
                    instance_id = settings.InstanceId,
                    access_token = settings.AccessToken
                };

                using var jsonContent = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(jsonBody),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                using var response = await client.PostAsync(url, jsonContent);
                stopwatch.Stop();

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;
                result.DurationMs = stopwatch.ElapsedMilliseconds;

                _logger.LogInformation("WhatsApp Bot Cloud API response: Status={StatusCode}, Body={Body}", result.StatusCode, result.ResponseBody);

                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"HTTP {result.StatusCode}: {result.ResponseBody}";
                    _logger.LogWarning("WhatsApp Bot Cloud message failed: {Error}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                _logger.LogError(ex, "Error sending WhatsApp Bot Cloud message to group {GroupId}", groupId);
            }

            return result;
        }

        public async Task<WhatsAppBotCloudSendResult> SendMessageAsync(string phoneNumber, string message)
        {
            var settings = await GetSettingsAsync();
            var stopwatch = Stopwatch.StartNew();
            var result = new WhatsAppBotCloudSendResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                // API endpoint: POST /send
                var url = $"{baseUrl}/send";

                // Format phone number (remove + if present and any spaces)
                var formattedPhone = phoneNumber.TrimStart('+').Replace(" ", "").Replace("-", "");

                _logger.LogInformation("Sending WhatsApp message to {Phone} via {Url}", formattedPhone, url);

                // JSON body according to API documentation
                var jsonBody = new
                {
                    number = formattedPhone,
                    type = "text",
                    message = message,
                    instance_id = settings.InstanceId,
                    access_token = settings.AccessToken
                };

                using var jsonContent = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(jsonBody),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                using var response = await client.PostAsync(url, jsonContent);
                stopwatch.Stop();

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;
                result.DurationMs = stopwatch.ElapsedMilliseconds;

                _logger.LogInformation("WhatsApp Bot Cloud API response: Status={StatusCode}, Body={Body}", result.StatusCode, result.ResponseBody);

                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"HTTP {result.StatusCode}: {result.ResponseBody}";
                    _logger.LogWarning("WhatsApp Bot Cloud message failed: {Error}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                _logger.LogError(ex, "Error sending WhatsApp Bot Cloud message to {Phone}", phoneNumber);
            }

            return result;
        }

        #endregion
    }
}
