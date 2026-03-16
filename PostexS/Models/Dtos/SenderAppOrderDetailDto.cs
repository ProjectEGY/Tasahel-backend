using PostexS.Models.Domain;
using PostexS.Models.Enums;
using System;
using System.Collections.Generic;

namespace PostexS.Models.Dtos
{
    public class SenderAppOrderDetailDto : SenderOrderDto
    {
        public List<OrderTimelineEntry> Timeline { get; set; } = new List<OrderTimelineEntry>();
        public string BarcodeImageBase64 { get; set; }
        public string SenderName { get; set; }
        public string SenderPhone { get; set; }
        public string SenderCode { get; set; }
        public bool IsPrinted { get; set; }

        public SenderAppOrderDetailDto() { }

        public SenderAppOrderDetailDto(Order order, ApplicationUser sender, string deliveryName = null, string deliveryPhone = null)
            : base(order, deliveryName, deliveryPhone)
        {
            if (order.BarcodeImage != null)
                BarcodeImageBase64 = Convert.ToBase64String(order.BarcodeImage);

            IsPrinted = order.IsPrinted;

            // Respect hide settings
            SenderName = sender.HideSenderName ? null : sender.Name;
            SenderPhone = sender.HideSenderPhone ? null : sender.PhoneNumber;
            SenderCode = sender.HideSenderCode ? null : sender.Id;
        }
    }
}
