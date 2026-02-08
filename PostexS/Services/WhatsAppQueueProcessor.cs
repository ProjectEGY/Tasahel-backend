using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using PostexS.Models.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PostexS.Services
{
    public class WhatsAppQueueProcessor : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WhatsAppQueueProcessor> _logger;
        private DateTime _lastCleanupDate = DateTime.MinValue;

        public WhatsAppQueueProcessor(
            IServiceProvider serviceProvider,
            ILogger<WhatsAppQueueProcessor> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WhatsApp Queue Processor started at: {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var wapilotService = scope.ServiceProvider.GetRequiredService<IWapilotService>();
                        var providerService = scope.ServiceProvider.GetRequiredService<IWhatsAppProviderService>();
                        var activeProvider = await providerService.GetActiveProviderAsync();

                        // Determine settings based on active provider
                        bool isActive = false;
                        int intervalSeconds = 60;

                        if (activeProvider == WhatsAppProvider.WhaStack)
                        {
                            var whaStackService = scope.ServiceProvider.GetRequiredService<IWhaStackService>();
                            var whaStackSettings = await whaStackService.GetSettingsAsync();
                            isActive = whaStackSettings != null && whaStackSettings.IsActive && !string.IsNullOrEmpty(whaStackSettings.SessionId);
                            intervalSeconds = whaStackSettings?.MessageIntervalSeconds ?? 60;
                        }
                        else
                        {
                            // Wapilot or WhatsAppBotCloud - use Wapilot settings
                            var settings = await wapilotService.GetSettingsAsync();
                            isActive = settings != null && settings.IsActive && !string.IsNullOrEmpty(settings.InstanceId);
                            intervalSeconds = settings?.MessageIntervalSeconds ?? 60;
                        }

                        if (isActive)
                        {
                            // Dequeue and process one message
                            var queueItem = await wapilotService.DequeueNextMessageAsync();

                            if (queueItem != null)
                            {
                                _logger.LogInformation("Processing queue item {QueueItemId} for order {OrderCode} via {Provider}", queueItem.Id, queueItem.OrderCode, activeProvider);

                                bool success;

                                if (activeProvider == WhatsAppProvider.WhaStack)
                                {
                                    success = await ProcessQueueItemViaWhaStack(scope, wapilotService, queueItem);
                                }
                                else
                                {
                                    success = await wapilotService.ProcessQueueItemAsync(queueItem);
                                }

                                if (success)
                                {
                                    _logger.LogInformation("Successfully sent message for order {OrderCode}", queueItem.OrderCode);
                                }
                                else
                                {
                                    _logger.LogWarning("Failed to send message for order {OrderCode}, retry count: {RetryCount}", queueItem.OrderCode, queueItem.RetryCount);
                                }
                            }

                            // Wait for configured interval before checking again
                            intervalSeconds = Math.Max(intervalSeconds, 5); // Minimum 5 seconds
                            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
                        }
                        else
                        {
                            // If not active or not configured, check again in 30 seconds
                            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                        }

                        // Daily cleanup of old logs (run once per day)
                        if (DateTime.UtcNow.Date > _lastCleanupDate)
                        {
                            try
                            {
                                _logger.LogInformation("Running daily cleanup of old WhatsApp logs");
                                await wapilotService.CleanupOldLogsAsync(30);
                                _lastCleanupDate = DateTime.UtcNow.Date;
                            }
                            catch (Exception cleanupEx)
                            {
                                _logger.LogError(cleanupEx, "Error during daily cleanup");
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in WhatsApp Queue Processor");

                    // Wait before retrying to avoid tight loop on errors
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }

            _logger.LogInformation("WhatsApp Queue Processor stopped at: {time}", DateTimeOffset.Now);
        }

        private async Task<bool> ProcessQueueItemViaWhaStack(IServiceScope scope, IWapilotService wapilotService, WhatsAppMessageQueue item)
        {
            var whaStackService = scope.ServiceProvider.GetRequiredService<IWhaStackService>();
            var whaStackSettings = await whaStackService.GetSettingsAsync();

            var result = await whaStackService.SendMessageAsync(item.ChatId, item.MessageContent);

            // Log the request using centralized logging
            var log = new WhatsAppMessageLog
            {
                QueueItemId = item.Id,
                RequestUrl = $"{whaStackSettings.BaseUrl}/messages/send",
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
            await wapilotService.LogRequestAsync(log);

            if (result.Success)
            {
                await wapilotService.UpdateQueueItemStatusAsync(item.Id, MessageQueueStatus.Sent);
                return true;
            }
            else
            {
                item.RetryCount++;
                if (item.RetryCount >= item.MaxRetries)
                {
                    await wapilotService.UpdateQueueItemStatusAsync(item.Id, MessageQueueStatus.Failed, result.ErrorMessage);
                }
                else
                {
                    // Reset to pending and schedule for retry with exponential backoff
                    await wapilotService.UpdateQueueItemStatusAsync(item.Id, MessageQueueStatus.Pending, result.ErrorMessage);
                }
                return false;
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WhatsApp Queue Processor is stopping");
            await base.StopAsync(stoppingToken);
        }
    }
}
