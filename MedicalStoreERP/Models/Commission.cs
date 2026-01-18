using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalStoreERP.Models
{
    public class CommissionSetting
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal Percentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinSaleAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxSaleAmount { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public string? CreatedBy { get; set; }
    }

    public class CommissionRecord
    {
        public int Id { get; set; }

        [Required]
        public DateTime RecordDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Period { get; set; } = string.Empty; // Daily, Weekly, Monthly, Yearly

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalSales { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal CommissionPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CommissionAmount { get; set; }

        public int OnlineOrdersCount { get; set; }

        public int OfflineOrdersCount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OnlineSales { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OfflineSales { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? Notes { get; set; }
    }
}
