using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PostexS.Interfaces;
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

                        // Get settings to determine interval
                        var settings = await wapilotService.GetSettingsAsync();

                        if (settings != null && settings.IsActive && !string.IsNullOrEmpty(settings.InstanceId))
                        {
                            // Dequeue and process one message
                            var queueItem = await wapilotService.DequeueNextMessageAsync();

                            if (queueItem != null)
                            {
                                _logger.LogInformation("Processing queue item {QueueItemId} for order {OrderCode}", queueItem.Id, queueItem.OrderCode);

                                var success = await wapilotService.ProcessQueueItemAsync(queueItem);

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
                            var intervalSeconds = Math.Max(settings.MessageIntervalSeconds, 5); // Minimum 5 seconds
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

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WhatsApp Queue Processor is stopping");
            await base.StopAsync(stoppingToken);
        }
    }
}
