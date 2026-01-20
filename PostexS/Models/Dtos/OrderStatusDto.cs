using PostexS.Models.Enums;
using System;

namespace PostexS.Models.Dtos
{
    public class OrderStatusDto
    {
        public string OrderCode { get; set; }
        public OrderStatus Status { get; set; }
        public string StatusArabic { get; set; }
        public DateTime? LastUpdated { get; set; }

        public OrderStatusDto() { }

        public OrderStatusDto(string code, OrderStatus status, DateTime? lastUpdated)
        {
            OrderCode = code;
            Status = status;
            StatusArabic = GetStatusInArabic(status);
            LastUpdated = lastUpdated;
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
