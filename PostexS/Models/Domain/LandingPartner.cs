using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Domain
{
    public class LandingPartner : BaseModel
    {
        public string Name { get; set; }
        public string LogoUrl { get; set; }
        public string WebsiteUrl { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
