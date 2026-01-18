using Microsoft.EntityFrameworkCore;
using MedicalStoreERP.Data;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;

namespace MedicalStoreERP.Services
{
    public class MedicineService : IMedicineService
    {
        private readonly ApplicationDbContext _context;

        public MedicineService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<MedicineViewModel>> GetMedicinesAsync(
            string? searchTerm = null,
            int? categoryId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            bool? inStock = null,
            string? sortBy = null,
            int page = 1,
            int pageSize = 12)
        {
            var today = DateTime.Today;
            var query = _context.Medicines
                .Include(m => m.Category)
                .Where(m => m.IsActive && m.ExpiryDate > today)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(m =>
                    m.Name.ToLower().Contains(searchTerm) ||
                    m.GenericName.ToLower().Contains(searchTerm) ||
                    (m.Manufacturer != null && m.Manufacturer.ToLower().Contains(searchTerm)) ||
                    (m.Description != null && m.Description.ToLower().Contains(searchTerm)));
            }

            if (categoryId.HasValue && categoryId > 0)
            {
                query = query.Where(m => m.CategoryId == categoryId);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(m => m.SalePrice >= minPrice);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(m => m.SalePrice <= maxPrice);
            }

            if (inStock.HasValue && inStock.Value)
            {
                query = query.Where(m => m.StockQty > 0);
            }

            // Apply sorting
            query = sortBy?.ToLower() switch
            {
                "name" => query.OrderBy(m => m.Name),
                "name_desc" => query.OrderByDescending(m => m.Name),
                "price" => query.OrderBy(m => m.SalePrice),
                "price_desc" => query.OrderByDescending(m => m.SalePrice),
                "newest" => query.OrderByDescending(m => m.CreatedAt),
                "popularity" => query.OrderByDescending(m => m.OrderItems.Count),
                _ => query.OrderBy(m => m.Name)
            };

            var totalCount = await query.CountAsync();

            var medicines = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MedicineViewModel
                {
                    Id = m.Id,
                    Name = m.Name,
                    GenericName = m.GenericName,
                    BatchNo = m.BatchNo,
                    ManufactureDate = m.ManufactureDate,
                    ExpiryDate = m.ExpiryDate,
                    MRP = m.MRP,
                    SalePrice = m.SalePrice,
                    PurchasePrice = m.PurchasePrice,
                    StockQty = m.StockQty,
                    ReorderLevel = m.ReorderLevel,
                    HSNCode = m.HSNCode,
                    GSTPercent = m.GSTPercent,
                    Description = m.Description,
                    Manufacturer = m.Manufacturer,
                    Composition = m.Composition,
                    Dosage = m.Dosage,
                    PackSize = m.PackSize,
                    ImageUrl = m.ImageUrl,
                    RequiresPrescription = m.RequiresPrescription,
                    IsActive = m.IsActive,
                    IsFeatured = m.IsFeatured,
                    CategoryId = m.CategoryId,
                    CategoryName = m.Category != null ? m.Category.Name : null,
                    IsExpired = m.ExpiryDate <= DateTime.Today,
                    IsNearExpiry = m.ExpiryDate <= DateTime.Today.AddDays(90) && m.ExpiryDate > DateTime.Today,
                    IsLowStock = m.StockQty <= m.ReorderLevel,
                    DaysToExpiry = (m.ExpiryDate - DateTime.Today).Days,
                    DiscountPercent = m.MRP > 0 ? Math.Round((m.MRP - m.SalePrice) / m.MRP * 100, 2) : 0
                })
                .ToListAsync();

            return new PaginatedResult<MedicineViewModel>
            {
                Items = medicines,
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<MedicineDetailViewModel> GetMedicineDetailAsync(int id, string? userId = null)
        {
            var medicine = await _context.Medicines
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medicine == null)
            {
                return new MedicineDetailViewModel();
            }

            var viewModel = new MedicineDetailViewModel
            {
                Medicine = MapToViewModel(medicine),
                IsInWishlist = false,
                IsInCart = false
            };

            if (!string.IsNullOrEmpty(userId))
            {
                viewModel.IsInWishlist = await _context.WishlistItems
                    .AnyAsync(w => w.UserId == userId && w.MedicineId == id);
                viewModel.IsInCart = await _context.CartItems
                    .AnyAsync(c => c.UserId == userId && c.MedicineId == id);
            }

            // Get related medicines from same category
            var today = DateTime.Today;
            viewModel.RelatedMedicines = await _context.Medicines
                .Include(m => m.Category)
                .Where(m => m.CategoryId == medicine.CategoryId && m.Id != id && m.IsActive && m.ExpiryDate > today)
                .Take(4)
                .Select(m => MapToViewModel(m))
                .ToListAsync();

            return viewModel;
        }

        public async Task<List<MedicineViewModel>> GetFeaturedMedicinesAsync(int count = 8)
        {
            var today = DateTime.Today;
            return await _context.Medicines
                .Include(m => m.Category)
                .Where(m => m.IsActive && m.IsFeatured && m.ExpiryDate > today && m.StockQty > 0)
                .Take(count)
                .Select(m => MapToViewModel(m))
                .ToListAsync();
        }

        public async Task<List<MedicineViewModel>> GetNewArrivalsAsync(int count = 8)
        {
            var today = DateTime.Today;
            return await _context.Medicines
                .Include(m => m.Category)
                .Where(m => m.IsActive && m.ExpiryDate > today && m.StockQty > 0)
                .OrderByDescending(m => m.CreatedAt)
                .Take(count)
                .Select(m => MapToViewModel(m))
                .ToListAsync();
        }

        public async Task<List<MedicineViewModel>> GetBestSellersAsync(int count = 8)
        {
            var today = DateTime.Today;
            return await _context.Medicines
                .Include(m => m.Category)
                .Include(m => m.OrderItems)
                .Where(m => m.IsActive && m.ExpiryDate > today && m.StockQty > 0)
                .OrderByDescending(m => m.OrderItems.Sum(oi => oi.Quantity))
                .Take(count)
                .Select(m => MapToViewModel(m))
                .ToListAsync();
        }

        public async Task<List<MedicineViewModel>> GetOnSaleMedicinesAsync(int count = 8)
        {
            var today = DateTime.Today;
            return await _context.Medicines
                .Include(m => m.Category)
                .Where(m => m.IsActive && m.ExpiryDate > today && m.StockQty > 0 && m.SalePrice < m.MRP)
                .OrderByDescending(m => (m.MRP - m.SalePrice) / m.MRP)
                .Take(count)
                .Select(m => MapToViewModel(m))
                .ToListAsync();
        }

        public async Task<Medicine?> GetByIdAsync(int id)
        {
            return await _context.Medicines
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<ApiResponse<Medicine>> CreateAsync(MedicineCreateViewModel model)
        {
            try
            {
                var medicine = new Medicine
                {
                    Name = model.Name,
                    GenericName = model.GenericName,
                    BatchNo = model.BatchNo,
                    ManufactureDate = model.ManufactureDate,
                    ExpiryDate = model.ExpiryDate,
                    MRP = model.MRP,
                    SalePrice = model.SalePrice,
                    PurchasePrice = model.PurchasePrice,
                    StockQty = model.StockQty,
                    ReorderLevel = model.ReorderLevel,
                    HSNCode = model.HSNCode,
                    GSTPercent = model.GSTPercent,
                    Description = model.Description,
                    Manufacturer = model.Manufacturer,
                    Composition = model.Composition,
                    Dosage = model.Dosage,
                    PackSize = model.PackSize,
                    ImageUrl = model.ImageUrl,
                    RequiresPrescription = model.RequiresPrescription,
                    IsActive = model.IsActive,
                    IsFeatured = model.IsFeatured,
                    CategoryId = model.CategoryId,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Medicines.AddAsync(medicine);
                await _context.SaveChangesAsync();

                return new ApiResponse<Medicine>
                {
                    Success = true,
                    Message = "Medicine created successfully",
                    Data = medicine
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Medicine>
                {
                    Success = false,
                    Message = $"Error creating medicine: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<Medicine>> UpdateAsync(int id, MedicineCreateViewModel model)
        {
            try
            {
                var medicine = await _context.Medicines.FindAsync(id);
                if (medicine == null)
                {
                    return new ApiResponse<Medicine> { Success = false, Message = "Medicine not found" };
                }

                medicine.Name = model.Name;
                medicine.GenericName = model.GenericName;
                medicine.BatchNo = model.BatchNo;
                medicine.ManufactureDate = model.ManufactureDate;
                medicine.ExpiryDate = model.ExpiryDate;
                medicine.MRP = model.MRP;
                medicine.SalePrice = model.SalePrice;
                medicine.PurchasePrice = model.PurchasePrice;
                medicine.StockQty = model.StockQty;
                medicine.ReorderLevel = model.ReorderLevel;
                medicine.HSNCode = model.HSNCode;
                medicine.GSTPercent = model.GSTPercent;
                medicine.Description = model.Description;
                medicine.Manufacturer = model.Manufacturer;
                medicine.Composition = model.Composition;
                medicine.Dosage = model.Dosage;
                medicine.PackSize = model.PackSize;
                medicine.ImageUrl = model.ImageUrl;
                medicine.RequiresPrescription = model.RequiresPrescription;
                medicine.IsActive = model.IsActive;
                medicine.IsFeatured = model.IsFeatured;
                medicine.CategoryId = model.CategoryId;
                medicine.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new ApiResponse<Medicine>
                {
                    Success = true,
                    Message = "Medicine updated successfully",
                    Data = medicine
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<Medicine>
                {
                    Success = false,
                    Message = $"Error updating medicine: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            try
            {
                var medicine = await _context.Medicines.FindAsync(id);
                if (medicine == null)
                {
                    return new ApiResponse<bool> { Success = false, Message = "Medicine not found" };
                }

                // Soft delete
                medicine.IsActive = false;
                await _context.SaveChangesAsync();

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Medicine deleted successfully",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error deleting medicine: {ex.Message}"
                };
            }
        }

        public async Task<List<MedicineViewModel>> GetLowStockMedicinesAsync(int threshold = 10)
        {
            return await _context.Medicines
                .Include(m => m.Category)
                .Where(m => m.IsActive && m.StockQty <= m.ReorderLevel)
                .OrderBy(m => m.StockQty)
                .Select(m => MapToViewModel(m))
                .ToListAsync();
        }

        public async Task<List<MedicineViewModel>> GetNearExpiryMedicinesAsync(int days = 90)
        {
            var expiryDate = DateTime.Today.AddDays(days);
            return await _context.Medicines
                .Include(m => m.Category)
                .Where(m => m.IsActive && m.ExpiryDate <= expiryDate && m.ExpiryDate > DateTime.Today)
                .OrderBy(m => m.ExpiryDate)
                .Select(m => MapToViewModel(m))
                .ToListAsync();
        }

        public async Task<List<MedicineViewModel>> GetExpiredMedicinesAsync()
        {
            return await _context.Medicines
                .Include(m => m.Category)
                .Where(m => m.ExpiryDate <= DateTime.Today)
                .OrderBy(m => m.ExpiryDate)
                .Select(m => MapToViewModel(m))
                .ToListAsync();
        }

        public async Task<bool> UpdateStockAsync(int medicineId, int quantity, bool isAddition)
        {
            var medicine = await _context.Medicines.FindAsync(medicineId);
            if (medicine == null) return false;

            if (isAddition)
            {
                medicine.StockQty += quantity;
            }
            else
            {
                if (medicine.StockQty < quantity) return false;
                medicine.StockQty -= quantity;
            }

            medicine.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private static MedicineViewModel MapToViewModel(Medicine m)
        {
            return new MedicineViewModel
            {
                Id = m.Id,
                Name = m.Name,
                GenericName = m.GenericName,
                BatchNo = m.BatchNo,
                ManufactureDate = m.ManufactureDate,
                ExpiryDate = m.ExpiryDate,
                MRP = m.MRP,
                SalePrice = m.SalePrice,
                PurchasePrice = m.PurchasePrice,
                StockQty = m.StockQty,
                ReorderLevel = m.ReorderLevel,
                HSNCode = m.HSNCode,
                GSTPercent = m.GSTPercent,
                Description = m.Description,
                Manufacturer = m.Manufacturer,
                Composition = m.Composition,
                Dosage = m.Dosage,
                PackSize = m.PackSize,
                ImageUrl = m.ImageUrl,
                RequiresPrescription = m.RequiresPrescription,
                IsActive = m.IsActive,
                IsFeatured = m.IsFeatured,
                CategoryId = m.CategoryId,
                CategoryName = m.Category?.Name,
                IsExpired = m.ExpiryDate <= DateTime.Today,
                IsNearExpiry = m.ExpiryDate <= DateTime.Today.AddDays(90) && m.ExpiryDate > DateTime.Today,
                IsLowStock = m.StockQty <= m.ReorderLevel,
                DaysToExpiry = (m.ExpiryDate - DateTime.Today).Days,
                DiscountPercent = m.MRP > 0 ? Math.Round((m.MRP - m.SalePrice) / m.MRP * 100, 2) : 0
            };
        }
    }
}
