using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;
using MedicalStoreERP.Services;
using System.Security.Claims;

namespace MedicalStoreERP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class ReturnsController : Controller
    {
        private readonly IReturnService _returnService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ReturnsController> _logger;

        public ReturnsController(
            IReturnService returnService,
            INotificationService notificationService,
            ILogger<ReturnsController> logger)
        {
            _returnService = returnService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(ReturnStatus? status, DateTime? fromDate, DateTime? toDate, string? searchTerm, int page = 1)
        {
            var returns = await _returnService.GetAllReturnRequestsAsync(
                status: status,
                fromDate: fromDate,
                toDate: toDate,
                searchTerm: searchTerm,
                page: page,
                pageSize: 20
            );

            var statistics = await _returnService.GetReturnStatisticsAsync(status, fromDate, toDate);

            var model = new ReturnListViewModel
            {
                Returns = returns,
                StatusFilter = status,
                SearchTerm = searchTerm,
                FromDate = fromDate,
                ToDate = toDate,
                TotalReturns = statistics.TotalReturns,
                PendingReturns = statistics.PendingReturns,
                ApprovedReturns = statistics.ApprovedReturns,
                CompletedReturns = statistics.CompletedReturns,
                TotalRefundedAmount = statistics.TotalRefundedAmount
            };

            ViewBag.Statuses = new SelectList(
                Enum.GetValues<ReturnStatus>().Select(s => new { Value = (int)s, Text = s.ToString() }), 
                "Value", "Text", status);

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var returnRequest = await _returnService.GetReturnRequestByIdAsync(id);
            if (returnRequest == null)
            {
                return NotFound();
            }

            return View(returnRequest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, decimal approvedAmount, string? adminNotes)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _returnService.ApproveReturnRequestAsync(id, userId, approvedAmount, adminNotes);

            if (result.Success)
            {
                TempData["Success"] = "Return request approved successfully";
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string rejectionReason, string? adminNotes)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["Error"] = "Please provide a rejection reason";
                return RedirectToAction(nameof(Details), new { id });
            }

            var result = await _returnService.RejectReturnRequestAsync(id, userId, rejectionReason, adminNotes);

            if (result.Success)
            {
                TempData["Success"] = "Return request rejected";
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> ReceiveItems(int id)
        {
            var returnRequest = await _returnService.GetReturnRequestByIdAsync(id);
            if (returnRequest == null)
            {
                return NotFound();
            }

            if (returnRequest.Status != ReturnStatus.Approved)
            {
                TempData["Error"] = "Only approved returns can have items received";
                return RedirectToAction(nameof(Details), new { id });
            }

            var model = new ProcessReturnViewModel
            {
                ReturnRequestId = id,
                ReturnRequest = returnRequest,
                ItemsInventory = returnRequest.Items.Select(i => new ReturnItemInventoryViewModel
                {
                    ReturnItemId = i.Id,
                    MedicineName = i.MedicineName,
                    BatchNo = i.BatchNo,
                    MaxQuantity = i.Quantity,
                    ReturnToInventory = true,
                    QuantityToReturn = i.Quantity,
                    IsResaleable = true
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveItems(ProcessReturnViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _returnService.MarkItemsReceivedAsync(
                model.ReturnRequestId, 
                userId, 
                model.ItemsInventory, 
                model.AdminNotes);

            if (result.Success)
            {
                TempData["Success"] = "Items received and inventory updated";
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction(nameof(Details), new { id = model.ReturnRequestId });
        }

        public async Task<IActionResult> ProcessRefund(int id)
        {
            var returnRequest = await _returnService.GetReturnRequestByIdAsync(id);
            if (returnRequest == null)
            {
                return NotFound();
            }

            if (returnRequest.Status != ReturnStatus.ItemsReceived)
            {
                TempData["Error"] = "Items must be received before processing refund";
                return RedirectToAction(nameof(Details), new { id });
            }

            var model = new ProcessReturnViewModel
            {
                ReturnRequestId = id,
                ReturnRequest = returnRequest,
                ActualRefundedAmount = returnRequest.TotalReturnAmount
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRefund(ProcessReturnViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (!model.ActualRefundedAmount.HasValue || model.ActualRefundedAmount <= 0)
            {
                TempData["Error"] = "Please enter a valid refund amount";
                return RedirectToAction(nameof(ProcessRefund), new { id = model.ReturnRequestId });
            }

            var result = await _returnService.ProcessRefundAsync(
                model.ReturnRequestId,
                userId,
                model.ActualRefundedAmount.Value,
                model.RefundTransactionId,
                model.AdminNotes);

            if (result.Success)
            {
                TempData["Success"] = "Refund processed successfully";
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction(nameof(Details), new { id = model.ReturnRequestId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string? adminNotes)
        {
            var result = await _returnService.CompleteReturnAsync(id, adminNotes);

            if (result.Success)
            {
                TempData["Success"] = "Return completed successfully";
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // API endpoints for AJAX calls
        [HttpGet]
        public async Task<IActionResult> GetStatistics(DateTime? fromDate, DateTime? toDate)
        {
            var stats = await _returnService.GetReturnStatisticsAsync(null, fromDate, toDate);
            return Json(new
            {
                totalReturns = stats.TotalReturns,
                pendingReturns = stats.PendingReturns,
                approvedReturns = stats.ApprovedReturns,
                completedReturns = stats.CompletedReturns,
                totalRefundedAmount = stats.TotalRefundedAmount
            });
        }
    }
}
