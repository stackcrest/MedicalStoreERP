using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;
using MedicalStoreERP.Services;
using System.Security.Claims;

namespace MedicalStoreERP.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly INotificationService _notificationService;
        private readonly IDeliveryService _deliveryService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CheckoutController(
            ICartService cartService,
            IOrderService orderService,
            INotificationService notificationService,
            IDeliveryService deliveryService,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _cartService = cartService;
            _orderService = orderService;
            _notificationService = notificationService;
            _deliveryService = deliveryService;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private bool IsAdminOrSuperAdmin() => User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

        public async Task<IActionResult> Index()
        {
            if (IsAdminOrSuperAdmin())
            {
                TempData["Warning"] = "Checkout functionality is not available for admin users. Please use the Admin Dashboard for order management.";
                return RedirectToAction("Index", "Shop");
            }

            var userId = GetUserId();
            var cart = await _cartService.GetCartAsync(userId);

            if (!cart.Items.Any())
            {
                TempData["Error"] = "Your cart is empty";
                return RedirectToAction("Index", "Cart");
            }

            var user = await _userManager.GetUserAsync(User);

            var model = new CheckoutViewModel
            {
                Cart = cart,
                CustomerName = user?.FullName ?? "",
                CustomerPhone = user?.PhoneNumber ?? "",
                CustomerEmail = user?.Email ?? "",
                DeliveryAddress = user?.Address ?? "",
                City = user?.City ?? "",
                State = user?.State ?? "",
                PinCode = user?.PinCode ?? "",
                RequiresPrescription = cart.RequiresPrescription
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var userId = GetUserId();
            model.Cart = await _cartService.GetCartAsync(userId);
            model.RequiresPrescription = model.Cart.RequiresPrescription;

            if (!model.Cart.Items.Any())
            {
                TempData["Error"] = "Your cart is empty";
                return RedirectToAction("Index", "Cart");
            }

            // Validate delivery location
            if (!model.DeliveryLatitude.HasValue || !model.DeliveryLongitude.HasValue)
            {
                ModelState.AddModelError("", "Please select your delivery location on the map.");
                return View("Index", model);
            }

            // Check delivery availability using server-side validation
            var deliveryCheck = _deliveryService.CheckDeliveryAvailability(
                model.DeliveryLatitude.Value, 
                model.DeliveryLongitude.Value);

            if (!deliveryCheck.IsDeliverable)
            {
                ModelState.AddModelError("", deliveryCheck.Message);
                TempData["Error"] = $"Delivery not available. Your location is {deliveryCheck.DistanceKm} km away. We currently deliver within {_deliveryService.GetStoreLocation().MaxDeliveryRadiusKm} km only.";
                return View("Index", model);
            }

            // Update model with delivery info from server
            model.DistanceFromStore = deliveryCheck.DistanceKm;
            model.SameDayDeliveryAvailable = deliveryCheck.SameDayDeliveryAvailable;
            model.EstimatedDeliveryTime = deliveryCheck.EstimatedDeliveryTime;

            // Validate prescription upload if required
            if (model.Cart.RequiresPrescription && model.PrescriptionFile == null)
            {
                ModelState.AddModelError("PrescriptionFile", "Prescription is required for medicines that need a doctor's prescription.");
                return View("Index", model);
            }

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            // Handle prescription file upload
            string? prescriptionUrl = null;
            string? prescriptionFileName = null;

            if (model.PrescriptionFile != null && model.PrescriptionFile.Length > 0)
            {
                // Validate file type
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(model.PrescriptionFile.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("PrescriptionFile", "Only PDF, JPG, JPEG, and PNG files are allowed.");
                    return View("Index", model);
                }

                // Validate file size (max 5MB)
                if (model.PrescriptionFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("PrescriptionFile", "File size must be less than 5MB.");
                    return View("Index", model);
                }

                // Save file
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "prescriptions");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.PrescriptionFile.CopyToAsync(stream);
                }

                prescriptionUrl = $"/uploads/prescriptions/{uniqueFileName}";
                prescriptionFileName = model.PrescriptionFile.FileName;
            }

            var result = await _orderService.PlaceOrderAsync(userId, model, prescriptionUrl, prescriptionFileName);

            if (result.Success && result.Data != null)
            {
                // Send notification to admin about new order
                await _notificationService.CreateOrderNotificationForAdminAsync(result.Data);
                
                TempData["Success"] = $"Order placed successfully! Your order number is {result.Data.OrderNumber}";
                return RedirectToAction("Confirmation", new { id = result.Data.Id });
            }

            TempData["Error"] = result.Message;
            return View("Index", model);
        }

        public async Task<IActionResult> Confirmation(int id)
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
