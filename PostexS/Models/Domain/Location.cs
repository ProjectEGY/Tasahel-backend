using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Domain
{
    public class Location : BaseModel
    {
        public string DeliveryId { get; set; }
        [ForeignKey("DeliveryId")]
        public virtual ApplicationUser Delivery { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Address { get; set; }
    }
}
