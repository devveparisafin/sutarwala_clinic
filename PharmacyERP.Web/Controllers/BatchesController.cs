using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Common;

namespace PharmacyERP.Web.Controllers
{
    [Authorize]
    public class BatchesController : Controller
    {
        private readonly IBaseRepository<MedicineBatch> _batchRepo;
        private readonly IBaseRepository<StockTransaction> _transactionRepo;
        private readonly IBaseRepository<Medicine> _medicineRepo;

        public BatchesController(
            IBaseRepository<MedicineBatch> batchRepo, 
            IBaseRepository<StockTransaction> transactionRepo,
            IBaseRepository<Medicine> medicineRepo)
        {
            _batchRepo = batchRepo;
            _transactionRepo = transactionRepo;
            _medicineRepo = medicineRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string medicineId)
        {
            var batchesTask = _batchRepo.FindAsync(x => x.MedicineId == medicineId && !x.IsDeleted);
            var medicineTask = _medicineRepo.GetByIdAsync(medicineId);

            await Task.WhenAll(batchesTask, medicineTask);

            var batches = batchesTask.Result;
            var medicine = medicineTask.Result;

            ViewBag.MedicineId = medicineId;
            ViewBag.MedicineName = medicine?.Name ?? "Unknown Medicine";
            return View(batches);
        }

        [HttpGet]
        public IActionResult Create(string medicineId)
        {
            return View(new MedicineBatch { MedicineId = medicineId, ExpiryDate = DateTime.UtcNow.AddYears(1) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MedicineBatch batch)
        {
            if (ModelState.IsValid)
            {
                await _batchRepo.CreateAsync(batch);
                
                // Record initial stock if any
                if (batch.CurrentQty > 0)
                {
                    var transaction = new StockTransaction
                    {
                        MedicineId = batch.MedicineId,
                        BatchId = batch.Id!,
                        Type = TransactionType.Purchase,
                        Quantity = batch.CurrentQty,
                        Remarks = "Initial Batch Stock",
                        TransactionDate = DateTime.UtcNow
                    };
                    await _transactionRepo.CreateAsync(transaction);
                }
                
                return RedirectToAction(nameof(Index), new { medicineId = batch.MedicineId });
            }
            return View(batch);
        }
    }
}
