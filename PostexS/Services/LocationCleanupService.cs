using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PostexS.Models.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PostexS.Services
{
    /// <summary>
    /// Background service يمسح جدول اللوكيشن بشكل دوري عشان الجدول ما يكبرش على الفاضي.
    /// بيشتغل مرة كل ساعة ويحذف أي لوكيشن عمره أكبر من RetentionDays (افتراضي 7 أيام).
    /// </summary>
    public class LocationCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LocationCleanupService> _logger;

        // كم يوم نحتفظ باللوكيشنز قبل الحذف التلقائي
        private const int RetentionDays = 7;
        // كل قد ايه يشتغل الـ cleanup (بالساعات)
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);

        public LocationCleanupService(
            IServiceProvider serviceProvider,
            ILogger<LocationCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Location Cleanup Service started at: {time}", DateTimeOffset.Now);

            // تأخير أول تشغيل دقيقة بعد بدء التطبيق
            try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
            catch (TaskCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);
                    var deleted = await context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM Locations WHERE CreateOn < {0}", cutoff);

                    if (deleted > 0)
                    {
                        _logger.LogInformation(
                            "LocationCleanup: حذف {Count} لوكيشن أقدم من {Cutoff}",
                            deleted, cutoff);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "LocationCleanup: فشل أثناء الحذف التلقائي");
                }

                try { await Task.Delay(CleanupInterval, stoppingToken); }
                catch (TaskCanceledException) { break; }
            }

            _logger.LogInformation("Location Cleanup Service stopped at: {time}", DateTimeOffset.Now);
        }
    }
}
