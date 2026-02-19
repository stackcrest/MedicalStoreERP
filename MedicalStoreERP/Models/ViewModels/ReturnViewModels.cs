using System.ComponentModel.DataAnnotations;

namespace MedicalStoreERP.Models.ViewModels
{
    // Return Request View Models
    public class ReturnRequestViewModel
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public ReturnReason Reason { get; set; }
        public string ReasonDisplay => Reason.ToString().Replace("_", " ");
        public string? ReasonDetails { get; set; }
        public ReturnStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public string StatusBadgeClass => Status switch
        {
            ReturnStatus.Pending => "warning",
            ReturnStatus.Approved => "info",
            ReturnStatus.Rejected => "danger",
            ReturnStatus.ItemsReceived => "primary",
            ReturnStatus.RefundProcessed => "success",
            ReturnStatus.Completed => "success",
            ReturnStatus.Cancelled => "secondary",
            _ => "secondary"
        };
        public decimal TotalReturnAmount { get; set; }
        public decimal RefundedAmount { get; set; }
        public RefundMethod RefundMethod { get; set; }
        public string RefundMethodDisplay => RefundMethod.ToString().Replace("_", " ");
        public string? RefundTransactionId { get; set; }
        public string? AdminNotes { get; set; }
        public string? CustomerNotes { get; set; }
        public string? ImageUrl1 { get; set; }
        public string? ImageUrl2 { get; set; }
        public string? ImageUrl3 { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ItemsReceivedAt { get; set; }
        public string? ReceivedByName { get; set; }
        public DateTime? RefundProcessedAt { get; set; }
        public string? RefundProcessedByName { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public string? BankAccountName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankIFSC { get; set; }
        public string? UPIId { get; set; }
        public List<ReturnItemViewModel> Items { get; set; } = new();

        // Order details
        public DateTime OrderDate { get; set; }
        public decimal OrderTotalAmount { get; set; }
    }

    public class ReturnItemViewModel
    {
        public int Id { get; set; }
        public int ReturnRequestId { get; set; }
        public int OrderItemId { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string BatchNo { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Reason { get; set; }
        public bool ReturnedToInventory { get; set; }
        public int ReturnedQuantity { get; set; }
        public DateTime? ReturnedToInventoryAt { get; set; }
        public string? ReturnedToInventoryByName { get; set; }
        public string? InventoryNotes { get; set; }
        public bool IsResaleable { get; set; }
        public string? ConditionNotes { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class CreateReturnRequestViewModel
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        public ReturnReason Reason { get; set; }

        [StringLength(1000)]
        public string? ReasonDetails { get; set; }

        [StringLength(500)]
        public string? CustomerNotes { get; set; }

        public RefundMethod PreferredRefundMethod { get; set; } = RefundMethod.OriginalPaymentMethod;

        // Bank details (required if PreferredRefundMethod is BankTransfer)
        [StringLength(100)]
        public string? BankAccountName { get; set; }

        [StringLength(20)]
        public string? BankAccountNumber { get; set; }

        [StringLength(15)]
        public string? BankIFSC { get; set; }

        [StringLength(100)]
        public string? UPIId { get; set; }

        public List<CreateReturnItemViewModel> Items { get; set; } = new();

        // For view
        public OrderViewModel? Order { get; set; }
    }

    public class CreateReturnItemViewModel
    {
        public int OrderItemId { get; set; }
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string BatchNo { get; set; } = string.Empty;
        public int MaxQuantity { get; set; }
        public decimal UnitPrice { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        public bool Selected { get; set; }
    }

    public class ProcessReturnViewModel
    {
        public int ReturnRequestId { get; set; }
        public ReturnRequestViewModel? ReturnRequest { get; set; }

        [Required]
        public ReturnStatus NewStatus { get; set; }

        [StringLength(500)]
        public string? AdminNotes { get; set; }

        // For approval
        public decimal? ApprovedRefundAmount { get; set; }

        // For rejection
        [StringLength(500)]
        public string? RejectionReason { get; set; }

        // For items received - inventory management
        public List<ReturnItemInventoryViewModel> ItemsInventory { get; set; } = new();

        // For refund processing
        public string? RefundTransactionId { get; set; }
        public decimal? ActualRefundedAmount { get; set; }
    }

    public class ReturnItemInventoryViewModel
    {
        public int ReturnItemId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string BatchNo { get; set; } = string.Empty;
        public int ReturnedQuantity { get; set; }
        public int MaxQuantity { get; set; }
        public bool IsResaleable { get; set; }
        public bool ReturnToInventory { get; set; }
        public int QuantityToReturn { get; set; }

        [StringLength(200)]
        public string? ConditionNotes { get; set; }

        [StringLength(500)]
        public string? InventoryNotes { get; set; }
    }

    public class ReturnListViewModel
    {
        public PaginatedResult<ReturnRequestViewModel> Returns { get; set; } = new();
        public ReturnStatus? StatusFilter { get; set; }
        public string? SearchTerm { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // Statistics
        public int TotalReturns { get; set; }
        public int PendingReturns { get; set; }
        public int ApprovedReturns { get; set; }
        public int CompletedReturns { get; set; }
        public decimal TotalRefundedAmount { get; set; }
    }

    public class CustomerReturnListViewModel
    {
        public List<ReturnRequestViewModel> Returns { get; set; } = new();
        public int TotalReturns { get; set; }
        public int ActiveReturns { get; set; }
        public decimal TotalRefunded { get; set; }
    }
}
