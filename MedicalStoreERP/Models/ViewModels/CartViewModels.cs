using System.ComponentModel.DataAnnotations;

namespace MedicalStoreERP.Models.ViewModels
{
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
        public decimal SubTotal { get; set; }
        public decimal CGSTAmount { get; set; }
        public decimal SGSTAmount { get; set; }
        public decimal TotalGSTAmount { get; set; }
        public decimal GSTRate { get; set; } = 18; // Default GST rate
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal ShippingCharges { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalItems { get; set; }
        public bool IsDiscountApplied { get; set; }
        public bool RequiresPrescription { get; set; }
        public List<string> PrescriptionMedicines { get; set; } = new List<string>();
    }

    public class CartItemViewModel
    {
        public int Id { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string BatchNo { get; set; } = string.Empty;
        public string HSNCode { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal MRP { get; set; }
        public decimal GSTPercent { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal TotalPrice { get; set; }
        public int AvailableStock { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool RequiresPrescription { get; set; }
    }

    public class AddToCartRequest
    {
        [Required]
        public int MedicineId { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartRequest
    {
        [Required]
        public int CartItemId { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
        public int Quantity { get; set; }
    }

    // Wishlist ViewModels
    public class WishlistViewModel
    {
        public List<WishlistItemDisplayModel> Items { get; set; } = new();
        public int TotalItems { get; set; }
    }

    public class WishlistItemDisplayModel
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public bool IsInStock { get; set; }
        public int StockQuantity { get; set; }
        public DateTime AddedAt { get; set; }
    }
}
