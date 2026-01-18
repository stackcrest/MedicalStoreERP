using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedicalStoreERP.Services;
using MedicalStoreERP.Models;
using System.Security.Claims;

namespace MedicalStoreERP.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IPdfService _pdfService;

        public OrdersController(IOrderService orderService, IPdfService pdfService)
        {
            _orderService = orderService;
            _pdfService = pdfService;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private bool IsAdminOrSuperAdmin() => User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

        public async Task<IActionResult> Index(int page = 1)
        {
            if (IsAdminOrSuperAdmin())
            {
                TempData["Warning"] = "Customer order history is not available for admin users. Use the Admin Dashboard for order management.";
                return RedirectToAction("Index", "Shop");
            }

            var userId = GetUserId();
            var orders = await _orderService.GetUserOrdersAsync(userId, page, 10);
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = GetUserId();
            var order = await _orderService.GetOrderByIdAsync(id, userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            var userId = GetUserId();
            var result = await _orderService.CancelOrderAsync(id, userId, reason);

            if (result.Success)
            {
                TempData["Success"] = "Order cancelled successfully";
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction("Details", new { id });
        }

        public async Task<IActionResult> Invoice(int id)
        {
            var userId = GetUserId();
            var order = await _orderService.GetOrderByIdAsync(id, userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        public async Task<IActionResult> DownloadInvoice(int id)
        {
            var userId = GetUserId();
            var order = await _orderService.GetOrderByIdAsync(id, userId);

            if (order == null)
            {
                return NotFound();
            }

            try
            {
                var pdfBytes = await _pdfService.GenerateInvoicePdfAsync(id);
                return File(pdfBytes, "application/pdf", $"Invoice_{order.OrderNumber}.pdf");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error generating invoice: {ex.Message}";
                return RedirectToAction("Detail", new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Track(int id)
        {
            var userId = GetUserId();
            var order = await _orderService.GetOrderByIdAsync(id, userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}
