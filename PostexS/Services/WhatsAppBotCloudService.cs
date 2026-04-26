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

        public async Task<WhatsAppBotCloudGetGroupsResult> GetGroupsAsync()
        {
            var settings = await GetSettingsAsync();
            var result = new WhatsAppBotCloudGetGroupsResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/get_groups?instance_id={Uri.EscapeDataString(settings.InstanceId ?? "")}&access_token={Uri.EscapeDataString(settings.AccessToken ?? "")}";

                using var response = await client.GetAsync(url);
                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var jsonResponse = JsonDocument.Parse(result.ResponseBody);

                        if (jsonResponse.RootElement.TryGetProperty("status", out var statusElement))
                        {
                            var status = statusElement.GetString();
                            if (status != "success")
                            {
                                result.Success = false;
                                if (jsonResponse.RootElement.TryGetProperty("message", out var msgElement))
                                    result.ErrorMessage = msgElement.GetString();
                                return result;
                            }
                        }

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
                                if (group.TryGetProperty("id", out var idElement)) groupInfo.GroupId = idElement.GetString();
                                else if (group.TryGetProperty("group_id", out var groupIdElement)) groupInfo.GroupId = groupIdElement.GetString();

                                if (group.TryGetProperty("name", out var nameElement)) groupInfo.GroupName = nameElement.GetString();
                                else if (group.TryGetProperty("subject", out var subjectElement)) groupInfo.GroupName = subjectElement.GetString();

                                if (group.TryGetProperty("description", out var descElement)) groupInfo.Description = descElement.GetString();
                                else if (group.TryGetProperty("desc", out var descProp)) groupInfo.Description = descProp.GetString();

                                if (!string.IsNullOrEmpty(groupInfo.GroupId)) result.Groups.Add(groupInfo);
                            }
                        }

                        if (result.Groups.Count == 0)
                            result.ErrorMessage = "لم يتم العثور على جروبات في الاستجابة";
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning(parseEx, "Could not parse groups");
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
            return await SendCoreAsync($"{settings.BaseUrl?.TrimEnd('/')}/send_group", new
            {
                group_id = groupId,
                type = "text",
                message,
                instance_id = settings.InstanceId,
                access_token = settings.AccessToken
            });
        }

        public async Task<WhatsAppBotCloudSendResult> SendMessageAsync(string phoneNumber, string message)
        {
            var settings = await GetSettingsAsync();
            var formattedPhone = (phoneNumber ?? "").TrimStart('+').Replace(" ", "").Replace("-", "");
            return await SendCoreAsync($"{settings.BaseUrl?.TrimEnd('/')}/send", new
            {
                number = formattedPhone,
                type = "text",
                message,
                instance_id = settings.InstanceId,
                access_token = settings.AccessToken
            });
        }

        private async Task<WhatsAppBotCloudSendResult> SendCoreAsync(string url, object jsonBody)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new WhatsAppBotCloudSendResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
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
                _logger.LogError(ex, "Error sending WhatsApp Bot Cloud message");
            }

            return result;
        }
    }
}
