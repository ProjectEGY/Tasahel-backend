using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PostexS.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }
        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<ContactUs> ContactUs { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderOperationHistory> OrdersOperationsHistories { get; set; }
        public DbSet<OrderTransferrHistory> OrderTransferrHistories { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<DeviceTokens> DeviceTokens { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<OrderNotes> OrderNotes { get; set; }
        public DbSet<TermsAndCondition> TermsAndConditions { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<WapilotSettings> WapilotSettings { get; set; }
        public DbSet<WhatsAppMessageQueue> WhatsAppMessageQueues { get; set; }
        public DbSet<WhatsAppMessageLog> WhatsAppMessageLogs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApplicationUser>().
                HasMany(w => w.Clients).
                WithOne(s => s.Client).
                HasForeignKey(s => s.ClientId);
            modelBuilder.Entity<ApplicationUser>().
               HasMany(w => w.Deliveries).
               WithOne(s => s.Delivery).
               HasForeignKey(s => s.DeliveryId);
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(x => x.WalletClient)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId);
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(x => x.ActualUser)
                .WithOne(x => x.ActualUser)
                .HasForeignKey(x => x.ActualUserId);
            base.OnModelCreating(modelBuilder);
        }
    }
}
