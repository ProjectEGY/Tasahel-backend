using PostexS.Models.Domain;
using PostexS.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.ViewModels
{
    public class TransferrHistory
    {
        public string Transfer_UserName { get; set; }
        public DateTime TransferDate { get; set; }
        public string? AcceptTransfer_UserName { get; set; }
        public DateTime? AcceptTransferDate { get; set; }
        public bool TransferCancel { get; set; } = false;
        public string FromBranchName { get; set; }
        public string ToBranchName { get; set; }
    }
}
