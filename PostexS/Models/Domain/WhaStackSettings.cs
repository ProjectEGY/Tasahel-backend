using System;
using System.ComponentModel.DataAnnotations;

namespace PostexS.Models.Domain
{
    public class WhaStackSettings : BaseModel
    {
        [Required]
        [Display(Name = "Base URL")]
        public string BaseUrl { get; set; } = "https://whastackapi.com/api/v1";

        [Required]
        [Display(Name = "Session ID")]
        public string SessionId { get; set; }

        [Required]
        [Display(Name = "API Key")]
        public string ApiKey { get; set; }

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
