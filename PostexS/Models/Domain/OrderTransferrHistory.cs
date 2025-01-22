using PostexS.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Domain
{
    public class OrderTransferrHistory : BaseModel
    {
        public long OrderId { get; set; }
        //[ForeignKey("OrderId")]
        //public virtual Order Order { get; set; }
        public string Transfer_UserId { get; set; }
        //[ForeignKey("Transfer_UserId")]
        //public virtual ApplicationUser Transfer_User { get; set; }
        public string? AcceptTransfer_UserId { get; set; }
        //[ForeignKey("AcceptTransfer_UserId")]
        //public virtual ApplicationUser? AcceptTransfer_User { get; set; }
        public DateTime? AcceptTransferDate { get; set; }
        public bool TransferCancel { get; set; } = false;

        public long FromBranchId { get; set; }
        //[ForeignKey("FromBranchId")]
        //public virtual Branch FromBranch { get; set; }

        public long ToBranchId { get; set; }
        //[ForeignKey("ToBranchId")]
        //public virtual Branch ToBranch { get; set; }


        public long? PreviousBranchId { get; set; }
    }
}
