using PostexS.Models.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PostexS.Interfaces
{
    public interface IWhaStackService
    {
        // Settings Management
        Task<WhaStackSettings> GetSettingsAsync();
        Task<bool> UpdateSettingsAsync(WhaStackSettings settings, string updatedBy);

        // Message Sending
        Task<WhaStackSendResult> SendMessageAsync(string phoneNumber, string message);
        Task<WhaStackSendResult> SendGroupMessageAsync(string groupId, string message);
        Task<WhaStackSendResult> SendImageAsync(string phoneNumber, string mediaUrl, string caption = null);
        Task<WhaStackSendResult> SendDocumentAsync(string phoneNumber, string mediaUrl, string fileName, string mimetype);
        Task<WhaStackSendResult> SendGroupImageAsync(string groupId, string mediaUrl, string caption = null);
        Task<WhaStackSendResult> SendGroupDocumentAsync(string groupId, string mediaUrl, string fileName);

        // Group Management
        Task<WhaStackGetGroupsResult> GetGroupsAsync();

        // Quota
        Task<WhaStackQuotaResult> GetQuotaAsync();

        // Sessions
        Task<WhaStackSessionsResult> GetSessionsAsync();
    }

    public class WhaStackSessionInfo
    {
        public string SessionId { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class WhaStackSessionsResult
    {
        public bool Success { get; set; }
        public List<WhaStackSessionInfo> Sessions { get; set; } = new List<WhaStackSessionInfo>();
        public string ResponseBody { get; set; }
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class WhaStackSendResult
    {
        public bool Success { get; set; }
        public string ResponseBody { get; set; }
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; }
        public double DurationMs { get; set; }
    }

    public class WhaStackGetGroupsResult
    {
        public bool Success { get; set; }
        public List<WhatsAppGroupInfo> Groups { get; set; } = new List<WhatsAppGroupInfo>();
        public string ResponseBody { get; set; }
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class WhaStackQuotaResult
    {
        public bool Success { get; set; }
        public int? TotalQuota { get; set; }
        public int? RemainingQuota { get; set; }
        public string ResponseBody { get; set; }
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}
