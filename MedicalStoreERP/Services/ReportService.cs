using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using MedicalStoreERP.Data;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;

namespace MedicalStoreERP.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var dashboard = new DashboardViewModel
            {
                // Sales metrics
                TodaySales = await _context.Orders
                    .Where(o => o.OrderDate.Date == today && o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => o.TotalAmount),

                WeekSales = await _context.Orders
                    .Where(o => o.OrderDate >= weekStart && o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => o.TotalAmount),

                MonthSales = await _context.Orders
                    .Where(o => o.OrderDate >= monthStart && o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => o.TotalAmount),

                TotalRevenue = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Delivered)
                    .SumAsync(o => o.TotalAmount),

                // Order metrics
                TotalOrders = await _context.Orders.CountAsync(),
                PendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending),
                ProcessingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Processing || o.Status == OrderStatus.Confirmed),
                DeliveredOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Delivered),

                // Inventory metrics
                TotalMedicines = await _context.Medicines.CountAsync(m => m.IsActive),
                LowStockCount = await _context.Medicines.CountAsync(m => m.IsActive && m.StockQty <= m.ReorderLevel),
                NearExpiryCount = await _context.Medicines.CountAsync(m => m.IsActive && m.ExpiryDate <= today.AddDays(90) && m.ExpiryDate > today),
                ExpiredCount = await _context.Medicines.CountAsync(m => m.ExpiryDate <= today),

                // Customer metrics
                TotalCustomers = await _userManager.Users.CountAsync(u => u.IsActive),
                NewCustomersToday = await _userManager.Users.CountAsync(u => u.CreatedAt.Date == today)
            };

            // Sales chart data (last 7 days)
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var sales = await _context.Orders
                    .Where(o => o.OrderDate.Date == date && o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => o.TotalAmount);

                dashboard.SalesChartData.Add(new ChartData
                {
                    Label = date.ToString("MMM dd"),
                    Value = sales
                });
            }

            // Orders chart data (last 7 days)
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var orders = await _context.Orders
                    .CountAsync(o => o.OrderDate.Date == date);

                dashboard.OrdersChartData.Add(new ChartData
                {
                    Label = date.ToString("MMM dd"),
                    Value = orders
                });
            }

            // Category sales data
            dashboard.CategorySalesData = await _context.Categories
                .Where(c => c.IsActive)
                .Select(c => new CategorySalesData
                {
                    CategoryName = c.Name,
                    TotalSales = c.Medicines
                        .SelectMany(m => m.OrderItems)
                        .Where(oi => oi.Order != null && oi.Order.Status != OrderStatus.Cancelled)
                        .Sum(oi => oi.TotalPrice),
                    OrderCount = c.Medicines
                        .SelectMany(m => m.OrderItems)
                        .Where(oi => oi.Order != null && oi.Order.Status != OrderStatus.Cancelled)
                        .Select(oi => oi.OrderId)
                        .Distinct()
                        .Count()
                })
                .OrderByDescending(c => c.TotalSales)
                .Take(5)
                .ToListAsync();

            // Recent orders
            dashboard.RecentOrders = await _context.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new OrderViewModel
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.CustomerName,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    OrderDate = o.OrderDate
                })
                .ToListAsync();

            // Low stock medicines
            dashboard.LowStockMedicines = await _context.Medicines
                .Include(m => m.Category)
                .Where(m => m.IsActive && m.StockQty <= m.ReorderLevel)
                .OrderBy(m => m.StockQty)
                .Take(5)
                .Select(m => new MedicineViewModel
                {
                    Id = m.Id,
                    Name = m.Name,
                    StockQty = m.StockQty,
                    ReorderLevel = m.ReorderLevel,
                    CategoryName = m.Category != null ? m.Category.Name : null
                })
                .ToListAsync();

            // Near expiry medicines
            dashboard.NearExpiryMedicines = await _context.Medicines
                .Include(m => m.Category)
                .Where(m => m.IsActive && m.ExpiryDate <= today.AddDays(90) && m.ExpiryDate > today)
                .OrderBy(m => m.ExpiryDate)
                .Take(5)
                .Select(m => new MedicineViewModel
                {
                    Id = m.Id,
                    Name = m.Name,
                    ExpiryDate = m.ExpiryDate,
                    StockQty = m.StockQty,
                    CategoryName = m.Category != null ? m.Category.Name : null,
                    DaysToExpiry = (m.ExpiryDate - today).Days
                })
                .ToListAsync();

            // Top selling medicines
            dashboard.TopSellingMedicines = await GetTopSellingMedicinesAsync(5);

            return dashboard;
        }

        public async Task<SalesReportViewModel> GetSalesReportAsync(DateTime fromDate, DateTime toDate, string groupBy = "Day")
        {
            var orders = await _context.Orders
                .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate.AddDays(1) && o.Status != OrderStatus.Cancelled)
                .ToListAsync();

            var report = new SalesReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                GroupBy = groupBy,
                TotalSales = orders.Sum(o => o.TotalAmount),
                TotalGST = orders.Sum(o => o.TotalGSTAmount),
                TotalDiscount = orders.Sum(o => o.DiscountAmount),
                TotalOrders = orders.Count
            };

            // Group data based on groupBy parameter
            IEnumerable<IGrouping<object, Order>> groupedOrders = groupBy.ToLower() switch
            {
                "week" => orders.GroupBy(o => (object)new { Year = o.OrderDate.Year, Week = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(o.OrderDate, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday) }),
                "month" => orders.GroupBy(o => (object)new { o.OrderDate.Year, o.OrderDate.Month }),
                _ => orders.GroupBy(o => (object)o.OrderDate.Date)
            };

            foreach (var group in groupedOrders.OrderBy(g => g.Key))
            {
                var period = groupBy.ToLower() switch
                {
                    "week" => $"Week {((dynamic)group.Key).Week}, {((dynamic)group.Key).Year}",
                    "month" => new DateTime(((dynamic)group.Key).Year, ((dynamic)group.Key).Month, 1).ToString("MMM yyyy"),
                    _ => ((DateTime)group.Key).ToString("dd MMM yyyy")
                };

                report.Items.Add(new SalesReportItem
                {
                    Date = group.First().OrderDate,
                    Period = period,
                    OrderCount = group.Count(),
                    SubTotal = group.Sum(o => o.SubTotal),
                    GSTAmount = group.Sum(o => o.TotalGSTAmount),
                    DiscountAmount = group.Sum(o => o.DiscountAmount),
                    TotalAmount = group.Sum(o => o.TotalAmount)
                });

                report.ChartData.Add(new ChartData
                {
                    Label = period,
                    Value = group.Sum(o => o.TotalAmount)
                });
            }

            return report;
        }

        public async Task<StockReportViewModel> GetStockReportAsync(string reportType = "All")
        {
            var today = DateTime.Today;
            var query = _context.Medicines
                .Include(m => m.Category)
                .Include(m => m.OrderItems)
                .Where(m => m.IsActive);

            switch (reportType.ToLower())
            {
                case "lowstock":
                    query = query.Where(m => m.StockQty <= m.ReorderLevel);
                    break;
                case "nearexpiry":
                    query = query.Where(m => m.ExpiryDate <= today.AddDays(90) && m.ExpiryDate > today);
                    break;
                case "expired":
                    query = query.Where(m => m.ExpiryDate <= today);
                    break;
                case "fastmoving":
                    query = query.OrderByDescending(m => m.OrderItems.Sum(oi => oi.Quantity));
                    break;
                case "slowmoving":
                    query = query.Where(m => !m.OrderItems.Any() || m.OrderItems.Max(oi => oi.Order!.OrderDate) < today.AddDays(-90));
                    break;
            }

            var medicines = await query.ToListAsync();

            var report = new StockReportViewModel
            {
                ReportType = reportType,
                TotalItems = medicines.Count,
                TotalStockValue = medicines.Sum(m => m.StockQty * m.PurchasePrice),
                Items = medicines.Select(m => new StockReportItem
                {
                    MedicineId = m.Id,
                    MedicineName = m.Name,
                    BatchNo = m.BatchNo,
                    CategoryName = m.Category?.Name ?? "",
                    StockQty = m.StockQty,
                    ReorderLevel = m.ReorderLevel,
                    PurchasePrice = m.PurchasePrice,
                    SalePrice = m.SalePrice,
                    ExpiryDate = m.ExpiryDate,
                    DaysToExpiry = (m.ExpiryDate - today).Days,
                    StockValue = m.StockQty * m.PurchasePrice,
                    Status = m.ExpiryDate <= today ? "Expired" :
                            m.ExpiryDate <= today.AddDays(90) ? "Near Expiry" :
                            m.StockQty <= m.ReorderLevel ? "Low Stock" : "Normal"
                }).ToList()
            };

            return report;
        }

        public async Task<List<CustomerReportViewModel>> GetCustomerReportsAsync()
        {
            var users = await _userManager.Users
                .Include(u => u.Orders)
                .Where(u => u.IsActive)
                .ToListAsync();

            return users.Select(u => new CustomerReportViewModel
            {
                UserId = u.Id,
                CustomerName = u.FullName,
                Email = u.Email ?? "",
                Phone = u.PhoneNumber ?? "",
                RegisteredOn = u.CreatedAt,
                TotalOrders = u.Orders.Count,
                TotalPurchaseAmount = u.Orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount),
                LastOrderDate = u.Orders.OrderByDescending(o => o.OrderDate).FirstOrDefault()?.OrderDate
            }).OrderByDescending(c => c.TotalPurchaseAmount).ToList();
        }

        public async Task<CustomerReportViewModel?> GetCustomerLedgerAsync(string userId)
        {
            var user = await _userManager.Users
                .Include(u => u.Orders)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return null;

            var ledgerItems = await _context.CustomerLedgers
                .Where(l => l.UserId == userId)
                .OrderBy(l => l.TransactionDate)
                .Select(l => new CustomerLedgerItem
                {
                    Date = l.TransactionDate,
                    Description = l.Description,
                    ReferenceNo = l.ReferenceNo ?? "",
                    Debit = l.Debit,
                    Credit = l.Credit,
                    Balance = l.Balance
                })
                .ToListAsync();

            return new CustomerReportViewModel
            {
                UserId = user.Id,
                CustomerName = user.FullName,
                Email = user.Email ?? "",
                Phone = user.PhoneNumber ?? "",
                RegisteredOn = user.CreatedAt,
                TotalOrders = user.Orders.Count,
                TotalPurchaseAmount = user.Orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount),
                LastOrderDate = user.Orders.OrderByDescending(o => o.OrderDate).FirstOrDefault()?.OrderDate,
                Ledger = ledgerItems
            };
        }

        public async Task<List<TopSellingMedicine>> GetTopSellingMedicinesAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.OrderItems
                .Include(oi => oi.Medicine)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order != null && oi.Order.Status != OrderStatus.Cancelled);

            if (fromDate.HasValue)
            {
                query = query.Where(oi => oi.Order!.OrderDate >= fromDate);
            }

            if (toDate.HasValue)
            {
                query = query.Where(oi => oi.Order!.OrderDate <= toDate.Value.AddDays(1));
            }

            return await query
                .GroupBy(oi => new { oi.MedicineId, oi.MedicineName })
                .Select(g => new TopSellingMedicine
                {
                    MedicineId = g.Key.MedicineId,
                    MedicineName = g.Key.MedicineName,
                    TotalQuantitySold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.TotalPrice)
                })
                .OrderByDescending(t => t.TotalQuantitySold)
                .Take(count)
                .ToListAsync();
        }
    }
}
