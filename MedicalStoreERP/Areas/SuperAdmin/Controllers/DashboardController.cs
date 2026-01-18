using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalStoreERP.Data;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;
using MedicalStoreERP.Services;

namespace MedicalStoreERP.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Policy = "SuperAdminOnly")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var yearStart = new DateTime(today.Year, 1, 1);

            var model = new SuperAdminDashboardViewModel
            {
                // Sales Summary
                TodaySales = await GetTotalSalesAsync(today, today),
                WeeklySales = await GetTotalSalesAsync(weekStart, today),
                MonthlySales = await GetTotalSalesAsync(monthStart, today),
                YearlySales = await GetTotalSalesAsync(yearStart, today),

                // Order Counts
                TodayOrders = await GetOrderCountAsync(today, today),
                WeeklyOrders = await GetOrderCountAsync(weekStart, today),
                MonthlyOrders = await GetOrderCountAsync(monthStart, today),
                YearlyOrders = await GetOrderCountAsync(yearStart, today),

                // User Statistics
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalAdmins = (await _userManager.GetUsersInRoleAsync("Admin")).Count,
                TotalCustomers = (await _userManager.GetUsersInRoleAsync("User")).Count,
                ActiveUsers = await _userManager.Users.CountAsync(u => u.IsActive),

                // Commission Settings
                CurrentCommissionRate = await GetCurrentCommissionRateAsync(),
                TodayCommission = 0,
                WeeklyCommission = 0,
                MonthlyCommission = 0,
                YearlyCommission = 0
            };

            // Calculate commissions
            model.TodayCommission = model.TodaySales * (model.CurrentCommissionRate / 100);
            model.WeeklyCommission = model.WeeklySales * (model.CurrentCommissionRate / 100);
            model.MonthlyCommission = model.MonthlySales * (model.CurrentCommissionRate / 100);
            model.YearlyCommission = model.YearlySales * (model.CurrentCommissionRate / 100);

            // Sales Chart Data (Last 30 days)
            for (int i = 29; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var sales = await GetTotalSalesAsync(date, date);
                model.DailySalesData.Add(new ChartDataItem
                {
                    Label = date.ToString("MMM dd"),
                    Value = sales
                });
            }

            // Monthly Sales (Last 12 months)
            for (int i = 11; i >= 0; i--)
            {
                var monthDate = today.AddMonths(-i);
                var mStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var mEnd = mStart.AddMonths(1).AddDays(-1);
                var sales = await GetTotalSalesAsync(mStart, mEnd);
                model.MonthlySalesData.Add(new ChartDataItem
                {
                    Label = mStart.ToString("MMM yyyy"),
                    Value = sales
                });
            }

            // Recent Orders
            model.RecentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new RecentOrderItem
                {
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.User != null ? o.User.FullName : o.CustomerName ?? "Guest",
                    TotalAmount = o.TotalAmount,
                    Status = o.Status.ToString(),
                    OrderDate = o.OrderDate
                })
                .ToListAsync();

            return View(model);
        }

        private async Task<decimal> GetTotalSalesAsync(DateTime startDate, DateTime endDate)
        {
            var onlineSales = await _context.Orders
                .Where(o => o.OrderDate.Date >= startDate && o.OrderDate.Date <= endDate && o.Status != OrderStatus.Cancelled)
                .SumAsync(o => o.TotalAmount);

            var offlineSales = await _context.OfflineSales
                .Where(o => o.SaleDate.Date >= startDate && o.SaleDate.Date <= endDate)
                .SumAsync(o => o.TotalAmount);

            return onlineSales + offlineSales;
        }

        private async Task<int> GetOrderCountAsync(DateTime startDate, DateTime endDate)
        {
            var onlineCount = await _context.Orders
                .CountAsync(o => o.OrderDate.Date >= startDate && o.OrderDate.Date <= endDate);

            var offlineCount = await _context.OfflineSales
                .CountAsync(o => o.SaleDate.Date >= startDate && o.SaleDate.Date <= endDate);

            return onlineCount + offlineCount;
        }

        private async Task<decimal> GetCurrentCommissionRateAsync()
        {
            var activeSetting = await _context.CommissionSettings
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            return activeSetting?.Percentage ?? 0;
        }
    }
}
