using PostexS.Models.Domain;

namespace PostexS.Models.Dtos
{
    public class walletDTO
    {
        public  string UserName { get; set; }
        public long? OrderNumber { get; set; }
        public double Amount { get; set; }
        public string Note { get; set; }
    }
}
