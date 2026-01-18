using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalStoreERP.Models
{
    public enum OrderStatus
    {
        Pending = 0,
        Confirmed = 1,
        Processing = 2,
        Shipped = 3,
        Delivered = 4,
        Cancelled = 5,
        Returned = 6
    }

    public enum PaymentMethod
    {
        COD = 0,
        UPI = 1,
        Card = 2,
        NetBanking = 3
    }

    public enum PaymentStatus
    {
        Pending = 0,
        Completed = 1,
        Failed = 2,
        Refunded = 3
    }

    public class Order
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string CustomerPhone { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string DeliveryAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string State { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string PinCode { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CGSTAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SGSTAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalGSTAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingCharges { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [StringLength(100)]
        public string? TransactionId { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public DateTime? ConfirmedAt { get; set; }

        public DateTime? ShippedAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        // Prescription fields
        public bool RequiresPrescription { get; set; }

        [StringLength(500)]
        public string? PrescriptionUrl { get; set; }

        [StringLength(200)]
        public string? PrescriptionFileName { get; set; }

        public bool PrescriptionVerified { get; set; }

        public DateTime? PrescriptionVerifiedAt { get; set; }

        public string? PrescriptionVerifiedByUserId { get; set; }

        [StringLength(500)]
        public string? PrescriptionNotes { get; set; }

        // Delivery location coordinates
        [Column(TypeName = "decimal(10,7)")]
        public decimal? DeliveryLatitude { get; set; }

        [Column(TypeName = "decimal(10,7)")]
        public decimal? DeliveryLongitude { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? DistanceFromStoreKm { get; set; }

        public bool SameDayDelivery { get; set; }

        public DateTime? EstimatedDeliveryDate { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("PrescriptionVerifiedByUserId")]
        public virtual ApplicationUser? PrescriptionVerifiedByUser { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public int MedicineId { get; set; }

        [Required]
        [StringLength(200)]
        public string MedicineName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string BatchNo { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string HSNCode { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

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
        public decimal TotalPrice { get; set; }

        // Navigation properties
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("MedicineId")]
        public virtual Medicine? Medicine { get; set; }
    }
}
