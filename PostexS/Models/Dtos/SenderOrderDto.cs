using PostexS.Models.Domain;
using PostexS.Models.Enums;
using System;

namespace PostexS.Models.Dtos
{
    public class SenderOrderDto
    {
        public string OrderCode { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverCode { get; set; }
        public string ReceiverPhone { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public double Cost { get; set; }
        public double DeliveryFees { get; set; }
        public double TotalCost { get; set; }
        public double ArrivedCost { get; set; }
        public OrderStatus Status { get; set; }
        public string StatusArabic { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string DeliveryAgentName { get; set; }
        public string DeliveryAgentPhone { get; set; }

        public SenderOrderDto() { }

        public SenderOrderDto(Order order, string deliveryName = null, string deliveryPhone = null)
        {
            OrderCode = order.Code;
            ReceiverName = order.ClientName;
            ReceiverCode = order.ClientCode;
            ReceiverPhone = order.ClientPhone;
            City = order.AddressCity;
            Address = order.Address;
            Cost = order.Cost;
            DeliveryFees = order.DeliveryFees;
            TotalCost = order.TotalCost;
            ArrivedCost = order.ArrivedCost;
            Status = order.Status;
            StatusArabic = GetStatusInArabic(order.Status);
            Notes = order.Notes;
            CreatedOn = order.CreateOn;
            LastUpdated = order.LastUpdated;
            DeliveryAgentName = deliveryName;
            DeliveryAgentPhone = deliveryPhone;
        }

        private static string GetStatusInArabic(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Placed => "جديد",
                OrderStatus.Assigned => "جارى التوصيل",
                OrderStatus.Delivered => "تم التوصيل",
                OrderStatus.Waiting => "مؤجل",
                OrderStatus.Rejected => "مرفوض",
                OrderStatus.Finished => "منتهي",
                OrderStatus.Completed => "تم تسويته",
                OrderStatus.PartialDelivered => "تم التوصيل جزئي",
                OrderStatus.Returned => "مرتجع كامل",
                OrderStatus.PartialReturned => "مرتجع جزئي",
                OrderStatus.Delivered_With_Edit_Price => "تم التوصيل مع تعديل السعر",
                OrderStatus.Returned_And_Paid_DeliveryCost => "مرتجع ودفع شحن",
                OrderStatus.Returned_And_DeliveryCost_On_Sender => "مرتجع وشحن على الراسل",
                _ => status.ToString()
            };
        }
    }
}
