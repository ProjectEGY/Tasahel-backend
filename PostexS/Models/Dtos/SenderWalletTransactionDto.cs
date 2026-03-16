using PostexS.Models.Enums;
using System;

namespace PostexS.Models.Dtos
{
    public class SenderWalletTransactionDto
    {
        public long Id { get; set; }
        public TransactionType TransactionType { get; set; }
        public double Amount { get; set; }
        public double? WalletBalanceAfter { get; set; }
        public string Note { get; set; }
        public long? OrderNumber { get; set; }
        public DateTime Date { get; set; }
    }
}
