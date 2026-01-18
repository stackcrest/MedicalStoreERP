using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MedicalStoreERP.Models.ViewModels;
using MedicalStoreERP.Services;

namespace MedicalStoreERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class GRNController : Controller
    {
        private readonly IGRNService _grnService;
        private readonly ISupplierService _supplierService;
        private readonly IMedicineService _medicineService;
        private readonly ILogger<GRNController> _logger;

        public GRNController(
            IGRNService grnService,
            ISupplierService supplierService,
            IMedicineService medicineService,
            ILogger<GRNController> logger)
        {
            _grnService = grnService;
            _supplierService = supplierService;
            _medicineService = medicineService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? supplierId, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            var grns = await _grnService.GetAllGRNsAsync(
                fromDate: fromDate,
                toDate: toDate,
                supplierId: supplierId
            );

            var suppliers = await _supplierService.GetAllSuppliersAsync();
            ViewBag.Suppliers = new SelectList(suppliers, "Id", "Name", supplierId);
            ViewBag.SupplierId = supplierId;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(grns);
        }

        public async Task<IActionResult> Create()
        {
            var suppliers = await _supplierService.GetAllSuppliersAsync();
            ViewBag.Suppliers = new SelectList(suppliers, "Id", "Name");

            return View(new GRNCreateViewModel
            {
                GRNDate = DateTime.Today,
                Items = new List<GRNItemCreateViewModel>()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GRNCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var suppliers = await _supplierService.GetAllSuppliersAsync();
                ViewBag.Suppliers = new SelectList(suppliers, "Id", "Name", model.SupplierId);
                return View(model);
            }

            if (model.Items == null || !model.Items.Any())
            {
                ModelState.AddModelError("", "Please add at least one item to the GRN.");
                var suppliers = await _supplierService.GetAllSuppliersAsync();
                ViewBag.Suppliers = new SelectList(suppliers, "Id", "Name", model.SupplierId);
                return View(model);
            }

            try
            {
                var createdBy = User.Identity?.Name ?? "System";
                var result = await _grnService.CreateAsync(model, createdBy);
                if (!result.Success || result.Data == null)
                {
                    ModelState.AddModelError("", result.Message ?? "An error occurred while creating the GRN.");
                    var suppliers = await _supplierService.GetAllSuppliersAsync();
                    ViewBag.Suppliers = new SelectList(suppliers, "Id", "Name", model.SupplierId);
                    return View(model);
                }
                TempData["Success"] = $"GRN '{result.Data.GRNNumber}' created successfully. Stock has been updated.";
                return RedirectToAction(nameof(Details), new { id = result.Data.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating GRN");
                ModelState.AddModelError("", "An error occurred while creating the GRN.");
                var suppliers = await _supplierService.GetAllSuppliersAsync();
                ViewBag.Suppliers = new SelectList(suppliers, "Id", "Name", model.SupplierId);
                return View(model);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var grn = await _grnService.GetByIdAsync(id);
            if (grn == null)
            {
                return NotFound();
            }

            return View(grn);
        }

        // Note: Delete functionality not available - GRNs are permanent records for inventory tracking
        // If delete is needed, add DeleteAsync method to IGRNService interface first

        [HttpGet]
        public async Task<IActionResult> SearchMedicines(string term)
        {
            var result = await _medicineService.GetMedicinesAsync(searchTerm: term, pageSize: 10);
            return Json(result.Items.Select(m => new
            {
                m.Id,
                m.Name,
                m.GenericName,
                BatchNumber = m.BatchNo,
                CostPrice = m.PurchasePrice,
                SellingPrice = m.SalePrice,
                StockQuantity = m.StockQty,
                ExpiryDate = m.ExpiryDate.ToString("yyyy-MM-dd")
            }));
        }

        [HttpPost]
        public IActionResult AddItemRow()
        {
            return PartialView("_GRNItemRow", new GRNItemViewModel());
        }

        public async Task<IActionResult> Print(int id)
        {
            var grn = await _grnService.GetByIdAsync(id);
            if (grn == null)
            {
                return NotFound();
            }

            return View(grn);
        }
    }
}
