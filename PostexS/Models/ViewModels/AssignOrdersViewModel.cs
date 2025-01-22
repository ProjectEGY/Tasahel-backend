using PostexS.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.ViewModels
{
    public class AssignOrdersViewModel
    {
        public List<long> Orders { get; set; }
        public string UserId { get; set; }
        public bool print { get; set; }

    }
}
