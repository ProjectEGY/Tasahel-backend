using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Domain
{
    public class LandingPageContent : BaseModel
    {
        public string SectionKey { get; set; }
        public string TitleAr { get; set; }
        public string SubTitleAr { get; set; }
        public string DescriptionAr { get; set; }
        public string ImageUrl { get; set; }
        public string IconClass { get; set; }
        public string LinkUrl { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
