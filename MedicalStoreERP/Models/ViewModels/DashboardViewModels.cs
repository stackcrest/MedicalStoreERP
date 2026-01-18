using System.ComponentModel.DataAnnotations;

namespace MedicalStoreERP.Models.ViewModels
{
    public class DashboardViewModel
    {
        // Summary Cards
        public decimal TodaySales { get; set; }
        public decimal WeekSales { get; set; }
        public decimal MonthSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int TotalMedicines { get; set; }
        public int LowStockCount { get; set; }
        public int NearExpiryCount { get; set; }
        public int ExpiredCount { get; set; }
        public int TotalCustomers { get; set; }
        public int NewCustomersToday { get; set; }

        // Charts Data
        public List<ChartData> SalesChartData { get; set; } = new List<ChartData>();
        public List<ChartData> OrdersChartData { get; set; } = new List<ChartData>();
        public List<CategorySalesData> CategorySalesData { get; set; } = new List<CategorySalesData>();

        // Recent Data
        public List<OrderViewModel> RecentOrders { get; set; } = new List<OrderViewModel>();
        public List<MedicineViewModel> LowStockMedicines { get; set; } = new List<MedicineViewModel>();
        public List<MedicineViewModel> NearExpiryMedicines { get; set; } = new List<MedicineViewModel>();
        public List<TopSellingMedicine> TopSellingMedicines { get; set; } = new List<TopSellingMedicine>();
    }

    public class ChartData
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public class CategorySalesData
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public int OrderCount { get; set; }
    }

    public class TopSellingMedicine
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class SalesReportViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string GroupBy { get; set; } = "Day"; // Day, Week, Month
        public decimal TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalGST { get; set; }
        public decimal TotalDiscount { get; set; }
        public int TotalOrders { get; set; }
        public List<SalesReportItem> Items { get; set; } = new List<SalesReportItem>();
        public List<ChartData> ChartData { get; set; } = new List<ChartData>();
        public List<DailySalesData> DailySales { get; set; } = new();
        public List<CategorySalesData> CategorySales { get; set; } = new();
        public List<TopProductData> TopProducts { get; set; } = new();
        public List<OrderReportItem> Orders { get; set; } = new();
    }

    public class DailySalesData
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class TopProductData
    {
        public int MedicineId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class OrderReportItem
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SalesReportItem
    {
        public DateTime Date { get; set; }
        public string Period { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal SubTotal { get; set; }
        public decimal GSTAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class StockReportViewModel
    {
        public string ReportType { get; set; } = "All"; // All, LowStock, NearExpiry, Expired, FastMoving, SlowMoving
        public List<StockReportItem> Items { get; set; } = new List<StockReportItem>();
        public decimal TotalStockValue { get; set; }
        public int TotalItems { get; set; }
    }

    public class StockReportItem
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public string BatchNo { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int StockQty { get; set; }
        public int ReorderLevel { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int DaysToExpiry { get; set; }
        public decimal StockValue { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class CustomerReportViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime RegisteredOn { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalPurchaseAmount { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public List<CustomerLedgerItem> Ledger { get; set; } = new List<CustomerLedgerItem>();
    }

    public class CustomerLedgerItem
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ReferenceNo { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }

    // Inventory Report ViewModels
    public class InventoryReportViewModel
    {
        public int TotalProducts { get; set; }
        public decimal TotalStockValue { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public List<InventoryItem> Items { get; set; } = new();
        public List<CategoryStockData> CategoryBreakdown { get; set; } = new();
    }

    public class InventoryItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public int ReorderLevel { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal StockValue { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    public class CategoryStockData
    {
        public string CategoryName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public decimal StockValue { get; set; }
    }

    // GST Report ViewModels
    public class GSTReportViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalTaxableValue { get; set; }
        public decimal TotalCGST { get; set; }
        public decimal TotalSGST { get; set; }
        public decimal TotalGST { get; set; }
        public List<GSTRateSummary> RatewiseSummary { get; set; } = new();
        public List<GSTInvoiceItem> Invoices { get; set; } = new();
    }

    public class GSTRateSummary
    {
        public decimal GSTRate { get; set; }
        public decimal TaxableValue { get; set; }
        public decimal CGST { get; set; }
        public decimal SGST { get; set; }
        public decimal TotalGST { get; set; }
    }

    public class GSTInvoiceItem
    {
        public int OrderId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TaxableValue { get; set; }
        public decimal CGST { get; set; }
        public decimal SGST { get; set; }
        public decimal TotalGST { get; set; }
        public decimal InvoiceTotal { get; set; }
    }

    // Expiry Report ViewModels
    public class ExpiryReportViewModel
    {
        public int MonthsAhead { get; set; } = 3;
        public int ExpiredCount { get; set; }
        public int ExpiringThisMonth { get; set; }
        public int ExpiringIn3Months { get; set; }
        public int ExpiringIn6Months { get; set; }
        public int ExpiringIn12Months { get; set; }
        public decimal TotalExpiryValue { get; set; }
        public List<ExpiryItem> Items { get; set; } = new();
        public List<CategoryExpiryData> CategoryBreakdown { get; set; } = new();
    }

    public class ExpiryItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public decimal CostPrice { get; set; }
        public decimal ValueAtRisk { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    public class CategoryExpiryData
    {
        public string CategoryName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Value { get; set; }
    }

    // Low Stock Report ViewModels
    public class LowStockReportViewModel
    {
        public int OutOfStockCount { get; set; }
        public int CriticalCount { get; set; }
        public int LowStockCount { get; set; }
        public decimal ReorderValue { get; set; }
        public List<LowStockItem> Items { get; set; } = new();
        public List<CategoryLowStockData> CategoryBreakdown { get; set; } = new();
    }

    public class LowStockItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public int ReorderLevel { get; set; }
        public decimal CostPrice { get; set; }
        public string? SupplierName { get; set; }
    }

    public class CategoryLowStockData
    {
        public string CategoryName { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
