using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PostexS.Interfaces;
using PostexS.Models.Data;
using PostexS.Models.Domain;
using PostexS.Models.Enums;
using PostexS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PostexS.Services
{
    public class WapilotService : IWapilotService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WapilotService> _logger;
        private readonly IWhatsAppProviderService _providerService;
        private readonly IWhatsAppBotCloudService _whatsAppBotCloudService;

        public WapilotService(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<WapilotService> logger,
            IWhatsAppProviderService providerService,
            IWhatsAppBotCloudService whatsAppBotCloudService)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _providerService = providerService;
            _whatsAppBotCloudService = whatsAppBotCloudService;
        }

        #region Settings Management

        public async Task<WapilotSettings> GetSettingsAsync()
        {
            var settings = await _context.WapilotSettings
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            return settings ?? new WapilotSettings();
        }

        public async Task<bool> UpdateSettingsAsync(WapilotSettings settings, string updatedBy)
        {
            var existing = await _context.WapilotSettings
                .Where(s => !s.IsDeleted)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.BaseUrl = settings.BaseUrl;
                existing.InstanceId = settings.InstanceId;
                existing.ApiToken = settings.ApiToken;
                existing.MessageIntervalSeconds = settings.MessageIntervalSeconds;
                existing.IsActive = settings.IsActive;
                existing.LastUpdatedBy = updatedBy;
                existing.LastUpdatedAt = DateTime.UtcNow;
                existing.ModifiedOn = DateTime.UtcNow;
                existing.IsModified = true;

                _context.WapilotSettings.Update(existing);
            }
            else
            {
                settings.LastUpdatedBy = updatedBy;
                settings.LastUpdatedAt = DateTime.UtcNow;
                settings.CreateOn = DateTime.UtcNow;
                await _context.WapilotSettings.AddAsync(settings);
            }

            return await _context.SaveChangesAsync() > 0;
        }

        #endregion

        #region Queue Management

        public async Task<bool> EnqueueMessageAsync(string message, string chatId, long? orderId, string orderCode, string createdBy, string senderId = null, string senderName = null, int priority = 5)
        {
            var queueItem = new WhatsAppMessageQueue
            {
                MessageContent = message,
                ChatId = chatId,
                OrderId = orderId,
                OrderCode = orderCode,
                Status = MessageQueueStatus.Pending,
                Priority = priority,
                CreatedBy = createdBy,
                SenderId = senderId,
                SenderName = senderName,
                CreateOn = DateTime.UtcNow
            };

            await _context.WhatsAppMessageQueues.AddAsync(queueItem);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> EnqueueOrderCompletionAsync(Order order, string createdBy)
        {
            var settings = await GetSettingsAsync();
            if (settings == null || !settings.IsActive)
                return false;

            // Get client's WhatsApp group - must have a group to send
            string chatId = null;
            string senderName = order.Client?.Name ?? order.ClientName;

            if (!string.IsNullOrEmpty(order.ClientId))
            {
                var client = await _context.Users
                    .Where(u => u.Id == order.ClientId)
                    .Select(u => new { u.Name, u.WhatsappGroupId })
                    .FirstOrDefaultAsync();

                if (client != null)
                {
                    senderName = client.Name ?? senderName;
                    chatId = client.WhatsappGroupId;
                }
            }

            // Skip if no chat ID is available (client must have a group)
            if (string.IsNullOrEmpty(chatId))
                return false;

            var message = FormatOrderCompletionMessage(order);
            return await EnqueueMessageAsync(
                message,
                chatId,
                order.Id,
                order.Code,
                createdBy,
                order.ClientId,
                senderName
            );
        }

        public async Task<bool> EnqueueOrderStatusUpdateAsync(Order order, string updatedBy, string statusChangeNote = "")
        {
            // Load order with related entities if not already loaded
            if (order.Client == null || order.Branch == null || (order.Delivery == null && !string.IsNullOrEmpty(order.DeliveryId)))
            {
                order = await _context.Orders
                    .Include(o => o.Client)
                    .Include(o => o.Branch)
                    .Include(o => o.Delivery)
                        .ThenInclude(d => d != null ? d.Branch : null)
                    .FirstOrDefaultAsync(o => o.Id == order.Id);

                if (order == null)
                    return false;
            }

            // Get active provider
            var activeProvider = await _providerService.GetActiveProviderAsync();
            
            // Get client's WhatsApp group - must have a group to send
            string chatId = null;
            string senderName = order.Client?.Name ?? order.ClientName;

            if (!string.IsNullOrEmpty(order.ClientId))
            {
                var client = await _context.Users
                    .Where(u => u.Id == order.ClientId)
                    .Select(u => new { u.Name, u.WhatsappGroupId })
                    .FirstOrDefaultAsync();

                if (client != null)
                {
                    senderName = client.Name ?? senderName;
                    chatId = client.WhatsappGroupId;
                }
            }

            // Skip if no chat ID is available (client must have a group)
            if (string.IsNullOrEmpty(chatId))
                return false;

            var message = FormatOrderStatusUpdateMessage(order, statusChangeNote);

            // Use WhatsApp Bot Cloud if it's the active provider
            if (activeProvider == WhatsAppProvider.WhatsAppBotCloud)
            {
                var botCloudSettings = await _whatsAppBotCloudService.GetSettingsAsync();
                if (botCloudSettings != null && botCloudSettings.IsActive)
                {
                    // Send directly via WhatsApp Bot Cloud (for group messages)
                    var result = await _whatsAppBotCloudService.SendGroupMessageAsync(chatId, message);
                    return result.Success;
                }
            }

            // Default to Wapilot (enqueue for processing)
            return await EnqueueMessageAsync(
                message,
                chatId,
                order.Id,
                order.Code,
                updatedBy,
                order.ClientId,
                senderName
            );
        }

        public async Task<bool> EnqueueBulkOrderCompletionAsync(IEnumerable<Order> orders, string createdBy)
        {
            var settings = await GetSettingsAsync();
            if (settings == null || !settings.IsActive)
                return false;

            var queueItems = new List<WhatsAppMessageQueue>();

            // Get unique client IDs to fetch their WhatsApp Group IDs and phone numbers
            var clientIds = orders.Where(o => !string.IsNullOrEmpty(o.ClientId)).Select(o => o.ClientId).Distinct().ToList();
            var clients = await _context.Users
                .Where(u => clientIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            // Track clients that need group creation
            var clientsNeedingGroups = new Dictionary<string, ApplicationUser>();

            foreach (var order in orders)
            {
                // Use client's WhatsApp group if available, otherwise create one
                string chatId = null;
                string senderName = order.ClientName;

                if (!string.IsNullOrEmpty(order.ClientId) && clients.TryGetValue(order.ClientId, out var client))
                {
                    senderName = client.Name ?? order.ClientName;

                    // Use client's group if they have one
                    if (!string.IsNullOrEmpty(client.WhatsappGroupId))
                    {
                        chatId = client.WhatsappGroupId;
                    }
                    else if (!string.IsNullOrEmpty(client.WhatsappPhone) || !string.IsNullOrEmpty(client.PhoneNumber))
                    {
                        // Mark client for group creation (only once per client)
                        if (!clientsNeedingGroups.ContainsKey(client.Id))
                        {
                            clientsNeedingGroups[client.Id] = client;
                        }
                    }
                }

                // Skip if no chat ID - will be processed after group creation
                if (string.IsNullOrEmpty(chatId))
                    continue;

                var message = FormatOrderCompletionMessage(order);
                var queueItem = new WhatsAppMessageQueue
                {
                    MessageContent = message,
                    ChatId = chatId,
                    OrderId = order.Id,
                    OrderCode = order.Code,
                    Status = MessageQueueStatus.Pending,
                    Priority = 5,
                    CreatedBy = createdBy,
                    SenderId = order.ClientId,
                    SenderName = senderName,
                    CreateOn = DateTime.UtcNow
                };
                queueItems.Add(queueItem);
            }

            // Try to create groups for clients without one, fallback to direct message if fails
            foreach (var kvp in clientsNeedingGroups)
            {
                var client = kvp.Value;
                var clientPhone = client.WhatsappPhone ?? client.PhoneNumber;

                if (string.IsNullOrEmpty(clientPhone))
                    continue;

                var groupName = $"تسهيل اكسبريس - {client.Name}";
                var createResult = await CreateGroupAsync(groupName, clientPhone);

                if (createResult.Success && !string.IsNullOrEmpty(createResult.GroupId))
                {
                    // Update client's WhatsappGroupId in database
                    var dbClient = await _context.Users.FindAsync(client.Id);
                    if (dbClient != null)
                    {
                        dbClient.WhatsappGroupId = createResult.GroupId;
                        await _context.SaveChangesAsync();

                        // Update our local reference
                        client.WhatsappGroupId = createResult.GroupId;

                        _logger.LogInformation("Created WhatsApp group {GroupId} for client {ClientName}", createResult.GroupId, client.Name);
                    }
                }
                else
                {
                    // Group creation failed - use phone number directly for messaging
                    _logger.LogWarning("Failed to create WhatsApp group for client {ClientName}: {Error}. Will send to phone directly.", client.Name, createResult.ErrorMessage);
                }
            }

            // Now queue messages for clients that needed groups (send to group if created, otherwise to phone)
            foreach (var order in orders)
            {
                if (string.IsNullOrEmpty(order.ClientId) || !clientsNeedingGroups.ContainsKey(order.ClientId))
                    continue;

                var client = clientsNeedingGroups[order.ClientId];
                var clientPhone = client.WhatsappPhone ?? client.PhoneNumber;

                // Use group ID if available, otherwise use phone number directly
                var chatId = !string.IsNullOrEmpty(client.WhatsappGroupId)
                    ? client.WhatsappGroupId
                    : clientPhone?.TrimStart('+');

                if (string.IsNullOrEmpty(chatId))
                    continue; // No way to contact this client

                var message = FormatOrderCompletionMessage(order);
                var queueItem = new WhatsAppMessageQueue
                {
                    MessageContent = message,
                    ChatId = chatId,
                    OrderId = order.Id,
                    OrderCode = order.Code,
                    Status = MessageQueueStatus.Pending,
                    Priority = 5,
                    CreatedBy = createdBy,
                    SenderId = order.ClientId,
                    SenderName = client.Name ?? order.ClientName,
                    CreateOn = DateTime.UtcNow
                };
                queueItems.Add(queueItem);
            }

            if (queueItems.Any())
            {
                await _context.WhatsAppMessageQueues.AddRangeAsync(queueItems);
                return await _context.SaveChangesAsync() > 0;
            }

            return true;
        }

        public async Task<WhatsAppMessageQueue> DequeueNextMessageAsync()
        {
            var nextItem = await _context.WhatsAppMessageQueues
                .Where(q => q.Status == MessageQueueStatus.Pending && !q.IsDeleted)
                .Where(q => q.ScheduledFor == null || q.ScheduledFor <= DateTime.UtcNow)
                .OrderBy(q => q.Priority)
                .ThenBy(q => q.CreateOn)
                .FirstOrDefaultAsync();

            if (nextItem != null)
            {
                nextItem.Status = MessageQueueStatus.Processing;
                await _context.SaveChangesAsync();
            }

            return nextItem;
        }

        public async Task<bool> UpdateQueueItemStatusAsync(long queueItemId, MessageQueueStatus status, string errorMessage = null)
        {
            var item = await _context.WhatsAppMessageQueues.FindAsync(queueItemId);
            if (item == null) return false;

            item.Status = status;
            if (status == MessageQueueStatus.Sent || status == MessageQueueStatus.Failed)
            {
                item.ProcessedAt = DateTime.UtcNow;
            }
            if (!string.IsNullOrEmpty(errorMessage))
            {
                item.ErrorMessage = errorMessage;
            }

            return await _context.SaveChangesAsync() > 0;
        }

        #endregion

        #region Message Sending

        public async Task<WapilotSendResult> SendMessageAsync(string message, string chatId)
        {
            var settings = await GetSettingsAsync();
            var stopwatch = Stopwatch.StartNew();
            var result = new WapilotSendResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/{settings.InstanceId}/send-message";

                _logger.LogInformation("Sending WhatsApp message to {ChatId} via {Url}", chatId, url);

                using var formContent = new MultipartFormDataContent();
                formContent.Add(new StringContent(message), "text");
                formContent.Add(new StringContent(chatId), "chat_id");
                formContent.Add(new StringContent(settings.ApiToken), "token");
                formContent.Add(new StringContent("5"), "priority");

                client.DefaultRequestHeaders.Add("token", settings.ApiToken);

                using var response = await client.PostAsync(url, formContent);
                stopwatch.Stop();

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;
                result.DurationMs = stopwatch.ElapsedMilliseconds;

                _logger.LogInformation("WhatsApp API response: Status={StatusCode}, Body={Body}", result.StatusCode, result.ResponseBody);

                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"HTTP {result.StatusCode}: {result.ResponseBody}";
                    _logger.LogWarning("WhatsApp message failed: {Error}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                _logger.LogError(ex, "Error sending WhatsApp message to {ChatId}", chatId);
            }

            return result;
        }

        public async Task<WapilotSendResult> SendTestMessageAsync(string phoneNumber, string message)
        {
            // Format phone number (remove + if present)
            var formattedPhone = phoneNumber.TrimStart('+');
            return await SendMessageAsync(message, formattedPhone);
        }

        #endregion

        #region Group Management

        public async Task<WapilotCreateGroupResult> CreateGroupAsync(string groupName, string clientPhone)
        {
            var settings = await GetSettingsAsync();
            var result = new WapilotCreateGroupResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                var url = $"{baseUrl}/{settings.InstanceId}/create-group";

                // Format phone number (remove + if present)
                var formattedPhone = clientPhone.TrimStart('+');

                using var formContent = new MultipartFormDataContent();
                formContent.Add(new StringContent(groupName), "subject");
                formContent.Add(new StringContent(formattedPhone), "participants");
                formContent.Add(new StringContent(settings.ApiToken), "token");

                client.DefaultRequestHeaders.Add("token", settings.ApiToken);

                using var response = await client.PostAsync(url, formContent);

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;

                if (response.IsSuccessStatusCode)
                {
                    // Parse the response to get the group ID
                    // Expected response: {"status":"success","group_id":"120363XXXXXXXXXX@g.us"} or similar
                    try
                    {
                        var jsonResponse = System.Text.Json.JsonDocument.Parse(result.ResponseBody);
                        if (jsonResponse.RootElement.TryGetProperty("group_id", out var groupIdElement))
                        {
                            result.GroupId = groupIdElement.GetString();
                        }
                        else if (jsonResponse.RootElement.TryGetProperty("id", out var idElement))
                        {
                            result.GroupId = idElement.GetString();
                        }
                        else if (jsonResponse.RootElement.TryGetProperty("gid", out var gidElement))
                        {
                            result.GroupId = gidElement.GetString();
                        }
                    }
                    catch
                    {
                        // If JSON parsing fails, try to extract group ID from response
                        _logger.LogWarning("Could not parse group ID from response: {Response}", result.ResponseBody);
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
                _logger.LogError(ex, "Error creating WhatsApp group for {ClientPhone}", clientPhone);
            }

            return result;
        }

        public async Task<WapilotChatIdLookupResult> GetChatIdByPhoneAsync(string phoneNumber)
        {
            var settings = await GetSettingsAsync();
            var result = new WapilotChatIdLookupResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                // API endpoint: /chat-id-lookup/lids/by-phone
                var url = $"{baseUrl}/{settings.InstanceId}/chat-id-lookup/lids/by-phone";

                // Format phone number (remove + if present)
                var formattedPhone = phoneNumber.TrimStart('+');

                _logger.LogInformation("Looking up Chat ID by phone: {Phone} via {Url}", formattedPhone, url);

                using var formContent = new MultipartFormDataContent();
                formContent.Add(new StringContent(formattedPhone), "phone");
                formContent.Add(new StringContent(settings.ApiToken), "token");

                client.DefaultRequestHeaders.Add("token", settings.ApiToken);

                using var response = await client.PostAsync(url, formContent);

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var jsonResponse = System.Text.Json.JsonDocument.Parse(result.ResponseBody);

                        // Try different property names for chat ID
                        if (jsonResponse.RootElement.TryGetProperty("chat_id", out var chatIdElement))
                        {
                            result.ChatId = chatIdElement.GetString();
                        }
                        else if (jsonResponse.RootElement.TryGetProperty("id", out var idElement))
                        {
                            result.ChatId = idElement.GetString();
                        }
                        else if (jsonResponse.RootElement.TryGetProperty("lid", out var lidElement))
                        {
                            result.ChatId = lidElement.GetString();
                        }

                        // Check if it's a group (ends with @g.us)
                        if (!string.IsNullOrEmpty(result.ChatId))
                        {
                            result.IsGroup = result.ChatId.EndsWith("@g.us");
                        }

                        // Try to get chat name
                        if (jsonResponse.RootElement.TryGetProperty("name", out var nameElement))
                        {
                            result.ChatName = nameElement.GetString();
                        }
                        else if (jsonResponse.RootElement.TryGetProperty("subject", out var subjectElement))
                        {
                            result.ChatName = subjectElement.GetString();
                        }

                        if (string.IsNullOrEmpty(result.ChatId))
                        {
                            result.Success = false;
                            result.ErrorMessage = "لم يتم العثور على Chat ID في الاستجابة";
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning(parseEx, "Could not parse chat ID from response: {Response}", result.ResponseBody);
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
                _logger.LogError(ex, "Error looking up Chat ID by phone: {Phone}", phoneNumber);
            }

            return result;
        }

        public async Task<WapilotChatIdLookupResult> GetChatIdByLidAsync(string lid)
        {
            var settings = await GetSettingsAsync();
            var result = new WapilotChatIdLookupResult();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var baseUrl = settings.BaseUrl.TrimEnd('/');
                // API endpoint: /chat-id-lookup/lids/by-lid
                var url = $"{baseUrl}/{settings.InstanceId}/chat-id-lookup/lids/by-lid";

                _logger.LogInformation("Looking up Chat ID by LID: {Lid} via {Url}", lid, url);

                using var formContent = new MultipartFormDataContent();
                formContent.Add(new StringContent(lid), "lid");
                formContent.Add(new StringContent(settings.ApiToken), "token");

                client.DefaultRequestHeaders.Add("token", settings.ApiToken);

                using var response = await client.PostAsync(url, formContent);

                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = await response.Content.ReadAsStringAsync();
                result.Success = response.IsSuccessStatusCode;

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var jsonResponse = System.Text.Json.JsonDocument.Parse(result.ResponseBody);

                        // Try different property names for chat ID
                        if (jsonResponse.RootElement.TryGetProperty("chat_id", out var chatIdElement))
                        {
                            result.ChatId = chatIdElement.GetString();
                        }
                        else if (jsonResponse.RootElement.TryGetProperty("id", out var idElement))
                        {
                            result.ChatId = idElement.GetString();
                        }
                        else if (jsonResponse.RootElement.TryGetProperty("lid", out var lidElement))
                        {
                            result.ChatId = lidElement.GetString();
                        }

                        // Check if it's a group (ends with @g.us)
                        if (!string.IsNullOrEmpty(result.ChatId))
                        {
                            result.IsGroup = result.ChatId.EndsWith("@g.us");
                        }

                        // Try to get chat name
                        if (jsonResponse.RootElement.TryGetProperty("name", out var nameElement))
                        {
                            result.ChatName = nameElement.GetString();
                        }
                        else if (jsonResponse.RootElement.TryGetProperty("subject", out var subjectElement))
                        {
                            result.ChatName = subjectElement.GetString();
                        }

                        if (string.IsNullOrEmpty(result.ChatId))
                        {
                            result.Success = false;
                            result.ErrorMessage = "لم يتم العثور على Chat ID في الاستجابة";
                        }
                    }
                    catch (Exception parseEx)
                    {
                        _logger.LogWarning(parseEx, "Could not parse chat ID from response: {Response}", result.ResponseBody);
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
                _logger.LogError(ex, "Error looking up Chat ID by LID: {Lid}", lid);
            }

            return result;
        }

        #endregion

        #region Queue Processing

        public async Task<bool> ProcessQueueItemAsync(WhatsAppMessageQueue item)
        {
            var settings = await GetSettingsAsync();
            var result = await SendMessageAsync(item.MessageContent, item.ChatId);

            // Log the request
            var log = new WhatsAppMessageLog
            {
                QueueItemId = item.Id,
                RequestUrl = $"{settings.BaseUrl}/{settings.InstanceId}/send-message",
                RequestBody = $"chat_id={item.ChatId}, text={item.MessageContent.Substring(0, Math.Min(100, item.MessageContent.Length))}...",
                ResponseStatusCode = result.StatusCode,
                ResponseBody = result.ResponseBody,
                IsSuccess = result.Success,
                ErrorMessage = result.ErrorMessage,
                OrderId = item.OrderId,
                OrderCode = item.OrderCode,
                RequestDurationMs = result.DurationMs,
                RequestedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                CreateOn = DateTime.UtcNow
            };
            await LogRequestAsync(log);

            if (result.Success)
            {
                await UpdateQueueItemStatusAsync(item.Id, MessageQueueStatus.Sent);
                return true;
            }
            else
            {
                item.RetryCount++;
                if (item.RetryCount >= item.MaxRetries)
                {
                    await UpdateQueueItemStatusAsync(item.Id, MessageQueueStatus.Failed, result.ErrorMessage);
                }
                else
                {
                    // Reset to pending and schedule for retry with exponential backoff
                    item.Status = MessageQueueStatus.Pending;
                    item.ScheduledFor = DateTime.UtcNow.AddMinutes(item.RetryCount * 2);
                    item.ErrorMessage = result.ErrorMessage;
                    await _context.SaveChangesAsync();
                }
                return false;
            }
        }

        #endregion

        #region Logging

        public async Task<bool> LogRequestAsync(WhatsAppMessageLog log)
        {
            await _context.WhatsAppMessageLogs.AddAsync(log);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<WhatsAppMessageLog>> GetLogsAsync(int days = 30, int page = 1, int pageSize = 50)
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);
            return await _context.WhatsAppMessageLogs
                .Where(l => l.CreateOn >= fromDate)
                .OrderByDescending(l => l.CreateOn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetLogsCountAsync(int days = 30)
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);
            return await _context.WhatsAppMessageLogs
                .Where(l => l.CreateOn >= fromDate)
                .CountAsync();
        }

        public async Task<bool> CleanupOldLogsAsync(int daysToKeep = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            const int batchSize = 500;

            // Delete old logs in batches to avoid loading all into memory
            int deletedLogs;
            do
            {
                var oldLogIds = await _context.WhatsAppMessageLogs
                    .Where(l => l.CreateOn < cutoffDate)
                    .Select(l => l.Id)
                    .Take(batchSize)
                    .ToListAsync();

                deletedLogs = oldLogIds.Count;
                if (deletedLogs > 0)
                {
                    var logsToDelete = await _context.WhatsAppMessageLogs
                        .Where(l => oldLogIds.Contains(l.Id))
                        .ToListAsync();
                    _context.WhatsAppMessageLogs.RemoveRange(logsToDelete);
                    await _context.SaveChangesAsync();
                }
            } while (deletedLogs == batchSize);

            // Also clean up old sent/failed queue items in batches
            int deletedQueueItems;
            do
            {
                var oldQueueIds = await _context.WhatsAppMessageQueues
                    .Where(q => q.ProcessedAt != null && q.ProcessedAt < cutoffDate)
                    .Where(q => q.Status == MessageQueueStatus.Sent || q.Status == MessageQueueStatus.Failed)
                    .Select(q => q.Id)
                    .Take(batchSize)
                    .ToListAsync();

                deletedQueueItems = oldQueueIds.Count;
                if (deletedQueueItems > 0)
                {
                    var queueItemsToDelete = await _context.WhatsAppMessageQueues
                        .Where(q => oldQueueIds.Contains(q.Id))
                        .ToListAsync();
                    _context.WhatsAppMessageQueues.RemoveRange(queueItemsToDelete);
                    await _context.SaveChangesAsync();
                }
            } while (deletedQueueItems == batchSize);

            return true;
        }

        #endregion

        #region Queue Monitoring

        public async Task<IEnumerable<WhatsAppMessageQueue>> GetPendingQueueAsync(int page = 1, int pageSize = 50)
        {
            return await _context.WhatsAppMessageQueues
                .Where(q => (q.Status == MessageQueueStatus.Pending || q.Status == MessageQueueStatus.Processing) && !q.IsDeleted)
                .OrderBy(q => q.Priority)
                .ThenBy(q => q.CreateOn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(q => q.Order)
                .Include(q => q.Sender)
                .ToListAsync();
        }

        public async Task<IEnumerable<WhatsAppMessageQueue>> GetSentQueueAsync(int page = 1, int pageSize = 50)
        {
            return await _context.WhatsAppMessageQueues
                .Where(q => q.Status == MessageQueueStatus.Sent && !q.IsDeleted)
                .OrderByDescending(q => q.ProcessedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(q => q.Order)
                .Include(q => q.Sender)
                .ToListAsync();
        }

        public async Task<IEnumerable<WhatsAppMessageQueue>> GetFailedQueueAsync(int page = 1, int pageSize = 50)
        {
            return await _context.WhatsAppMessageQueues
                .Where(q => q.Status == MessageQueueStatus.Failed && !q.IsDeleted)
                .OrderByDescending(q => q.ProcessedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(q => q.Order)
                .Include(q => q.Sender)
                .ToListAsync();
        }

        public async Task<int> GetPendingCountAsync()
        {
            return await _context.WhatsAppMessageQueues
                .CountAsync(q => (q.Status == MessageQueueStatus.Pending || q.Status == MessageQueueStatus.Processing) && !q.IsDeleted);
        }

        public async Task<int> GetSentCountAsync()
        {
            return await _context.WhatsAppMessageQueues
                .CountAsync(q => q.Status == MessageQueueStatus.Sent && !q.IsDeleted);
        }

        public async Task<int> GetFailedCountAsync()
        {
            return await _context.WhatsAppMessageQueues
                .CountAsync(q => q.Status == MessageQueueStatus.Failed && !q.IsDeleted);
        }

        public async Task<QueueStatistics> GetQueueStatisticsAsync()
        {
            var today = DateTime.UtcNow.Date;

            return new QueueStatistics
            {
                PendingCount = await _context.WhatsAppMessageQueues
                    .CountAsync(q => q.Status == MessageQueueStatus.Pending && !q.IsDeleted),
                ProcessingCount = await _context.WhatsAppMessageQueues
                    .CountAsync(q => q.Status == MessageQueueStatus.Processing && !q.IsDeleted),
                SentTodayCount = await _context.WhatsAppMessageQueues
                    .CountAsync(q => q.Status == MessageQueueStatus.Sent && q.ProcessedAt >= today && !q.IsDeleted),
                FailedTodayCount = await _context.WhatsAppMessageQueues
                    .CountAsync(q => q.Status == MessageQueueStatus.Failed && q.ProcessedAt >= today && !q.IsDeleted),
                TotalSentCount = await _context.WhatsAppMessageQueues
                    .CountAsync(q => q.Status == MessageQueueStatus.Sent && !q.IsDeleted)
            };
        }

        #endregion

        #region Message Formatting

        public string FormatOrderCompletionMessage(Order order)
        {
            var statusArabic = GetStatusInArabic(order.Status);
            var senderName = order.Client?.Name ?? order.ClientName ?? "غير محدد";

            var message = $@"تم تسوية طلب
كود الطلب: {order.Code}
الراسل: {senderName}
اسم العميل: {order.ClientName}
الحالة: {statusArabic}
المبلغ المستحق: {order.ArrivedCost:N2} جنيه
نسبة التوصيل: {order.DeliveryCost:N2} جنيه
صافي الراسل: {order.ClientCost:N2} جنيه";

            return message;
        }

        public string FormatOrderStatusUpdateMessage(Order order, string statusChangeNote = "")
        {
            // Load related data if not already loaded
            var senderName = order.Client?.Name ?? "غير محدد";
            var senderPhone = order.Client?.PhoneNumber ?? "";
            var branchName = order.Branch?.Name ?? "غير محدد";
            var deliveryName = order.Delivery?.Name ?? "غير محدد";
            var deliveryPhone = order.Delivery?.PhoneNumber ?? "";
            var deliveryBranch = order.Delivery?.Branch?.Name ?? "";
            var statusArabic = GetStatusInArabic(order.Status);
            
            var address = string.IsNullOrEmpty(order.AddressCity) 
                ? order.Address 
                : $"{order.AddressCity} - {order.Address}";
            
            var deliveryInfo = string.IsNullOrEmpty(deliveryPhone)
                ? deliveryName
                : $"{deliveryName} : {deliveryPhone}";
            
            if (!string.IsNullOrEmpty(deliveryBranch))
            {
                deliveryInfo = $"{deliveryInfo} - {deliveryBranch}";
            }

            var merchantInfo = senderName;
            if (!string.IsNullOrEmpty(senderPhone))
            {
                merchantInfo = $"{merchantInfo} - {senderPhone}";
            }

            var message = $@"{statusChangeNote}

رقم الشحنة : {order.Code}
اسم العميل : {order.ClientName ?? "غير محدد"}
رقم هاتف العميل : {order.ClientPhone ?? "غير محدد"}
العنوان : {address ?? "غير محدد"}
المندوب : {deliveryInfo}
الفرع : {branchName}
الاجمالي : {order.TotalCost:N0} جنية
التاجر : {merchantInfo}";

            if (!string.IsNullOrEmpty(order.Notes))
            {
                message += $"\nملاحظات : {order.Notes}";
            }

            return message;
        }

        private string GetStatusInArabic(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Placed => "جديد",
                OrderStatus.Assigned => "جارى التوصيل",
                OrderStatus.Delivered => "تم التوصيل",
                OrderStatus.Waiting => "مؤجل",
                OrderStatus.Rejected => "مرفوض",
                OrderStatus.Finished => "منتهي",
                OrderStatus.Completed => "تم تسويته",
                OrderStatus.PartialDelivered => "تم التوصيل جزئي",
                OrderStatus.Returned => "مرتجع كامل",
                OrderStatus.PartialReturned => "مرتجع جزئي",
                OrderStatus.Delivered_With_Edit_Price => "تم التوصيل مع تعديل السعر",
                OrderStatus.Returned_And_Paid_DeliveryCost => "مرتجع ودفع شحن",
                OrderStatus.Returned_And_DeliveryCost_On_Sender => "مرتجع وشحن على الراسل",
                _ => status.ToString()
            };
        }

        #endregion
    }
}
