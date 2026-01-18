using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MedicalStoreERP.Models;
using MedicalStoreERP.Services;
using System.Security.Claims;

namespace MedicalStoreERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IPdfService _pdfService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            IOrderService orderService,
            IPdfService pdfService,
            INotificationService notificationService,
            ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _pdfService = pdfService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(OrderStatus? status, DateTime? fromDate, DateTime? toDate, string? searchTerm, int page = 1)
        {
            var orders = await _orderService.GetAllOrdersAsync(
                status: status,
                fromDate: fromDate,
                toDate: toDate,
                searchTerm: searchTerm,
                page: page,
                pageSize: 20
            );

            ViewBag.Statuses = new SelectList(Enum.GetValues<OrderStatus>().Select(s => new { Value = (int)s, Text = s.ToString() }), "Value", "Text", status);
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.Search = searchTerm;

            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            try
            {
                // Get current order to capture old status
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }
                
                var oldStatus = order.Status;
                
                var result = await _orderService.UpdateOrderStatusAsync(id, status);
                if (!result.Success)
                {
                    TempData["Error"] = result.Message ?? "Order not found or cannot be updated.";
                    return RedirectToAction(nameof(Index));
                }

                // Send notification to user about status change
                order.Status = status; // Update the status for notification
                await _notificationService.CreateOrderStatusNotificationForUserAsync(order, oldStatus, status);

                TempData["Success"] = $"Order status updated to {status}.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for order {OrderId}", id);
                TempData["Error"] = "An error occurred while updating the order status.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessOrder(int id)
        {
            // Check if prescription verification is required before processing
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction(nameof(Index));
            }

            if (order.RequiresPrescription && !order.PrescriptionVerified)
            {
                TempData["Error"] = "This order requires prescription verification before it can be processed. Please verify the prescription first.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return await UpdateStatus(id, OrderStatus.Processing);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShipOrder(int id, string? trackingNumber)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                var oldStatus = order.Status;

                // Update tracking number if provided
                // This would require additional service method

                var result = await _orderService.UpdateOrderStatusAsync(id, OrderStatus.Shipped);
                if (!result.Success)
                {
                    TempData["Error"] = result.Message ?? "Failed to update order status.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Send notification to user
                order.Status = OrderStatus.Shipped;
                await _notificationService.CreateOrderStatusNotificationForUserAsync(order, oldStatus, OrderStatus.Shipped);

                TempData["Success"] = "Order has been shipped.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shipping order {OrderId}", id);
                TempData["Error"] = "An error occurred while shipping the order.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeliverOrder(int id)
        {
            return await UpdateStatus(id, OrderStatus.Delivered);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id, string? reason)
        {
            try
            {
                // Get order to find userId for the cancel method
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                var oldStatus = order.Status;

                var result = await _orderService.CancelOrderAsync(id, "admin", reason ?? "Cancelled by admin");
                if (!result.Success)
                {
                    TempData["Error"] = result.Message ?? "Order not found or cannot be cancelled.";
                    return RedirectToAction(nameof(Index));
                }

                // Send notification to user about cancellation
                order.Status = OrderStatus.Cancelled;
                await _notificationService.CreateOrderStatusNotificationForUserAsync(order, oldStatus, OrderStatus.Cancelled);

                TempData["Success"] = "Order has been cancelled.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", id);
                TempData["Error"] = "An error occurred while cancelling the order.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPrescription(int id, bool approve, string? notes)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                var result = await _orderService.VerifyPrescriptionAsync(id, userId, approve, notes);

                if (!result.Success)
                {
                    TempData["Error"] = result.Message;
                    return RedirectToAction(nameof(Details), new { id });
                }

                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying prescription for order {OrderId}", id);
                TempData["Error"] = "An error occurred while verifying the prescription.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        public async Task<IActionResult> Invoice(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        public async Task<IActionResult> DownloadInvoice(int id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    return NotFound();
                }

                var pdfBytes = await _pdfService.GenerateInvoicePdfAsync(id);
                return File(pdfBytes, "application/pdf", $"Invoice_{order.OrderNumber}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice for order {OrderId}", id);
                TempData["Error"] = "An error occurred while generating the invoice.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderStats()
        {
            // Get all orders and count by status manually since GetOrderCountByStatusAsync doesn't exist
            var pendingOrders = await _orderService.GetAllOrdersAsync(status: OrderStatus.Pending, page: 1, pageSize: 1);
            var processingOrders = await _orderService.GetAllOrdersAsync(status: OrderStatus.Processing, page: 1, pageSize: 1);
            var shippedOrders = await _orderService.GetAllOrdersAsync(status: OrderStatus.Shipped, page: 1, pageSize: 1);
            var deliveredOrders = await _orderService.GetAllOrdersAsync(status: OrderStatus.Delivered, page: 1, pageSize: 1);

            return Json(new { 
                pending = pendingOrders.TotalCount, 
                processing = processingOrders.TotalCount, 
                shipped = shippedOrders.TotalCount, 
                delivered = deliveredOrders.TotalCount 
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentOrders(int count = 10)
        {
            var orders = await _orderService.GetAllOrdersAsync(page: 1, pageSize: count);
            return Json(orders.Items.Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.CustomerName,
                o.TotalAmount,
                Status = o.Status.ToString(),
                o.OrderDate
            }));
        }

        public async Task<IActionResult> PendingOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync(status: OrderStatus.Pending, page: 1, pageSize: 100);
            return View("Index", orders);
        }
    }
}
