using PostexS.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.ViewModels
{
    public class UserWalletVM
    {
        public ApplicationUser User { get; set; }
        public List<Wallet> Wallets { get; set; } = new List<Wallet>();
    }
}
