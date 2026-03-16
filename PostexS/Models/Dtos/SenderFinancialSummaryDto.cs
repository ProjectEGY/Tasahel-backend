namespace PostexS.Models.Dtos
{
    public class SenderFinancialSummaryDto
    {
        public double WalletBalance { get; set; }
        public double TotalOrdersCost { get; set; }
        public double TotalDeliveryFees { get; set; }
        public double TotalCollected { get; set; }
        public int TotalOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int ReturnedOrders { get; set; }
        public int PendingOrders { get; set; }
    }
}
