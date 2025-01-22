namespace PostexS.Models.Dtos
{
    public class OrderExcel
    {
        public string Name { get; set; }
        public string NameAr { get; set; }
        public string Address { get; set; }
        public string ClientName { get; set; }
        public string ClientPhone { get; set; }
        public double Cost { get; set; }
        public double DeliveryFees { get; set; }
        public double TotalCost { get; set; }
        public bool Pending { get; set; }
        public double ArrivedCost { get; set; }
        public double DeliveryCost { get; set; }
        public double ClietnCost { get; set; }
        public bool Finshed { get; set; }
    }
}
