using Microsoft.EntityFrameworkCore;
using MedicalStoreERP.Data;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;

namespace MedicalStoreERP.Services
{
    public interface IReturnService
    {
        Task<ApiResponse<ReturnRequest>> CreateReturnRequestAsync(string userId, CreateReturnRequestViewModel model);
        Task<ReturnRequestViewModel?> GetReturnRequestByIdAsync(int id, string? userId = null);
        Task<List<ReturnRequestViewModel>> GetUserReturnRequestsAsync(string userId);
        Task<PaginatedResult<ReturnRequestViewModel>> GetAllReturnRequestsAsync(
            ReturnStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 10);
        Task<ApiResponse<bool>> ApproveReturnRequestAsync(int returnRequestId, string approvedByUserId, decimal approvedAmount, string? notes = null);
        Task<ApiResponse<bool>> RejectReturnRequestAsync(int returnRequestId, string rejectedByUserId, string reason, string? notes = null);
        Task<ApiResponse<bool>> MarkItemsReceivedAsync(int returnRequestId, string receivedByUserId, List<ReturnItemInventoryViewModel> items, string? notes = null);
        Task<ApiResponse<bool>> ProcessRefundAsync(int returnRequestId, string processedByUserId, decimal refundedAmount, string? transactionId = null, string? notes = null);
        Task<ApiResponse<bool>> CompleteReturnAsync(int returnRequestId, string? notes = null);
        Task<ApiResponse<bool>> CancelReturnRequestAsync(int returnRequestId, string userId, string? reason = null);
        Task<string> GenerateReturnNumberAsync();
        Task<ReturnListViewModel> GetReturnStatisticsAsync(ReturnStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null);
        Task<bool> CanCreateReturnForOrderAsync(int orderId, string userId);
        Task<OrderViewModel?> GetOrderForReturnAsync(int orderId, string userId);
    }

    public class ReturnService : IReturnService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ReturnService> _logger;

        public ReturnService(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<ReturnService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<ApiResponse<ReturnRequest>> CreateReturnRequestAsync(string userId, CreateReturnRequestViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validate order
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Medicine)
                    .FirstOrDefaultAsync(o => o.Id == model.OrderId && o.UserId == userId);

                if (order == null)
                {
                    return new ApiResponse<ReturnRequest> { Success = false, Message = "Order not found" };
                }

                // Check if order is delivered
                if (order.Status != OrderStatus.Delivered)
                {
                    return new ApiResponse<ReturnRequest> { Success = false, Message = "Only delivered orders can be returned" };
                }

                // Check return window (e.g., 7 days from delivery)
                var returnWindowDays = 7;
                if (order.DeliveredAt.HasValue && (DateTime.UtcNow - order.DeliveredAt.Value).TotalDays > returnWindowDays)
                {
                    return new ApiResponse<ReturnRequest> { Success = false, Message = $"Return window of {returnWindowDays} days has expired" };
                }

                // Check if there's already an active return for this order
                var existingReturn = await _context.ReturnRequests
                    .Where(r => r.OrderId == model.OrderId && 
                           r.Status != ReturnStatus.Rejected && 
                           r.Status != ReturnStatus.Cancelled &&
                           r.Status != ReturnStatus.Completed)
                    .FirstOrDefaultAsync();

                if (existingReturn != null)
                {
                    return new ApiResponse<ReturnRequest> { Success = false, Message = "There's already an active return request for this order" };
                }

                // Validate selected items
                var selectedItems = model.Items.Where(i => i.Selected && i.Quantity > 0).ToList();
                if (!selectedItems.Any())
                {
                    return new ApiResponse<ReturnRequest> { Success = false, Message = "Please select at least one item to return" };
                }

                // Calculate total return amount
                decimal totalReturnAmount = 0;
                var returnItems = new List<ReturnItem>();

                foreach (var item in selectedItems)
                {
                    var orderItem = order.OrderItems.FirstOrDefault(oi => oi.Id == item.OrderItemId);
                    if (orderItem == null)
                    {
                        return new ApiResponse<ReturnRequest> { Success = false, Message = $"Order item not found: {item.MedicineName}" };
                    }

                    // Check if quantity is valid
                    var alreadyReturnedQty = await _context.ReturnItems
                        .Where(ri => ri.OrderItemId == item.OrderItemId && 
                               ri.ReturnRequest!.Status != ReturnStatus.Rejected &&
                               ri.ReturnRequest!.Status != ReturnStatus.Cancelled)
                        .SumAsync(ri => ri.Quantity);

                    var availableToReturn = orderItem.Quantity - alreadyReturnedQty;
                    if (item.Quantity > availableToReturn)
                    {
                        return new ApiResponse<ReturnRequest> { Success = false, Message = $"Cannot return more than {availableToReturn} units of {item.MedicineName}" };
                    }

                    var itemTotal = orderItem.UnitPrice * item.Quantity;
                    totalReturnAmount += itemTotal;

                    returnItems.Add(new ReturnItem
                    {
                        OrderItemId = item.OrderItemId,
                        MedicineId = orderItem.MedicineId,
                        MedicineName = orderItem.MedicineName,
                        BatchNo = orderItem.BatchNo,
                        Quantity = item.Quantity,
                        UnitPrice = orderItem.UnitPrice,
                        TotalPrice = itemTotal,
                        Reason = item.Reason
                    });
                }

                // Create return request
                var returnNumber = await GenerateReturnNumberAsync();
                var returnRequest = new ReturnRequest
                {
                    ReturnNumber = returnNumber,
                    OrderId = model.OrderId,
                    UserId = userId,
                    Reason = model.Reason,
                    ReasonDetails = model.ReasonDetails,
                    CustomerNotes = model.CustomerNotes,
                    TotalReturnAmount = totalReturnAmount,
                    RefundMethod = model.PreferredRefundMethod,
                    BankAccountName = model.BankAccountName,
                    BankAccountNumber = model.BankAccountNumber,
                    BankIFSC = model.BankIFSC,
                    UPIId = model.UPIId,
                    Status = ReturnStatus.Pending,
                    ReturnItems = returnItems
                };

                _context.ReturnRequests.Add(returnRequest);
                await _context.SaveChangesAsync();

                // Create notification for admin
                await _notificationService.CreateNotificationAsync(new Notification
                {
                    UserId = userId, // Required field - will be overridden by TargetRole
                    Title = "New Return Request",
                    Message = $"Return request {returnNumber} has been submitted for order #{order.OrderNumber}",
                    Type = "warning",
                    Icon = "fas fa-undo-alt",
                    Link = $"/Admin/Returns/Details/{returnRequest.Id}",
                    TargetRole = "Admin"
                });

                await transaction.CommitAsync();

                _logger.LogInformation("Return request {ReturnNumber} created for order {OrderNumber} by user {UserId}", 
                    returnNumber, order.OrderNumber, userId);

                return new ApiResponse<ReturnRequest> 
                { 
                    Success = true, 
                    Message = "Return request submitted successfully", 
                    Data = returnRequest 
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating return request for order {OrderId}", model.OrderId);
                return new ApiResponse<ReturnRequest> { Success = false, Message = "An error occurred while creating return request" };
            }
        }

        public async Task<ReturnRequestViewModel?> GetReturnRequestByIdAsync(int id, string? userId = null)
        {
            var query = _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.User)
                .Include(r => r.ReturnItems)
                    .ThenInclude(ri => ri.Medicine)
                .Include(r => r.ApprovedByUser)
                .Include(r => r.ReceivedByUser)
                .Include(r => r.RefundProcessedByUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(r => r.UserId == userId);
            }

            var returnRequest = await query.FirstOrDefaultAsync(r => r.Id == id);

            if (returnRequest == null) return null;

            return MapToViewModel(returnRequest);
        }

        public async Task<List<ReturnRequestViewModel>> GetUserReturnRequestsAsync(string userId)
        {
            var returns = await _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.ReturnItems)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return returns.Select(MapToViewModel).ToList();
        }

        public async Task<PaginatedResult<ReturnRequestViewModel>> GetAllReturnRequestsAsync(
            ReturnStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.User)
                .Include(r => r.ReturnItems)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt <= toDate.Value.AddDays(1));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(r => 
                    r.ReturnNumber.ToLower().Contains(searchTerm) ||
                    r.Order!.OrderNumber.ToLower().Contains(searchTerm) ||
                    r.User!.FullName.ToLower().Contains(searchTerm) ||
                    r.User.Email!.ToLower().Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();

            var returns = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedResult<ReturnRequestViewModel>
            {
                Items = returns.Select(MapToViewModel).ToList(),
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<ApiResponse<bool>> ApproveReturnRequestAsync(int returnRequestId, string approvedByUserId, decimal approvedAmount, string? notes = null)
        {
            var returnRequest = await _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == returnRequestId);

            if (returnRequest == null)
            {
                return new ApiResponse<bool> { Success = false, Message = "Return request not found" };
            }

            if (returnRequest.Status != ReturnStatus.Pending)
            {
                return new ApiResponse<bool> { Success = false, Message = "Only pending return requests can be approved" };
            }

            returnRequest.Status = ReturnStatus.Approved;
            returnRequest.ApprovedAt = DateTime.UtcNow;
            returnRequest.ApprovedByUserId = approvedByUserId;
            returnRequest.TotalReturnAmount = approvedAmount;
            returnRequest.AdminNotes = notes;

            await _context.SaveChangesAsync();

            // Notify customer
            await _notificationService.CreateNotificationAsync(new Notification
            {
                UserId = returnRequest.UserId,
                Title = "Return Request Approved",
                Message = $"Your return request {returnRequest.ReturnNumber} has been approved. Please ship the items back.",
                Type = "success",
                Icon = "fas fa-check-circle",
                Link = $"/Returns/Details/{returnRequestId}"
            });

            _logger.LogInformation("Return request {ReturnNumber} approved by {UserId}", returnRequest.ReturnNumber, approvedByUserId);

            return new ApiResponse<bool> { Success = true, Message = "Return request approved successfully", Data = true };
        }

        public async Task<ApiResponse<bool>> RejectReturnRequestAsync(int returnRequestId, string rejectedByUserId, string reason, string? notes = null)
        {
            var returnRequest = await _context.ReturnRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == returnRequestId);

            if (returnRequest == null)
            {
                return new ApiResponse<bool> { Success = false, Message = "Return request not found" };
            }

            if (returnRequest.Status != ReturnStatus.Pending)
            {
                return new ApiResponse<bool> { Success = false, Message = "Only pending return requests can be rejected" };
            }

            returnRequest.Status = ReturnStatus.Rejected;
            returnRequest.CancelledAt = DateTime.UtcNow;
            returnRequest.CancellationReason = reason;
            returnRequest.AdminNotes = notes;

            await _context.SaveChangesAsync();

            // Notify customer
            await _notificationService.CreateNotificationAsync(new Notification
            {
                UserId = returnRequest.UserId,
                Title = "Return Request Rejected",
                Message = $"Your return request {returnRequest.ReturnNumber} has been rejected. Reason: {reason}",
                Type = "danger",
                Icon = "fas fa-times-circle",
                Link = $"/Returns/Details/{returnRequestId}"
            });

            _logger.LogInformation("Return request {ReturnNumber} rejected by {UserId}. Reason: {Reason}", 
                returnRequest.ReturnNumber, rejectedByUserId, reason);

            return new ApiResponse<bool> { Success = true, Message = "Return request rejected", Data = true };
        }

        public async Task<ApiResponse<bool>> MarkItemsReceivedAsync(int returnRequestId, string receivedByUserId, List<ReturnItemInventoryViewModel> items, string? notes = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var returnRequest = await _context.ReturnRequests
                    .Include(r => r.ReturnItems)
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == returnRequestId);

                if (returnRequest == null)
                {
                    return new ApiResponse<bool> { Success = false, Message = "Return request not found" };
                }

                if (returnRequest.Status != ReturnStatus.Approved)
                {
                    return new ApiResponse<bool> { Success = false, Message = "Only approved return requests can be marked as received" };
                }

                // Process each item
                foreach (var itemInput in items)
                {
                    var returnItem = returnRequest.ReturnItems.FirstOrDefault(ri => ri.Id == itemInput.ReturnItemId);
                    if (returnItem == null) continue;

                    returnItem.IsResaleable = itemInput.IsResaleable;
                    returnItem.ConditionNotes = itemInput.ConditionNotes;
                    returnItem.InventoryNotes = itemInput.InventoryNotes;

                    // Return to inventory if resaleable
                    if (itemInput.ReturnToInventory && itemInput.QuantityToReturn > 0)
                    {
                        var medicine = await _context.Medicines.FindAsync(returnItem.MedicineId);
                        if (medicine != null && !medicine.IsExpired)
                        {
                            medicine.StockQty += itemInput.QuantityToReturn;
                            returnItem.ReturnedToInventory = true;
                            returnItem.ReturnedQuantity = itemInput.QuantityToReturn;
                            returnItem.ReturnedToInventoryAt = DateTime.UtcNow;
                            returnItem.ReturnedToInventoryByUserId = receivedByUserId;

                            _logger.LogInformation("Returned {Qty} units of {Medicine} to inventory from return {ReturnNumber}",
                                itemInput.QuantityToReturn, returnItem.MedicineName, returnRequest.ReturnNumber);
                        }
                    }
                }

                returnRequest.Status = ReturnStatus.ItemsReceived;
                returnRequest.ItemsReceivedAt = DateTime.UtcNow;
                returnRequest.ReceivedByUserId = receivedByUserId;
                if (!string.IsNullOrEmpty(notes))
                {
                    returnRequest.AdminNotes = string.IsNullOrEmpty(returnRequest.AdminNotes) 
                        ? notes 
                        : $"{returnRequest.AdminNotes}\n{notes}";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Notify customer
                await _notificationService.CreateNotificationAsync(new Notification
                {
                    UserId = returnRequest.UserId,
                    Title = "Return Items Received",
                    Message = $"We have received the returned items for {returnRequest.ReturnNumber}. Refund will be processed shortly.",
                    Type = "info",
                    Icon = "fas fa-box",
                    Link = $"/Returns/Details/{returnRequestId}"
                });

                return new ApiResponse<bool> { Success = true, Message = "Items marked as received and inventory updated", Data = true };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error marking items received for return {ReturnRequestId}", returnRequestId);
                return new ApiResponse<bool> { Success = false, Message = "An error occurred while processing" };
            }
        }

        public async Task<ApiResponse<bool>> ProcessRefundAsync(int returnRequestId, string processedByUserId, decimal refundedAmount, string? transactionId = null, string? notes = null)
        {
            var returnRequest = await _context.ReturnRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == returnRequestId);

            if (returnRequest == null)
            {
                return new ApiResponse<bool> { Success = false, Message = "Return request not found" };
            }

            if (returnRequest.Status != ReturnStatus.ItemsReceived)
            {
                return new ApiResponse<bool> { Success = false, Message = "Items must be received before processing refund" };
            }

            returnRequest.Status = ReturnStatus.RefundProcessed;
            returnRequest.RefundedAmount = refundedAmount;
            returnRequest.RefundTransactionId = transactionId;
            returnRequest.RefundProcessedAt = DateTime.UtcNow;
            returnRequest.RefundProcessedByUserId = processedByUserId;

            if (!string.IsNullOrEmpty(notes))
            {
                returnRequest.AdminNotes = string.IsNullOrEmpty(returnRequest.AdminNotes)
                    ? notes
                    : $"{returnRequest.AdminNotes}\n{notes}";
            }

            await _context.SaveChangesAsync();

            // Notify customer
            await _notificationService.CreateNotificationAsync(new Notification
            {
                UserId = returnRequest.UserId,
                Title = "Refund Processed",
                Message = $"Your refund of ₹{refundedAmount:N2} for return {returnRequest.ReturnNumber} has been processed." +
                    (!string.IsNullOrEmpty(transactionId) ? $" Transaction ID: {transactionId}" : ""),
                Type = "success",
                Icon = "fas fa-money-bill-wave",
                Link = $"/Returns/Details/{returnRequestId}"
            });

            _logger.LogInformation("Refund of {Amount} processed for return {ReturnNumber} by {UserId}",
                refundedAmount, returnRequest.ReturnNumber, processedByUserId);

            return new ApiResponse<bool> { Success = true, Message = "Refund processed successfully", Data = true };
        }

        public async Task<ApiResponse<bool>> CompleteReturnAsync(int returnRequestId, string? notes = null)
        {
            var returnRequest = await _context.ReturnRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == returnRequestId);

            if (returnRequest == null)
            {
                return new ApiResponse<bool> { Success = false, Message = "Return request not found" };
            }

            if (returnRequest.Status != ReturnStatus.RefundProcessed)
            {
                return new ApiResponse<bool> { Success = false, Message = "Refund must be processed before completing the return" };
            }

            returnRequest.Status = ReturnStatus.Completed;
            returnRequest.CompletedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(notes))
            {
                returnRequest.AdminNotes = string.IsNullOrEmpty(returnRequest.AdminNotes)
                    ? notes
                    : $"{returnRequest.AdminNotes}\n{notes}";
            }

            await _context.SaveChangesAsync();

            // Notify customer
            await _notificationService.CreateNotificationAsync(new Notification
            {
                UserId = returnRequest.UserId,
                Title = "Return Completed",
                Message = $"Your return request {returnRequest.ReturnNumber} has been completed successfully.",
                Type = "success",
                Icon = "fas fa-check-double",
                Link = $"/Returns/Details/{returnRequestId}"
            });

            _logger.LogInformation("Return {ReturnNumber} completed", returnRequest.ReturnNumber);

            return new ApiResponse<bool> { Success = true, Message = "Return completed successfully", Data = true };
        }

        public async Task<ApiResponse<bool>> CancelReturnRequestAsync(int returnRequestId, string userId, string? reason = null)
        {
            var returnRequest = await _context.ReturnRequests
                .FirstOrDefaultAsync(r => r.Id == returnRequestId && r.UserId == userId);

            if (returnRequest == null)
            {
                return new ApiResponse<bool> { Success = false, Message = "Return request not found" };
            }

            if (returnRequest.Status != ReturnStatus.Pending)
            {
                return new ApiResponse<bool> { Success = false, Message = "Only pending return requests can be cancelled" };
            }

            returnRequest.Status = ReturnStatus.Cancelled;
            returnRequest.CancelledAt = DateTime.UtcNow;
            returnRequest.CancellationReason = reason ?? "Cancelled by customer";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Return request {ReturnNumber} cancelled by user {UserId}", returnRequest.ReturnNumber, userId);

            return new ApiResponse<bool> { Success = true, Message = "Return request cancelled successfully", Data = true };
        }

        public async Task<string> GenerateReturnNumberAsync()
        {
            var today = DateTime.UtcNow;
            var prefix = $"RET{today:yyyyMMdd}";

            var lastReturn = await _context.ReturnRequests
                .Where(r => r.ReturnNumber.StartsWith(prefix))
                .OrderByDescending(r => r.ReturnNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastReturn != null)
            {
                var lastNumberStr = lastReturn.ReturnNumber.Substring(prefix.Length);
                if (int.TryParse(lastNumberStr, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }

        public async Task<ReturnListViewModel> GetReturnStatisticsAsync(ReturnStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.ReturnRequests.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(r => r.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(r => r.CreatedAt <= toDate.Value.AddDays(1));

            var stats = new ReturnListViewModel
            {
                TotalReturns = await query.CountAsync(),
                PendingReturns = await query.CountAsync(r => r.Status == ReturnStatus.Pending),
                ApprovedReturns = await query.CountAsync(r => r.Status == ReturnStatus.Approved || r.Status == ReturnStatus.ItemsReceived),
                CompletedReturns = await query.CountAsync(r => r.Status == ReturnStatus.Completed || r.Status == ReturnStatus.RefundProcessed),
                TotalRefundedAmount = await query.Where(r => r.Status == ReturnStatus.Completed || r.Status == ReturnStatus.RefundProcessed).SumAsync(r => r.RefundedAmount),
                StatusFilter = status,
                FromDate = fromDate,
                ToDate = toDate
            };

            return stats;
        }

        public async Task<bool> CanCreateReturnForOrderAsync(int orderId, string userId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null || order.Status != OrderStatus.Delivered)
                return false;

            // Check return window
            var returnWindowDays = 7;
            if (order.DeliveredAt.HasValue && (DateTime.UtcNow - order.DeliveredAt.Value).TotalDays > returnWindowDays)
                return false;

            // Check for existing active return
            var existingReturn = await _context.ReturnRequests
                .AnyAsync(r => r.OrderId == orderId &&
                          r.Status != ReturnStatus.Rejected &&
                          r.Status != ReturnStatus.Cancelled &&
                          r.Status != ReturnStatus.Completed);

            return !existingReturn;
        }

        public async Task<OrderViewModel?> GetOrderForReturnAsync(int orderId, string userId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Medicine)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null) return null;

            // Get already returned quantities
            var returnedQuantities = await _context.ReturnItems
                .Where(ri => ri.ReturnRequest!.OrderId == orderId &&
                        ri.ReturnRequest.Status != ReturnStatus.Rejected &&
                        ri.ReturnRequest.Status != ReturnStatus.Cancelled)
                .GroupBy(ri => ri.OrderItemId)
                .Select(g => new { OrderItemId = g.Key, ReturnedQty = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.OrderItemId, x => x.ReturnedQty);

            return new OrderViewModel
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerName = order.CustomerName,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                OrderDate = order.OrderDate,
                DeliveredAt = order.DeliveredAt,
                Items = order.OrderItems.Select(oi => new OrderItemViewModel
                {
                    Id = oi.Id,
                    MedicineId = oi.MedicineId,
                    MedicineName = oi.MedicineName,
                    BatchNo = oi.BatchNo,
                    Quantity = oi.Quantity - (returnedQuantities.ContainsKey(oi.Id) ? returnedQuantities[oi.Id] : 0),
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice
                }).Where(i => i.Quantity > 0).ToList()
            };
        }

        private ReturnRequestViewModel MapToViewModel(ReturnRequest r)
        {
            return new ReturnRequestViewModel
            {
                Id = r.Id,
                ReturnNumber = r.ReturnNumber,
                OrderId = r.OrderId,
                OrderNumber = r.Order?.OrderNumber ?? "",
                UserId = r.UserId,
                CustomerName = r.User?.FullName ?? "",
                CustomerEmail = r.User?.Email ?? "",
                CustomerPhone = r.User?.PhoneNumber ?? "",
                Reason = r.Reason,
                ReasonDetails = r.ReasonDetails,
                Status = r.Status,
                TotalReturnAmount = r.TotalReturnAmount,
                RefundedAmount = r.RefundedAmount,
                RefundMethod = r.RefundMethod,
                RefundTransactionId = r.RefundTransactionId,
                AdminNotes = r.AdminNotes,
                CustomerNotes = r.CustomerNotes,
                ImageUrl1 = r.ImageUrl1,
                ImageUrl2 = r.ImageUrl2,
                ImageUrl3 = r.ImageUrl3,
                CreatedAt = r.CreatedAt,
                ApprovedAt = r.ApprovedAt,
                ApprovedByName = r.ApprovedByUser?.FullName,
                ItemsReceivedAt = r.ItemsReceivedAt,
                ReceivedByName = r.ReceivedByUser?.FullName,
                RefundProcessedAt = r.RefundProcessedAt,
                RefundProcessedByName = r.RefundProcessedByUser?.FullName,
                CompletedAt = r.CompletedAt,
                CancelledAt = r.CancelledAt,
                CancellationReason = r.CancellationReason,
                BankAccountName = r.BankAccountName,
                BankAccountNumber = r.BankAccountNumber,
                BankIFSC = r.BankIFSC,
                UPIId = r.UPIId,
                OrderDate = r.Order?.OrderDate ?? DateTime.MinValue,
                OrderTotalAmount = r.Order?.TotalAmount ?? 0,
                Items = r.ReturnItems.Select(ri => new ReturnItemViewModel
                {
                    Id = ri.Id,
                    ReturnRequestId = ri.ReturnRequestId,
                    OrderItemId = ri.OrderItemId,
                    MedicineId = ri.MedicineId,
                    MedicineName = ri.MedicineName,
                    BatchNo = ri.BatchNo,
                    Quantity = ri.Quantity,
                    UnitPrice = ri.UnitPrice,
                    TotalPrice = ri.TotalPrice,
                    Reason = ri.Reason,
                    ReturnedToInventory = ri.ReturnedToInventory,
                    ReturnedQuantity = ri.ReturnedQuantity,
                    ReturnedToInventoryAt = ri.ReturnedToInventoryAt,
                    ReturnedToInventoryByName = ri.ReturnedToInventoryByUser?.FullName,
                    InventoryNotes = ri.InventoryNotes,
                    IsResaleable = ri.IsResaleable,
                    ConditionNotes = ri.ConditionNotes,
                    ImageUrl = ri.Medicine?.ImageUrl
                }).ToList()
            };
        }
    }
}
