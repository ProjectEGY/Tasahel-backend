using Microsoft.AspNetCore.Identity;
using PostexS.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Domain
{
    public class ApplicationUser : IdentityUser
    {
        public string Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool Tracking { get; set; } = false;
        public string Name { get; set; }
        public string? PublicKey { get; set; }
        public string? PrivateKey { get; set; }
        public DateTime? APIkeys_UpdateOn { get; set; }
        public string? OrdersGeneralNote { get; set; }
        public UserType UserType { get; set; }
        //public Site site { get; set; } = Site.Domain;
        public bool IsDeleted { get; set; }
        public bool IsPending { get; set; } = false;
        public double Wallet { get; set; }
        public double? DeliveryCost { get; set; }
        public string WhatsappPhone { get; set; }
        public string? WhatsappGroupId { get; set; }
        public string? IdentityFrontPhoto { get; set; }
        public string? IdentityBackPhoto { get; set; }
        public string? RidingLecencePhoto { get; set; }
        public string? ViecleLecencePhoto { get; set; }
        public string? FishPhotoPhoto { get; set; }
        public long BranchId { get; set; }
        [ForeignKey("BranchId")]
        public virtual Branch Branch { get; set; }
        public virtual ICollection<Order> Clients { get; set; }
        public virtual ICollection<Wallet> ActualUser { get; set; }
        public virtual ICollection<Wallet> WalletClient { get; set; }
        public virtual ICollection<Order> Deliveries { get; set; }
        public virtual ICollection<OrderNotes> UserOrderNotes { get; set; }
    }
}
