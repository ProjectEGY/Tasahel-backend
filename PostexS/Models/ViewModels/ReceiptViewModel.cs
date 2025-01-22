using PostexS.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.ViewModels
{
    public class ReceiptViewModel
    {
        public string Code { get; set; }
        public string SenderName { get; set; }
        public int TotalDeliveredOrders { get; set; }
        public DateTime SettlementDate { get; set; }
        public double TotalAmount { get; set; }
        public TransactionType type { get; set; }
    }
}
