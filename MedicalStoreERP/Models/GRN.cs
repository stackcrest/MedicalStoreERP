using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalStoreERP.Models
{
    public class GRN
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string GRNNumber { get; set; } = string.Empty;

        public int SupplierId { get; set; }

        [StringLength(50)]
        public string? SupplierInvoiceNo { get; set; }

        public DateTime? SupplierInvoiceDate { get; set; }

        public DateTime GRNDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal GSTAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public bool IsPosted { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        // Navigation properties
        [ForeignKey("SupplierId")]
        public virtual Supplier? Supplier { get; set; }

        public virtual ICollection<GRNItem> Items { get; set; } = new List<GRNItem>();
    }

    public class GRNItem
    {
        public int Id { get; set; }

        public int GRNId { get; set; }

        public int MedicineId { get; set; }

        [Required]
        [StringLength(50)]
        public string BatchNo { get; set; } = string.Empty;

        public DateTime ExpiryDate { get; set; }

        [Required]
        public int Quantity { get; set; }

        public int FreeQuantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MRP { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal GSTPercent { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal GSTAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        // Navigation properties
        [ForeignKey("GRNId")]
        public virtual GRN? GRN { get; set; }

        [ForeignKey("MedicineId")]
        public virtual Medicine? Medicine { get; set; }
    }
}
