using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PostexS.Models.Enums;

namespace PostexS.Models.ViewModels
{
    public class WalletTransactionVM
    {
        public string UserId { get; set; }
        public TransactionType TransactionType { get; set; }
        public double Amount { get; set; }
        public bool IsAdd { get; set; }
        public string Note { get; set; }
    }
}
