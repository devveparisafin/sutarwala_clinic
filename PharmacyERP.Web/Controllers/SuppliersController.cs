using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Models.ViewModels;
using PharmacyERP.Web.Services;

namespace PharmacyERP.Web.Controllers
{
    [Authorize]
    public class SuppliersController : Controller
    {
        private readonly ISupplierService _supplierService;

        public SuppliersController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _supplierService.GetAllAsync();
            return View(list);
        }

        [HttpGet]
        public IActionResult Create() => View(new SupplierViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupplierViewModel model)
        {
            if (ModelState.IsValid)
            {
                var supplier = new Supplier
                {
                    Name = model.Name,
                    ContactPerson = model.ContactPerson,
                    Phone = model.Phone,
                    Email = model.Email,
                    Address = model.Address,
                    GSTIN = model.GSTIN,
                    OpeningBalance = model.OpeningBalance,
                    CurrentBalance = model.OpeningBalance,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.UtcNow
                };
                await _supplierService.CreateAsync(supplier);
                TempData["SuccessMessage"] = "Supplier added successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var s = await _supplierService.GetByIdAsync(id);
            if (s == null) return NotFound();

            var model = new SupplierViewModel
            {
                Id = s.Id,
                Name = s.Name,
                ContactPerson = s.ContactPerson,
                Phone = s.Phone,
                Email = s.Email,
                Address = s.Address,
                GSTIN = s.GSTIN,
                OpeningBalance = s.OpeningBalance,
                IsActive = s.IsActive
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SupplierViewModel model)
        {
            if (ModelState.IsValid)
            {
                var s = await _supplierService.GetByIdAsync(model.Id!);
                if (s == null) return NotFound();

                s.Name = model.Name;
                s.ContactPerson = model.ContactPerson;
                s.Phone = model.Phone;
                s.Email = model.Email;
                s.Address = model.Address;
                s.GSTIN = model.GSTIN;
                s.IsActive = model.IsActive;
                s.UpdatedAt = DateTime.UtcNow;

                await _supplierService.UpdateAsync(s.Id!, s);
                TempData["SuccessMessage"] = "Supplier updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        public async Task<IActionResult> Ledger(string id)
        {
            var ledger = await _supplierService.GetLedgerAsync(id);
            return View(ledger);
        }

        [HttpPost]
        public async Task<IActionResult> AddPayment(SupplierPayment payment)
        {
            await _supplierService.AddPaymentAsync(payment);
            return Json(new { success = true, message = "Payment recorded successfully." });
        }

        [HttpPost]
        public async Task<IActionResult> QuickAdd([FromBody] PharmacyERP.Web.Models.ViewModels.Masters.QuickAddSupplierViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data entered." });

            var (success, message, id) = await _supplierService.QuickAddAsync(model);
            return Json(new { success = success, message = message, id = id, text = model.Name.Trim() });
        }
    }
}
