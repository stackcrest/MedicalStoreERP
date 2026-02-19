using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalStoreERP.Models
{
    public enum ReturnStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        ItemsReceived = 3,
        RefundProcessed = 4,
        Completed = 5,
        Cancelled = 6
    }

    public enum ReturnReason
    {
        Damaged = 0,
        WrongItem = 1,
        NotAsDescribed = 2,
        Expired = 3,
        QualityIssue = 4,
        AllergyReaction = 5,
        DoctorAdvice = 6,
        Other = 7
    }

    public enum RefundMethod
    {
        OriginalPaymentMethod = 0,
        StoreCredit = 1,
        BankTransfer = 2,
        UPI = 3
    }

    public class ReturnRequest
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string ReturnNumber { get; set; } = string.Empty;

        public int OrderId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public ReturnReason Reason { get; set; }

        [StringLength(1000)]
        public string? ReasonDetails { get; set; }

        public ReturnStatus Status { get; set; } = ReturnStatus.Pending;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalReturnAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundedAmount { get; set; }

        public RefundMethod RefundMethod { get; set; } = RefundMethod.OriginalPaymentMethod;

        [StringLength(100)]
        public string? RefundTransactionId { get; set; }

        [StringLength(500)]
        public string? AdminNotes { get; set; }

        [StringLength(500)]
        public string? CustomerNotes { get; set; }

        // Image proof for damaged/wrong items
        [StringLength(500)]
        public string? ImageUrl1 { get; set; }

        [StringLength(500)]
        public string? ImageUrl2 { get; set; }

        [StringLength(500)]
        public string? ImageUrl3 { get; set; }

        // Tracking
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }

        public string? ApprovedByUserId { get; set; }

        public DateTime? ItemsReceivedAt { get; set; }

        public string? ReceivedByUserId { get; set; }

        public DateTime? RefundProcessedAt { get; set; }

        public string? RefundProcessedByUserId { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        [StringLength(500)]
        public string? CancellationReason { get; set; }

        // Bank details for refund (if bank transfer)
        [StringLength(100)]
        public string? BankAccountName { get; set; }

        [StringLength(20)]
        public string? BankAccountNumber { get; set; }

        [StringLength(15)]
        public string? BankIFSC { get; set; }

        [StringLength(100)]
        public string? UPIId { get; set; }

        // Navigation properties
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("ApprovedByUserId")]
        public virtual ApplicationUser? ApprovedByUser { get; set; }

        [ForeignKey("ReceivedByUserId")]
        public virtual ApplicationUser? ReceivedByUser { get; set; }

        [ForeignKey("RefundProcessedByUserId")]
        public virtual ApplicationUser? RefundProcessedByUser { get; set; }

        public virtual ICollection<ReturnItem> ReturnItems { get; set; } = new List<ReturnItem>();
    }

    public class ReturnItem
    {
        public int Id { get; set; }

        public int ReturnRequestId { get; set; }

        public int OrderItemId { get; set; }

        public int MedicineId { get; set; }

        [Required]
        [StringLength(200)]
        public string MedicineName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string BatchNo { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        // Return to inventory tracking
        public bool ReturnedToInventory { get; set; }

        public int ReturnedQuantity { get; set; }

        public DateTime? ReturnedToInventoryAt { get; set; }

        public string? ReturnedToInventoryByUserId { get; set; }

        [StringLength(500)]
        public string? InventoryNotes { get; set; }

        // Item condition assessment
        public bool IsResaleable { get; set; }

        [StringLength(200)]
        public string? ConditionNotes { get; set; }

        // Navigation properties
        [ForeignKey("ReturnRequestId")]
        public virtual ReturnRequest? ReturnRequest { get; set; }

        [ForeignKey("OrderItemId")]
        public virtual OrderItem? OrderItem { get; set; }

        [ForeignKey("MedicineId")]
        public virtual Medicine? Medicine { get; set; }

        [ForeignKey("ReturnedToInventoryByUserId")]
        public virtual ApplicationUser? ReturnedToInventoryByUser { get; set; }
    }
}
