using PostexS.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Domain
{
    public class Order : BaseModel
    {
        public string Code { get; set; }
        public string Notes { get; set; }
        public string AddressCity { get; set; }
        public string Address { get; set; }
        public string ClientCode { get; set; }
        public string ClientName { get; set; }
        public string ClientPhone { get; set; }
        public double Cost { get; set; }
        public double DeliveryFees { get; set; }
        public double TotalCost { get; set; }
        public bool Pending { get; set; }
        public bool TransferredConfirmed { get; set; } = false;
        public bool PendingReturnTransferrConfirmed { get; set; } = false;
        public double ArrivedCost { get; set; }
        public double DeliveryCost { get; set; }
        public string? Returned_Image { get; set; }
        public double ClientCost { get; set; }
        public double? ReturnedCost { get; set; }
        public byte[]? BarcodeImage { get; set; }
        public bool Finished { get; set; }
        public OrderStatus Status { get; set; }
        public OrderCompleted OrderCompleted { get; set; }
        public long? CompletedId { get; set; }
        public string ClientId { get; set; }
        public DateTime? CompletedOn { get; set; }
        public DateTime? LastUpdated { get; set; }
        [ForeignKey("ClientId")]
        public virtual ApplicationUser Client { get; set; }
        public string DeliveryId { get; set; }
        [ForeignKey("DeliveryId")]
        public virtual ApplicationUser Delivery { get; set; }
        public long BranchId { get; set; }
        [ForeignKey("BranchId")]
        public virtual Branch Branch { get; set; }
        public long? PreviousBranchId { get; set; }
        [ForeignKey("PreviousBranchId")]
        public virtual Branch? PreviousBranch { get; set; }
        public long? OrderOperationHistoryId { get; set; }
        [ForeignKey("OrderOperationHistoryId")]
        public virtual OrderOperationHistory? OrderOperationHistory { get; set; }
        public long? WalletId { get; set; }
        [ForeignKey("WalletId")]
        public virtual Wallet Wallet { get; set; }
        public virtual ICollection<OrderNotes> OrderNotes { get; set; }
        public virtual ICollection<OrderTransferrHistory> OrderTransferrHistories { get; set; }



        #region Special for Status [ Returned_And_Paid_DeliveryCost - Returned_And_DeliveryCost_On_Sender ]
        public bool ReturnedFinished { get; set; } = false;
        public OrderCompleted ReturnedOrderCompleted { get; set; } = OrderCompleted.NOK;
        public long? ReturnedWalletId { get; set; }
        public long? ReturnedCompletedId { get; set; }
        #endregion
    }
}
