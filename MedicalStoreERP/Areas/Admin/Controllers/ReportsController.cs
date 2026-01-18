using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedicalStoreERP.Services;

namespace MedicalStoreERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;
        private readonly IPdfService _pdfService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            IReportService reportService,
            IPdfService pdfService,
            ILogger<ReportsController> logger)
        {
            _reportService = reportService;
            _pdfService = pdfService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Sales(DateTime? fromDate, DateTime? toDate)
        {
            fromDate ??= DateTime.Today.AddDays(-30);
            toDate ??= DateTime.Today;

            var report = await _reportService.GetSalesReportAsync(fromDate.Value, toDate.Value);
            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            return View(report);
        }

        public async Task<IActionResult> Inventory()
        {
            var report = await _reportService.GetStockReportAsync();
            return View(report);
        }

        public async Task<IActionResult> LowStock()
        {
            var report = await _reportService.GetStockReportAsync("lowstock");
            return View(report);
        }

        public async Task<IActionResult> ExpiryReport(int? daysThreshold)
        {
            daysThreshold ??= 30;
            var report = await _reportService.GetStockReportAsync("nearexpiry");
            ViewBag.DaysThreshold = daysThreshold;
            return View(report);
        }

        public async Task<IActionResult> GST(DateTime? fromDate, DateTime? toDate)
        {
            fromDate ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            toDate ??= DateTime.Today;

            var report = await _reportService.GetSalesReportAsync(fromDate.Value, toDate.Value);
            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            return View(report);
        }

        public async Task<IActionResult> CustomerReport(string? userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                var topCustomers = await _reportService.GetCustomerReportsAsync();
                return View("CustomerList", topCustomers);
            }

            var report = await _reportService.GetCustomerLedgerAsync(userId);
            return View(report);
        }

        public async Task<IActionResult> SupplierReport(int? supplierId)
        {
            // Supplier reports are available through dashboard data
            var dashboard = await _reportService.GetDashboardDataAsync();
            return View("SupplierList", dashboard);
        }

        public async Task<IActionResult> ProfitLoss(DateTime? fromDate, DateTime? toDate)
        {
            fromDate ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            toDate ??= DateTime.Today;

            var report = await _reportService.GetSalesReportAsync(fromDate.Value, toDate.Value);
            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            return View(report);
        }

        [HttpGet]
        public async Task<IActionResult> ExportSalesReport(DateTime fromDate, DateTime toDate, string format = "pdf")
        {
            try
            {
                var report = await _reportService.GetSalesReportAsync(fromDate, toDate);

                if (format.ToLower() == "pdf")
                {
                    var pdfBytes = await _pdfService.GenerateReportPdfAsync("Sales", report);
                    return File(pdfBytes, "application/pdf", $"SalesReport_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.pdf");
                }
                else if (format.ToLower() == "csv")
                {
                    var csv = GenerateSalesCsv(report);
                    return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"SalesReport_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.csv");
                }

                return BadRequest("Invalid format specified.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting sales report");
                TempData["Error"] = "An error occurred while exporting the report.";
                return RedirectToAction(nameof(Sales), new { fromDate, toDate });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportInventoryReport(string format = "pdf")
        {
            try
            {
                var report = await _reportService.GetStockReportAsync();

                if (format.ToLower() == "pdf")
                {
                    var pdfBytes = await _pdfService.GenerateReportPdfAsync("Inventory", report);
                    return File(pdfBytes, "application/pdf", $"InventoryReport_{DateTime.Today:yyyyMMdd}.pdf");
                }
                else if (format.ToLower() == "csv")
                {
                    var csv = GenerateInventoryCsv(report);
                    return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"InventoryReport_{DateTime.Today:yyyyMMdd}.csv");
                }

                return BadRequest("Invalid format specified.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting inventory report");
                TempData["Error"] = "An error occurred while exporting the report.";
                return RedirectToAction(nameof(Inventory));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesChartData(DateTime fromDate, DateTime toDate)
        {
            var report = await _reportService.GetSalesReportAsync(fromDate, toDate);
            return Json(new
            {
                labels = report.DailySales.Select(d => d.Date.ToString("MMM dd")),
                sales = report.DailySales.Select(d => d.Revenue),
                orders = report.DailySales.Select(d => d.OrderCount)
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCategorySalesData(DateTime fromDate, DateTime toDate)
        {
            var report = await _reportService.GetSalesReportAsync(fromDate, toDate);
            return Json(new
            {
                labels = report.CategorySales.Select(c => c.CategoryName),
                data = report.CategorySales.Select(c => c.TotalSales)
            });
        }

        private string GenerateSalesCsv(dynamic report)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Date,Orders,Total Sales,GST Amount");

            foreach (var day in report.DailySales)
            {
                sb.AppendLine($"{day.Date:yyyy-MM-dd},{day.OrderCount},{day.TotalSales:F2},{day.GSTAmount:F2}");
            }

            return sb.ToString();
        }

        private string GenerateInventoryCsv(dynamic report)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Name,Category,Stock,Cost Price,Selling Price,Stock Value,Expiry Date,Status");

            foreach (var item in report.Items)
            {
                sb.AppendLine($"\"{item.Name}\",\"{item.CategoryName}\",{item.Stock},{item.CostPrice:F2},{item.SellingPrice:F2},{item.StockValue:F2},{item.ExpiryDate:yyyy-MM-dd},{item.Status}");
            }

            return sb.ToString();
        }
    }
}
