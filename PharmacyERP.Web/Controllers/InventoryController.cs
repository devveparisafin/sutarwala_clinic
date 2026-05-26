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
        private readonly IBaseRepository<Medicine> _medicineRepo;

        public InventoryController(IStockService stockService, IMedicineService medicineService, IBaseRepository<MedicineBatch> batchRepo, IBaseRepository<Medicine> medicineRepo)
        {
            _stockService = stockService;
            _medicineService = medicineService;
            _batchRepo = batchRepo;
            _medicineRepo = medicineRepo;
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

        [HttpGet]
        public async Task<IActionResult> GetLowStockAlerts()
        {
            // Fetch all active medicines
            var medicines = await _medicineRepo.FindAsync(x => x.IsActive && !x.IsDeleted);

            // Get total stock per medicine from active batches
            var allBatches = await _batchRepo.FindAsync(x => x.IsActive && !x.IsDeleted);
            var stockDict = allBatches
                .GroupBy(x => x.MedicineId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.CurrentQty));

            // Filter medicines where stock <= LowStockThreshold (threshold > 0 means it's configured)
            var lowStock = medicines
                .Select(m => new
                {
                    id = m.Id,
                    name = m.Name,
                    currentStock = stockDict.TryGetValue(m.Id!, out var qty) ? qty : 0,
                    threshold = m.LowStockThreshold
                })
                .Where(x => x.threshold > 0 && x.currentStock <= x.threshold)
                .OrderBy(x => x.currentStock)
                .ToList();

            return Json(new { success = true, count = lowStock.Count, items = lowStock });
        }
    }
}
