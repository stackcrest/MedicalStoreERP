using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MedicalStoreERP.Models;

namespace MedicalStoreERP.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<SupplierLedger> SupplierLedgers { get; set; }
        public DbSet<GRN> GRNs { get; set; }
        public DbSet<GRNItem> GRNItems { get; set; }
        public DbSet<CustomerLedger> CustomerLedgers { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<ArticleComment> ArticleComments { get; set; }
        public DbSet<OfflineSale> OfflineSales { get; set; }
        public DbSet<OfflineSaleItem> OfflineSaleItems { get; set; }
        public DbSet<CommissionSetting> CommissionSettings { get; set; }
        public DbSet<CommissionRecord> CommissionRecords { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }
        public DbSet<ReturnRequest> ReturnRequests { get; set; }
        public DbSet<ReturnItem> ReturnItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure indexes
            builder.Entity<Medicine>()
                .HasIndex(m => m.Name);

            builder.Entity<Medicine>()
                .HasIndex(m => m.BatchNo);

            builder.Entity<Medicine>()
                .HasIndex(m => m.ExpiryDate);

            builder.Entity<Medicine>()
                .HasIndex(m => m.CategoryId);

            builder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();

            builder.Entity<Order>()
                .HasIndex(o => o.UserId);

            builder.Entity<Order>()
                .HasIndex(o => o.OrderDate);

            builder.Entity<Order>()
                .HasIndex(o => o.Status);

            builder.Entity<CartItem>()
                .HasIndex(c => new { c.UserId, c.MedicineId })
                .IsUnique();

            builder.Entity<WishlistItem>()
                .HasIndex(w => new { w.UserId, w.MedicineId })
                .IsUnique();

            builder.Entity<GRN>()
                .HasIndex(g => g.GRNNumber)
                .IsUnique();

            // Configure relationships
            builder.Entity<Medicine>()
                .HasOne(m => m.Category)
                .WithMany(c => c.Medicines)
                .HasForeignKey(m => m.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Medicine)
                .WithMany(m => m.OrderItems)
                .HasForeignKey(oi => oi.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CartItem>()
                .HasOne(ci => ci.Medicine)
                .WithMany(m => m.CartItems)
                .HasForeignKey(ci => ci.MedicineId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WishlistItem>()
                .HasOne(wi => wi.Medicine)
                .WithMany(m => m.WishlistItems)
                .HasForeignKey(wi => wi.MedicineId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GRN>()
                .HasOne(g => g.Supplier)
                .WithMany(s => s.GRNs)
                .HasForeignKey(g => g.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GRNItem>()
                .HasOne(gi => gi.GRN)
                .WithMany(g => g.Items)
                .HasForeignKey(gi => gi.GRNId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GRNItem>()
                .HasOne(gi => gi.Medicine)
                .WithMany(m => m.GRNItems)
                .HasForeignKey(gi => gi.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SupplierLedger>()
                .HasOne(sl => sl.Supplier)
                .WithMany(s => s.Ledgers)
                .HasForeignKey(sl => sl.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CustomerLedger>()
                .HasOne(cl => cl.User)
                .WithMany()
                .HasForeignKey(cl => cl.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CustomerLedger>()
                .HasOne(cl => cl.Order)
                .WithMany()
                .HasForeignKey(cl => cl.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            // Order-User relationships
            builder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Order>()
                .HasOne(o => o.PrescriptionVerifiedByUser)
                .WithMany()
                .HasForeignKey(o => o.PrescriptionVerifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Decimal precision configurations
            builder.Entity<Medicine>()
                .Property(m => m.MRP)
                .HasPrecision(18, 2);

            builder.Entity<Medicine>()
                .Property(m => m.SalePrice)
                .HasPrecision(18, 2);

            builder.Entity<Medicine>()
                .Property(m => m.PurchasePrice)
                .HasPrecision(18, 2);

            builder.Entity<Medicine>()
                .Property(m => m.GSTPercent)
                .HasPrecision(5, 2);

            builder.Entity<Order>()
                .Property(o => o.SubTotal)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.TotalPrice)
                .HasPrecision(18, 2);

            // OfflineSale configurations
            builder.Entity<OfflineSale>()
                .HasIndex(s => s.InvoiceNumber)
                .IsUnique();

            builder.Entity<OfflineSale>()
                .HasMany(s => s.Items)
                .WithOne(i => i.OfflineSale)
                .HasForeignKey(i => i.OfflineSaleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OfflineSaleItem>()
                .HasOne(i => i.Medicine)
                .WithMany()
                .HasForeignKey(i => i.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            // ReturnRequest configurations
            builder.Entity<ReturnRequest>()
                .HasIndex(r => r.ReturnNumber)
                .IsUnique();

            builder.Entity<ReturnRequest>()
                .HasIndex(r => r.OrderId);

            builder.Entity<ReturnRequest>()
                .HasIndex(r => r.UserId);

            builder.Entity<ReturnRequest>()
                .HasIndex(r => r.Status);

            builder.Entity<ReturnRequest>()
                .HasOne(r => r.Order)
                .WithMany()
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReturnRequest>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReturnRequest>()
                .HasOne(r => r.ApprovedByUser)
                .WithMany()
                .HasForeignKey(r => r.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReturnRequest>()
                .HasOne(r => r.ReceivedByUser)
                .WithMany()
                .HasForeignKey(r => r.ReceivedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReturnRequest>()
                .HasOne(r => r.RefundProcessedByUser)
                .WithMany()
                .HasForeignKey(r => r.RefundProcessedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReturnRequest>()
                .Property(r => r.TotalReturnAmount)
                .HasPrecision(18, 2);

            builder.Entity<ReturnRequest>()
                .Property(r => r.RefundedAmount)
                .HasPrecision(18, 2);

            // ReturnItem configurations
            builder.Entity<ReturnItem>()
                .HasOne(ri => ri.ReturnRequest)
                .WithMany(r => r.ReturnItems)
                .HasForeignKey(ri => ri.ReturnRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ReturnItem>()
                .HasOne(ri => ri.OrderItem)
                .WithMany()
                .HasForeignKey(ri => ri.OrderItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReturnItem>()
                .HasOne(ri => ri.Medicine)
                .WithMany()
                .HasForeignKey(ri => ri.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReturnItem>()
                .HasOne(ri => ri.ReturnedToInventoryByUser)
                .WithMany()
                .HasForeignKey(ri => ri.ReturnedToInventoryByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReturnItem>()
                .Property(ri => ri.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<ReturnItem>()
                .Property(ri => ri.TotalPrice)
                .HasPrecision(18, 2);
        }
    }
}
