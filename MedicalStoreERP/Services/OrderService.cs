using Microsoft.EntityFrameworkCore;
using MedicalStoreERP.Data;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;

namespace MedicalStoreERP.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;
        private readonly IMedicineService _medicineService;
        private readonly IConfiguration _configuration;

        public OrderService(
            ApplicationDbContext context,
            ICartService cartService,
            IMedicineService medicineService,
            IConfiguration configuration)
        {
            _context = context;
            _cartService = cartService;
            _medicineService = medicineService;
            _configuration = configuration;
        }

        public async Task<ApiResponse<Order>> PlaceOrderAsync(string userId, CheckoutViewModel model, string? prescriptionUrl = null, string? prescriptionFileName = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cart = await _cartService.GetCartAsync(userId);

                if (!cart.Items.Any())
                {
                    return new ApiResponse<Order> { Success = false, Message = "Your cart is empty" };
                }

                // Validate stock and expiry
                foreach (var item in cart.Items)
                {
                    var medicine = await _context.Medicines.FindAsync(item.MedicineId);
                    if (medicine == null)
                    {
                        return new ApiResponse<Order> { Success = false, Message = $"Medicine '{item.MedicineName}' not found" };
                    }
                    if (medicine.StockQty < item.Quantity)
                    {
                        return new ApiResponse<Order> { Success = false, Message = $"Insufficient stock for '{item.MedicineName}'. Available: {medicine.StockQty}" };
                    }
                    if (medicine.IsExpired)
                    {
                        return new ApiResponse<Order> { Success = false, Message = $"'{item.MedicineName}' has expired and cannot be sold" };
                    }
                }

                var orderNumber = await GenerateOrderNumberAsync();

                var order = new Order
                {
                    OrderNumber = orderNumber,
                    UserId = userId,
                    CustomerName = model.CustomerName,
                    CustomerPhone = model.CustomerPhone,
                    CustomerEmail = model.CustomerEmail,
                    DeliveryAddress = model.DeliveryAddress,
                    City = model.City,
                    State = model.State,
                    PinCode = model.PinCode,
                    SubTotal = cart.SubTotal,
                    CGSTAmount = cart.CGSTAmount,
                    SGSTAmount = cart.SGSTAmount,
                    TotalGSTAmount = cart.TotalGSTAmount,
                    DiscountAmount = cart.DiscountAmount,
                    ShippingCharges = cart.ShippingCharges,
                    TotalAmount = cart.TotalAmount,
                    PaymentMethod = model.PaymentMethod,
                    PaymentStatus = model.PaymentMethod == PaymentMethod.COD ? PaymentStatus.Pending : PaymentStatus.Completed,
                    Status = OrderStatus.Pending,
                    Notes = model.Notes,
                    OrderDate = DateTime.UtcNow,
                    RequiresPrescription = cart.RequiresPrescription,
                    PrescriptionUrl = prescriptionUrl,
                    PrescriptionFileName = prescriptionFileName,
                    PrescriptionVerified = false,
                    // Delivery location fields
                    DeliveryLatitude = model.DeliveryLatitude.HasValue ? (decimal)model.DeliveryLatitude.Value : null,
                    DeliveryLongitude = model.DeliveryLongitude.HasValue ? (decimal)model.DeliveryLongitude.Value : null,
                    DistanceFromStoreKm = model.DistanceFromStore.HasValue ? (decimal)model.DistanceFromStore.Value : null,
                    SameDayDelivery = model.SameDayDeliveryAvailable,
                    EstimatedDeliveryDate = model.SameDayDeliveryAvailable ? DateTime.UtcNow.Date : DateTime.UtcNow.Date.AddDays(1)
                };

                // Add order items
                foreach (var item in cart.Items)
                {
                    var orderItem = new OrderItem
                    {
                        MedicineId = item.MedicineId,
                        MedicineName = item.MedicineName,
                        BatchNo = item.BatchNo,
                        HSNCode = item.HSNCode,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        MRP = item.MRP,
                        GSTPercent = item.GSTPercent,
                        GSTAmount = item.GSTAmount,
                        TotalPrice = item.TotalPrice
                    };
                    order.OrderItems.Add(orderItem);

                    // Reduce stock
                    await _medicineService.UpdateStockAsync(item.MedicineId, item.Quantity, false);
                }

                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();

                // Add to customer ledger
                var ledgerEntry = new CustomerLedger
                {
                    UserId = userId,
                    OrderId = order.Id,
                    TransactionDate = DateTime.UtcNow,
                    Description = $"Order #{order.OrderNumber}",
                    ReferenceNo = order.OrderNumber,
                    Debit = order.TotalAmount,
                    Credit = 0,
                    Balance = order.TotalAmount
                };
                await _context.CustomerLedgers.AddAsync(ledgerEntry);

                // Clear cart
                await _cartService.ClearCartAsync(userId);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ApiResponse<Order>
                {
                    Success = true,
                    Message = "Order placed successfully",
                    Data = order
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ApiResponse<Order>
                {
                    Success = false,
                    Message = $"Error placing order: {ex.Message}"
                };
            }
        }

        public async Task<OrderViewModel?> GetOrderByIdAsync(int orderId, string? userId = null)
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Medicine)
                .Include(o => o.PrescriptionVerifiedByUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(o => o.UserId == userId);
            }

            var order = await query.FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return null;

            return MapToViewModel(order);
        }

        public async Task<List<OrderViewModel>> GetUserOrdersAsync(string userId, int page = 1, int pageSize = 10)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Medicine)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return orders.Select(MapToViewModel).ToList();
        }

        public async Task<PaginatedResult<OrderViewModel>> GetAllOrdersAsync(
            OrderStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= fromDate);
            }

            if (toDate.HasValue)
            {
                query = query.Where(o => o.OrderDate <= toDate.Value.AddDays(1));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(o =>
                    o.OrderNumber.ToLower().Contains(searchTerm) ||
                    o.CustomerName.ToLower().Contains(searchTerm) ||
                    o.CustomerPhone.Contains(searchTerm) ||
                    o.CustomerEmail.ToLower().Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedResult<OrderViewModel>
            {
                Items = orders.Select(MapToViewModel).ToList(),
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<ApiResponse<bool>> UpdateOrderStatusAsync(int orderId, OrderStatus status, string? notes = null)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return new ApiResponse<bool> { Success = false, Message = "Order not found" };
                }

                order.Status = status;

                switch (status)
                {
                    case OrderStatus.Confirmed:
                        order.ConfirmedAt = DateTime.UtcNow;
                        break;
                    case OrderStatus.Shipped:
                        order.ShippedAt = DateTime.UtcNow;
                        break;
                    case OrderStatus.Delivered:
                        order.DeliveredAt = DateTime.UtcNow;
                        if (order.PaymentMethod == PaymentMethod.COD)
                        {
                            order.PaymentStatus = PaymentStatus.Completed;
                        }
                        break;
                    case OrderStatus.Cancelled:
                        order.CancelledAt = DateTime.UtcNow;
                        // Restore stock
                        var orderItems = await _context.OrderItems
                            .Where(oi => oi.OrderId == orderId)
                            .ToListAsync();
                        foreach (var item in orderItems)
                        {
                            await _medicineService.UpdateStockAsync(item.MedicineId, item.Quantity, true);
                        }
                        break;
                }

                if (!string.IsNullOrEmpty(notes))
                {
                    order.Notes = (order.Notes ?? "") + $"\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Status changed to {status}: {notes}";
                }

                await _context.SaveChangesAsync();

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = $"Order status updated to {status}",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error updating order status: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<bool>> CancelOrderAsync(int orderId, string userId, string? reason = null)
        {
            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

                if (order == null)
                {
                    return new ApiResponse<bool> { Success = false, Message = "Order not found" };
                }

                if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
                {
                    return new ApiResponse<bool> { Success = false, Message = "This order cannot be cancelled" };
                }

                return await UpdateOrderStatusAsync(orderId, OrderStatus.Cancelled, reason);
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error cancelling order: {ex.Message}"
                };
            }
        }

        public async Task<string> GenerateOrderNumberAsync()
        {
            var today = DateTime.Today;
            var ordersToday = await _context.Orders
                .CountAsync(o => o.OrderDate.Date == today);

            return $"ORD{today:yyyyMMdd}{(ordersToday + 1):D4}";
        }

        public async Task<ApiResponse<bool>> VerifyPrescriptionAsync(int orderId, string verifiedByUserId, bool approve, string? notes = null)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return new ApiResponse<bool> { Success = false, Message = "Order not found" };
                }

                if (!order.RequiresPrescription)
                {
                    return new ApiResponse<bool> { Success = false, Message = "This order does not require prescription verification" };
                }

                if (string.IsNullOrEmpty(order.PrescriptionUrl))
                {
                    return new ApiResponse<bool> { Success = false, Message = "No prescription uploaded for this order" };
                }

                order.PrescriptionVerified = approve;
                order.PrescriptionVerifiedAt = DateTime.UtcNow;
                order.PrescriptionVerifiedByUserId = verifiedByUserId;
                order.PrescriptionNotes = notes;

                if (approve && order.Status == OrderStatus.Pending)
                {
                    order.Status = OrderStatus.Processing;
                    order.ConfirmedAt = DateTime.UtcNow;
                }
                else if (!approve)
                {
                    order.Status = OrderStatus.Cancelled;
                    order.CancelledAt = DateTime.UtcNow;
                    order.Notes = (order.Notes ?? "") + $"\nPrescription rejected: {notes}";
                }

                await _context.SaveChangesAsync();

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = approve ? "Prescription verified and order approved" : "Prescription rejected and order cancelled",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error verifying prescription: {ex.Message}"
                };
            }
        }
        private static OrderViewModel MapToViewModel(Order order)
        {
            return new OrderViewModel
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderNumber = order.OrderNumber,
                CustomerName = order.CustomerName,
                CustomerPhone = order.CustomerPhone,
                CustomerEmail = order.CustomerEmail,
                DeliveryAddress = order.DeliveryAddress,
                City = order.City,
                State = order.State,
                PinCode = order.PinCode,
                SubTotal = order.SubTotal,
                CGSTAmount = order.CGSTAmount,
                SGSTAmount = order.SGSTAmount,
                TotalGSTAmount = order.TotalGSTAmount,
                DiscountAmount = order.DiscountAmount,
                ShippingCharges = order.ShippingCharges,
                TotalAmount = order.TotalAmount,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                TransactionId = order.TransactionId,
                Status = order.Status,
                Notes = order.Notes,
                OrderDate = order.OrderDate,
                ConfirmedAt = order.ConfirmedAt,
                ShippedAt = order.ShippedAt,
                DeliveredAt = order.DeliveredAt,
                RequiresPrescription = order.RequiresPrescription,
                PrescriptionUrl = order.PrescriptionUrl,
                PrescriptionFileName = order.PrescriptionFileName,
                PrescriptionVerified = order.PrescriptionVerified,
                PrescriptionVerifiedAt = order.PrescriptionVerifiedAt,
                PrescriptionVerifiedByName = order.PrescriptionVerifiedByUser?.FullName,
                PrescriptionNotes = order.PrescriptionNotes,
                Items = order.OrderItems.Select(oi => new OrderItemViewModel
                {
                    Id = oi.Id,
                    MedicineId = oi.MedicineId,
                    MedicineName = oi.MedicineName,
                    BatchNo = oi.BatchNo,
                    HSNCode = oi.HSNCode,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    MRP = oi.MRP,
                    GSTPercent = oi.GSTPercent,
                    GSTAmount = oi.GSTAmount,
                    TotalPrice = oi.TotalPrice
                }).ToList()
            };
        }
    }
}
