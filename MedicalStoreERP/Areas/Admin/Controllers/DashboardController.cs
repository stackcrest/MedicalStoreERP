using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedicalStoreERP.Services;

namespace MedicalStoreERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class DashboardController : Controller
    {
        private readonly IReportService _reportService;

        public DashboardController(IReportService reportService)
        {
            _reportService = reportService;
        }

        public async Task<IActionResult> Index()
        {
            var dashboard = await _reportService.GetDashboardDataAsync();
            return View(dashboard);
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesData()
        {
            var dashboard = await _reportService.GetDashboardDataAsync();
            return Json(new
            {
                labels = dashboard.SalesChartData.Select(d => d.Label),
                data = dashboard.SalesChartData.Select(d => d.Value)
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetOrdersData()
        {
            var dashboard = await _reportService.GetDashboardDataAsync();
            return Json(new
            {
                labels = dashboard.OrdersChartData.Select(d => d.Label),
                data = dashboard.OrdersChartData.Select(d => d.Value)
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCategorySalesData()
        {
            var dashboard = await _reportService.GetDashboardDataAsync();
            return Json(new
            {
                labels = dashboard.CategorySalesData.Select(d => d.CategoryName),
                data = dashboard.CategorySalesData.Select(d => d.TotalSales)
            });
        }
    }
}
