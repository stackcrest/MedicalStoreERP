using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalStoreERP.Models
{
    public class Medicine
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string GenericName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string BatchNo { get; set; } = string.Empty;

        [Required]
        public DateTime ManufactureDate { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MRP { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; }

        [Required]
        public int StockQty { get; set; }

        public int ReorderLevel { get; set; } = 10;

        [Required]
        [StringLength(20)]
        public string HSNCode { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal GSTPercent { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Manufacturer { get; set; }

        [StringLength(200)]
        public string? Composition { get; set; }

        [StringLength(200)]
        public string? Dosage { get; set; }

        [StringLength(100)]
        public string? PackSize { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        public bool RequiresPrescription { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsFeatured { get; set; }

        public int CategoryId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<GRNItem> GRNItems { get; set; } = new List<GRNItem>();
        public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();

        // Computed properties
        [NotMapped]
        public bool IsExpired => ExpiryDate <= DateTime.Today;

        [NotMapped]
        public bool IsNearExpiry => ExpiryDate <= DateTime.Today.AddDays(90) && !IsExpired;

        [NotMapped]
        public bool IsLowStock => StockQty <= ReorderLevel;

        [NotMapped]
        public int DaysToExpiry => (ExpiryDate - DateTime.Today).Days;

        [NotMapped]
        public decimal DiscountPercent => MRP > 0 ? Math.Round((MRP - SalePrice) / MRP * 100, 2) : 0;
    }
}
