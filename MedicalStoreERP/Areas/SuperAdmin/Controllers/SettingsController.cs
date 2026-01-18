using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;
using MedicalStoreERP.Services;
using System.Security.Claims;

namespace MedicalStoreERP.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Policy = "SuperAdminOnly")]
    public class SettingsController : Controller
    {
        private readonly IAppSettingsService _settingsService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(
            IAppSettingsService settingsService,
            IWebHostEnvironment webHostEnvironment,
            ILogger<SettingsController> logger)
        {
            _settingsService = settingsService;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        public async Task<IActionResult> Index()
        {
            var settings = await _settingsService.GetAllSettingEntitiesAsync();
            
            var groups = settings
                .GroupBy(s => s.SettingGroup ?? "Other")
                .Select(g => new SettingsEditViewModel
                {
                    Group = g.Key,
                    GroupDisplayName = GetGroupDisplayName(g.Key),
                    Settings = g.Select(s => new SettingItemViewModel
                    {
                        Id = s.Id,
                        Key = s.SettingKey,
                        Value = s.SettingValue,
                        Description = s.Description,
                        SettingType = s.SettingType,
                        Group = s.SettingGroup ?? "Other",
                        IsEditable = s.IsEditable,
                        UpdatedAt = s.UpdatedAt,
                        UpdatedByName = s.UpdatedByUser?.FullName
                    }).ToList()
                })
                .OrderBy(g => GetGroupOrder(g.Group))
                .ToList();

            ViewBag.Groups = groups.Select(g => g.Group).ToList();
            return View(groups);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string group)
        {
            if (string.IsNullOrEmpty(group))
                return RedirectToAction(nameof(Index));

            var settings = await _settingsService.GetSettingEntitiesByGroupAsync(group);
            
            var model = new SettingsEditViewModel
            {
                Group = group,
                GroupDisplayName = GetGroupDisplayName(group),
                Settings = settings.Select(s => new SettingItemViewModel
                {
                    Id = s.Id,
                    Key = s.SettingKey,
                    Value = s.SettingValue,
                    Description = s.Description,
                    SettingType = s.SettingType,
                    Group = s.SettingGroup ?? "Other",
                    IsEditable = s.IsEditable,
                    UpdatedAt = s.UpdatedAt,
                    UpdatedByName = s.UpdatedByUser?.FullName
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string group, BulkSettingsUpdateModel model)
        {
            var userId = GetUserId();

            try
            {
                foreach (var item in model.Settings)
                {
                    await _settingsService.SetSettingAsync(item.Key, item.Value, userId);
                }

                TempData["Success"] = $"{GetGroupDisplayName(group)} settings updated successfully!";
                _logger.LogInformation("Settings updated for group {Group} by user {UserId}", group, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating settings for group {Group}", group);
                TempData["Error"] = "An error occurred while updating settings.";
            }

            return RedirectToAction(nameof(Edit), new { group });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImage(string key, IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return Json(new { success = false, message = "No file selected" });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".ico" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return Json(new { success = false, message = "Invalid file type. Allowed: " + string.Join(", ", allowedExtensions) });
            }

            if (imageFile.Length > 5 * 1024 * 1024)
            {
                return Json(new { success = false, message = "File size must be less than 5MB" });
            }

            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "settings");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{key.ToLower().Replace(" ", "-")}_{Guid.NewGuid():N}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                var imageUrl = $"/uploads/settings/{uniqueFileName}";
                await _settingsService.SetSettingAsync(key, imageUrl, GetUserId());

                return Json(new { success = true, imageUrl, message = "Image uploaded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image for setting {Key}", key);
                return Json(new { success = false, message = "Error uploading image" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSetting(string key, string? value)
        {
            var result = await _settingsService.SetSettingAsync(key, value, GetUserId());
            
            if (result)
            {
                return Json(new { success = true, message = "Setting updated successfully" });
            }

            return Json(new { success = false, message = "Failed to update setting" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetDefaults()
        {
            try
            {
                await _settingsService.InitializeDefaultSettingsAsync();
                TempData["Success"] = "Settings have been reset to defaults.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting settings to defaults");
                TempData["Error"] = "An error occurred while resetting settings.";
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Preview()
        {
            var settings = _settingsService.GetCachedSettings();
            return View(settings);
        }

        [HttpGet]
        public IActionResult GetMapPreview(double lat, double lng, int zoom = 13, double radius = 10)
        {
            return PartialView("_MapPreview", new { Latitude = lat, Longitude = lng, Zoom = zoom, Radius = radius });
        }

        private static string GetGroupDisplayName(string group)
        {
            return group switch
            {
                SettingGroups.General => "🏪 General Settings",
                SettingGroups.Theme => "🎨 Theme & Colors",
                SettingGroups.Contact => "📞 Contact Information",
                SettingGroups.Business => "🏢 Business Details",
                SettingGroups.Map => "🗺️ Map & Delivery",
                SettingGroups.SocialMedia => "📱 Social Media",
                SettingGroups.WhatsApp => "💬 WhatsApp",
                SettingGroups.About => "ℹ️ About Page",
                SettingGroups.Footer => "📄 Footer",
                SettingGroups.SEO => "🔍 SEO Settings",
                SettingGroups.Ecommerce => "🛒 E-commerce",
                SettingGroups.Notifications => "🔔 Notifications",
                SettingGroups.Homepage => "🏠 Homepage",
                _ => group
            };
        }

        private static int GetGroupOrder(string group)
        {
            return group switch
            {
                SettingGroups.General => 1,
                SettingGroups.Theme => 2,
                SettingGroups.Contact => 3,
                SettingGroups.Business => 4,
                SettingGroups.Map => 5,
                SettingGroups.SocialMedia => 6,
                SettingGroups.WhatsApp => 7,
                SettingGroups.Homepage => 8,
                SettingGroups.About => 9,
                SettingGroups.Footer => 10,
                SettingGroups.SEO => 11,
                SettingGroups.Ecommerce => 12,
                SettingGroups.Notifications => 13,
                _ => 99
            };
        }
    }
}
