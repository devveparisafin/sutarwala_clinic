using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Models.ViewModels;
using PharmacyERP.Web.Services;
using System.Security.Claims;

namespace PharmacyERP.Web.Controllers
{
    [Authorize]
    public class PurchasesController : Controller
    {
        private readonly IPurchaseService _purchaseService;
        private readonly ISupplierService _supplierService;
        private readonly IMedicineService _medicineService;
        private readonly IBaseRepository<Medicine> _medicineRepo;

        public PurchasesController(
            IPurchaseService purchaseService,
            ISupplierService supplierService,
            IMedicineService medicineService,
            IBaseRepository<Medicine> medicineRepo)
        {
            _purchaseService = purchaseService;
            _supplierService = supplierService;
            _medicineService = medicineService;
            _medicineRepo = medicineRepo;
        }

        public async Task<IActionResult> Index()
        {
            var purchases = await _purchaseService.GetAllPurchasesAsync();
            var suppliers = await _supplierService.GetAllAsync();
            var supplierDict = suppliers.ToDictionary(x => x.Id!, x => x.Name);

            var viewModel = purchases.Select(p => new PurchaseSummaryViewModel
            {
                Id = p.Id!,
                InvoiceNo = p.InvoiceNo,
                PurchaseDate = p.PurchaseDate,
                SupplierName = supplierDict.TryGetValue(p.SupplierId, out var name) ? name : "Unknown (" + p.SupplierId + ")",
                SubTotal = p.SubTotal,
                TaxAmount = p.TaxAmount,
                TotalAmount = p.TotalAmount
            }).OrderByDescending(x => x.PurchaseDate);

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Suppliers = (await _supplierService.FindAsync(x => x.IsActive)).Select(x => new SelectListItem(x.Name, x.Id));
            return View(new PurchaseEntryViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PurchaseEntryViewModel model)
        {
            if (model == null || !model.Items.Any())
                return Json(new { success = false, message = "Invalid purchase data." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
            var result = await _purchaseService.ProcessPurchaseAsync(model, userId);
            
            if (result)
                return Json(new { success = true, message = "Purchase recorded successfully.", redirectUrl = Url.Action("Index") });
            
            return Json(new { success = false, message = "Failed to process purchase." });
        }

        [HttpGet]
        public async Task<IActionResult> SearchMedicine(string? term)
        {
            if (string.IsNullOrEmpty(term)) return Json(new List<object>());
            var results = await _medicineRepo.FindAsync(x => x.Name.ToLower().Contains(term.ToLower()) && x.IsActive);
            return Json(results.Select(x => new { id = x.Id, text = x.Name, gst = x.GST, unitsPerStrip = x.UnitsPerStrip }));
        }

        public async Task<IActionResult> Details(string id)
        {
            var master = await _purchaseService.GetPurchaseByIdAsync(id);
            if (master == null) return NotFound();

            var details = await _purchaseService.GetPurchaseDetailsAsync(id);
            var medicines = await _medicineRepo.GetAllAsync();
            var medDict = medicines.ToDictionary(x => x.Id!, x => x.Name);

            var detailViewModels = details.Select(d => new PurchaseItemViewModel
            {
                MedicineId = d.MedicineId,
                MedicineName = medDict.TryGetValue(d.MedicineId, out var name) ? name : "Unknown (" + d.MedicineId + ")",
                BatchNo = d.BatchNo,
                ExpiryDate = d.ExpiryDate,
                Qty = d.Qty,
                PurchaseRate = d.PurchaseRate,
                SaleRate = d.SaleRate,
                MRP = d.MRP,
                GST = d.GST,
                TotalPrice = d.TotalPrice
            });

            ViewBag.Details = detailViewModels;
            
            // Fetch supplier name
            var supplier = await _supplierService.GetByIdAsync(master.SupplierId);
            ViewBag.SupplierName = supplier?.Name ?? "N/A";

            return View(master);
        }
    }
}
