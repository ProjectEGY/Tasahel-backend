using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PostexS.Models.Enums;

namespace PostexS.Models.Domain
{
    public class WhatsAppMessageQueue : BaseModel
    {
        [Required]
        public string MessageContent { get; set; }

        [Required]
        public string ChatId { get; set; }

        public long? OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        public string OrderCode { get; set; }

        public MessageQueueStatus Status { get; set; } = MessageQueueStatus.Pending;

        public int RetryCount { get; set; } = 0;

        public int MaxRetries { get; set; } = 3;

        public DateTime? ProcessedAt { get; set; }

        public DateTime? ScheduledFor { get; set; }

        public string ErrorMessage { get; set; }

        // Priority: 1 = High, 5 = Normal, 10 = Low
        public int Priority { get; set; } = 5;

        public string CreatedBy { get; set; }
        [ForeignKey("CreatedBy")]
        public virtual ApplicationUser CreatedByUser { get; set; }

        // Sender info for the message
        public string SenderId { get; set; }
        [ForeignKey("SenderId")]
        public virtual ApplicationUser Sender { get; set; }

        public string SenderName { get; set; }
    }
}
