using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Models.ViewModels;

namespace PharmacyERP.Web.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _customerService.GetAllCustomersAsync();
            return View(customers);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
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
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerViewModel model)
        {
            if (ModelState.IsValid)
            {
                await _customerService.UpdateCustomerAsync(model);
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _customerService.DeleteCustomerAsync(id);
            return Json(new { success = true, message = "Customer deleted successfully." });
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var history = await _customerService.GetCustomerHistoryAsync(id);
            if (history.Customer == null) return NotFound();
            return View(history);
        }

        [HttpGet]
        public async Task<IActionResult> Ledger(string id)
        {
            var ledger = await _customerService.GetLedgerAsync(id);
            if (ledger == null) return NotFound();
            return View(ledger);
        }

        [HttpPost]
        public async Task<IActionResult> AddPayment(CustomerPayment payment)
        {
            var success = await _customerService.AddPaymentAsync(payment);
            if (success)
            {
                return Json(new { success = true, message = "Payment recorded successfully." });
            }
            return Json(new { success = false, message = "Failed to record payment." });
        }
        [HttpPost]
        public async Task<IActionResult> SetReminder([FromForm] string id, [FromForm] DateTime? reminderDate, [FromForm] string? reminderFrequency, [FromForm] string? reminderNote)
        {
            var success = await _customerService.SetReminderAsync(id, reminderDate, reminderFrequency, reminderNote);
            if (success)
            {
                return Json(new { success = true, message = "Reminder set successfully." });
            }
            return Json(new { success = false, message = "Failed to set reminder." });
        }
    }
}
