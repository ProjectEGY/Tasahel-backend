using PostexS.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Dtos
{
    public class DriverSubmitOrderDto
    {
        public string Note { get; set; }
        public long OrderId { get; set; }
        public double Paid { get; set; }
        public OrderStatus Status { get; set; }
        public string? Image { get; set; }

    }
    public class DriverSubmitOrdersDto
    {
        public string DeliveryId { get; set; }
        public string Note { get; set; }
        public List<long> OrdersIds { get; set; }
        public OrderStatus Status { get; set; }

    }
    public class DriverNotesOrderDto
    {
        public string Note { get; set; }
        public long OrderId { get; set; }
    }
    public class NotesOrderDto
    {
        public string NewNote { get; set; }
        public long OrderId { get; set; }
    }
}
