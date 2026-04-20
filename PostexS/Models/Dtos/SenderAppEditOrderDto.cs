namespace PostexS.Models.Dtos
{
    public class SenderAppEditOrderDto
    {
        public string Notes { get; set; }
        public string ClientCity { get; set; }
        public string Address { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string ClientPhone { get; set; }
        public string ClientSecondaryPhone { get; set; }
        public double? Cost { get; set; }
        public double? DeliveryFees { get; set; }
    }
}
