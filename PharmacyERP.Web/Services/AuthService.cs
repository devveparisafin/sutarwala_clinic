using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using PharmacyERP.Web.Helpers;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Models.ViewModels;
using PharmacyERP.Web.Common;
using PharmacyERP.Web.Repositories;

namespace PharmacyERP.Web.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Message, User? User)> LoginAsync(LoginViewModel model, HttpContext httpContext);
        Task LogoutAsync(HttpContext httpContext);
        Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IBaseRepository<Role> _roleRepository;

        public AuthService(IUserRepository userRepository, IBaseRepository<Role> roleRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        public async Task<(bool Success, string Message, User? User)> LoginAsync(LoginViewModel model, HttpContext httpContext)
        {
            var user = await _userRepository.GetByUsernameAsync(model.Username);
            if (user == null || !user.IsActive)
            {
                return (false, "Invalid username or account is inactive.", null);
            }

            if (!PasswordHasher.VerifyPassword(model.Password, user.PasswordHash))
            {
                return (false, "Invalid password.", null);
            }

            var role = await _roleRepository.GetByIdAsync(user.RoleId);
            var roleName = role?.Name ?? "User";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id!),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("FullName", user.FullName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, "PharmacyAuth");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            };

            await httpContext.SignInAsync("PharmacyAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

            // Update last login
            user.LastLogin = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user.Id!, user);

            // Set session
            httpContext.Session.SetString(AppConstants.SessionKeys.UserId, user.Id!);
            httpContext.Session.SetString(AppConstants.SessionKeys.UserName, user.Username);
            httpContext.Session.SetString(AppConstants.SessionKeys.UserRole, roleName);

            return (true, "Login successful.", user);
        }

        public async Task LogoutAsync(HttpContext httpContext)
        {
            await httpContext.SignOutAsync("PharmacyAuth");
            httpContext.Session.Clear();
        }

        public async Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !PasswordHasher.VerifyPassword(oldPassword, user.PasswordHash))
            {
                return false;
            }

            user.PasswordHash = PasswordHasher.HashPassword(newPassword);
            await _userRepository.UpdateAsync(userId, user);
            return true;
        }
    }
}
