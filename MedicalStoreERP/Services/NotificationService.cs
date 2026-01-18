using Microsoft.EntityFrameworkCore;
using MedicalStoreERP.Data;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;

namespace MedicalStoreERP.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(Notification notification);
        Task CreateOrderNotificationForAdminAsync(Order order);
        Task CreateOrderNotificationForAdminAsync(OrderViewModel order);
        Task CreateOrderStatusNotificationForUserAsync(Order order, OrderStatus oldStatus, OrderStatus newStatus);
        Task CreateOrderStatusNotificationForUserAsync(OrderViewModel order, OrderStatus oldStatus, OrderStatus newStatus);
        Task<List<Notification>> GetUserNotificationsAsync(string userId, int count = 10);
        Task<List<Notification>> GetAdminNotificationsAsync(int count = 10);
        Task<int> GetUnreadCountAsync(string userId);
        Task<int> GetAdminUnreadCountAsync();
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(string userId);
        Task MarkAllAdminAsReadAsync();
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CreateNotificationAsync(Notification notification)
        {
            try
            {
                notification.CreatedAt = DateTime.UtcNow;
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
            }
        }

        public async Task CreateOrderNotificationForAdminAsync(Order order)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = order.UserId,
                    Title = "New Order Received",
                    Message = $"Order #{order.OrderNumber} has been placed by {order.CustomerName} for ₹{order.TotalAmount:N2}",
                    Type = "success",
                    Icon = "fas fa-shopping-cart",
                    Link = $"/Admin/Orders/Details/{order.Id}",
                    OrderId = order.Id,
                    TargetRole = "Admin", // This notification is for admins
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created order notification for admin: Order #{OrderNumber}", order.OrderNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin notification for order {OrderId}", order.Id);
            }
        }

        public async Task CreateOrderNotificationForAdminAsync(OrderViewModel order)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = order.UserId ?? "",
                    Title = "New Order Received",
                    Message = $"Order #{order.OrderNumber} has been placed by {order.CustomerName} for ₹{order.TotalAmount:N2}",
                    Type = "success",
                    Icon = "fas fa-shopping-cart",
                    Link = $"/Admin/Orders/Details/{order.Id}",
                    OrderId = order.Id,
                    TargetRole = "Admin",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created order notification for admin: Order #{OrderNumber}", order.OrderNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin notification for order {OrderId}", order.Id);
            }
        }

        public async Task CreateOrderStatusNotificationForUserAsync(Order order, OrderStatus oldStatus, OrderStatus newStatus)
        {
            try
            {
                var statusMessages = new Dictionary<OrderStatus, (string title, string message, string icon, string type)>
                {
                    { OrderStatus.Confirmed, ("Order Confirmed", $"Your order #{order.OrderNumber} has been confirmed!", "fas fa-check-circle", "success") },
                    { OrderStatus.Processing, ("Order Processing", $"Your order #{order.OrderNumber} is being processed.", "fas fa-cog", "info") },
                    { OrderStatus.Shipped, ("Order Shipped", $"Your order #{order.OrderNumber} has been shipped! Track your delivery.", "fas fa-truck", "info") },
                    { OrderStatus.Delivered, ("Order Delivered", $"Your order #{order.OrderNumber} has been delivered successfully. Thank you for shopping!", "fas fa-box-open", "success") },
                    { OrderStatus.Cancelled, ("Order Cancelled", $"Your order #{order.OrderNumber} has been cancelled. If you have questions, contact support.", "fas fa-times-circle", "danger") },
                    { OrderStatus.Returned, ("Order Returned", $"Your order #{order.OrderNumber} has been returned. Refund will be processed soon.", "fas fa-undo", "warning") }
                };

                if (!statusMessages.ContainsKey(newStatus))
                {
                    return;
                }

                var (title, message, icon, type) = statusMessages[newStatus];

                var notification = new Notification
                {
                    UserId = order.UserId,
                    Title = title,
                    Message = message,
                    Type = type,
                    Icon = icon,
                    Link = $"/Orders/Details/{order.Id}",
                    OrderId = order.Id,
                    TargetRole = null, // This is for specific user only
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created status notification for user {UserId}: Order #{OrderNumber} status changed to {NewStatus}", 
                    order.UserId, order.OrderNumber, newStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user notification for order {OrderId}", order.Id);
            }
        }

        public async Task CreateOrderStatusNotificationForUserAsync(OrderViewModel order, OrderStatus oldStatus, OrderStatus newStatus)
        {
            try
            {
                var statusMessages = new Dictionary<OrderStatus, (string title, string message, string icon, string type)>
                {
                    { OrderStatus.Confirmed, ("Order Confirmed", $"Your order #{order.OrderNumber} has been confirmed!", "fas fa-check-circle", "success") },
                    { OrderStatus.Processing, ("Order Processing", $"Your order #{order.OrderNumber} is being processed.", "fas fa-cog", "info") },
                    { OrderStatus.Shipped, ("Order Shipped", $"Your order #{order.OrderNumber} has been shipped! Track your delivery.", "fas fa-truck", "info") },
                    { OrderStatus.Delivered, ("Order Delivered", $"Your order #{order.OrderNumber} has been delivered successfully. Thank you for shopping!", "fas fa-box-open", "success") },
                    { OrderStatus.Cancelled, ("Order Cancelled", $"Your order #{order.OrderNumber} has been cancelled. If you have questions, contact support.", "fas fa-times-circle", "danger") },
                    { OrderStatus.Returned, ("Order Returned", $"Your order #{order.OrderNumber} has been returned. Refund will be processed soon.", "fas fa-undo", "warning") }
                };

                if (!statusMessages.ContainsKey(newStatus))
                {
                    return;
                }

                var (title, message, icon, type) = statusMessages[newStatus];

                var notification = new Notification
                {
                    UserId = order.UserId ?? "",
                    Title = title,
                    Message = message,
                    Type = type,
                    Icon = icon,
                    Link = $"/Orders/Details/{order.Id}",
                    OrderId = order.Id,
                    TargetRole = null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created status notification for user {UserId}: Order #{OrderNumber} status changed to {NewStatus}", 
                    order.UserId, order.OrderNumber, newStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user notification for order {OrderId}", order.Id);
            }
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int count = 10)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && n.TargetRole == null)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetAdminNotificationsAsync(int count = 10)
        {
            return await _context.Notifications
                .Where(n => n.TargetRole == "Admin" || n.TargetRole == "SuperAdmin")
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && n.TargetRole == null && !n.IsRead)
                .CountAsync();
        }

        public async Task<int> GetAdminUnreadCountAsync()
        {
            return await _context.Notifications
                .Where(n => (n.TargetRole == "Admin" || n.TargetRole == "SuperAdmin") && !n.IsRead)
                .CountAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.TargetRole == null && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task MarkAllAdminAsReadAsync()
        {
            var notifications = await _context.Notifications
                .Where(n => (n.TargetRole == "Admin" || n.TargetRole == "SuperAdmin") && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }
}
