using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyERP.Web.Models.ViewModels;
using PharmacyERP.Web.Services;
using System.Security.Claims;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Repositories;

namespace PharmacyERP.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;
        private readonly IBaseRepository<Role> _roleRepository;
        private readonly ISettingsService _settingsService;

        public AccountController(IAuthService authService, IUserRepository userRepository, IBaseRepository<Role> roleRepository, ISettingsService settingsService)
        {
            _authService = authService;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _settingsService = settingsService;
        }

        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var settings = await _settingsService.GetSettingsAsync();
            ViewBag.LogoPath = settings?.LogoPath ?? "/img/logo.jpg";
            ViewBag.StoreName = settings?.StoreName ?? "PHARMACY ERP";

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authService.LoginAsync(model, HttpContext);
            if (result.Success)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Dashboard");
            }

            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync(HttpContext);
            return RedirectToAction("Login");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login");

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return NotFound();

            var role = await _roleRepository.GetByIdAsync(user.RoleId);

            var model = new UserProfileViewModel
            {
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                RoleName = role?.Name ?? "N/A",
                LastLogin = user.LastLogin
            };

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login");

            var result = await _authService.ChangePasswordAsync(userId, model.OldPassword, model.NewPassword);
            if (result)
            {
                TempData["SuccessMessage"] = "Password changed successfully.";
                return RedirectToAction("Profile");
            }

            ModelState.AddModelError(string.Empty, "Invalid current password.");
            return View(model);
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
