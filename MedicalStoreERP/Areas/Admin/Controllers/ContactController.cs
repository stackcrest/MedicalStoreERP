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
    public class ContactController : Controller
    {
        private readonly IContactService _contactService;
        private readonly ILogger<ContactController> _logger;

        public ContactController(IContactService contactService, ILogger<ContactController> logger)
        {
            _contactService = contactService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
            ContactStatus? status,
            ContactCategory? category,
            string? searchTerm,
            int page = 1)
        {
            var messages = await _contactService.GetAllMessagesAsync(
                status: status,
                category: category,
                searchTerm: searchTerm,
                page: page,
                pageSize: 20
            );

            ViewBag.Statuses = new SelectList(
                Enum.GetValues<ContactStatus>().Select(s => new { Value = (int)s, Text = s.ToString() }),
                "Value", "Text", status);
            ViewBag.Categories = new SelectList(
                Enum.GetValues<ContactCategory>().Select(c => new { Value = (int)c, Text = c.ToString() }),
                "Value", "Text", category);
            ViewBag.Status = status;
            ViewBag.Category = category;
            ViewBag.Search = searchTerm;

            return View(messages);
        }

        public async Task<IActionResult> Details(int id)
        {
            var message = await _contactService.GetMessageByIdAsync(id);
            if (message == null)
            {
                return NotFound();
            }

            return View(message);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, ContactStatus status)
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var result = await _contactService.UpdateStatusAsync(id, status, adminUserId);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Respond(int id, string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                TempData["Error"] = "Response cannot be empty.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var result = await _contactService.RespondToMessageAsync(id, response, adminUserId);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _contactService.GetUnreadCountAsync();
            return Json(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentMessages()
        {
            var messages = await _contactService.GetRecentMessagesAsync(5);
            return Json(messages.Select(m => new
            {
                m.Id,
                m.Name,
                m.Subject,
                m.Email,
                Status = m.Status.ToString(),
                Category = m.Category.ToString(),
                CreatedAt = m.CreatedAt.ToString("MMM dd, HH:mm")
            }));
        }
    }
}
