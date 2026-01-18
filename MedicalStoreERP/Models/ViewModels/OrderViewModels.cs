using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MedicalStoreERP.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public CartViewModel Cart { get; set; } = new CartViewModel();

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(15)]
        public string CustomerPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Delivery address is required")]
        [StringLength(500)]
        public string DeliveryAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required")]
        [StringLength(50)]
        public string State { get; set; } = string.Empty;

        [Required(ErrorMessage = "PIN code is required")]
        [StringLength(10)]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "PIN code must be 6 digits")]
        public string PinCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Payment method is required")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;

        [StringLength(500)]
        public string? Notes { get; set; }

        // Prescription fields
        public bool RequiresPrescription { get; set; }
        public IFormFile? PrescriptionFile { get; set; }

        // Delivery location coordinates
        public double? DeliveryLatitude { get; set; }
        public double? DeliveryLongitude { get; set; }
        public double? DistanceFromStore { get; set; }
        public bool SameDayDeliveryAvailable { get; set; }
        public string? EstimatedDeliveryTime { get; set; }
    }

    public class OrderViewModel
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PinCode { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal CGSTAmount { get; set; }
        public decimal SGSTAmount { get; set; }
        public decimal TotalGSTAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingCharges { get; set; }
        public decimal TotalAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string? TransactionId { get; set; }
        public OrderStatus Status { get; set; }
        public string? Notes { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        
        // Prescription fields
        public bool RequiresPrescription { get; set; }
        public string? PrescriptionUrl { get; set; }
        public string? PrescriptionFileName { get; set; }
        public bool PrescriptionVerified { get; set; }
        public DateTime? PrescriptionVerifiedAt { get; set; }
        public string? PrescriptionVerifiedByName { get; set; }
        public string? PrescriptionNotes { get; set; }
        
        public List<OrderItemViewModel> Items { get; set; } = new List<OrderItemViewModel>();
        public UserViewModel? User { get; set; }
    }

    public class OrderItemViewModel
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

    public class OrderListViewModel
    {
        public List<OrderViewModel> Orders { get; set; } = new List<OrderViewModel>();
        public int TotalOrders { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public string? StatusFilter { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        public OrderStatus Status { get; set; }

        public string? Notes { get; set; }
    }
}
