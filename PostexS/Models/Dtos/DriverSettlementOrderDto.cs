using PostexS.Models.Enums;

namespace PostexS.Models.Dtos
{
    public class DriverSettlementOrderDto
    {
        public string Code { get; set; }
        public string ClientName { get; set; }
        public string ClientPhone { get; set; }
        public string SenderName { get; set; }
        public double ArrivedCost { get; set; }
        public double DeliveryCost { get; set; }
        public double Cost { get; set; }
        public double TotalCost { get; set; }
        public OrderStatus Status { get; set; }
        public string StatusArabic { get; set; }
    }
}
