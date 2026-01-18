using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MedicalStoreERP.Models.ViewModels;
using MedicalStoreERP.Services;

namespace MedicalStoreERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class SuppliersController : Controller
    {
        private readonly ISupplierService _supplierService;
        private readonly ILogger<SuppliersController> _logger;

        public SuppliersController(
            ISupplierService supplierService,
            ILogger<SuppliersController> logger)
        {
            _supplierService = supplierService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, bool? activeOnly, int page = 1)
        {
            var allSuppliers = await _supplierService.GetAllSuppliersAsync();
            
            // Apply filters
            var filteredSuppliers = allSuppliers.AsQueryable();
            
            if (!string.IsNullOrEmpty(search))
            {
                filteredSuppliers = filteredSuppliers.Where(s => 
                    s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    s.ContactPerson.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    s.Phone.Contains(search, StringComparison.OrdinalIgnoreCase));
            }
            
            if (activeOnly == true)
            {
                filteredSuppliers = filteredSuppliers.Where(s => s.IsActive);
            }

            ViewBag.Search = search;
            ViewBag.ActiveOnly = activeOnly;

            return View(filteredSuppliers.ToList());
        }

        public IActionResult Create()
        {
            return View(new SupplierCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupplierCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _supplierService.CreateAsync(model);
                if (result.Success && result.Data != null)
                {
                    TempData["Success"] = $"Supplier '{result.Data.Name}' created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", result.Message ?? "An error occurred while creating the supplier.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier");
                ModelState.AddModelError("", "An error occurred while creating the supplier.");
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await _supplierService.GetByIdAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }

            var model = new SupplierUpdateViewModel
            {
                Id = supplier.Id,
                Name = supplier.Name,
                ContactPerson = supplier.ContactPerson,
                Email = supplier.Email,
                Phone = supplier.Phone,
                Address = supplier.Address ?? string.Empty,
                City = supplier.City ?? string.Empty,
                State = supplier.State,
                PinCode = supplier.PinCode,
                GSTIN = supplier.GSTIN,
                PAN = supplier.PAN,
                IsActive = supplier.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SupplierUpdateViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _supplierService.UpdateAsync(id, model);
                if (!result.Success)
                {
                    if (result.Message?.Contains("not found") == true)
                    {
                        return NotFound();
                    }
                    ModelState.AddModelError("", result.Message ?? "An error occurred while updating the supplier.");
                    return View(model);
                }

                TempData["Success"] = $"Supplier '{result.Data?.Name}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier {SupplierId}", id);
                ModelState.AddModelError("", "An error occurred while updating the supplier.");
                return View(model);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var supplier = await _supplierService.GetByIdAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _supplierService.DeleteAsync(id);
                if (!result.Success)
                {
                    TempData["Error"] = result.Message ?? "Supplier not found or cannot be deleted (may have associated GRNs).";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Success"] = "Supplier deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting supplier {SupplierId}", id);
                TempData["Error"] = "An error occurred while deleting the supplier.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Ledger(int id)
        {
            var supplier = await _supplierService.GetByIdAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }

            var ledger = await _supplierService.GetLedgerAsync(id);
            ViewBag.Supplier = supplier;

            return View(ledger);
        }

        // Note: RecordPayment functionality needs to be added to ISupplierService if payment recording is required
        // For now, this action is disabled until the service method is implemented
        /*
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(int id, decimal amount, string? reference)
        {
            try
            {
                // TODO: Implement RecordPaymentAsync in ISupplierService
                TempData["Error"] = "Payment recording is not yet implemented.";
                return RedirectToAction(nameof(Ledger), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording payment for supplier {SupplierId}", id);
                TempData["Error"] = "An error occurred while recording the payment.";
                return RedirectToAction(nameof(Ledger), new { id });
            }
        }
        */

        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            var allSuppliers = await _supplierService.GetAllSuppliersAsync();
            var filtered = allSuppliers
                .Where(s => s.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            s.ContactPerson.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            s.Phone.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .Select(s => new { s.Id, s.Name, s.ContactPerson, s.Phone });
            return Json(filtered);
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplierSelect()
        {
            var allSuppliers = await _supplierService.GetAllSuppliersAsync();
            var activeSuppliers = allSuppliers.Where(s => s.IsActive).Select(s => new { s.Id, s.Name });
            return Json(activeSuppliers);
        }
    }
}
