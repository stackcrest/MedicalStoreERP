using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedicalStoreERP.Services;
using System.Security.Claims;

namespace MedicalStoreERP.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private bool IsAdminOrSuperAdmin() => User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

        public async Task<IActionResult> Index()
        {
            if (IsAdminOrSuperAdmin())
            {
                TempData["Warning"] = "Wishlist functionality is not available for admin users.";
                return RedirectToAction("Index", "Shop");
            }

            var userId = GetUserId();
            var wishlist = await _wishlistService.GetWishlistAsync(userId);
            return View(wishlist);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int medicineId)
        {
            if (IsAdminOrSuperAdmin())
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Wishlist functionality is not available for admin users." });
                }
                TempData["Warning"] = "Wishlist functionality is not available for admin users.";
                return RedirectToAction("Index", "Shop");
            }

            var userId = GetUserId();
            var result = await _wishlistService.AddToWishlistAsync(userId, medicineId);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var count = await _wishlistService.GetWishlistCountAsync(userId);
                return Json(new { success = result.Success, message = result.Message, count = count });
            }

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int medicineId)
        {
            var userId = GetUserId();
            var result = await _wishlistService.RemoveFromWishlistAsync(userId, medicineId);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var count = await _wishlistService.GetWishlistCountAsync(userId);
                return Json(new { success = result.Success, message = result.Message, count = count });
            }

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetCount()
        {
            var userId = GetUserId();
            var count = await _wishlistService.GetWishlistCountAsync(userId);
            return Json(new { count = count });
        }

        [HttpGet]
        public async Task<IActionResult> Check(int medicineId)
        {
            var userId = GetUserId();
            var isInWishlist = await _wishlistService.IsInWishlistAsync(userId, medicineId);
            return Json(new { isInWishlist = isInWishlist });
        }
    }
}
