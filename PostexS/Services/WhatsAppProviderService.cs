using Microsoft.EntityFrameworkCore;
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

        public WhatsAppProviderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<WhatsAppProviderSettings> GetProviderSettingsAsync()
        {
            var settings = await _context.WhatsAppProviderSettings
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            return settings ?? new WhatsAppProviderSettings();
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

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<WhatsAppProvider> GetActiveProviderAsync()
        {
            var settings = await GetProviderSettingsAsync();
            return settings.ActiveProvider;
        }
    }
}
