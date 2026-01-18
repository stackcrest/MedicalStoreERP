using System.ComponentModel.DataAnnotations;

namespace MedicalStoreERP.Models.ViewModels
{
    // SuperAdmin Dashboard ViewModels
    public class SuperAdminDashboardViewModel
    {
        // Sales Summary
        public decimal TodaySales { get; set; }
        public decimal WeeklySales { get; set; }
        public decimal MonthlySales { get; set; }
        public decimal YearlySales { get; set; }

        // Order Counts
        public int TodayOrders { get; set; }
        public int WeeklyOrders { get; set; }
        public int MonthlyOrders { get; set; }
        public int YearlyOrders { get; set; }

        // User Statistics
        public int TotalUsers { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalCustomers { get; set; }
        public int ActiveUsers { get; set; }

        // Commission
        public decimal CurrentCommissionRate { get; set; }
        public decimal TodayCommission { get; set; }
        public decimal WeeklyCommission { get; set; }
        public decimal MonthlyCommission { get; set; }
        public decimal YearlyCommission { get; set; }

        // Chart Data
        public List<ChartDataItem> DailySalesData { get; set; } = new();
        public List<ChartDataItem> MonthlySalesData { get; set; } = new();
        public List<RecentOrderItem> RecentOrders { get; set; } = new();
    }

    public class ChartDataItem
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public class RecentOrderItem
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
    }

    // SuperAdmin User ViewModels
    public class SuperAdminUserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class SuperAdminUserEditViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? State { get; set; }

        [StringLength(10)]
        public string? PinCode { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = "User";

        public bool IsActive { get; set; } = true;

        public List<string> AvailableRoles { get; set; } = new();
    }

    public class SuperAdminUserCreateViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? State { get; set; }

        [StringLength(10)]
        public string? PinCode { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = "User";

        public bool IsActive { get; set; } = true;

        public List<string> AvailableRoles { get; set; } = new();
    }

    public class SuperAdminResetPasswordViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm the password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // Commission ViewModels
    public class CommissionIndexViewModel
    {
        public List<CommissionSettingViewModel> Settings { get; set; } = new();
        public decimal CurrentRate { get; set; }
    }

    public class CommissionSettingViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Percentage is required")]
        [Range(0, 100, ErrorMessage = "Percentage must be between 0 and 100")]
        public decimal Percentage { get; set; }

        [Range(0, 10000000)]
        public decimal? MinSaleAmount { get; set; }

        [Range(0, 10000000)]
        public decimal? MaxSaleAmount { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class CommissionReportViewModel
    {
        public string PeriodType { get; set; } = "Daily";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal CommissionRate { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalCommission { get; set; }
        public List<CommissionReportItem> Items { get; set; } = new();
    }

    public class CommissionReportItem
    {
        public string Period { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public decimal CommissionRate { get; set; }
        public decimal CommissionAmount { get; set; }
        public int OrderCount { get; set; }
    }
}
