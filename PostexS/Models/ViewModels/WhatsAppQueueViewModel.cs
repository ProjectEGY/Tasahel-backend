using PostexS.Models.Domain;
using System.Collections.Generic;

namespace PostexS.Models.ViewModels
{
    public class WhatsAppQueueViewModel
    {
        public IEnumerable<WhatsAppMessageQueue> QueueItems { get; set; }
        public QueueStatistics Statistics { get; set; }
        public string Status { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }

    public class QueueStatistics
    {
        public int PendingCount { get; set; }
        public int ProcessingCount { get; set; }
        public int SentTodayCount { get; set; }
        public int FailedTodayCount { get; set; }
        public int TotalSentCount { get; set; }
    }

    public class WhatsAppLogsViewModel
    {
        public IEnumerable<WhatsAppMessageLog> Logs { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int Days { get; set; }
    }
}
