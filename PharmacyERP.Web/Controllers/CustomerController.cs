using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.ViewModels;

namespace PharmacyERP.Web.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CustomerController(ICustomerService customerService, IWebHostEnvironment webHostEnvironment)
        {
            _customerService = customerService;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _customerService.GetAllCustomersAsync();
            return View(customers);
        }

        public IActionResult Create()
        {
            return View(new CustomerViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerViewModel model)
        {
            if (ModelState.IsValid)
            {
                await _customerService.CreateCustomerAsync(model);
                TempData["SuccessMessage"] = "Customer created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null) return NotFound();

            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, CustomerViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var success = await _customerService.UpdateCustomerAsync(model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Customer updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "Unable to update customer.");
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var success = await _customerService.DeleteCustomerAsync(id);
            if (success)
            {
                return Json(new { success = true, message = "Customer deleted successfully." });
            }
            return Json(new { success = false, message = "Failed to delete customer." });
        }

        public async Task<IActionResult> History(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var history = await _customerService.GetCustomerHistoryAsync(id);
            if (history.Customer == null || history.Customer.Id == null) return NotFound();

            return View(history);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPrescription(CustomerPrescriptionViewModel model)
        {
            if (model.ImageFile == null || model.ImageFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select an image file to upload.";
                return RedirectToAction(nameof(History), new { id = model.CustomerId });
            }

            try
            {
                var extension = Path.GetExtension(model.ImageFile.FileName).ToLower();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };

                if (!allowedExtensions.Contains(extension))
                {
                    TempData["ErrorMessage"] = "Invalid file type. Only JPG, PNG, and PDF are allowed.";
                    return RedirectToAction(nameof(History), new { id = model.CustomerId });
                }

                await _customerService.AddPrescriptionAsync(model, _webHostEnvironment.WebRootPath);
                TempData["SuccessMessage"] = "Prescription uploaded successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error uploading prescription: " + ex.Message;
            }

            return RedirectToAction(nameof(History), new { id = model.CustomerId });
        }

        [HttpPost]
        public async Task<IActionResult> DeletePrescription(string id)
        {
            var success = await _customerService.DeletePrescriptionAsync(id);
            if (success)
            {
                return Json(new { success = true, message = "Prescription deleted successfully." });
            }
            return Json(new { success = false, message = "Failed to delete prescription." });
        }

        [HttpGet]
        public async Task<IActionResult> Search(string q)
        {
            var customers = await _customerService.SearchCustomersAsync(q);
            var results = customers.Select(c => new {
                id = c.Id,
                text = $"{c.Name} - {c.MobileNumber}"
            });
            return Json(results);
        }

        [HttpPost]
        public async Task<IActionResult> AcknowledgeReminder(string id)
        {
            var success = await _customerService.AcknowledgeReminderAsync(id);
            if (success)
            {
                return Json(new { success = true, message = "Reminder acknowledged and advanced." });
            }
            return Json(new { success = false, message = "Failed to acknowledge reminder." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetReminder(SetReminderViewModel model)
        {
            var customer = await _customerService.GetCustomerByIdAsync(model.CustomerId);
            if (customer == null) return Json(new { success = false, message = "Customer not found." });

            customer.ReminderDate = model.ReminderDate;
            customer.ReminderFrequency = model.ReminderFrequency;
            customer.ReminderNote = model.ReminderNote;

            var success = await _customerService.UpdateCustomerAsync(customer);
            if (success)
            {
                return Json(new { success = true, message = "Reminder set successfully." });
            }
            return Json(new { success = false, message = "Failed to set reminder." });
        }

        [HttpGet]
        public async Task<IActionResult> GetPrescriptions(string customerId)
        {
            var prescriptions = await _customerService.GetCustomerPrescriptionsAsync(customerId);
            return Json(new { success = true, data = prescriptions });
        }
    }
}
