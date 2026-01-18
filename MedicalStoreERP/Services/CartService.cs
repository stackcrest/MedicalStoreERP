using Microsoft.EntityFrameworkCore;
using MedicalStoreERP.Data;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;

namespace MedicalStoreERP.Services
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGSTService _gstService;
        private readonly IConfiguration _configuration;

        public CartService(ApplicationDbContext context, IGSTService gstService, IConfiguration configuration)
        {
            _context = context;
            _gstService = gstService;
            _configuration = configuration;
        }

        public async Task<CartViewModel> GetCartAsync(string userId)
        {
            var cartItems = await _context.CartItems
                .Include(c => c.Medicine)
                .ThenInclude(m => m!.Category)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var cart = new CartViewModel
            {
                Items = cartItems.Select(c => new CartItemViewModel
                {
                    Id = c.Id,
                    MedicineId = c.MedicineId,
                    MedicineName = c.Medicine?.Name ?? "",
                    BatchNo = c.Medicine?.BatchNo ?? "",
                    HSNCode = c.Medicine?.HSNCode ?? "",
                    ImageUrl = c.Medicine?.ImageUrl,
                    Quantity = c.Quantity,
                    UnitPrice = c.UnitPrice,
                    MRP = c.Medicine?.MRP ?? 0,
                    GSTPercent = c.Medicine?.GSTPercent ?? 0,
                    AvailableStock = c.Medicine?.StockQty ?? 0,
                    ExpiryDate = c.Medicine?.ExpiryDate ?? DateTime.MinValue,
                    RequiresPrescription = c.Medicine?.RequiresPrescription ?? false
                }).ToList(),
                TotalItems = cartItems.Sum(c => c.Quantity)
            };

            // Check if any item requires prescription
            cart.RequiresPrescription = cart.Items.Any(i => i.RequiresPrescription);
            cart.PrescriptionMedicines = cart.Items
                .Where(i => i.RequiresPrescription)
                .Select(i => i.MedicineName)
                .ToList();

            // Calculate totals
            foreach (var item in cart.Items)
            {
                item.TotalPrice = item.Quantity * item.UnitPrice;
                item.GSTAmount = _gstService.CalculateGST(item.TotalPrice, item.GSTPercent);
            }

            return CalculateTotals(cart);
        }

        private CartViewModel CalculateTotals(CartViewModel cart)
        {
            var discountThreshold = _configuration.GetValue<decimal>("AppSettings:DiscountThreshold", 500);
            var discountPercent = _configuration.GetValue<decimal>("AppSettings:DiscountPercent", 5);

            cart.SubTotal = cart.Items.Sum(i => i.TotalPrice);
            
            // Calculate GST from each item
            decimal totalGST = 0;
            foreach (var item in cart.Items)
            {
                var itemGST = _gstService.CalculateGST(item.TotalPrice, item.GSTPercent);
                totalGST += itemGST;
            }

            cart.TotalGSTAmount = totalGST;
            cart.CGSTAmount = totalGST / 2;
            cart.SGSTAmount = totalGST / 2;

            // Apply discount if eligible
            if (cart.SubTotal >= discountThreshold)
            {
                cart.DiscountAmount = Math.Round(cart.SubTotal * discountPercent / 100, 2);
                cart.DiscountPercent = discountPercent;
                cart.IsDiscountApplied = true;
            }

            // Shipping is free for orders above threshold
            cart.ShippingCharges = cart.SubTotal >= discountThreshold ? 0 : 40;

            cart.TotalAmount = cart.SubTotal + cart.TotalGSTAmount - cart.DiscountAmount + cart.ShippingCharges;

            return cart;
        }

        public async Task<ApiResponse<CartItemViewModel>> AddToCartAsync(string userId, int medicineId, int quantity)
        {
            try
            {
                var medicine = await _context.Medicines.FindAsync(medicineId);
                if (medicine == null)
                {
                    return new ApiResponse<CartItemViewModel> { Success = false, Message = "Medicine not found" };
                }

                if (!medicine.IsActive)
                {
                    return new ApiResponse<CartItemViewModel> { Success = false, Message = "This medicine is currently unavailable" };
                }

                if (medicine.IsExpired)
                {
                    return new ApiResponse<CartItemViewModel> { Success = false, Message = "This medicine has expired and cannot be sold" };
                }

                if (medicine.StockQty < quantity)
                {
                    return new ApiResponse<CartItemViewModel> { Success = false, Message = $"Only {medicine.StockQty} items available in stock" };
                }

                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.MedicineId == medicineId);

                if (existingItem != null)
                {
                    var newQuantity = existingItem.Quantity + quantity;
                    if (newQuantity > medicine.StockQty)
                    {
                        return new ApiResponse<CartItemViewModel> { Success = false, Message = $"Cannot add more. Only {medicine.StockQty} items available in stock" };
                    }
                    existingItem.Quantity = newQuantity;
                }
                else
                {
                    existingItem = new CartItem
                    {
                        UserId = userId,
                        MedicineId = medicineId,
                        Quantity = quantity,
                        UnitPrice = medicine.SalePrice,
                        AddedAt = DateTime.UtcNow
                    };
                    await _context.CartItems.AddAsync(existingItem);
                }

                await _context.SaveChangesAsync();

                var cartItemViewModel = new CartItemViewModel
                {
                    Id = existingItem.Id,
                    MedicineId = medicine.Id,
                    MedicineName = medicine.Name,
                    BatchNo = medicine.BatchNo,
                    HSNCode = medicine.HSNCode,
                    ImageUrl = medicine.ImageUrl,
                    Quantity = existingItem.Quantity,
                    UnitPrice = existingItem.UnitPrice,
                    MRP = medicine.MRP,
                    GSTPercent = medicine.GSTPercent,
                    TotalPrice = existingItem.Quantity * existingItem.UnitPrice,
                    AvailableStock = medicine.StockQty,
                    ExpiryDate = medicine.ExpiryDate
                };

                return new ApiResponse<CartItemViewModel>
                {
                    Success = true,
                    Message = "Item added to cart successfully",
                    Data = cartItemViewModel
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<CartItemViewModel> { Success = false, Message = $"Error adding to cart: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<CartItemViewModel>> UpdateCartItemAsync(string userId, int cartItemId, int quantity)
        {
            try
            {
                var cartItem = await _context.CartItems
                    .Include(c => c.Medicine)
                    .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

                if (cartItem == null)
                {
                    return new ApiResponse<CartItemViewModel> { Success = false, Message = "Cart item not found" };
                }

                if (cartItem.Medicine == null)
                {
                    return new ApiResponse<CartItemViewModel> { Success = false, Message = "Medicine not found" };
                }

                if (quantity > cartItem.Medicine.StockQty)
                {
                    return new ApiResponse<CartItemViewModel> { Success = false, Message = $"Only {cartItem.Medicine.StockQty} items available in stock" };
                }

                if (quantity <= 0)
                {
                    _context.CartItems.Remove(cartItem);
                    await _context.SaveChangesAsync();
                    return new ApiResponse<CartItemViewModel> { Success = true, Message = "Item removed from cart" };
                }

                cartItem.Quantity = quantity;
                await _context.SaveChangesAsync();

                var cartItemViewModel = new CartItemViewModel
                {
                    Id = cartItem.Id,
                    MedicineId = cartItem.MedicineId,
                    MedicineName = cartItem.Medicine.Name,
                    BatchNo = cartItem.Medicine.BatchNo,
                    HSNCode = cartItem.Medicine.HSNCode,
                    ImageUrl = cartItem.Medicine.ImageUrl,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.UnitPrice,
                    MRP = cartItem.Medicine.MRP,
                    GSTPercent = cartItem.Medicine.GSTPercent,
                    TotalPrice = cartItem.Quantity * cartItem.UnitPrice,
                    AvailableStock = cartItem.Medicine.StockQty,
                    ExpiryDate = cartItem.Medicine.ExpiryDate
                };

                return new ApiResponse<CartItemViewModel>
                {
                    Success = true,
                    Message = "Cart updated successfully",
                    Data = cartItemViewModel
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<CartItemViewModel> { Success = false, Message = $"Error updating cart: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<bool>> RemoveFromCartAsync(string userId, int cartItemId)
        {
            try
            {
                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

                if (cartItem == null)
                {
                    return new ApiResponse<bool> { Success = false, Message = "Cart item not found" };
                }

                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                return new ApiResponse<bool> { Success = true, Message = "Item removed from cart", Data = true };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool> { Success = false, Message = $"Error removing from cart: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<bool>> ClearCartAsync(string userId)
        {
            try
            {
                var cartItems = await _context.CartItems
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                return new ApiResponse<bool> { Success = true, Message = "Cart cleared successfully", Data = true };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool> { Success = false, Message = $"Error clearing cart: {ex.Message}" };
            }
        }

        public async Task<int> GetCartCountAsync(string userId)
        {
            return await _context.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity);
        }

        public async Task<CartViewModel> CalculateCartTotalsAsync(string userId)
        {
            return await GetCartAsync(userId);
        }
    }
}
