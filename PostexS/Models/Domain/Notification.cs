using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Domain
{
    public class Notification :BaseModel
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsSeen { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }
}
