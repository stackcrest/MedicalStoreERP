using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;

namespace MedicalStoreERP.Services
{
    public interface ICartService
    {
        Task<CartViewModel> GetCartAsync(string userId);
        Task<ApiResponse<CartItemViewModel>> AddToCartAsync(string userId, int medicineId, int quantity);
        Task<ApiResponse<CartItemViewModel>> UpdateCartItemAsync(string userId, int cartItemId, int quantity);
        Task<ApiResponse<bool>> RemoveFromCartAsync(string userId, int cartItemId);
        Task<ApiResponse<bool>> ClearCartAsync(string userId);
        Task<int> GetCartCountAsync(string userId);
        Task<CartViewModel> CalculateCartTotalsAsync(string userId);
    }

    public interface IMedicineService
    {
        Task<PaginatedResult<MedicineViewModel>> GetMedicinesAsync(
            string? searchTerm = null,
            int? categoryId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            bool? inStock = null,
            string? sortBy = null,
            int page = 1,
            int pageSize = 12);
        Task<MedicineDetailViewModel> GetMedicineDetailAsync(int id, string? userId = null);
        Task<List<MedicineViewModel>> GetFeaturedMedicinesAsync(int count = 8);
        Task<List<MedicineViewModel>> GetNewArrivalsAsync(int count = 8);
        Task<List<MedicineViewModel>> GetBestSellersAsync(int count = 8);
        Task<List<MedicineViewModel>> GetOnSaleMedicinesAsync(int count = 8);
        Task<Medicine?> GetByIdAsync(int id);
        Task<ApiResponse<Medicine>> CreateAsync(MedicineCreateViewModel model);
        Task<ApiResponse<Medicine>> UpdateAsync(int id, MedicineCreateViewModel model);
        Task<ApiResponse<bool>> DeleteAsync(int id);
        Task<List<MedicineViewModel>> GetLowStockMedicinesAsync(int threshold = 10);
        Task<List<MedicineViewModel>> GetNearExpiryMedicinesAsync(int days = 90);
        Task<List<MedicineViewModel>> GetExpiredMedicinesAsync();
        Task<bool> UpdateStockAsync(int medicineId, int quantity, bool isAddition);
    }

    public interface ICategoryService
    {
        Task<List<CategoryViewModel>> GetAllCategoriesAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<ApiResponse<Category>> CreateAsync(CategoryCreateViewModel model);
        Task<ApiResponse<Category>> UpdateAsync(int id, CategoryCreateViewModel model);
        Task<ApiResponse<bool>> DeleteAsync(int id);
    }

    public interface IOrderService
    {
        Task<ApiResponse<Order>> PlaceOrderAsync(string userId, CheckoutViewModel model, string? prescriptionUrl = null, string? prescriptionFileName = null);
        Task<OrderViewModel?> GetOrderByIdAsync(int orderId, string? userId = null);
        Task<List<OrderViewModel>> GetUserOrdersAsync(string userId, int page = 1, int pageSize = 10);
        Task<PaginatedResult<OrderViewModel>> GetAllOrdersAsync(
            OrderStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 10);
        Task<ApiResponse<bool>> UpdateOrderStatusAsync(int orderId, OrderStatus status, string? notes = null);
        Task<ApiResponse<bool>> CancelOrderAsync(int orderId, string userId, string? reason = null);
        Task<string> GenerateOrderNumberAsync();
        Task<ApiResponse<bool>> VerifyPrescriptionAsync(int orderId, string verifiedByUserId, bool approve, string? notes = null);
    }

    public interface IWishlistService
    {
        Task<WishlistViewModel> GetWishlistAsync(string userId);
        Task<ApiResponse<bool>> AddToWishlistAsync(string userId, int medicineId);
        Task<ApiResponse<bool>> RemoveFromWishlistAsync(string userId, int medicineId);
        Task<bool> IsInWishlistAsync(string userId, int medicineId);
        Task<int> GetWishlistCountAsync(string userId);
    }

    public interface IGSTService
    {
        decimal CalculateGST(decimal amount, decimal gstPercent);
        decimal CalculateCGST(decimal amount, decimal gstPercent);
        decimal CalculateSGST(decimal amount, decimal gstPercent);
        decimal CalculateDiscount(decimal amount, decimal discountPercent);
        CartViewModel ApplyDiscountAndGST(CartViewModel cart, decimal discountThreshold, decimal discountPercent);
    }

    public interface ISupplierService
    {
        Task<List<SupplierViewModel>> GetAllSuppliersAsync();
        Task<SupplierViewModel?> GetByIdAsync(int id);
        Task<ApiResponse<Supplier>> CreateAsync(SupplierCreateViewModel model);
        Task<ApiResponse<Supplier>> UpdateAsync(int id, SupplierCreateViewModel model);
        Task<ApiResponse<bool>> DeleteAsync(int id);
        Task<SupplierLedgerViewModel> GetLedgerAsync(int supplierId, DateTime? fromDate = null, DateTime? toDate = null);
    }

    public interface IGRNService
    {
        Task<List<GRNViewModel>> GetAllGRNsAsync(DateTime? fromDate = null, DateTime? toDate = null, int? supplierId = null);
        Task<GRNViewModel?> GetByIdAsync(int id);
        Task<ApiResponse<GRN>> CreateAsync(GRNCreateViewModel model, string createdBy);
        Task<string> GenerateGRNNumberAsync();
    }

    public interface IReportService
    {
        Task<DashboardViewModel> GetDashboardDataAsync();
        Task<SalesReportViewModel> GetSalesReportAsync(DateTime fromDate, DateTime toDate, string groupBy = "Day");
        Task<StockReportViewModel> GetStockReportAsync(string reportType = "All");
        Task<List<CustomerReportViewModel>> GetCustomerReportsAsync();
        Task<CustomerReportViewModel?> GetCustomerLedgerAsync(string userId);
        Task<List<TopSellingMedicine>> GetTopSellingMedicinesAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null);
    }

    public interface IPdfService
    {
        Task<byte[]> GenerateInvoicePdfAsync(int orderId);
        Task<byte[]> GenerateGRNPdfAsync(int grnId);
        Task<byte[]> GenerateReportPdfAsync(string reportType, object data);
    }
}
