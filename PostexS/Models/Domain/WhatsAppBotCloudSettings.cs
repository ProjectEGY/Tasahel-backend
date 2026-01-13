using System;
using System.ComponentModel.DataAnnotations;

namespace PostexS.Models.Domain
{
    public class WhatsAppBotCloudSettings : BaseModel
    {
        [Required]
        [Display(Name = "Base URL")]
        public string BaseUrl { get; set; } = "https://whatsbotcloud.com/api/";

        [Required]
        [Display(Name = "Instance ID")]
        public string InstanceId { get; set; }

        [Required]
        [Display(Name = "Access Token")]
        public string AccessToken { get; set; }

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
