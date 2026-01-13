using PostexS.Models.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PostexS.Interfaces
{
    public interface IWhatsAppBotCloudService
    {
        // Settings Management
        Task<WhatsAppBotCloudSettings> GetSettingsAsync();
        Task<bool> UpdateSettingsAsync(WhatsAppBotCloudSettings settings, string updatedBy);

        // Group Management
        Task<WhatsAppBotCloudGetGroupsResult> GetGroupsAsync();
        Task<WhatsAppBotCloudSendResult> SendGroupMessageAsync(string groupId, string message);
        Task<WhatsAppBotCloudSendResult> SendMessageAsync(string phoneNumber, string message);
    }

    public class WhatsAppBotCloudGetGroupsResult
    {
        public bool Success { get; set; }
        public List<WhatsAppGroupInfo> Groups { get; set; } = new List<WhatsAppGroupInfo>();
        public string ResponseBody { get; set; }
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class WhatsAppGroupInfo
    {
        public string GroupId { get; set; }
        public string GroupName { get; set; }
        public string Description { get; set; }
    }

    public class WhatsAppBotCloudSendResult
    {
        public bool Success { get; set; }
        public string ResponseBody { get; set; }
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; }
        public double DurationMs { get; set; }
    }
}
