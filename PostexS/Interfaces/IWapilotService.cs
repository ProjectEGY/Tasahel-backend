using PostexS.Models.Domain;
using PostexS.Models.Enums;
using PostexS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PostexS.Interfaces
{
    public interface IWapilotService
    {
        // Settings Management
        Task<WapilotSettings> GetSettingsAsync();
        Task<bool> UpdateSettingsAsync(WapilotSettings settings, string updatedBy);

        // Queue Management
        Task<bool> EnqueueMessageAsync(string message, string chatId, long? orderId, string orderCode, string createdBy, string senderId = null, string senderName = null, int priority = 5);
        Task<bool> EnqueueOrderCompletionAsync(Order order, string createdBy);
        Task<bool> EnqueueBulkOrderCompletionAsync(IEnumerable<Order> orders, string createdBy);
        Task<WhatsAppMessageQueue> DequeueNextMessageAsync();
        Task<bool> UpdateQueueItemStatusAsync(long queueItemId, MessageQueueStatus status, string errorMessage = null);

        // Message Sending
        Task<WapilotSendResult> SendMessageAsync(string message, string chatId);
        Task<WapilotSendResult> SendTestMessageAsync(string phoneNumber, string message);
        Task<bool> ProcessQueueItemAsync(WhatsAppMessageQueue item);

        // Group Management
        Task<WapilotCreateGroupResult> CreateGroupAsync(string groupName, string clientPhone);
        Task<WapilotChatIdLookupResult> GetChatIdByPhoneAsync(string phoneNumber);
        Task<WapilotChatIdLookupResult> GetChatIdByLidAsync(string lid);

        // Logging
        Task<bool> LogRequestAsync(WhatsAppMessageLog log);
        Task<IEnumerable<WhatsAppMessageLog>> GetLogsAsync(int days = 30, int page = 1, int pageSize = 50);
        Task<int> GetLogsCountAsync(int days = 30);
        Task<bool> CleanupOldLogsAsync(int daysToKeep = 30);

        // Queue Monitoring
        Task<IEnumerable<WhatsAppMessageQueue>> GetPendingQueueAsync(int page = 1, int pageSize = 50);
        Task<IEnumerable<WhatsAppMessageQueue>> GetSentQueueAsync(int page = 1, int pageSize = 50);
        Task<IEnumerable<WhatsAppMessageQueue>> GetFailedQueueAsync(int page = 1, int pageSize = 50);
        Task<int> GetPendingCountAsync();
        Task<int> GetSentCountAsync();
        Task<int> GetFailedCountAsync();
        Task<QueueStatistics> GetQueueStatisticsAsync();

        // Message Formatting
        string FormatOrderCompletionMessage(Order order);
        string FormatOrderStatusUpdateMessage(Order order, string statusChangeNote = "");
        
        // Order Status Update Notifications
        Task<bool> EnqueueOrderStatusUpdateAsync(Order order, string updatedBy, string statusChangeNote = "");
    }

    public class WapilotSendResult
    {
        public bool Success { get; set; }
        public string ResponseBody { get; set; }
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; }
        public double DurationMs { get; set; }
    }

    public class WapilotCreateGroupResult
    {
        public bool Success { get; set; }
        public string GroupId { get; set; }
        public string ResponseBody { get; set; }
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class WapilotChatIdLookupResult
    {
        public bool Success { get; set; }
        public string ChatId { get; set; }
        public bool IsGroup { get; set; }
        public string ChatName { get; set; }
        public string ResponseBody { get; set; }
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}
