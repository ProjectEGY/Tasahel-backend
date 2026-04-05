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
        public double DriverCommission { get; set; }
        public double Cost { get; set; }
        public double TotalCost { get; set; }
        public OrderStatus Status { get; set; }
        public string StatusArabic { get; set; }
    }

    public class DriverSettlementDetailsDto
    {
        public long Id { get; set; }
        public double Amount { get; set; }
        public double TotalDriverCommission { get; set; }
        public double TotalCollected { get; set; }
        public double TotalToCompany { get; set; }
        public System.DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public string SettledBy { get; set; }
        public string SettledAt { get; set; }
        public string Note { get; set; }

        // إحصائيات حسب الحالة
        public int DeliveredCount { get; set; }
        public int DeliveredWithEditPriceCount { get; set; }
        public int PartialDeliveredCount { get; set; }
        public int ReturnedCount { get; set; }
        public int ReturnedPaidDeliveryCount { get; set; }
        public int ReturnedOnSenderCount { get; set; }
        public int RejectedCount { get; set; }

        // ملخص الطلبات في القائمة
        public System.Collections.Generic.List<DriverSettlementOrderSummaryDto> OrdersSummary { get; set; }
    }

    public class DriverSettlementOrderSummaryDto
    {
        public string Code { get; set; }
        public string ClientName { get; set; }
        public double ArrivedCost { get; set; }
        public double DriverCommission { get; set; }
        public OrderStatus Status { get; set; }
        public string StatusArabic { get; set; }
    }
}
