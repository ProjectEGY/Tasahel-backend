using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Domain
{
    public class LandingTestimonial : BaseModel
    {
        public string ClientName { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public int Rating { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
