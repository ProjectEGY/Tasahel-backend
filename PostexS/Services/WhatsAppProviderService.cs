using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PostexS.Interfaces;
using PostexS.Models.Data;
using PostexS.Models.Domain;
using PostexS.Models.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Services
{
    public class WhatsAppProviderService : IWhatsAppProviderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private const string SettingsCacheKey = "WhatsAppProviderSettings";

        public WhatsAppProviderService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<WhatsAppProviderSettings> GetProviderSettingsAsync()
        {
            if (_cache.TryGetValue(SettingsCacheKey, out WhatsAppProviderSettings cached))
                return cached;

            var settings = await _context.WhatsAppProviderSettings
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            settings = settings ?? new WhatsAppProviderSettings();
            _cache.Set(SettingsCacheKey, settings, TimeSpan.FromMinutes(5));
            return settings;
        }

        public async Task<bool> UpdateProviderAsync(WhatsAppProvider provider, string updatedBy)
        {
            var existing = await _context.WhatsAppProviderSettings
                .Where(s => !s.IsDeleted)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.ActiveProvider = provider;
                existing.LastUpdatedBy = updatedBy;
                existing.LastUpdatedAt = DateTime.UtcNow;
                existing.ModifiedOn = DateTime.UtcNow;
                existing.IsModified = true;

                _context.WhatsAppProviderSettings.Update(existing);
            }
            else
            {
                var newSettings = new WhatsAppProviderSettings
                {
                    ActiveProvider = provider,
                    LastUpdatedBy = updatedBy,
                    LastUpdatedAt = DateTime.UtcNow,
                    CreateOn = DateTime.UtcNow
                };
                await _context.WhatsAppProviderSettings.AddAsync(newSettings);
            }
            var wapilotSettings = await _context.WapilotSettings.Where(s => !s.IsDeleted).FirstOrDefaultAsync();
            if (wapilotSettings != null)
            {
                wapilotSettings.IsActive = (provider == WhatsAppProvider.Wapilot);
                wapilotSettings.ModifiedOn = DateTime.UtcNow;
                wapilotSettings.IsModified = true;
            }

            var botCloudSettings = await _context.WhatsAppBotCloudSettings.Where(s => !s.IsDeleted).FirstOrDefaultAsync();
            if (botCloudSettings != null)
            {
                botCloudSettings.IsActive = (provider == WhatsAppProvider.WhatsAppBotCloud);
                botCloudSettings.ModifiedOn = DateTime.UtcNow;
                botCloudSettings.IsModified = true;
            }

            var whaStackSettings = await _context.WhaStackSettings.Where(s => !s.IsDeleted).FirstOrDefaultAsync();
            if (whaStackSettings != null)
            {
                whaStackSettings.IsActive = (provider == WhatsAppProvider.WhaStack);
                whaStackSettings.ModifiedOn = DateTime.UtcNow;
                whaStackSettings.IsModified = true;
            }

            var result = await _context.SaveChangesAsync() > 0;
            if (result)
            {
                _cache.Remove(SettingsCacheKey);
                _cache.Remove("WhaStackSettings");
            }
            return result;
        }

        public async Task<WhatsAppProvider> GetActiveProviderAsync()
        {
            var settings = await GetProviderSettingsAsync();
            return settings.ActiveProvider;
        }
    }
}
