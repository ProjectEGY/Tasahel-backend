using PostexS.Models.Domain;
using PostexS.Models.Enums;

namespace PostexS.Models.ViewModels
{
    public class WhatsAppSettingsViewModel
    {
        public WapilotSettings WapilotSettings { get; set; } = new WapilotSettings();
        public WhatsAppBotCloudSettings WhatsAppBotCloudSettings { get; set; } = new WhatsAppBotCloudSettings();
        public WhatsAppProviderSettings ProviderSettings { get; set; } = new WhatsAppProviderSettings();
        public QueueStatistics Statistics { get; set; }
        public WhatsAppProvider ActiveProvider { get; set; } = WhatsAppProvider.Wapilot;
    }
}
