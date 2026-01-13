using PostexS.Models.Domain;
using PostexS.Models.Enums;
using System.Threading.Tasks;

namespace PostexS.Interfaces
{
    public interface IWhatsAppProviderService
    {
        Task<WhatsAppProviderSettings> GetProviderSettingsAsync();
        Task<bool> UpdateProviderAsync(WhatsAppProvider provider, string updatedBy);
        Task<WhatsAppProvider> GetActiveProviderAsync();
    }
}
