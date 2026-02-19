using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MedicalStoreERP.Models.ViewModels;
using MedicalStoreERP.Services;
using OfficeOpenXml;
using MedicalStoreERP.Models;

namespace MedicalStoreERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class MedicinesController : Controller
    {
        private readonly IMedicineService _medicineService;
        private readonly ICategoryService _categoryService;
        private readonly ILogger<MedicinesController> _logger;

        public MedicinesController(
            IMedicineService medicineService,
            ICategoryService categoryService,
            ILogger<MedicinesController> logger)
        {
            _medicineService = medicineService;
            _categoryService = categoryService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, int? categoryId, bool? lowStock, bool? nearExpiry, int page = 1)
        {
            var medicines = await _medicineService.GetMedicinesAsync(
                searchTerm: search,
                categoryId: categoryId,
                inStock: null,
                page: page,
                pageSize: 20
            );

            if (lowStock == true)
            {
                medicines.Items = medicines.Items.Where(m => m.IsLowStock).ToList();
            }

            if (nearExpiry == true)
            {
                medicines.Items = medicines.Items.Where(m => m.IsNearExpiry).ToList();
            }

            ViewBag.Categories = new SelectList(await _categoryService.GetAllCategoriesAsync(), "Id", "Name", categoryId);
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.LowStock = lowStock;
            ViewBag.NearExpiry = nearExpiry;

            return View(medicines);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _categoryService.GetAllCategoriesAsync(), "Id", "Name");
            return View(new MedicineCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MedicineCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(await _categoryService.GetAllCategoriesAsync(), "Id", "Name", model.CategoryId);
                return View(model);
            }

            try
            {
                var response = await _medicineService.CreateAsync(model);
                if (!response.Success)
                {
                    ModelState.AddModelError("", response.Message);
                    ViewBag.Categories = new SelectList(await _categoryService.GetAllCategoriesAsync(), "Id", "Name", model.CategoryId);
                    return View(model);
                }
                TempData["Success"] = $"Medicine '{response.Data?.Name}' created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating medicine");
                ModelState.AddModelError("", "An error occurred while creating the medicine.");
                ViewBag.Categories = new SelectList(await _categoryService.GetAllCategoriesAsync(), "Id", "Name", model.CategoryId);
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var medicine = await _medicineService.GetByIdAsync(id);
            if (medicine == null)
            {
                return NotFound();
            }

            var model = new MedicineUpdateViewModel
            {
                Id = medicine.Id,
                Name = medicine.Name,
                GenericName = medicine.GenericName,
                Description = medicine.Description,
                Manufacturer = medicine.Manufacturer,
                BatchNo = medicine.BatchNo,
                PurchasePrice = medicine.PurchasePrice,
                SalePrice = medicine.SalePrice,
                MRP = medicine.MRP,
                StockQty = medicine.StockQty,
                ReorderLevel = medicine.ReorderLevel,
                ExpiryDate = medicine.ExpiryDate,
                ManufactureDate = medicine.ManufactureDate,
                CategoryId = medicine.CategoryId,
                RequiresPrescription = medicine.RequiresPrescription,
                IsActive = medicine.IsActive,
                ImageUrl = medicine.ImageUrl,
                HSNCode = medicine.HSNCode,
                GSTPercent = medicine.GSTPercent,
                Composition = medicine.Composition,
                Dosage = medicine.Dosage,
                PackSize = medicine.PackSize,
                IsFeatured = medicine.IsFeatured
            };

            ViewBag.Categories = new SelectList(await _categoryService.GetAllCategoriesAsync(), "Id", "Name", model.CategoryId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MedicineUpdateViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(await _categoryService.GetAllCategoriesAsync(), "Id", "Name", model.CategoryId);
                return View(model);
            }

            try
            {
                var response = await _medicineService.UpdateAsync(id, model);
                if (!response.Success)
                {
                    if (response.Message == "Medicine not found")
                    {
                        return NotFound();
                    }
                    ModelState.AddModelError("", response.Message);
                    ViewBag.Categories = new SelectList(await _categoryService.GetAllCategoriesAsync(), "Id", "Name", model.CategoryId);
                    return View(model);
                }

                TempData["Success"] = $"Medicine '{response.Data?.Name}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating medicine {MedicineId}", id);
                ModelState.AddModelError("", "An error occurred while updating the medicine.");
                ViewBag.Categories = new SelectList(await _categoryService.GetAllCategoriesAsync(), "Id", "Name", model.CategoryId);
                return View(model);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var medicine = await _medicineService.GetByIdAsync(id);
            if (medicine == null)
            {
                return NotFound();
            }

            return View(medicine);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var response = await _medicineService.DeleteAsync(id);
                if (!response.Success)
                {
                    TempData["Error"] = response.Message;
                    return RedirectToAction(nameof(Index));
                }

                TempData["Success"] = "Medicine deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting medicine {MedicineId}", id);
                TempData["Error"] = "An error occurred while deleting the medicine.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int id, int quantity, bool isAddition = true)
        {
            try
            {
                var result = await _medicineService.UpdateStockAsync(id, quantity, isAddition);
                if (!result)
                {
                    return Json(new { success = false, message = "Medicine not found or insufficient stock." });
                }

                return Json(new { success = true, message = "Stock updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for medicine {MedicineId}", id);
                return Json(new { success = false, message = "An error occurred while updating stock." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLowStockMedicines()
        {
            var medicines = await _medicineService.GetLowStockMedicinesAsync();
            return Json(medicines.Select(m => new { m.Id, m.Name, m.StockQty, m.ReorderLevel }));
        }

        [HttpGet]
        public async Task<IActionResult> GetExpiringMedicines()
        {
            var medicines = await _medicineService.GetNearExpiryMedicinesAsync(30);
            return Json(medicines.Select(m => new { m.Id, m.Name, m.ExpiryDate, m.DaysToExpiry }));
        }

        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            var result = await _medicineService.GetMedicinesAsync(searchTerm: term, pageSize: 10);
            return Json(result.Items.Select(m => new { m.Id, m.Name, m.GenericName, m.SalePrice, m.StockQty }));
        }

        // Bulk Upload Actions
        public async Task<IActionResult> BulkUpload()
        {
            ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();
            return View(new BulkUploadViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> DownloadTemplate()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Medicines");

            // Add headers with formatting
            var headers = new[]
            {
                "Name*", "GenericName*", "CategoryName*", "BatchNo*", "ManufactureDate* (DD/MM/YYYY)",
                "ExpiryDate* (DD/MM/YYYY)", "MRP*", "SalePrice*", "PurchasePrice*", "StockQty*",
                "ReorderLevel", "HSNCode*", "GSTPercent*", "Manufacturer", "Composition",
                "Dosage", "PackSize", "Description", "RequiresPrescription (Yes/No)", "IsActive (Yes/No)"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }

            // Add sample data row
            worksheet.Cells[2, 1].Value = "Paracetamol 500mg";
            worksheet.Cells[2, 2].Value = "Paracetamol";
            worksheet.Cells[2, 3].Value = "Pain Relief";
            worksheet.Cells[2, 4].Value = "BATCH001";
            worksheet.Cells[2, 5].Value = DateTime.Today.AddMonths(-1).ToString("dd/MM/yyyy");
            worksheet.Cells[2, 6].Value = DateTime.Today.AddYears(2).ToString("dd/MM/yyyy");
            worksheet.Cells[2, 7].Value = 25.00;
            worksheet.Cells[2, 8].Value = 22.00;
            worksheet.Cells[2, 9].Value = 15.00;
            worksheet.Cells[2, 10].Value = 100;
            worksheet.Cells[2, 11].Value = 20;
            worksheet.Cells[2, 12].Value = "3004";
            worksheet.Cells[2, 13].Value = 12;
            worksheet.Cells[2, 14].Value = "Sun Pharma";
            worksheet.Cells[2, 15].Value = "Paracetamol 500mg";
            worksheet.Cells[2, 16].Value = "1-2 tablets";
            worksheet.Cells[2, 17].Value = "10 tablets";
            worksheet.Cells[2, 18].Value = "Pain reliever and fever reducer";
            worksheet.Cells[2, 19].Value = "No";
            worksheet.Cells[2, 20].Value = "Yes";

            // Add Instructions sheet
            var instructionSheet = package.Workbook.Worksheets.Add("Instructions");
            instructionSheet.Cells[1, 1].Value = "MEDICINE BULK UPLOAD INSTRUCTIONS";
            instructionSheet.Cells[1, 1].Style.Font.Bold = true;
            instructionSheet.Cells[1, 1].Style.Font.Size = 14;

            var instructions = new[]
            {
                "",
                "1. Fields marked with * are mandatory.",
                "2. Do not modify the header row.",
                "3. CategoryName must match an existing category exactly (case-sensitive).",
                "4. Date format must be DD/MM/YYYY (e.g., 17/01/2026).",
                "5. MRP, SalePrice, PurchasePrice should be numeric values.",
                "6. StockQty and ReorderLevel should be whole numbers.",
                "7. GSTPercent should be a value like 5, 12, 18, or 28.",
                "8. RequiresPrescription and IsActive accept 'Yes' or 'No'.",
                "9. Delete the sample row before uploading your data.",
                "",
                "AVAILABLE CATEGORIES:"
            };

            for (int i = 0; i < instructions.Length; i++)
            {
                instructionSheet.Cells[i + 2, 1].Value = instructions[i];
            }

            // Add available categories
            var categories = await _categoryService.GetAllCategoriesAsync();
            int categoryRow = instructions.Length + 3;
            foreach (var category in categories)
            {
                instructionSheet.Cells[categoryRow, 1].Value = $"  - {category.Name}";
                categoryRow++;
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();
            instructionSheet.Cells.AutoFitColumns();

            var content = package.GetAsByteArray();
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Medicine_Upload_Template.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpload(BulkUploadViewModel model)
        {
            var result = new BulkUploadResultViewModel();

            if (model.ExcelFile == null || model.ExcelFile.Length == 0)
            {
                ModelState.AddModelError("ExcelFile", "Please select an Excel file to upload.");
                ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();
                return View(model);
            }

            var extension = Path.GetExtension(model.ExcelFile.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
            {
                ModelState.AddModelError("ExcelFile", "Please upload a valid Excel file (.xlsx or .xls).");
                ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();
                return View(model);
            }

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                using var stream = new MemoryStream();
                await model.ExcelFile.CopyToAsync(stream);
                stream.Position = 0;

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                
                if (worksheet == null)
                {
                    ModelState.AddModelError("ExcelFile", "The Excel file does not contain any worksheets.");
                    ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();
                    return View(model);
                }

                // Get all categories for mapping
                var categories = await _categoryService.GetAllCategoriesAsync();
                var categoryDict = categories.ToDictionary(c => c.Name.ToLower(), c => c.Id);

                int rowCount = worksheet.Dimension?.Rows ?? 0;
                result.TotalRecords = rowCount - 1; // Excluding header

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var name = worksheet.Cells[row, 1].Text?.Trim();
                        
                        // Skip empty rows
                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        var genericName = worksheet.Cells[row, 2].Text?.Trim();
                        var categoryName = worksheet.Cells[row, 3].Text?.Trim();
                        var batchNo = worksheet.Cells[row, 4].Text?.Trim();
                        
                        // Validate required fields
                        var validationErrors = new List<string>();
                        
                        if (string.IsNullOrWhiteSpace(genericName))
                            validationErrors.Add("GenericName is required");
                        if (string.IsNullOrWhiteSpace(categoryName))
                            validationErrors.Add("CategoryName is required");
                        if (string.IsNullOrWhiteSpace(batchNo))
                            validationErrors.Add("BatchNo is required");

                        // Validate category exists
                        int categoryId = 0;
                        if (!string.IsNullOrWhiteSpace(categoryName))
                        {
                            if (!categoryDict.TryGetValue(categoryName.ToLower(), out categoryId))
                            {
                                validationErrors.Add($"Category '{categoryName}' not found");
                            }
                        }

                        // Parse dates
                        if (!TryParseDate(worksheet.Cells[row, 5].Text, out DateTime manufactureDate))
                            validationErrors.Add("Invalid ManufactureDate format");
                        
                        if (!TryParseDate(worksheet.Cells[row, 6].Text, out DateTime expiryDate))
                            validationErrors.Add("Invalid ExpiryDate format");

                        // Parse numeric values
                        if (!decimal.TryParse(worksheet.Cells[row, 7].Text, out decimal mrp))
                            validationErrors.Add("Invalid MRP value");
                        if (!decimal.TryParse(worksheet.Cells[row, 8].Text, out decimal salePrice))
                            validationErrors.Add("Invalid SalePrice value");
                        if (!decimal.TryParse(worksheet.Cells[row, 9].Text, out decimal purchasePrice))
                            validationErrors.Add("Invalid PurchasePrice value");
                        if (!int.TryParse(worksheet.Cells[row, 10].Text, out int stockQty))
                            validationErrors.Add("Invalid StockQty value");

                        int.TryParse(worksheet.Cells[row, 11].Text, out int reorderLevel);
                        reorderLevel = reorderLevel > 0 ? reorderLevel : 10;

                        var hsnCode = worksheet.Cells[row, 12].Text?.Trim();
                        if (string.IsNullOrWhiteSpace(hsnCode))
                            validationErrors.Add("HSNCode is required");

                        if (!decimal.TryParse(worksheet.Cells[row, 13].Text, out decimal gstPercent))
                            validationErrors.Add("Invalid GSTPercent value");

                        if (validationErrors.Any())
                        {
                            result.Errors.Add(new BulkUploadError
                            {
                                RowNumber = row,
                                MedicineName = name ?? "Unknown",
                                ErrorMessage = string.Join("; ", validationErrors)
                            });
                            result.FailedCount++;
                            continue;
                        }

                        // Parse optional fields
                        var manufacturer = worksheet.Cells[row, 14].Text?.Trim();
                        var composition = worksheet.Cells[row, 15].Text?.Trim();
                        var dosage = worksheet.Cells[row, 16].Text?.Trim();
                        var packSize = worksheet.Cells[row, 17].Text?.Trim();
                        var description = worksheet.Cells[row, 18].Text?.Trim();
                        var requiresPrescription = ParseYesNo(worksheet.Cells[row, 19].Text);
                        var isActive = ParseYesNo(worksheet.Cells[row, 20].Text, true);

                        // Create medicine
                        var createModel = new MedicineCreateViewModel
                        {
                            Name = name!,
                            GenericName = genericName!,
                            CategoryId = categoryId,
                            BatchNo = batchNo!,
                            ManufactureDate = manufactureDate,
                            ExpiryDate = expiryDate,
                            MRP = mrp,
                            SalePrice = salePrice,
                            PurchasePrice = purchasePrice,
                            StockQty = stockQty,
                            ReorderLevel = reorderLevel,
                            HSNCode = hsnCode!,
                            GSTPercent = gstPercent,
                            Manufacturer = manufacturer,
                            Composition = composition,
                            Dosage = dosage,
                            PackSize = packSize,
                            Description = description,
                            RequiresPrescription = requiresPrescription,
                            IsActive = isActive
                        };

                        var response = await _medicineService.CreateAsync(createModel);
                        if (response.Success)
                        {
                            result.SuccessCount++;
                        }
                        else
                        {
                            result.Errors.Add(new BulkUploadError
                            {
                                RowNumber = row,
                                MedicineName = name!,
                                ErrorMessage = response.Message
                            });
                            result.FailedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(new BulkUploadError
                        {
                            RowNumber = row,
                            MedicineName = worksheet.Cells[row, 1].Text ?? "Unknown",
                            ErrorMessage = ex.Message
                        });
                        result.FailedCount++;
                    }
                }

                TempData["BulkUploadResult"] = System.Text.Json.JsonSerializer.Serialize(result);
                
                if (result.SuccessCount > 0)
                {
                    TempData["Success"] = $"Successfully uploaded {result.SuccessCount} medicines.";
                }
                
                if (result.FailedCount > 0)
                {
                    TempData["Warning"] = $"{result.FailedCount} records failed to upload. Check the details below.";
                }

                return RedirectToAction(nameof(BulkUploadResult));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk upload");
                ModelState.AddModelError("", "An error occurred while processing the file. Please ensure it's a valid Excel file.");
                ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();
                return View(model);
            }
        }

        public IActionResult BulkUploadResult()
        {
            var resultJson = TempData["BulkUploadResult"] as string;
            if (string.IsNullOrEmpty(resultJson))
            {
                return RedirectToAction(nameof(BulkUpload));
            }

            var result = System.Text.Json.JsonSerializer.Deserialize<BulkUploadResultViewModel>(resultJson);
            return View(result);
        }

        private bool TryParseDate(string? dateText, out DateTime result)
        {
            result = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(dateText))
                return false;

            // Try multiple date formats
            var formats = new[]
            {
                "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy",
                "yyyy-MM-dd", "MM/dd/yyyy", "M/d/yyyy"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateText.Trim(), format, 
                    System.Globalization.CultureInfo.InvariantCulture, 
                    System.Globalization.DateTimeStyles.None, out result))
                {
                    return true;
                }
            }

            // Try general parse as fallback
            return DateTime.TryParse(dateText.Trim(), out result);
        }

        private bool ParseYesNo(string? value, bool defaultValue = false)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            var trimmed = value.Trim().ToLower();
            return trimmed == "yes" || trimmed == "y" || trimmed == "true" || trimmed == "1";
        }

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
                    return Json(new { success = false, message = "Invalid file type. Allowed: " + string.Join(", ", allowedExtensions) });
                }

                // Check file size (max 5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "File size must be less than 5MB" });
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "medicines");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var url = $"/uploads/medicines/{uniqueFileName}";
                return Json(new { success = true, url = url });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading medicine image");
                return Json(new { success = false, message = $"Upload failed: {ex.Message}" });
            }
        }
    }
}
