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

        // خيارات الأعمدة الاختيارية في Excel
        public bool showProductName { get; set; }
        public bool showSenderPhone { get; set; }
        public bool showSenderName { get; set; }
        public bool showOrderCost { get; set; }
        public bool showDeliveryFees { get; set; }
        public bool showClientCode { get; set; }
    }
}
