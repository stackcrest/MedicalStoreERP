using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedicalStoreERP.Models.ViewModels;
using MedicalStoreERP.Services;
using System.Security.Claims;

namespace MedicalStoreERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class POSController : Controller
    {
        private readonly IOfflineSaleService _offlineSaleService;
        private readonly ILogger<POSController> _logger;

        public POSController(IOfflineSaleService offlineSaleService, ILogger<POSController> logger)
        {
            _offlineSaleService = offlineSaleService;
            _logger = logger;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        // GET: Admin/POS
        public async Task<IActionResult> Index()
        {
            var medicines = await _offlineSaleService.SearchMedicinesAsync("");
            var model = new POSViewModel
            {
                AvailableMedicines = medicines
            };
            return View(model);
        }

        // GET: Admin/POS/SearchMedicines
        [HttpGet]
        public async Task<IActionResult> SearchMedicines(string query)
        {
            var medicines = await _offlineSaleService.SearchMedicinesAsync(query ?? "");
            return Json(medicines);
        }

        // GET: Admin/POS/GetMedicine/5
        [HttpGet]
        public async Task<IActionResult> GetMedicine(int id)
        {
            var medicine = await _offlineSaleService.GetMedicineByIdAsync(id);
            if (medicine == null)
                return NotFound();
            return Json(medicine);
        }

        // POST: Admin/POS/CreateSale
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSale([FromBody] CreateOfflineSaleRequest request)
        {
            try
            {
                if (request.Items == null || !request.Items.Any())
                {
                    return BadRequest(new { success = false, message = "No items in cart" });
                }

                var userId = GetUserId();
                var sale = await _offlineSaleService.CreateSaleAsync(request, userId);

                if (sale == null)
                {
                    return BadRequest(new { success = false, message = "Failed to create sale" });
                }

                return Json(new { success = true, saleId = sale.Id, invoiceNumber = sale.InvoiceNumber });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating offline sale");
                return BadRequest(new { success = false, message = "An error occurred while processing the sale" });
            }
        }

        // GET: Admin/POS/Invoice/5
        public async Task<IActionResult> Invoice(int id)
        {
            var sale = await _offlineSaleService.GetSaleByIdAsync(id);
            if (sale == null)
                return NotFound();

            return View(sale);
        }

        // GET: Admin/POS/Sales
        public async Task<IActionResult> Sales(int page = 1, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var sales = await _offlineSaleService.GetSalesAsync(page, 20, fromDate, toDate);
            return View(sales);
        }

        // GET: Admin/POS/SaleDetails/5
        public async Task<IActionResult> SaleDetails(int id)
        {
            var sale = await _offlineSaleService.GetSaleByIdAsync(id);
            if (sale == null)
                return NotFound();

            return View(sale);
        }
    }
}
