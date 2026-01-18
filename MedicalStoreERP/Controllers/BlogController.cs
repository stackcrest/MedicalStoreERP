using Microsoft.AspNetCore.Mvc;
using MedicalStoreERP.Services;
using MedicalStoreERP.Models.ViewModels;
using System.Security.Claims;

namespace MedicalStoreERP.Controllers
{
    public class BlogController : Controller
    {
        private readonly IArticleService _articleService;

        public BlogController(IArticleService articleService)
        {
            _articleService = articleService;
        }

        // GET: /Blog or /Blog/Index
        public async Task<IActionResult> Index(string? category, string? search, string? tag, int page = 1)
        {
            // If searching by tag, include it in search
            var searchTerm = !string.IsNullOrEmpty(tag) ? tag : search;

            var model = await _articleService.GetArticlesAsync(category, searchTerm, page, 9);
            
            ViewBag.SelectedTag = tag;
            
            return View(model);
        }

        // GET: /Blog/Read/{slug}
        [Route("Blog/Read/{slug}")]
        public async Task<IActionResult> Read(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return NotFound();
            }

            var article = await _articleService.GetArticleBySlugAsync(slug);
            if (article == null)
            {
                return NotFound();
            }

            // Increment view count
            await _articleService.IncrementViewCountAsync(article.Id);

            // Get related articles
            ViewBag.RelatedArticles = await _articleService.GetRelatedArticlesAsync(article.Id, 4);
            ViewBag.RecentArticles = await _articleService.GetRecentArticlesAsync(5);
            ViewBag.PopularTags = await _articleService.GetPopularTagsAsync(15);

            return View(article);
        }

        // GET: /Blog/Article/{id}
        public async Task<IActionResult> Article(int id)
        {
            var article = await _articleService.GetArticleByIdAsync(id);
            if (article == null || !article.IsPublished)
            {
                return NotFound();
            }

            // Increment view count
            await _articleService.IncrementViewCountAsync(id);

            // Redirect to slug URL for SEO
            return RedirectToAction("Read", new { slug = article.Slug });
        }

        // POST: /Blog/AddComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(AddCommentRequest request)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill all required fields correctly.";
                return RedirectToAction("Article", new { id = request.ArticleId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _articleService.AddCommentAsync(request, userId);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            var article = await _articleService.GetArticleByIdAsync(request.ArticleId);
            return RedirectToAction("Read", new { slug = article?.Slug });
        }

        // GET: /Blog/Category/{category}
        [Route("Blog/Category/{category}")]
        public async Task<IActionResult> Category(string category, int page = 1)
        {
            var model = await _articleService.GetArticlesAsync(category, null, page, 9);
            ViewBag.CurrentCategory = category;
            return View("Index", model);
        }

        // GET: /Blog/Tag/{tag}
        [Route("Blog/Tag/{tag}")]
        public async Task<IActionResult> Tag(string tag, int page = 1)
        {
            return RedirectToAction("Index", new { tag = tag, page = page });
        }
    }
}
