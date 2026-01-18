using Microsoft.AspNetCore.Mvc;
using MedicalStoreERP.Services;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;
using System.Security.Claims;

namespace MedicalStoreERP.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMedicineService _medicineService;
        private readonly ICategoryService _categoryService;
        private readonly ICartService _cartService;
        private readonly IWishlistService _wishlistService;
        private readonly IContactService _contactService;

        public HomeController(
            IMedicineService medicineService,
            ICategoryService categoryService,
            ICartService cartService,
            IWishlistService wishlistService,
            IContactService contactService)
        {
            _medicineService = medicineService;
            _categoryService = categoryService;
            _cartService = cartService;
            _wishlistService = wishlistService;
            _contactService = contactService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var model = new HomeViewModel
            {
                FeaturedMedicines = await _medicineService.GetFeaturedMedicinesAsync(8),
                NewArrivals = await _medicineService.GetNewArrivalsAsync(8),
                BestSellers = await _medicineService.GetBestSellersAsync(8),
                OnSale = await _medicineService.GetOnSaleMedicinesAsync(8),
                Categories = await _categoryService.GetAllCategoriesAsync()
            };

            if (!string.IsNullOrEmpty(userId))
            {
                model.CartItemCount = await _cartService.GetCartCountAsync(userId);
                model.WishlistCount = await _wishlistService.GetWishlistCountAsync(userId);
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            var model = new ContactMessage();
            
            // Pre-fill user info if logged in
            if (User.Identity?.IsAuthenticated == true)
            {
                model.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                model.Name = User.Identity.Name ?? "";
                model.Email = User.FindFirstValue(ClaimTypes.Email) ?? "";
            }
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactMessage model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Set user ID if logged in
            if (User.Identity?.IsAuthenticated == true)
            {
                model.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            }

            // Capture IP address
            model.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _contactService.SubmitContactAsync(model);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Contact));
            }

            TempData["Error"] = result.Message;
            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
