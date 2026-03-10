using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;
using MedicalStoreERP.Services;
using System.Security.Claims;

namespace MedicalStoreERP.Controllers
{
    [Authorize]
    public class ReturnsController : Controller
    {
        private readonly IReturnService _returnService;
        private readonly IOrderService _orderService;
        private readonly ILogger<ReturnsController> _logger;

        public ReturnsController(
            IReturnService returnService,
            IOrderService orderService,
            ILogger<ReturnsController> logger)
        {
            _returnService = returnService;
            _orderService = orderService;
            _logger = logger;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var returns = await _returnService.GetUserReturnRequestsAsync(userId);

            var model = new CustomerReturnListViewModel
            {
                Returns = returns,
                TotalReturns = returns.Count,
                ActiveReturns = returns.Count(r => r.Status != ReturnStatus.Completed && 
                                                   r.Status != ReturnStatus.Rejected && 
                                                   r.Status != ReturnStatus.Cancelled),
                TotalRefunded = returns.Where(r => r.Status == ReturnStatus.Completed || 
                                                   r.Status == ReturnStatus.RefundProcessed)
                                       .Sum(r => r.RefundedAmount)
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = GetUserId();
            var returnRequest = await _returnService.GetReturnRequestByIdAsync(id, userId);

            if (returnRequest == null)
            {
                return NotFound();
            }

            return View(returnRequest);
        }

        public async Task<IActionResult> Create(int orderId)
        {
            var userId = GetUserId();

            // Check if return can be created for this order
            var canCreate = await _returnService.CanCreateReturnForOrderAsync(orderId, userId);
            if (!canCreate)
            {
                TempData["Error"] = "This order is not eligible for return. Either the return window has expired, or there's already an active return request.";
                return RedirectToAction("Details", "Orders", new { id = orderId });
            }

            var order = await _returnService.GetOrderForReturnAsync(orderId, userId);
            if (order == null || !order.Items.Any())
            {
                TempData["Error"] = "No items available for return";
                return RedirectToAction("Details", "Orders", new { id = orderId });
            }

            var model = new CreateReturnRequestViewModel
            {
                OrderId = orderId,
                Order = order,
                Items = order.Items.Select(i => new CreateReturnItemViewModel
                {
                    OrderItemId = i.Id,
                    MedicineId = i.MedicineId,
                    MedicineName = i.MedicineName,
                    BatchNo = i.BatchNo,
                    MaxQuantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    Selected = false
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReturnRequestViewModel model)
        {
            var userId = GetUserId();

            // Validate that at least one item is selected
            var selectedItems = model.Items.Where(i => i.Selected && i.Quantity > 0).ToList();
            if (!selectedItems.Any())
            {
                ModelState.AddModelError("", "Please select at least one item to return");
                
                // Reload order data for view
                model.Order = await _returnService.GetOrderForReturnAsync(model.OrderId, userId);
                return View(model);
            }

            // Validate bank details if bank transfer is selected
            if (model.PreferredRefundMethod == RefundMethod.BankTransfer)
            {
                if (string.IsNullOrWhiteSpace(model.BankAccountName) ||
                    string.IsNullOrWhiteSpace(model.BankAccountNumber) ||
                    string.IsNullOrWhiteSpace(model.BankIFSC))
                {
                    ModelState.AddModelError("", "Bank account details are required for bank transfer refund");
                    model.Order = await _returnService.GetOrderForReturnAsync(model.OrderId, userId);
                    return View(model);
                }
            }

            if (model.PreferredRefundMethod == RefundMethod.UPI)
            {
                if (string.IsNullOrWhiteSpace(model.UPIId))
                {
                    ModelState.AddModelError("", "UPI ID is required for UPI refund");
                    model.Order = await _returnService.GetOrderForReturnAsync(model.OrderId, userId);
                    return View(model);
                }
            }

            var result = await _returnService.CreateReturnRequestAsync(userId, model);

            if (result.Success)
            {
                TempData["Success"] = $"Return request {result.Data?.ReturnNumber} submitted successfully. We'll review it shortly.";
                return RedirectToAction(nameof(Details), new { id = result.Data?.Id });
            }

            TempData["Error"] = result.Message;
            model.Order = await _returnService.GetOrderForReturnAsync(model.OrderId, userId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            var userId = GetUserId();
            var result = await _returnService.CancelReturnRequestAsync(id, userId, reason);

            if (result.Success)
            {
                TempData["Success"] = "Return request cancelled successfully";
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // API endpoint to check return eligibility
        [HttpGet]
        public async Task<IActionResult> CheckEligibility(int orderId)
        {
            var userId = GetUserId();
            var canCreate = await _returnService.CanCreateReturnForOrderAsync(orderId, userId);
            return Json(new { eligible = canCreate });
        }
    }
}
