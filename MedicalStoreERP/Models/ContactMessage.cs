using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalStoreERP.Models
{
    public enum ContactStatus
    {
        New = 0,
        InProgress = 1,
        Resolved = 2,
        Closed = 3
    }

    public enum ContactCategory
    {
        General = 0,
        OrderInquiry = 1,
        ProductInquiry = 2,
        Complaint = 3,
        Feedback = 4,
        ReturnRefund = 5,
        Other = 6
    }

    public class ContactMessage
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(15)]
        public string? Phone { get; set; }

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        public ContactCategory Category { get; set; } = ContactCategory.General;

        public ContactStatus Status { get; set; } = ContactStatus.New;

        // If user is logged in
        public string? UserId { get; set; }

        // Related order if applicable
        public int? OrderId { get; set; }

        // Admin response
        [StringLength(2000)]
        public string? AdminResponse { get; set; }

        public string? RespondedByUserId { get; set; }

        public DateTime? RespondedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // IP Address for tracking
        [StringLength(50)]
        public string? IpAddress { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        [ForeignKey("RespondedByUserId")]
        public ApplicationUser? RespondedByUser { get; set; }
    }
}
