using PostexS.Models.Enums;
using System;

namespace PostexS.Models.Dtos
{
    public class SenderSettlementDto
    {
        public long Id { get; set; }
        public double Amount { get; set; }
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public TransactionType TransactionType { get; set; }
        public string Note { get; set; }
    }
}
