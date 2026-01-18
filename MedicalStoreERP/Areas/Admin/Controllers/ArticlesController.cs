using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedicalStoreERP.Services;
using MedicalStoreERP.Models.ViewModels;
using System.Security.Claims;
using System.Text.Json;

namespace MedicalStoreERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ArticlesController : Controller
    {
        private readonly IArticleService _articleService;

        public ArticlesController(IArticleService articleService)
        {
            _articleService = articleService;
        }

        // GET: /Admin/Articles
        public async Task<IActionResult> Index(string? search, string? category, int page = 1)
        {
            var articles = await _articleService.GetAllArticlesAdminAsync(search, category, page, 15);
            
            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.Categories = ArticleCreateViewModel.AvailableCategories;
            ViewBag.PendingComments = await _articleService.GetPendingCommentsCountAsync();

            return View(articles);
        }

        // GET: /Admin/Articles/Create
        public IActionResult Create()
        {
            var model = new ArticleCreateViewModel();
            ViewBag.Categories = ArticleCreateViewModel.AvailableCategories;
            return View(model);
        }

        // POST: /Admin/Articles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ArticleCreateViewModel model, string? linksJson)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = ArticleCreateViewModel.AvailableCategories;
                return View(model);
            }

            // Process external links
            if (!string.IsNullOrEmpty(linksJson))
            {
                model.ExternalLinks = linksJson;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var userName = User.Identity?.Name ?? "Admin";

            var result = await _articleService.CreateArticleAsync(model, userId, userName);

            if (result.Success)
            {
                TempData["Success"] = "Article created successfully!";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = result.Message;
            ViewBag.Categories = ArticleCreateViewModel.AvailableCategories;
            return View(model);
        }

        // GET: /Admin/Articles/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            var article = await _articleService.GetArticleByIdAsync(id);
            if (article == null)
            {
                return NotFound();
            }

            var model = new ArticleCreateViewModel
            {
                Id = article.Id,
                Title = article.Title,
                Summary = article.Summary,
                Content = article.Content,
                FeaturedImageUrl = article.FeaturedImageUrl,
                VideoUrl = article.VideoUrl,
                Category = article.Category,
                Tags = article.Tags,
                IsPublished = article.IsPublished,
                IsFeatured = article.IsFeatured,
                ExternalLinks = article.ExternalLinks
            };

            ViewBag.Categories = ArticleCreateViewModel.AvailableCategories;
            return View(model);
        }

        // POST: /Admin/Articles/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ArticleCreateViewModel model, string? linksJson)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = ArticleCreateViewModel.AvailableCategories;
                return View(model);
            }

            // Process external links
            if (!string.IsNullOrEmpty(linksJson))
            {
                model.ExternalLinks = linksJson;
            }

            var result = await _articleService.UpdateArticleAsync(model);

            if (result.Success)
            {
                TempData["Success"] = "Article updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = result.Message;
            ViewBag.Categories = ArticleCreateViewModel.AvailableCategories;
            return View(model);
        }

        // GET: /Admin/Articles/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var article = await _articleService.GetArticleByIdAsync(id);
            if (article == null)
            {
                return NotFound();
            }

            // Get all comments including unapproved
            article.Comments = await _articleService.GetCommentsAsync(id, false);

            return View(article);
        }

        // POST: /Admin/Articles/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _articleService.DeleteArticleAsync(id);

            if (result.Success)
            {
                TempData["Success"] = "Article deleted successfully!";
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Articles/TogglePublish/{id}
        [HttpPost]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var result = await _articleService.TogglePublishAsync(id);
            return Json(new { success = result.Success, message = result.Message, isPublished = result.Data });
        }

        // POST: /Admin/Articles/ToggleFeatured/{id}
        [HttpPost]
        public async Task<IActionResult> ToggleFeatured(int id)
        {
            var result = await _articleService.ToggleFeaturedAsync(id);
            return Json(new { success = result.Success, message = result.Message, isFeatured = result.Data });
        }

        // GET: /Admin/Articles/Comments
        public async Task<IActionResult> Comments()
        {
            var comments = await _articleService.GetPendingCommentsAsync();
            return View(comments);
        }

        // POST: /Admin/Articles/ApproveComment/{id}
        [HttpPost]
        public async Task<IActionResult> ApproveComment(int id)
        {
            var result = await _articleService.ApproveCommentAsync(id);
            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: /Admin/Articles/DeleteComment/{id}
        [HttpPost]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var result = await _articleService.DeleteCommentAsync(id);
            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: /Admin/Articles/UploadImage
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "No file uploaded" });
            }

            try
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(file.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    return Json(new { success = false, message = "Invalid file type" });
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "articles");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var url = $"/uploads/articles/{uniqueFileName}";
                return Json(new { success = true, url = url });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Upload failed: {ex.Message}" });
            }
        }
    }
}
