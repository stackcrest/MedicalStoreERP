using Microsoft.AspNetCore.Mvc;
using MedicalStoreERP.Services;
using MedicalStoreERP.Models.ViewModels;

namespace MedicalStoreERP.Controllers
{
    public class ShopController : Controller
    {
        private readonly IMedicineService _medicineService;
        private readonly ICategoryService _categoryService;
        private readonly IWishlistService _wishlistService;
        private readonly ICartService _cartService;

        public ShopController(
            IMedicineService medicineService,
            ICategoryService categoryService,
            IWishlistService wishlistService,
            ICartService cartService)
        {
            _medicineService = medicineService;
            _categoryService = categoryService;
            _wishlistService = wishlistService;
            _cartService = cartService;
        }

        public async Task<IActionResult> Index(
            string? search,
            int? category,
            decimal? minPrice,
            decimal? maxPrice,
            bool? inStock,
            string? sort,
            int page = 1)
        {
            var pageSize = 12;

            var result = await _medicineService.GetMedicinesAsync(
                searchTerm: search,
                categoryId: category,
                minPrice: minPrice,
                maxPrice: maxPrice,
                inStock: inStock,
                sortBy: sort,
                page: page,
                pageSize: pageSize);

            var model = new MedicineListViewModel
            {
                Medicines = result.Items,
                Categories = await _categoryService.GetAllCategoriesAsync()
                    .ContinueWith(t => t.Result.Select(c => new Models.Category 
                    { 
                        Id = c.Id, 
                        Name = c.Name 
                    }).ToList()),
                TotalItems = result.TotalCount,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = result.TotalPages,
                SearchTerm = search,
                CategoryId = category,
                SortBy = sort,
                InStock = inStock,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };

            return View(model);
        }

        [Route("Shop/Details/{id}")]
        [Route("Shop/Detail/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.Identity?.IsAuthenticated == true 
                ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                : null;

            var model = await _medicineService.GetMedicineDetailAsync(id, userId);

            if (model.Medicine == null || model.Medicine.Id == 0)
            {
                return NotFound();
            }

            return View(model);
        }

        public async Task<IActionResult> Category(int id, string? sort, int page = 1)
        {
            var pageSize = 12;

            var categories = await _categoryService.GetAllCategoriesAsync();
            var category = categories.FirstOrDefault(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            var result = await _medicineService.GetMedicinesAsync(
                categoryId: id,
                sortBy: sort,
                page: page,
                pageSize: pageSize);

            var model = new MedicineListViewModel
            {
                Medicines = result.Items,
                Categories = categories.Select(c => new Models.Category 
                { 
                    Id = c.Id, 
                    Name = c.Name 
                }).ToList(),
                TotalItems = result.TotalCount,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = result.TotalPages,
                CategoryId = id,
                SortBy = sort
            };

            ViewBag.CategoryName = category.Name;
            return View("Index", model);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index", new { search = q });
        }

        [HttpGet]
        public async Task<IActionResult> GetMedicinesJson(
            string? search,
            int? category,
            string? sort,
            int page = 1,
            int pageSize = 12)
        {
            var result = await _medicineService.GetMedicinesAsync(
                searchTerm: search,
                categoryId: category,
                sortBy: sort,
                page: page,
                pageSize: pageSize);

            return Json(new
            {
                success = true,
                data = result.Items,
                totalCount = result.TotalCount,
                currentPage = result.CurrentPage,
                totalPages = result.TotalPages
            });
        }

        [HttpGet]
        public async Task<IActionResult> QuickSearch(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new { results = new List<object>() });
            }

            var result = await _medicineService.GetMedicinesAsync(
                searchTerm: term,
                page: 1,
                pageSize: 5);

            var suggestions = result.Items.Select(m => new
            {
                id = m.Id,
                name = m.Name,
                price = m.SalePrice,
                image = m.ImageUrl ?? "/images/medicine-placeholder.png"
            });

            return Json(new { results = suggestions });
        }
    }
}
