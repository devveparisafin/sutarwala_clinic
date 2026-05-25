using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PharmacyERP.Web.Helpers;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Models.ViewModels;
using PharmacyERP.Web.Repositories;

namespace PharmacyERP.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IBaseRepository<Role> _roleRepository;

        public UsersController(IUserRepository userRepository, IBaseRepository<Role> roleRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userRepository.GetAllAsync();
            var roles = await _roleRepository.GetAllAsync();

            var model = users.Select(u => new UserListItemViewModel
            {
                Id = u.Id!,
                Username = u.Username,
                Email = u.Email,
                FullName = u.FullName,
                RoleName = roles.FirstOrDefault(r => r.Id == u.RoleId)?.Name ?? "N/A",
                IsActive = u.IsActive,
                LastLogin = u.LastLogin
            }).ToList();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new UserEditViewModel
            {
                Roles = (await _roleRepository.GetAllAsync()).Select(r => new SelectListItem(r.Name, r.Id))
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserEditViewModel model)
        {
            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("Password", "Password is required for new users.");
            }

            if (ModelState.IsValid)
            {
                var existing = await _userRepository.GetByUsernameAsync(model.Username);
                if (existing != null)
                {
                    ModelState.AddModelError("Username", "Username already exists.");
                }
                else
                {
                    var user = new User
                    {
                        Username = model.Username,
                        Email = model.Email,
                        FullName = model.FullName,
                        RoleId = model.RoleId,
                        IsActive = model.IsActive,
                        PasswordHash = PasswordHasher.HashPassword(model.Password!),
                        CreatedAt = DateTime.UtcNow
                    };

                    await _userRepository.CreateAsync(user);
                    TempData["SuccessMessage"] = "User created successfully.";
                    return RedirectToAction(nameof(Index));
                }
            }

            model.Roles = (await _roleRepository.GetAllAsync()).Select(r => new SelectListItem(r.Name, r.Id));
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound();

            var model = new UserEditViewModel
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                RoleId = user.RoleId,
                IsActive = user.IsActive,
                Roles = (await _roleRepository.GetAllAsync()).Select(r => new SelectListItem(r.Name, r.Id))
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userRepository.GetByIdAsync(model.Id!);
                if (user == null) return NotFound();

                user.Username = model.Username;
                user.Email = model.Email;
                user.FullName = model.FullName;
                user.RoleId = model.RoleId;
                user.IsActive = model.IsActive;
                user.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(model.Password))
                {
                    user.PasswordHash = PasswordHasher.HashPassword(model.Password);
                }

                await _userRepository.UpdateAsync(user.Id!, user);
                TempData["SuccessMessage"] = "User updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            model.Roles = (await _roleRepository.GetAllAsync()).Select(r => new SelectListItem(r.Name, r.Id));
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _userRepository.DeleteAsync(id);
            return Json(new { success = true, message = "User deleted successfully." });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return Json(new { success = false, message = "User not found." });

            user.IsActive = !user.IsActive;
            await _userRepository.UpdateAsync(id, user);
            return Json(new { success = true, message = $"User {(user.IsActive ? "activated" : "deactivated")} successfully." });
        }
    }
}
