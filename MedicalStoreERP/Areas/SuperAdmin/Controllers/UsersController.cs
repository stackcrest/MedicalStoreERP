using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalStoreERP.Data;
using MedicalStoreERP.Models;
using MedicalStoreERP.Models.ViewModels;

namespace MedicalStoreERP.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Policy = "SuperAdminOnly")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<UsersController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, string? role, int page = 1)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    u.FullName.Contains(search) ||
                    u.Email!.Contains(search) ||
                    u.PhoneNumber!.Contains(search));
            }

            var totalCount = await query.CountAsync();
            var pageSize = 20;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userViewModels = new List<SuperAdminUserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var userRole = roles.FirstOrDefault() ?? "User";

                if (!string.IsNullOrEmpty(role) && userRole != role)
                {
                    continue;
                }

                userViewModels.Add(new SuperAdminUserViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email!,
                    PhoneNumber = user.PhoneNumber,
                    Role = userRole,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                });
            }

            ViewBag.Search = search;
            ViewBag.Role = role;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Roles = new[] { "SuperAdmin", "Admin", "User" };

            return View(userViewModels);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var currentUserRole = roles.FirstOrDefault() ?? "User";

            // SuperAdmin can edit anyone except other SuperAdmins (unless it's themselves)
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserRole == "SuperAdmin" && user.Id != currentUserId)
            {
                TempData["Error"] = "You cannot edit other SuperAdmin accounts.";
                return RedirectToAction(nameof(Index));
            }

            var model = new SuperAdminUserEditViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                City = user.City,
                State = user.State,
                PinCode = user.PinCode,
                Role = currentUserRole,
                IsActive = user.IsActive,
                AvailableRoles = new[] { "Admin", "User" }.ToList() // SuperAdmin can assign Admin or User roles
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, SuperAdminUserEditViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                model.AvailableRoles = new[] { "Admin", "User" }.ToList();
                return View(model);
            }

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                var currentUserRole = currentRoles.FirstOrDefault() ?? "User";

                // SuperAdmin can edit anyone except other SuperAdmins
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (currentUserRole == "SuperAdmin" && user.Id != currentUserId)
                {
                    TempData["Error"] = "You cannot edit other SuperAdmin accounts.";
                    return RedirectToAction(nameof(Index));
                }

                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                user.Address = model.Address;
                user.City = model.City;
                user.State = model.State;
                user.PinCode = model.PinCode;
                user.IsActive = model.IsActive;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    model.AvailableRoles = new[] { "Admin", "User" }.ToList();
                    return View(model);
                }

                // Update role (only if not SuperAdmin)
                if (currentUserRole != "SuperAdmin" && currentUserRole != model.Role)
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                TempData["Success"] = $"User '{user.FullName}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                ModelState.AddModelError("", "An error occurred while updating the user.");
                model.AvailableRoles = new[] { "Admin", "User" }.ToList();
                return View(model);
            }
        }

        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userRole = roles.FirstOrDefault() ?? "User";

            // Cannot reset password for other SuperAdmins
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userRole == "SuperAdmin" && user.Id != currentUserId)
            {
                TempData["Error"] = "You cannot reset password for other SuperAdmin accounts.";
                return RedirectToAction(nameof(Index));
            }

            var model = new SuperAdminResetPasswordViewModel
            {
                UserId = user.Id,
                UserName = user.FullName,
                Email = user.Email!,
                Role = userRole
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(SuperAdminResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError("", "Password and confirmation password do not match.");
                return View(model);
            }

            try
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    return NotFound();
                }

                var roles = await _userManager.GetRolesAsync(user);
                var userRole = roles.FirstOrDefault() ?? "User";

                // Cannot reset password for other SuperAdmins
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (userRole == "SuperAdmin" && user.Id != currentUserId)
                {
                    TempData["Error"] = "You cannot reset password for other SuperAdmin accounts.";
                    return RedirectToAction(nameof(Index));
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }

                TempData["Success"] = $"Password for '{user.FullName}' has been reset successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", model.UserId);
                ModelState.AddModelError("", "An error occurred while resetting the password.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("SuperAdmin"))
                {
                    return Json(new { success = false, message = "Cannot change status of SuperAdmin accounts." });
                }

                user.IsActive = !user.IsActive;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    return Json(new { success = false, message = "Failed to update user status." });
                }

                return Json(new { success = true, isActive = user.IsActive, message = $"User {(user.IsActive ? "activated" : "deactivated")} successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status {UserId}", id);
                return Json(new { success = false, message = "An error occurred." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("SuperAdmin"))
                {
                    TempData["Error"] = "Cannot delete SuperAdmin accounts.";
                    return RedirectToAction(nameof(Index));
                }

                // Don't allow deleting yourself
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (user.Id == currentUserId)
                {
                    TempData["Error"] = "You cannot delete your own account.";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    TempData["Error"] = "Failed to delete user.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Success"] = "User deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                TempData["Error"] = "An error occurred while deleting the user.";
                return RedirectToAction(nameof(Index));
            }
        }

        public IActionResult Create()
        {
            var model = new SuperAdminUserCreateViewModel
            {
                AvailableRoles = new[] { "Admin", "User" }.ToList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SuperAdminUserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableRoles = new[] { "Admin", "User" }.ToList();
                return View(model);
            }

            try
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "A user with this email already exists.");
                    model.AvailableRoles = new[] { "Admin", "User" }.ToList();
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    City = model.City,
                    State = model.State,
                    PinCode = model.PinCode,
                    EmailConfirmed = true,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    model.AvailableRoles = new[] { "Admin", "User" }.ToList();
                    return View(model);
                }

                await _userManager.AddToRoleAsync(user, model.Role);

                TempData["Success"] = $"User '{user.FullName}' created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                ModelState.AddModelError("", "An error occurred while creating the user.");
                model.AvailableRoles = new[] { "Admin", "User" }.ToList();
                return View(model);
            }
        }
    }
}
