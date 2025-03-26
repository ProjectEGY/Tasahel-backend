using PostexS.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Domain
{
    public class OrderVM
    {
        public string? Notes { get; set; }
        public string ClientCity { get; set; }
        public string Address { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string ClientPhone { get; set; }
        public double Cost { get; set; } = 0;
        public double DeliveryFees { get; set; } = 0;
    }
}
