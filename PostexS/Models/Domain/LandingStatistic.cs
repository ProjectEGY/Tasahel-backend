using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Domain
{
    public class LandingStatistic : BaseModel
    {
        public string Title { get; set; }
        public string Value { get; set; }
        public string IconClass { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
