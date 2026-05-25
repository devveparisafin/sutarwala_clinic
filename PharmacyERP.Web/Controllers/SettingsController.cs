using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.ViewModels;

namespace PharmacyERP.Web.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly ISettingsService _settingsService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SettingsController(ISettingsService settingsService, IWebHostEnvironment webHostEnvironment)
        {
            _settingsService = settingsService;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _settingsService.GetSettingsAsync();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(SettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var success = await _settingsService.UpdateSettingsAsync(model, _webHostEnvironment.WebRootPath);
                if (success)
                {
                    TempData["SuccessMessage"] = "Settings updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Failed to update settings.");
            }
            
            TempData["ErrorMessage"] = "Please correct the errors and try again.";
            return View("Index", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TriggerBackup()
        {
            var success = await _settingsService.TriggerBackupAsync();
            if (success)
            {
                return Json(new { success = true, message = "Database backup created successfully." });
            }
            return Json(new { success = false, message = "Failed to create database backup. Check server logs and ensure mongodump is installed." });
        }
    }
}
