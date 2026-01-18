using Microsoft.EntityFrameworkCore;
using MedicalStoreERP.Data;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;

namespace MedicalStoreERP.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly ApplicationDbContext _context;

        public WishlistService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<WishlistViewModel> GetWishlistAsync(string userId)
        {
            var items = await _context.WishlistItems
                .Include(w => w.Medicine)
                .ThenInclude(m => m!.Category)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedAt)
                .Select(w => new WishlistItemDisplayModel
                {
                    MedicineId = w.Medicine!.Id,
                    MedicineName = w.Medicine.Name,
                    GenericName = w.Medicine.GenericName,
                    CategoryName = w.Medicine.Category != null ? w.Medicine.Category.Name : string.Empty,
                    ImageUrl = w.Medicine.ImageUrl,
                    Price = w.Medicine.SalePrice,
                    IsInStock = w.Medicine.StockQty > 0,
                    StockQuantity = w.Medicine.StockQty,
                    AddedAt = w.AddedAt
                })
                .ToListAsync();

            return new WishlistViewModel
            {
                Items = items,
                TotalItems = items.Count
            };
        }

        public async Task<ApiResponse<bool>> AddToWishlistAsync(string userId, int medicineId)
        {
            try
            {
                var medicine = await _context.Medicines.FindAsync(medicineId);
                if (medicine == null)
                {
                    return new ApiResponse<bool> { Success = false, Message = "Medicine not found" };
                }

                var exists = await _context.WishlistItems
                    .AnyAsync(w => w.UserId == userId && w.MedicineId == medicineId);

                if (exists)
                {
                    return new ApiResponse<bool> { Success = false, Message = "Already in wishlist" };
                }

                var wishlistItem = new WishlistItem
                {
                    UserId = userId,
                    MedicineId = medicineId,
                    AddedAt = DateTime.UtcNow
                };

                await _context.WishlistItems.AddAsync(wishlistItem);
                await _context.SaveChangesAsync();

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Added to wishlist",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error adding to wishlist: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<bool>> RemoveFromWishlistAsync(string userId, int medicineId)
        {
            try
            {
                var wishlistItem = await _context.WishlistItems
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.MedicineId == medicineId);

                if (wishlistItem == null)
                {
                    return new ApiResponse<bool> { Success = false, Message = "Item not in wishlist" };
                }

                _context.WishlistItems.Remove(wishlistItem);
                await _context.SaveChangesAsync();

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Removed from wishlist",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error removing from wishlist: {ex.Message}"
                };
            }
        }

        public async Task<bool> IsInWishlistAsync(string userId, int medicineId)
        {
            return await _context.WishlistItems
                .AnyAsync(w => w.UserId == userId && w.MedicineId == medicineId);
        }

        public async Task<int> GetWishlistCountAsync(string userId)
        {
            return await _context.WishlistItems
                .CountAsync(w => w.UserId == userId);
        }
    }
}
