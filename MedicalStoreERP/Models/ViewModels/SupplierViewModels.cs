using System.ComponentModel.DataAnnotations;

namespace MedicalStoreERP.Models.ViewModels
{
    public class SupplierViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PinCode { get; set; }
        public string? GSTIN { get; set; }
        public string? PAN { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal CurrentBalance { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalGRNs { get; set; }
        public decimal TotalPurchases { get; set; }
    }

    public class SupplierCreateViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Supplier name is required")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact person is required")]
        [StringLength(100)]
        public string ContactPerson { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(15)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [StringLength(50)]
        public string? State { get; set; }

        [StringLength(10)]
        [Display(Name = "PIN Code")]
        public string? PinCode { get; set; }

        [StringLength(15)]
        [Display(Name = "GST Number")]
        public string? GSTIN { get; set; }

        // Alias for GSTIN to maintain view compatibility
        public string? GSTNumber
        {
            get => GSTIN;
            set => GSTIN = value;
        }

        [StringLength(10)]
        [Display(Name = "PAN Number")]
        public string? PAN { get; set; }

        [Range(-999999999, 999999999)]
        public decimal OpeningBalance { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // SupplierUpdateViewModel inherits from SupplierCreateViewModel for edit functionality
    public class SupplierUpdateViewModel : SupplierCreateViewModel
    {
    }

    public class GRNViewModel
    {
        public int Id { get; set; }
        public string GRNNumber { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string? SupplierInvoiceNo { get; set; }
        public DateTime? SupplierInvoiceDate { get; set; }
        public DateTime GRNDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
        public bool IsPosted { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public List<GRNItemViewModel> Items { get; set; } = new List<GRNItemViewModel>();
    }

    public class GRNItemViewModel
    {
        public int Id { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string BatchNo { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public int Quantity { get; set; }
        public int FreeQuantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal MRP { get; set; }
        public decimal GSTPercent { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class GRNCreateViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Supplier is required")]
        public int SupplierId { get; set; }

        [StringLength(50)]
        public string? SupplierInvoiceNo { get; set; }

        [DataType(DataType.Date)]
        public DateTime? SupplierInvoiceDate { get; set; }

        [Required(ErrorMessage = "GRN date is required")]
        [DataType(DataType.Date)]
        public DateTime GRNDate { get; set; } = DateTime.Today;

        [StringLength(500)]
        public string? Notes { get; set; }

        public List<GRNItemCreateViewModel> Items { get; set; } = new List<GRNItemCreateViewModel>();
    }

    public class GRNItemCreateViewModel
    {
        [Required(ErrorMessage = "Medicine is required")]
        public int MedicineId { get; set; }

        [Required(ErrorMessage = "Batch number is required")]
        [StringLength(50)]
        public string BatchNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Expiry date is required")]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Range(0, int.MaxValue)]
        public int FreeQuantity { get; set; }

        [Required(ErrorMessage = "Purchase price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Purchase price must be greater than 0")]
        public decimal PurchasePrice { get; set; }

        [Required(ErrorMessage = "MRP is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "MRP must be greater than 0")]
        public decimal MRP { get; set; }

        [Required]
        [Range(0, 28)]
        public decimal GSTPercent { get; set; } = 12;
    }

    public class SupplierLedgerViewModel
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string? SupplierPhone { get; set; }
        public string? SupplierEmail { get; set; }
        public SupplierViewModel? Supplier { get; set; }
        public List<SupplierLedgerItem> Entries { get; set; } = new List<SupplierLedgerItem>();
        public List<SupplierLedgerItem> Ledger { get; set; } = new List<SupplierLedgerItem>();
        public decimal OpeningBalance { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class SupplierLedgerItem
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public string? ReferenceNo { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }
}
