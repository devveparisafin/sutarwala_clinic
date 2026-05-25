using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Services;

namespace PharmacyERP.Web.Controllers
{
    [Authorize]
    public class InventoryController : Controller
    {
        private readonly IStockService _stockService;
        private readonly IMedicineService _medicineService;
        private readonly IBaseRepository<MedicineBatch> _batchRepo;

        public InventoryController(IStockService stockService, IMedicineService medicineService, IBaseRepository<MedicineBatch> batchRepo)
        {
            _stockService = stockService;
            _medicineService = medicineService;
            _batchRepo = batchRepo;
        }

        public async Task<IActionResult> StockSummary()
        {
            var medicines = await _medicineService.GetMedicineListAsync();
            return View(medicines);
        }

        public async Task<IActionResult> ExpiryAlerts()
        {
            var nearExpiry = await _batchRepo.FindAsync(x => x.ExpiryDate <= DateTime.UtcNow.AddDays(90) && x.CurrentQty > 0 && !x.IsDeleted);
            // In a real app, join with Medicine name
            return View(nearExpiry);
        }

        [HttpGet]
        public async Task<IActionResult> AdjustStock(string medicineId)
        {
            ViewBag.MedicineId = medicineId;
            var batches = await _batchRepo.FindAsync(x => x.MedicineId == medicineId && !x.IsDeleted);
            return View(batches);
        }

        [HttpPost]
        public async Task<IActionResult> AdjustStock(InventoryAdjustment adjustment)
        {
            await _stockService.AdjustStockAsync(adjustment);
            TempData["SuccessMessage"] = "Stock adjusted successfully.";
            return RedirectToAction(nameof(StockSummary));
        }
    }
}
