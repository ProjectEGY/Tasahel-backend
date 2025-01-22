using PostexS.Models.Domain;
using PostexS.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Dtos
{
    public class OrderDto
    {
        public long Id { get; set; }
        public string OrderNumber { get; set; }
        public string AgentName { get; set; }
        public string date { get; set; }
        public double Cost { get; set; }
        public double DeliveryCost { get; set; }
        public double OrderCostWithoutDeliveryCost { get; set; }
        public string SenderName { get; set; }
        public string SenderNumber { get; set; }
        public string ReciverName { get; set; }
        public string ReciverNumber { get; set; }
        public OrderStatus Status { get; set; }
        public string Address { get; set; }
        public OrderDto(Order order)
        {
            var date = TimeZoneInfo.ConvertTimeFromUtc(order.CreateOn, TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time"));
            this.Id = order.Id;
            this.OrderNumber = order.Code;
            this.date = date.ToString("dddd MMM, yyyy");
            this.Cost = order.TotalCost;
            this.OrderCostWithoutDeliveryCost = order.Cost;
            this.ReciverName = order.ClientName;
            this.ReciverNumber = order.ClientPhone;
            this.Status = order.Status;
            this.Address = order.Address;
            this.DeliveryCost = order.DeliveryCost;
        }
    }
}
