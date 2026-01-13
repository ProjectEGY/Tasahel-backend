using System;
using System.ComponentModel.DataAnnotations;

namespace PostexS.Models.Domain
{
    public class WapilotSettings : BaseModel
    {
        [Required]
        [Display(Name = "Base URL")]
        public string BaseUrl { get; set; } = "https://message-pro.com/api/v1/";

        [Required]
        [Display(Name = "Instance ID")]
        public string InstanceId { get; set; }

        [Required]
        [Display(Name = "API Token")]
        public string ApiToken { get; set; }

        // GroupChatId removed - each client now has their own WhatsappGroupId

        [Range(1, 3600)]
        [Display(Name = "Message Interval (seconds)")]
        public int MessageIntervalSeconds { get; set; } = 60;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Last Updated By")]
        public string LastUpdatedBy { get; set; }

        public DateTime? LastUpdatedAt { get; set; }
    }
}
