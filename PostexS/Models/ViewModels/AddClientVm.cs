using PostexS.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.ViewModels
{
    public class AddClientVm
    {
        public string Name { get; set; }
        public string WhatsappPhone { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public UserType UserType { get; set; }
        //public Site site { get; set; } = Site.Domain;
        public long BranchId { get; set; }
    }
}
