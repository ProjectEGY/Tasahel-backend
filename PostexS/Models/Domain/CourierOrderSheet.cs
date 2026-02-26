using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostexS.Models.Domain
{
    public class CourierOrderSheet : BaseModel
    {
        public string CourierId { get; set; }
        [ForeignKey("CourierId")]
        public virtual ApplicationUser Courier { get; set; }

        public string CreatedById { get; set; }
        [ForeignKey("CreatedById")]
        public virtual ApplicationUser CreatedBy { get; set; }

        public int TotalOrders { get; set; }

        public virtual ICollection<CourierOrderSheetItem> Items { get; set; }
    }
}
