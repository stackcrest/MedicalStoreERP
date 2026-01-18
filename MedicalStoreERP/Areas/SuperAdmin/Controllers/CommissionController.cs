using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalStoreERP.Data;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;

namespace MedicalStoreERP.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Policy = "SuperAdminOnly")]
    public class CommissionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CommissionController> _logger;

        public CommissionController(
            ApplicationDbContext context,
            ILogger<CommissionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _context.CommissionSettings
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var activeSetting = settings.FirstOrDefault(c => c.IsActive);

            var model = new CommissionIndexViewModel
            {
                Settings = settings.Select(s => new CommissionSettingViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Percentage = s.Percentage,
                    MinSaleAmount = s.MinSaleAmount,
                    MaxSaleAmount = s.MaxSaleAmount,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt
                }).ToList(),
                CurrentRate = activeSetting?.Percentage ?? 0
            };

            return View(model);
        }

        public IActionResult Create()
        {
            return View(new CommissionSettingViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CommissionSettingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                // If this is active, deactivate all others
                if (model.IsActive)
                {
                    var activeSettings = await _context.CommissionSettings
                        .Where(c => c.IsActive)
                        .ToListAsync();

                    foreach (var setting in activeSettings)
                    {
                        setting.IsActive = false;
                    }
                }

                var commissionSetting = new CommissionSetting
                {
                    Name = model.Name,
                    Description = model.Description,
                    Percentage = model.Percentage,
                    MinSaleAmount = model.MinSaleAmount,
                    MaxSaleAmount = model.MaxSaleAmount,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };

                _context.CommissionSettings.Add(commissionSetting);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Commission setting created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating commission setting");
                ModelState.AddModelError("", "An error occurred while creating the commission setting.");
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var setting = await _context.CommissionSettings.FindAsync(id);
            if (setting == null)
            {
                return NotFound();
            }

            var model = new CommissionSettingViewModel
            {
                Id = setting.Id,
                Name = setting.Name,
                Description = setting.Description,
                Percentage = setting.Percentage,
                MinSaleAmount = setting.MinSaleAmount,
                MaxSaleAmount = setting.MaxSaleAmount,
                IsActive = setting.IsActive,
                CreatedAt = setting.CreatedAt
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CommissionSettingViewModel model)
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
                var setting = await _context.CommissionSettings.FindAsync(id);
                if (setting == null)
                {
                    return NotFound();
                }

                // If this is being set to active, deactivate all others
                if (model.IsActive && !setting.IsActive)
                {
                    var activeSettings = await _context.CommissionSettings
                        .Where(c => c.IsActive && c.Id != id)
                        .ToListAsync();

                    foreach (var s in activeSettings)
                    {
                        s.IsActive = false;
                    }
                }

                setting.Name = model.Name;
                setting.Description = model.Description;
                setting.Percentage = model.Percentage;
                setting.MinSaleAmount = model.MinSaleAmount;
                setting.MaxSaleAmount = model.MaxSaleAmount;
                setting.IsActive = model.IsActive;
                setting.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Commission setting updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating commission setting {Id}", id);
                ModelState.AddModelError("", "An error occurred while updating the commission setting.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetActive(int id)
        {
            try
            {
                // Deactivate all
                var allSettings = await _context.CommissionSettings.ToListAsync();
                foreach (var s in allSettings)
                {
                    s.IsActive = s.Id == id;
                    s.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Commission rate activated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating commission setting {Id}", id);
                TempData["Error"] = "An error occurred.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var setting = await _context.CommissionSettings.FindAsync(id);
                if (setting == null)
                {
                    TempData["Error"] = "Commission setting not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (setting.IsActive)
                {
                    TempData["Error"] = "Cannot delete an active commission setting. Please activate another setting first.";
                    return RedirectToAction(nameof(Index));
                }

                _context.CommissionSettings.Remove(setting);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Commission setting deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting commission setting {Id}", id);
                TempData["Error"] = "An error occurred.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Reports(string periodType = "Daily", DateTime? startDate = null, DateTime? endDate = null)
        {
            var today = DateTime.Today;
            startDate ??= today.AddDays(-30);
            endDate ??= today;

            var model = new CommissionReportViewModel
            {
                PeriodType = periodType,
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                Items = new List<CommissionReportItem>()
            };

            var activeSetting = await _context.CommissionSettings
                .Where(c => c.IsActive)
                .FirstOrDefaultAsync();

            var commissionRate = activeSetting?.Percentage ?? 0;

            if (periodType == "Daily")
            {
                for (var date = startDate.Value; date <= endDate.Value; date = date.AddDays(1))
                {
                    var onlineSales = await _context.Orders
                        .Where(o => o.OrderDate.Date == date && o.Status != OrderStatus.Cancelled)
                        .SumAsync(o => o.TotalAmount);

                    var offlineSales = await _context.OfflineSales
                        .Where(o => o.SaleDate.Date == date)
                        .SumAsync(o => o.TotalAmount);

                    var totalSales = onlineSales + offlineSales;
                    
                    var orderCount = await _context.Orders
                        .Where(o => o.OrderDate.Date == date && o.Status != OrderStatus.Cancelled)
                        .CountAsync();
                    orderCount += await _context.OfflineSales
                        .Where(o => o.SaleDate.Date == date)
                        .CountAsync();

                    if (totalSales > 0)
                    {
                        model.Items.Add(new CommissionReportItem
                        {
                            Period = date.ToString("dd/MM/yyyy"),
                            TotalSales = totalSales,
                            CommissionRate = commissionRate,
                            CommissionAmount = totalSales * (commissionRate / 100),
                            OrderCount = orderCount
                        });
                    }
                }
            }
            else if (periodType == "Weekly")
            {
                var weekStart = startDate.Value;
                while (weekStart <= endDate.Value)
                {
                    var weekEnd = weekStart.AddDays(6);
                    if (weekEnd > endDate.Value) weekEnd = endDate.Value;

                    var onlineSales = await _context.Orders
                        .Where(o => o.OrderDate.Date >= weekStart && o.OrderDate.Date <= weekEnd && o.Status != OrderStatus.Cancelled)
                        .SumAsync(o => o.TotalAmount);

                    var offlineSales = await _context.OfflineSales
                        .Where(o => o.SaleDate.Date >= weekStart && o.SaleDate.Date <= weekEnd)
                        .SumAsync(o => o.TotalAmount);

                    var totalSales = onlineSales + offlineSales;
                    
                    var orderCount = await _context.Orders
                        .Where(o => o.OrderDate.Date >= weekStart && o.OrderDate.Date <= weekEnd && o.Status != OrderStatus.Cancelled)
                        .CountAsync();
                    orderCount += await _context.OfflineSales
                        .Where(o => o.SaleDate.Date >= weekStart && o.SaleDate.Date <= weekEnd)
                        .CountAsync();

                    if (totalSales > 0)
                    {
                        model.Items.Add(new CommissionReportItem
                        {
                            Period = $"{weekStart:dd/MM} - {weekEnd:dd/MM/yyyy}",
                            TotalSales = totalSales,
                            CommissionRate = commissionRate,
                            CommissionAmount = totalSales * (commissionRate / 100),
                            OrderCount = orderCount
                        });
                    }

                    weekStart = weekEnd.AddDays(1);
                }
            }
            else if (periodType == "Monthly")
            {
                var monthStart = new DateTime(startDate.Value.Year, startDate.Value.Month, 1);
                while (monthStart <= endDate.Value)
                {
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                    if (monthEnd > endDate.Value) monthEnd = endDate.Value;

                    var onlineSales = await _context.Orders
                        .Where(o => o.OrderDate.Date >= monthStart && o.OrderDate.Date <= monthEnd && o.Status != OrderStatus.Cancelled)
                        .SumAsync(o => o.TotalAmount);

                    var offlineSales = await _context.OfflineSales
                        .Where(o => o.SaleDate.Date >= monthStart && o.SaleDate.Date <= monthEnd)
                        .SumAsync(o => o.TotalAmount);

                    var totalSales = onlineSales + offlineSales;
                    
                    var orderCount = await _context.Orders
                        .Where(o => o.OrderDate.Date >= monthStart && o.OrderDate.Date <= monthEnd && o.Status != OrderStatus.Cancelled)
                        .CountAsync();
                    orderCount += await _context.OfflineSales
                        .Where(o => o.SaleDate.Date >= monthStart && o.SaleDate.Date <= monthEnd)
                        .CountAsync();

                    model.Items.Add(new CommissionReportItem
                    {
                        Period = monthStart.ToString("MMMM yyyy"),
                        TotalSales = totalSales,
                        CommissionRate = commissionRate,
                        CommissionAmount = totalSales * (commissionRate / 100),
                        OrderCount = orderCount
                    });

                    monthStart = monthStart.AddMonths(1);
                }
            }
            else if (periodType == "Yearly")
            {
                var yearStart = new DateTime(startDate.Value.Year, 1, 1);
                while (yearStart <= endDate.Value)
                {
                    var yearEnd = new DateTime(yearStart.Year, 12, 31);
                    if (yearEnd > endDate.Value) yearEnd = endDate.Value;

                    var onlineSales = await _context.Orders
                        .Where(o => o.OrderDate.Date >= yearStart && o.OrderDate.Date <= yearEnd && o.Status != OrderStatus.Cancelled)
                        .SumAsync(o => o.TotalAmount);

                    var offlineSales = await _context.OfflineSales
                        .Where(o => o.SaleDate.Date >= yearStart && o.SaleDate.Date <= yearEnd)
                        .SumAsync(o => o.TotalAmount);

                    var totalSales = onlineSales + offlineSales;
                    
                    var orderCount = await _context.Orders
                        .Where(o => o.OrderDate.Date >= yearStart && o.OrderDate.Date <= yearEnd && o.Status != OrderStatus.Cancelled)
                        .CountAsync();
                    orderCount += await _context.OfflineSales
                        .Where(o => o.SaleDate.Date >= yearStart && o.SaleDate.Date <= yearEnd)
                        .CountAsync();

                    model.Items.Add(new CommissionReportItem
                    {
                        Period = yearStart.Year.ToString(),
                        TotalSales = totalSales,
                        CommissionRate = commissionRate,
                        CommissionAmount = totalSales * (commissionRate / 100),
                        OrderCount = orderCount
                    });

                    yearStart = yearStart.AddYears(1);
                }
            }

            model.TotalSales = model.Items.Sum(r => r.TotalSales);
            model.TotalCommission = model.Items.Sum(r => r.CommissionAmount);
            model.CommissionRate = commissionRate;

            return View(model);
        }
    }
}
