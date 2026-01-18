using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalStoreERP.Models
{
    public class Article
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Summary { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [StringLength(500)]
        public string? FeaturedImageUrl { get; set; }

        [StringLength(500)]
        public string? VideoUrl { get; set; }

        [StringLength(100)]
        public string Category { get; set; } = "General"; // Health Facts, Prevention, Cure, Wellness, News

        [StringLength(500)]
        public string? Tags { get; set; } // Comma-separated tags

        [StringLength(200)]
        public string? MetaDescription { get; set; }

        [StringLength(200)]
        public string Slug { get; set; } = string.Empty;

        public string? AuthorId { get; set; }

        [ForeignKey("AuthorId")]
        public ApplicationUser? Author { get; set; }

        [StringLength(100)]
        public string AuthorName { get; set; } = "Admin";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? PublishedAt { get; set; }

        public bool IsPublished { get; set; } = false;

        public bool IsFeatured { get; set; } = false;

        public int ViewCount { get; set; } = 0;

        public int LikeCount { get; set; } = 0;

        // Related links/references
        public string? ExternalLinks { get; set; } // JSON array of links

        // Navigation
        public virtual ICollection<ArticleComment>? Comments { get; set; }
    }

    public class ArticleComment
    {
        public int Id { get; set; }

        public int ArticleId { get; set; }

        [ForeignKey("ArticleId")]
        public Article? Article { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Email { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsApproved { get; set; } = false;
    }
}
