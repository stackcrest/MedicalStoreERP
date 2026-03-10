using Microsoft.EntityFrameworkCore;
using MedicalStoreERP.Data;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;
using System.Text.RegularExpressions;

namespace MedicalStoreERP.Services
{
    public interface IArticleService
    {
        Task<ArticleListViewModel> GetArticlesAsync(string? category, string? search, int page = 1, int pageSize = 10);
        Task<ArticleViewModel?> GetArticleByIdAsync(int id);
        Task<ArticleViewModel?> GetArticleBySlugAsync(string slug);
        Task<List<ArticleViewModel>> GetFeaturedArticlesAsync(int count = 5);
        Task<List<ArticleViewModel>> GetRelatedArticlesAsync(int articleId, int count = 4);
        Task<List<ArticleViewModel>> GetRecentArticlesAsync(int count = 5);
        Task<ApiResponse<ArticleViewModel>> CreateArticleAsync(ArticleCreateViewModel model, string authorId, string authorName);
        Task<ApiResponse<ArticleViewModel>> UpdateArticleAsync(ArticleCreateViewModel model);
        Task<ApiResponse<bool>> DeleteArticleAsync(int id);
        Task<ApiResponse<bool>> TogglePublishAsync(int id);
        Task<ApiResponse<bool>> ToggleFeaturedAsync(int id);
        Task IncrementViewCountAsync(int id);
        Task<ApiResponse<bool>> AddCommentAsync(AddCommentRequest request, string? userId);
        Task<List<CommentViewModel>> GetCommentsAsync(int articleId, bool approvedOnly = true);
        Task<ApiResponse<bool>> ApproveCommentAsync(int commentId);
        Task<ApiResponse<bool>> DeleteCommentAsync(int commentId);
        Task<List<string>> GetPopularTagsAsync(int count = 10);
        Task<List<string>> GetCategoriesAsync();

        // Admin methods
        Task<PaginatedResult<ArticleViewModel>> GetAllArticlesAdminAsync(string? search, string? category, int page = 1, int pageSize = 20);
        Task<int> GetPendingCommentsCountAsync();
        Task<List<CommentViewModel>> GetPendingCommentsAsync();
    }

    public class ArticleService : IArticleService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHtmlSanitizerService _htmlSanitizer;

        public ArticleService(ApplicationDbContext context, IHtmlSanitizerService htmlSanitizer)
        {
            _context = context;
            _htmlSanitizer = htmlSanitizer;
        }

        public async Task<ArticleListViewModel> GetArticlesAsync(string? category, string? search, int page = 1, int pageSize = 10)
        {
            var query = _context.Articles
                .Where(a => a.IsPublished)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(a => a.Category == category);
            }

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(a => a.Title.ToLower().Contains(search) ||
                                         a.Summary.ToLower().Contains(search) ||
                                         a.Tags!.ToLower().Contains(search));
            }

            var totalArticles = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalArticles / (double)pageSize);

            var articles = await query
                .OrderByDescending(a => a.PublishedAt ?? a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new ArticleViewModel
                {
                    Id = a.Id,
                    Title = a.Title,
                    Summary = a.Summary,
                    Content = a.Content,
                    FeaturedImageUrl = a.FeaturedImageUrl,
                    VideoUrl = a.VideoUrl,
                    Category = a.Category,
                    Tags = a.Tags,
                    Slug = a.Slug,
                    AuthorName = a.AuthorName,
                    CreatedAt = a.CreatedAt,
                    PublishedAt = a.PublishedAt,
                    IsPublished = a.IsPublished,
                    IsFeatured = a.IsFeatured,
                    ViewCount = a.ViewCount,
                    LikeCount = a.LikeCount,
                    CommentCount = a.Comments != null ? a.Comments.Count(c => c.IsApproved) : 0
                })
                .ToListAsync();

            return new ArticleListViewModel
            {
                Articles = articles,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalArticles = totalArticles,
                Category = category,
                SearchTerm = search,
                Categories = await GetCategoriesAsync(),
                FeaturedArticles = await GetFeaturedArticlesAsync(3),
                PopularTags = await GetPopularTagsAsync(15)
            };
        }

        public async Task<ArticleViewModel?> GetArticleByIdAsync(int id)
        {
            var article = await _context.Articles
                .Include(a => a.Comments!.Where(c => c.IsApproved))
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null) return null;

            return MapToViewModel(article);
        }

        public async Task<ArticleViewModel?> GetArticleBySlugAsync(string slug)
        {
            var article = await _context.Articles
                .Include(a => a.Comments!.Where(c => c.IsApproved))
                .FirstOrDefaultAsync(a => a.Slug == slug && a.IsPublished);

            if (article == null) return null;

            return MapToViewModel(article);
        }

        public async Task<List<ArticleViewModel>> GetFeaturedArticlesAsync(int count = 5)
        {
            return await _context.Articles
                .Where(a => a.IsPublished && a.IsFeatured)
                .OrderByDescending(a => a.PublishedAt ?? a.CreatedAt)
                .Take(count)
                .Select(a => new ArticleViewModel
                {
                    Id = a.Id,
                    Title = a.Title,
                    Summary = a.Summary,
                    FeaturedImageUrl = a.FeaturedImageUrl,
                    Category = a.Category,
                    Slug = a.Slug,
                    AuthorName = a.AuthorName,
                    PublishedAt = a.PublishedAt,
                    ViewCount = a.ViewCount
                })
                .ToListAsync();
        }

        public async Task<List<ArticleViewModel>> GetRelatedArticlesAsync(int articleId, int count = 4)
        {
            var article = await _context.Articles.FindAsync(articleId);
            if (article == null) return new();

            return await _context.Articles
                .Where(a => a.IsPublished && a.Id != articleId && a.Category == article.Category)
                .OrderByDescending(a => a.ViewCount)
                .Take(count)
                .Select(a => new ArticleViewModel
                {
                    Id = a.Id,
                    Title = a.Title,
                    Summary = a.Summary,
                    FeaturedImageUrl = a.FeaturedImageUrl,
                    Category = a.Category,
                    Slug = a.Slug,
                    PublishedAt = a.PublishedAt
                })
                .ToListAsync();
        }

        public async Task<List<ArticleViewModel>> GetRecentArticlesAsync(int count = 5)
        {
            return await _context.Articles
                .Where(a => a.IsPublished)
                .OrderByDescending(a => a.PublishedAt ?? a.CreatedAt)
                .Take(count)
                .Select(a => new ArticleViewModel
                {
                    Id = a.Id,
                    Title = a.Title,
                    Summary = a.Summary,
                    FeaturedImageUrl = a.FeaturedImageUrl,
                    Category = a.Category,
                    Slug = a.Slug,
                    PublishedAt = a.PublishedAt
                })
                .ToListAsync();
        }

        public async Task<ApiResponse<ArticleViewModel>> CreateArticleAsync(ArticleCreateViewModel model, string authorId, string authorName)
        {
            try
            {
                var article = new Article
                {
                    Title = model.Title,
                    Summary = model.Summary,
                    Content = _htmlSanitizer.Sanitize(model.Content),
                    FeaturedImageUrl = model.FeaturedImageUrl,
                    VideoUrl = model.VideoUrl,
                    Category = model.Category,
                    Tags = model.Tags,
                    MetaDescription = model.MetaDescription ?? model.Summary.Substring(0, Math.Min(160, model.Summary.Length)),
                    Slug = GenerateSlug(model.Title),
                    AuthorId = authorId,
                    AuthorName = authorName,
                    CreatedAt = DateTime.UtcNow,
                    IsPublished = model.IsPublished,
                    IsFeatured = model.IsFeatured,
                    PublishedAt = model.IsPublished ? DateTime.UtcNow : null,
                    ExternalLinks = model.ExternalLinks
                };

                _context.Articles.Add(article);
                await _context.SaveChangesAsync();

                return new ApiResponse<ArticleViewModel>
                {
                    Success = true,
                    Message = "Article created successfully",
                    Data = MapToViewModel(article)
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ArticleViewModel>
                {
                    Success = false,
                    Message = $"Error creating article: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<ArticleViewModel>> UpdateArticleAsync(ArticleCreateViewModel model)
        {
            try
            {
                var article = await _context.Articles.FindAsync(model.Id);
                if (article == null)
                {
                    return new ApiResponse<ArticleViewModel> { Success = false, Message = "Article not found" };
                }

                var wasPublished = article.IsPublished;

                article.Title = model.Title;
                article.Summary = model.Summary;
                article.Content = _htmlSanitizer.Sanitize(model.Content);
                article.FeaturedImageUrl = model.FeaturedImageUrl;
                article.VideoUrl = model.VideoUrl;
                article.Category = model.Category;
                article.Tags = model.Tags;
                article.MetaDescription = model.MetaDescription;
                article.IsPublished = model.IsPublished;
                article.IsFeatured = model.IsFeatured;
                article.UpdatedAt = DateTime.UtcNow;
                article.ExternalLinks = model.ExternalLinks;

                // Set published date if publishing for first time
                if (model.IsPublished && !wasPublished)
                {
                    article.PublishedAt = DateTime.UtcNow;
                }

                // Update slug if title changed
                if (article.Slug != GenerateSlug(model.Title))
                {
                    article.Slug = GenerateSlug(model.Title);
                }

                await _context.SaveChangesAsync();

                return new ApiResponse<ArticleViewModel>
                {
                    Success = true,
                    Message = "Article updated successfully",
                    Data = MapToViewModel(article)
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ArticleViewModel>
                {
                    Success = false,
                    Message = $"Error updating article: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<bool>> DeleteArticleAsync(int id)
        {
            try
            {
                var article = await _context.Articles
                    .Include(a => a.Comments)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (article == null)
                {
                    return new ApiResponse<bool> { Success = false, Message = "Article not found" };
                }

                if (article.Comments?.Any() == true)
                {
                    _context.ArticleComments.RemoveRange(article.Comments);
                }

                _context.Articles.Remove(article);
                await _context.SaveChangesAsync();

                return new ApiResponse<bool> { Success = true, Message = "Article deleted successfully", Data = true };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool> { Success = false, Message = $"Error deleting article: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<bool>> TogglePublishAsync(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null)
            {
                return new ApiResponse<bool> { Success = false, Message = "Article not found" };
            }

            article.IsPublished = !article.IsPublished;
            if (article.IsPublished && article.PublishedAt == null)
            {
                article.PublishedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return new ApiResponse<bool>
            {
                Success = true,
                Message = article.IsPublished ? "Article published" : "Article unpublished",
                Data = article.IsPublished
            };
        }

        public async Task<ApiResponse<bool>> ToggleFeaturedAsync(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article == null)
            {
                return new ApiResponse<bool> { Success = false, Message = "Article not found" };
            }

            article.IsFeatured = !article.IsFeatured;
            await _context.SaveChangesAsync();

            return new ApiResponse<bool>
            {
                Success = true,
                Message = article.IsFeatured ? "Article marked as featured" : "Article removed from featured",
                Data = article.IsFeatured
            };
        }

        public async Task IncrementViewCountAsync(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            if (article != null)
            {
                article.ViewCount++;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<ApiResponse<bool>> AddCommentAsync(AddCommentRequest request, string? userId)
        {
            try
            {
                var comment = new ArticleComment
                {
                    ArticleId = request.ArticleId,
                    UserId = userId,
                    Name = request.Name,
                    Email = request.Email,
                    Content = request.Content,
                    CreatedAt = DateTime.UtcNow,
                    IsApproved = false // Require admin approval
                };

                _context.ArticleComments.Add(comment);
                await _context.SaveChangesAsync();

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Comment submitted. It will appear after approval.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error adding comment: {ex.Message}"
                };
            }
        }

        public async Task<List<CommentViewModel>> GetCommentsAsync(int articleId, bool approvedOnly = true)
        {
            var query = _context.ArticleComments
                .Where(c => c.ArticleId == articleId);

            if (approvedOnly)
            {
                query = query.Where(c => c.IsApproved);
            }

            return await query
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    IsApproved = c.IsApproved
                })
                .ToListAsync();
        }

        public async Task<ApiResponse<bool>> ApproveCommentAsync(int commentId)
        {
            var comment = await _context.ArticleComments.FindAsync(commentId);
            if (comment == null)
            {
                return new ApiResponse<bool> { Success = false, Message = "Comment not found" };
            }

            comment.IsApproved = true;
            await _context.SaveChangesAsync();

            return new ApiResponse<bool> { Success = true, Message = "Comment approved", Data = true };
        }

        public async Task<ApiResponse<bool>> DeleteCommentAsync(int commentId)
        {
            var comment = await _context.ArticleComments.FindAsync(commentId);
            if (comment == null)
            {
                return new ApiResponse<bool> { Success = false, Message = "Comment not found" };
            }

            _context.ArticleComments.Remove(comment);
            await _context.SaveChangesAsync();

            return new ApiResponse<bool> { Success = true, Message = "Comment deleted", Data = true };
        }

        public async Task<List<string>> GetPopularTagsAsync(int count = 10)
        {
            var articles = await _context.Articles
                .Where(a => a.IsPublished && !string.IsNullOrEmpty(a.Tags))
                .Select(a => a.Tags)
                .ToListAsync();

            var tags = articles
                .SelectMany(t => t!.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(t => t.Trim())
                .GroupBy(t => t.ToLower())
                .OrderByDescending(g => g.Count())
                .Take(count)
                .Select(g => g.First())
                .ToList();

            return tags;
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            // Return all available categories (from the predefined list)
            // This ensures categories are always available even if no articles exist yet
            var predefinedCategories = ArticleCreateViewModel.AvailableCategories;
            
            // Also get any custom categories from existing articles
            var existingCategories = await _context.Articles
                .Where(a => a.IsPublished)
                .Select(a => a.Category)
                .Distinct()
                .ToListAsync();
            
            // Combine and return unique categories
            return predefinedCategories
                .Union(existingCategories)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }

        public async Task<PaginatedResult<ArticleViewModel>> GetAllArticlesAdminAsync(string? search, string? category, int page = 1, int pageSize = 20)
        {
            var query = _context.Articles.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(a => a.Category == category);
            }

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(a => a.Title.ToLower().Contains(search));
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var articles = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new ArticleViewModel
                {
                    Id = a.Id,
                    Title = a.Title,
                    Summary = a.Summary,
                    Category = a.Category,
                    Slug = a.Slug,
                    AuthorName = a.AuthorName,
                    CreatedAt = a.CreatedAt,
                    PublishedAt = a.PublishedAt,
                    IsPublished = a.IsPublished,
                    IsFeatured = a.IsFeatured,
                    ViewCount = a.ViewCount,
                    CommentCount = a.Comments != null ? a.Comments.Count : 0
                })
                .ToListAsync();

            return new PaginatedResult<ArticleViewModel>
            {
                Items = articles,
                CurrentPage = page,
                TotalCount = totalItems,
                PageSize = pageSize
            };
        }

        public async Task<int> GetPendingCommentsCountAsync()
        {
            return await _context.ArticleComments.CountAsync(c => !c.IsApproved);
        }

        public async Task<List<CommentViewModel>> GetPendingCommentsAsync()
        {
            return await _context.ArticleComments
                .Where(c => !c.IsApproved)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    IsApproved = c.IsApproved
                })
                .ToListAsync();
        }

        private ArticleViewModel MapToViewModel(Article article)
        {
            return new ArticleViewModel
            {
                Id = article.Id,
                Title = article.Title,
                Summary = article.Summary,
                Content = article.Content,
                FeaturedImageUrl = article.FeaturedImageUrl,
                VideoUrl = article.VideoUrl,
                Category = article.Category,
                Tags = article.Tags,
                Slug = article.Slug,
                AuthorName = article.AuthorName,
                CreatedAt = article.CreatedAt,
                PublishedAt = article.PublishedAt,
                IsPublished = article.IsPublished,
                IsFeatured = article.IsFeatured,
                ViewCount = article.ViewCount,
                LikeCount = article.LikeCount,
                ExternalLinks = article.ExternalLinks,
                CommentCount = article.Comments?.Count(c => c.IsApproved) ?? 0,
                Comments = article.Comments?
                    .Where(c => c.IsApproved)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new CommentViewModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Content = c.Content,
                        CreatedAt = c.CreatedAt,
                        IsApproved = c.IsApproved
                    })
                    .ToList() ?? new()
            };
        }

        private string GenerateSlug(string title)
        {
            var slug = title.ToLower()
                .Replace(" ", "-")
                .Replace("&", "and");

            // Remove special characters
            slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");

            // Remove multiple dashes
            slug = Regex.Replace(slug, @"-+", "-");

            // Trim dashes from ends
            slug = slug.Trim('-');

            // Add timestamp to ensure uniqueness
            return $"{slug}-{DateTime.UtcNow.Ticks % 10000}";
        }
    }
}
