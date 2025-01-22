using PostexS.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Domain
{
    public class OrderOperationHistory : BaseModel
    {
        public long OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }
        public string Create_UserId { get; set; }
        [ForeignKey("Create_UserId")]
        public virtual ApplicationUser Create_User { get; set; }
        public DateTime CreateDate { get; set; }
        public string Reject_UserId { get; set; }
        [ForeignKey("Reject_UserId")]
        public virtual ApplicationUser Reject_User { get; set; }
        public DateTime RejectDate { get; set; }
        public string Edit_UserId { get; set; }
        [ForeignKey("Edit_UserId")]
        public virtual ApplicationUser Edit_User { get; set; }
        public DateTime EditDate { get; set; }
        public string Delete_UserId { get; set; }
        [ForeignKey("Delete_UserId")]
        public virtual ApplicationUser Delete_User { get; set; }
        public DateTime DeleteDate { get; set; }
        public string Restore_UserId { get; set; }
        [ForeignKey("Restore_UserId")]
        public virtual ApplicationUser Restore_User { get; set; }
        public DateTime RestoreDate { get; set; }
        public string Finish_UserId { get; set; }
        [ForeignKey("Finish_UserId")]
        public virtual ApplicationUser Finish_User { get; set; }
        public DateTime FinishDate { get; set; }
        public string Complete_UserId { get; set; }
        [ForeignKey("Complete_UserId")]
        public virtual ApplicationUser Complete_User { get; set; }
        public DateTime CompleteDate { get; set; }
        #region Returned
        public string ReturnedFinish_UserId { get; set; }
        [ForeignKey("ReturnedFinish_UserId")]
        public virtual ApplicationUser ReturnedFinish_User { get; set; }
        public DateTime ReturnedFinishDate { get; set; }
        public string ReturnedComplete_UserId { get; set; }
        [ForeignKey("ReturnedComplete_UserId")]
        public virtual ApplicationUser ReturnedComplete_User { get; set; }
        public DateTime ReturnedCompleteDate { get; set; }
        #endregion
        public string EditComplete_UserId { get; set; }
        [ForeignKey("EditComplete_UserId")]
        public virtual ApplicationUser EditComplete_User { get; set; }
        public DateTime EditCompleteDate { get; set; }
        public string Assign_To_Driver_UserId { get; set; }
        [ForeignKey("Assign_To_Driver_UserId")]
        public virtual ApplicationUser Assign_To_Driver_User { get; set; }
        public DateTime Assign_To_DriverDate { get; set; }
        public string Accept_UserId { get; set; }
        [ForeignKey("Accept_UserId")]
        public virtual ApplicationUser Accept_User { get; set; }
        public DateTime AcceptDate { get; set; }
        public string Transfer_UserId { get; set; }
        [ForeignKey("Transfer_UserId")]
        public virtual ApplicationUser Transfer_User { get; set; }
        public DateTime TransferDate { get; set; }
        public string AcceptTransfer_UserId { get; set; }
        [ForeignKey("AcceptTransfer_UserId")]
        public virtual ApplicationUser AcceptTransfer_User { get; set; }
        public DateTime AcceptTransferDate { get; set; }

        public string TransferReturned_UserId { get; set; }
        [ForeignKey("TransferReturned_UserId")]
        public virtual ApplicationUser TransferReturned_User { get; set; }
        public DateTime TransferReturnedDate { get; set; }
        public string AcceptTransferReturned_UserId { get; set; }
        [ForeignKey("AcceptTransferReturned_UserId")]
        public virtual ApplicationUser AcceptTransferReturned_User { get; set; }
        public DateTime AcceptTransferReturnedDate { get; set; }

    }
}
