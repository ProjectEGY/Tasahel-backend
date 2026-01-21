using PostexS.Models.Domain;
using PostexS.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Dtos
{
    public class BranchWalletsDTO
    {
        public long BranchId { get; set; }
        public string BranchName { get; set; }
        public string BranchAddress { get; set; }
        public List<BranchAccountDTO> Accounts { get; set; }
        public double WalletsSummation { get; set; }
    }
    public class BranchAccountDTO
    {
        public string Id { get; set; }
        public string AccountName { get; set; }
        public string Eamil { get; set; }
        public double Wallet { get; set; }
        public UserType UserType { get; set; }
    }
}
