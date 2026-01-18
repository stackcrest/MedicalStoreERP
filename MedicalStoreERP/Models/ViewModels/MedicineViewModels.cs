using System.ComponentModel.DataAnnotations;

namespace MedicalStoreERP.Models.ViewModels
{
    public class MedicineViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string BatchNo { get; set; } = string.Empty;
        public DateTime ManufactureDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal MRP { get; set; }
        public decimal SalePrice { get; set; }
        public decimal PurchasePrice { get; set; }
        public int StockQty { get; set; }
        public int ReorderLevel { get; set; }
        public string HSNCode { get; set; } = string.Empty;
        public decimal GSTPercent { get; set; }
        public string? Description { get; set; }
        public string? Manufacturer { get; set; }
        public string? Composition { get; set; }
        public string? Dosage { get; set; }
        public string? PackSize { get; set; }
        public string? ImageUrl { get; set; }
        public bool RequiresPrescription { get; set; }
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public bool IsExpired { get; set; }
        public bool IsNearExpiry { get; set; }
        public bool IsLowStock { get; set; }
        public int DaysToExpiry { get; set; }
        public decimal DiscountPercent { get; set; }
    }

    public class MedicineCreateViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Medicine name is required")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Generic name is required")]
        [StringLength(100)]
        public string GenericName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Batch number is required")]
        [StringLength(50)]
        public string BatchNo { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime ManufactureDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Expiry date is required")]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; } = DateTime.Today.AddYears(2);

        [Required(ErrorMessage = "MRP is required")]
        [Range(0.01, 100000, ErrorMessage = "MRP must be greater than 0")]
        public decimal MRP { get; set; }

        [Required(ErrorMessage = "Sale price is required")]
        [Range(0.01, 100000, ErrorMessage = "Sale price must be greater than 0")]
        public decimal SalePrice { get; set; }

        [Required(ErrorMessage = "Purchase price is required")]
        [Range(0.01, 100000, ErrorMessage = "Purchase price must be greater than 0")]
        public decimal PurchasePrice { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, 100000, ErrorMessage = "Stock must be a positive number")]
        public int StockQty { get; set; }

        [Range(0, 1000)]
        public int ReorderLevel { get; set; } = 10;

        [StringLength(20)]
        public string HSNCode { get; set; } = "3004";

        [Required(ErrorMessage = "GST percentage is required")]
        [Range(0, 28, ErrorMessage = "GST must be between 0 and 28")]
        public decimal GSTPercent { get; set; } = 12;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Manufacturer { get; set; }

        [StringLength(50)]
        public string? Composition { get; set; }

        [StringLength(50)]
        public string? Dosage { get; set; }

        [StringLength(50)]
        public string? PackSize { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        public bool RequiresPrescription { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsFeatured { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }
    }

    public class MedicineListViewModel
    {
        public List<MedicineViewModel> Medicines { get; set; } = new List<MedicineViewModel>();
        public List<Category> Categories { get; set; } = new List<Category>();
        public int TotalItems { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public string? SortBy { get; set; }
        public bool? InStock { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
    }

    public class MedicineDetailViewModel
    {
        public MedicineViewModel Medicine { get; set; } = new MedicineViewModel();
        public List<MedicineViewModel> RelatedMedicines { get; set; } = new List<MedicineViewModel>();
        public bool IsInWishlist { get; set; }
        public bool IsInCart { get; set; }
    }

    // MedicineUpdateViewModel is an alias for editing - uses the same properties as Create
    public class MedicineUpdateViewModel : MedicineCreateViewModel
    {
    }

    // Bulk Upload ViewModels
    public class BulkUploadViewModel
    {
        [Required(ErrorMessage = "Please select an Excel file")]
        public IFormFile? ExcelFile { get; set; }
    }

    public class BulkUploadResultViewModel
    {
        public int TotalRecords { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<BulkUploadError> Errors { get; set; } = new List<BulkUploadError>();
        public bool IsSuccess => FailedCount == 0;
    }

    public class BulkUploadError
    {
        public int RowNumber { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class MedicineExcelRow
    {
        public string Name { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string BatchNo { get; set; } = string.Empty;
        public DateTime ManufactureDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal MRP { get; set; }
        public decimal SalePrice { get; set; }
        public decimal PurchasePrice { get; set; }
        public int StockQty { get; set; }
        public int ReorderLevel { get; set; }
        public string HSNCode { get; set; } = string.Empty;
        public decimal GSTPercent { get; set; }
        public string? Description { get; set; }
        public string? Manufacturer { get; set; }
        public string? Composition { get; set; }
        public string? Dosage { get; set; }
        public string? PackSize { get; set; }
        public bool RequiresPrescription { get; set; }
        public bool IsActive { get; set; }
    }
}
