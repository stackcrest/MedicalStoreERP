using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedicalStoreERP.Services;
using MedicalStoreERP.Models.ViewModels;
using System.Security.Claims;

namespace MedicalStoreERP.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private bool IsAdminOrSuperAdmin() => User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

        public async Task<IActionResult> Index()
        {
            if (IsAdminOrSuperAdmin())
            {
                TempData["Warning"] = "Cart functionality is not available for admin users. Please use the Admin Dashboard for order management.";
                return RedirectToAction("Index", "Shop");
            }

            var userId = GetUserId();
            var cart = await _cartService.GetCartAsync(userId);
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddToCartRequest request)
        {
            if (IsAdminOrSuperAdmin())
            {
                return Json(new { success = false, message = "Cart functionality is not available for admin users." });
            }

            var userId = GetUserId();
            var result = await _cartService.AddToCartAsync(userId, request.MedicineId, request.Quantity);

            if (result.Success)
            {
                var cartCount = await _cartService.GetCartCountAsync(userId);
                return Json(new { 
                    success = true, 
                    message = result.Message,
                    cartCount = cartCount,
                    data = result.Data 
                });
            }

            return Json(new { success = false, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] UpdateCartRequest request)
        {
            var userId = GetUserId();
            var result = await _cartService.UpdateCartItemAsync(userId, request.CartItemId, request.Quantity);

            if (result.Success)
            {
                var cart = await _cartService.GetCartAsync(userId);
                return Json(new { 
                    success = true, 
                    message = result.Message,
                    cart = cart
                });
            }

            return Json(new { success = false, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = GetUserId();
            var result = await _cartService.RemoveFromCartAsync(userId, id);

            if (result.Success)
            {
                var cart = await _cartService.GetCartAsync(userId);
                return Json(new { 
                    success = true, 
                    message = result.Message,
                    cart = cart
                });
            }

            return Json(new { success = false, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            var userId = GetUserId();
            var result = await _cartService.ClearCartAsync(userId);

            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetUserId();
            var cart = await _cartService.GetCartAsync(userId);
            return Json(new { success = true, cart = cart });
        }

        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var userId = GetUserId();
            var count = await _cartService.GetCartCountAsync(userId);
            return Json(new { count = count });
        }

        [HttpGet]
        public async Task<IActionResult> MiniCart()
        {
            var userId = GetUserId();
            var cart = await _cartService.GetCartAsync(userId);
            return PartialView("_MiniCart", cart);
        }
    }
}
