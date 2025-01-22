using PostexS.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Domain
{
    public class Wallet : BaseModel
    {
        public TransactionType TransactionType { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
        public string ActualUserId { get; set; }
        [ForeignKey("ActualUserId")]
        public virtual ApplicationUser ActualUser { get; set; }
        public long? OrderNumber { get; set; }
        public double Amount { get; set; }
        public bool AddedToAdminWallet { get; set; } = false;
        public double? UserWalletLast { get; set; }
        public string Note { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public string? Complete_UserId { get; set; }
        [ForeignKey("Complete_UserId")]
        public virtual ApplicationUser Complete_User { get; set; }

    }
}
