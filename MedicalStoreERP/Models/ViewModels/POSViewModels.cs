using System.ComponentModel.DataAnnotations;

namespace MedicalStoreERP.Models.ViewModels
{
    public class POSViewModel
    {
        public List<MedicineSearchResult> AvailableMedicines { get; set; } = new();
        public List<POSCartItem> CartItems { get; set; } = new();
        public POSCheckout Checkout { get; set; } = new();
    }

    public class MedicineSearchResult
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string BatchNo { get; set; } = string.Empty;
        public string HSNCode { get; set; } = string.Empty;
        public decimal MRP { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal GSTPercent { get; set; }
        public int AvailableStock { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    public class POSCartItem
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string BatchNo { get; set; } = string.Empty;
        public string HSNCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal MRP { get; set; }
        public decimal GSTPercent { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal TotalPrice { get; set; }
        public int AvailableStock { get; set; }
    }

    public class POSCheckout
    {
        [StringLength(100)]
        public string? CustomerName { get; set; }

        [Phone]
        [StringLength(15)]
        public string? CustomerPhone { get; set; }

        [StringLength(200)]
        public string? CustomerAddress { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;

        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AmountReceived { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class CreateOfflineSaleRequest
    {
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal AmountReceived { get; set; }
        public string? Notes { get; set; }
        public List<OfflineSaleItemRequest> Items { get; set; } = new();
    }

    public class OfflineSaleItemRequest
    {
        public int MedicineId { get; set; }
        public int Quantity { get; set; }
    }

    public class OfflineSaleViewModel
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal CGSTAmount { get; set; }
        public decimal SGSTAmount { get; set; }
        public decimal TotalGSTAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public decimal AmountReceived { get; set; }
        public decimal ChangeAmount { get; set; }
        public string? Notes { get; set; }
        public string? CreatedByName { get; set; }
        public List<OfflineSaleItemViewModel> Items { get; set; } = new();
    }

    public class OfflineSaleItemViewModel
    {
        public int Id { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string BatchNo { get; set; } = string.Empty;
        public string HSNCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal MRP { get; set; }
        public decimal GSTPercent { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class OfflineSaleListViewModel
    {
        public List<OfflineSaleViewModel> Sales { get; set; } = new();
        public int TotalSales { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
