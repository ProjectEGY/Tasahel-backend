using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostexS.Models.Domain
{
    public class WhatsAppMessageLog : BaseModel
    {
        public long? QueueItemId { get; set; }
        [ForeignKey("QueueItemId")]
        public virtual WhatsAppMessageQueue QueueItem { get; set; }

        [Required]
        public string RequestUrl { get; set; }

        public string RequestBody { get; set; }

        public string RequestHeaders { get; set; }

        public int? ResponseStatusCode { get; set; }

        public string ResponseBody { get; set; }

        public bool IsSuccess { get; set; }

        public string ErrorMessage { get; set; }

        public long? OrderId { get; set; }

        public string OrderCode { get; set; }

        public double RequestDurationMs { get; set; }

        public DateTime RequestedAt { get; set; }

        public DateTime? CompletedAt { get; set; }
    }
}
