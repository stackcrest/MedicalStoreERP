using System.ComponentModel.DataAnnotations;

namespace MedicalStoreERP.Models.ViewModels
{
    // Article List ViewModel
    public class ArticleListViewModel
    {
        public List<ArticleViewModel> Articles { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalArticles { get; set; }
        public string? Category { get; set; }
        public string? SearchTerm { get; set; }
        public List<string> Categories { get; set; } = new();
        public List<ArticleViewModel> FeaturedArticles { get; set; } = new();
        public List<string> PopularTags { get; set; } = new();
    }

    // Article Display ViewModel
    public class ArticleViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? FeaturedImageUrl { get; set; }
        public string? VideoUrl { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? Tags { get; set; }
        public List<string> TagList => string.IsNullOrEmpty(Tags) ? new() : Tags.Split(',').Select(t => t.Trim()).ToList();
        public string Slug { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public bool IsPublished { get; set; }
        public bool IsFeatured { get; set; }
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public string? ExternalLinks { get; set; }
        public List<ExternalLinkItem> Links => ParseExternalLinks();
        public int CommentCount { get; set; }
        public List<CommentViewModel> Comments { get; set; } = new();

        public string ReadTime => CalculateReadTime();

        private string CalculateReadTime()
        {
            var wordCount = Content?.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
            var minutes = Math.Max(1, wordCount / 200);
            return $"{minutes} min read";
        }

        private List<ExternalLinkItem> ParseExternalLinks()
        {
            if (string.IsNullOrEmpty(ExternalLinks)) return new();
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<ExternalLinkItem>>(ExternalLinks) ?? new();
            }
            catch
            {
                return new();
            }
        }
    }

    public class ExternalLinkItem
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Type { get; set; } = "link"; // link, video, pdf, image
    }

    // Article Create/Edit ViewModel
    public class ArticleCreateViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Summary is required")]
        [StringLength(500, ErrorMessage = "Summary cannot exceed 500 characters")]
        public string Summary { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content is required")]
        public string Content { get; set; } = string.Empty;

        [Url(ErrorMessage = "Please enter a valid URL")]
        [StringLength(500)]
        public string? FeaturedImageUrl { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL")]
        [StringLength(500)]
        public string? VideoUrl { get; set; }

        [Required]
        public string Category { get; set; } = "General";

        [StringLength(500)]
        public string? Tags { get; set; }

        [StringLength(200)]
        public string? MetaDescription { get; set; }

        public bool IsPublished { get; set; } = true; // Default to published

        public bool IsFeatured { get; set; } = false;

        public string? ExternalLinks { get; set; } // JSON string

        public List<ExternalLinkItem> LinksList { get; set; } = new();

        // Additional properties for Edit view
        public string Slug { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public List<CommentViewModel>? Comments { get; set; }

        public static List<string> AvailableCategories => new()
        {
            "Health Facts",
            "Prevention",
            "Cure & Treatment",
            "Wellness & Lifestyle",
            "Nutrition",
            "Mental Health",
            "First Aid",
            "Medicine Guide",
            "Health News",
            "General"
        };
    }

    // Comment ViewModel
    public class CommentViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsApproved { get; set; }
    }

    public class AddCommentRequest
    {
        public int ArticleId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Comment is required")]
        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
        public string Content { get; set; } = string.Empty;
    }
}
