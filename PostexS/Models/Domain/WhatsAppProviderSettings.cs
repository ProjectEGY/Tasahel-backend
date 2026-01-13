using System;
using System.ComponentModel.DataAnnotations;
using PostexS.Models.Enums;

namespace PostexS.Models.Domain
{
    public class WhatsAppProviderSettings : BaseModel
    {
        [Display(Name = "Active Provider")]
        public WhatsAppProvider ActiveProvider { get; set; } = WhatsAppProvider.Wapilot;

        [Display(Name = "Last Updated By")]
        public string LastUpdatedBy { get; set; }

        public DateTime? LastUpdatedAt { get; set; }
    }
}
