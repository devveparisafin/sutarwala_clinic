using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Models.ViewModels;
using PharmacyERP.Web.Services;
using System.Security.Claims;

namespace PharmacyERP.Web.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly ISalesService _salesService;
        private readonly IMedicineService _medicineService;
        private readonly IStockService _stockService;
        private readonly IBaseRepository<Medicine> _medicineRepo;
        private readonly IBaseRepository<Rack> _rackRepo;
        private readonly ICustomerService _customerService;
        private readonly ISettingsService _settingsService;
        private readonly IBaseRepository<DoctorPrescription> _prescriptionRepo;

        public SalesController(
            ISalesService salesService,
            IMedicineService medicineService,
            IStockService stockService,
            IBaseRepository<Medicine> medicineRepo,
            IBaseRepository<Rack> rackRepo,
            ICustomerService customerService,
            ISettingsService settingsService,
            IBaseRepository<DoctorPrescription> prescriptionRepo)
        {
            _salesService = salesService;
            _medicineService = medicineService;
            _stockService = stockService;
            _medicineRepo = medicineRepo;
            _rackRepo = rackRepo;
            _customerService = customerService;
            _settingsService = settingsService;
            _prescriptionRepo = prescriptionRepo;
        }

        public async Task<IActionResult> Index()
        {
            var sales = await _salesService.GetAllSalesAsync();
            return View(sales);
        }

        [HttpGet]
        public async Task<IActionResult> Pos(string? prescriptionId)
        {
            var settings = await _settingsService.GetSettingsAsync();
            ViewBag.DefaultGst = settings?.DefaultGstPercentage ?? 18m;
            ViewBag.PrescriptionId = prescriptionId;
            return View(new SalesEntryViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Pos([FromBody] SalesEntryViewModel model)
        {
            if (model == null || !model.Items.Any())
                return Json(new { success = false, message = "No items in the cart." });

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
                var saleId = await _salesService.ProcessSaleAsync(model, userId);

                // If this sale was converted from a Doctor Prescription, mark it as Dispensed
                if (!string.IsNullOrEmpty(model.PrescriptionId))
                {
                    var prescription = await _prescriptionRepo.GetByIdAsync(model.PrescriptionId);
                    if (prescription != null)
                    {
                        prescription.Status = "Dispensed";
                        prescription.SaleId = saleId;
                        await _prescriptionRepo.UpdateAsync(prescription.Id!, prescription);
                    }
                }

                return Json(new { success = true, message = "Sale completed successfully.", saleId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchMedicine(string? term)
        {
            if (string.IsNullOrEmpty(term)) return Json(new List<object>());
            
            var medicines = await _medicineRepo.FindAsync(x => (x.Name.ToLower().Contains(term.ToLower()) || x.Barcode == term) && x.IsActive);
            var racks = await _rackRepo.GetAllAsync();
            
            var settings = await _settingsService.GetSettingsAsync();
            var defaultGst = settings?.DefaultGstPercentage ?? 18m;

            var results = new List<object>();
            foreach (var m in medicines)
            {
                var stock = await _stockService.GetCurrentStockAsync(m.Id!);
                if (stock > 0)
                {
                    var batches = await _stockService.GetBatchesForSaleAsync(m.Id!, 1);
                    var latestBatch = batches.FirstOrDefault();
                    var rackName = racks.FirstOrDefault(r => r.Id == m.RackId)?.Name ?? "N/A";
                    
                    results.Add(new { 
                        id = m.Id, 
                        text = $"{m.Name} (Stock: {stock}) - [Rack: {rackName}]", 
                        name = m.Name,
                        price = latestBatch?.SaleRate ?? 0,
                        gst = defaultGst, // Using settings GST instead of medicine GST
                        stock = stock,
                        rack = rackName,
                        unitsPerStrip = m.UnitsPerStrip,
                        isLooseSale = m.IsLooseSale
                    });
                }
            }
            return Json(results);
        }

        [HttpGet]
        public async Task<IActionResult> SearchCustomers(string? term)
        {
            if (string.IsNullOrEmpty(term)) return Json(new List<object>());
            var customers = await _customerService.SearchCustomersAsync(term);
            var results = customers.Select(c => new {
                id = c.Id,
                text = $"{c.Name} ({c.MobileNumber})",
                name = c.Name,
                phone = c.MobileNumber
            });
            return Json(results);
        }

        public async Task<IActionResult> Receipt(string id)
        {
            var sale = await _salesService.GetSaleByIdAsync(id);
            if (sale == null) return NotFound();

            var details = await _salesService.GetSaleDetailsAsync(id);
            
            // Fetch medicine names for display
            var medicineIds = details.Select(d => d.MedicineId).Distinct();
            var medicines = await _medicineRepo.FindAsync(m => medicineIds.Contains(m.Id));
            var medDict = medicines.ToDictionary(m => m.Id!, m => m.Name+"_"+m.UnitsPerStrip);

            // Fetch actual batches used
            var batchIds = details.Select(d => d.BatchId).Distinct().Where(id => !string.IsNullOrEmpty(id));
            var batches = (await _stockService.GetBatchesByIdsAsync(batchIds)).ToList();
            var batchDict = batches.ToDictionary(b => b.Id!, b => b);
            
            ViewBag.MedicineNames = medDict;
            ViewBag.BatchDetails = batchDict; // BatchId -> MedicineBatch
            ViewBag.Details = details;

            var settings = await _settingsService.GetSettingsAsync();
            ViewBag.Settings = settings;
            
            return View(sale);
        }

        public async Task<IActionResult> RevisedReceipt(string id)
        {
            var sale = await _salesService.GetSaleByIdAsync(id);
            if (sale == null) return NotFound();

            var details = await _salesService.GetSaleDetailsAsync(id);
            
            // Fetch medicine names for display
            var medicineIds = details.Select(d => d.MedicineId).Distinct();
            var medicines = await _medicineRepo.FindAsync(m => medicineIds.Contains(m.Id));
            var medDict = medicines.ToDictionary(m => m.Id!, m => m.Name+"_"+m.UnitsPerStrip);

            // Fetch actual batches used
            var batchIds = details.Select(d => d.BatchId).Distinct().Where(batchId => !string.IsNullOrEmpty(batchId));
            var batches = (await _stockService.GetBatchesByIdsAsync(batchIds)).ToList();
            var batchDict = batches.ToDictionary(b => b.Id!, b => b);
            
            ViewBag.MedicineNames = medDict;
            ViewBag.BatchDetails = batchDict;
            ViewBag.Details = details;

            var settings = await _settingsService.GetSettingsAsync();
            ViewBag.Settings = settings;
            
            return View("RevisedReceipt", sale);
        }

        [HttpGet]
        public IActionResult Return()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoiceForReturn(string invoiceNo)
        {
            if (string.IsNullOrEmpty(invoiceNo)) return Json(new { success = false, message = "Invoice number required" });

            var saleObj = await _salesService.GetSaleByInvoiceAsync(invoiceNo);
            if (saleObj == null) return Json(new { success = false, message = "Invoice not found" });

            if (saleObj.Status == "Returned") return Json(new { success = false, message = "Invoice is already fully returned" });

            var details = await _salesService.GetSaleDetailsAsync(saleObj.Id!);
            
            var medicineIds = details.Select(d => d.MedicineId).Distinct();
            var medicines = (await _medicineRepo.FindAsync(m => medicineIds.Contains(m.Id))).ToDictionary(m => m.Id!, m => m.Name);

            var resultDetails = details.Select(d => new {
                SaleDetailId = d.Id,
                MedicineId = d.MedicineId,
                BatchId = d.BatchId,
                MedicineName = medicines.ContainsKey(d.MedicineId) ? medicines[d.MedicineId] : "Unknown",
                SoldQty = d.Qty,
                ReturnedQty = d.ReturnedQty,
                AvailableToReturn = d.Qty - d.ReturnedQty,
                Rate = d.Rate,
                GST = d.GST,
                TotalPrice = d.TotalPrice,
                UnitRefund = Math.Round(d.TotalPrice / (d.Qty == 0 ? 1 : d.Qty), 2)
            });

            return Json(new { success = true, saleId = saleObj.Id, invoiceNo = saleObj.InvoiceNo, date = saleObj.SaleDate.ToString("yyyy-MM-dd HH:mm"), items = resultDetails });
        }

        [HttpPost]
        public async Task<IActionResult> ProcessReturn([FromBody] SaleReturnViewModel model)
        {
            if (model == null || !model.Items.Any(x => x.ReturnQty > 0))
                return Json(new { success = false, message = "No items to return." });

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
                await _salesService.ProcessSaleReturnAsync(model, userId);
                return Json(new { success = true, message = "Return processed successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> QuickAddCustomer([FromBody] PharmacyERP.Web.Models.ViewModels.Masters.QuickAddCustomerViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data entered." });

            var (success, message, id) = await _customerService.QuickAddAsync(model);
            return Json(new
            {
                success = success,
                message = message,
                id = id,
                text = $"{model.Name.Trim()} ({model.Phone.Trim()})",
                name = model.Name.Trim(),
                phone = model.Phone.Trim()
            });
        }
    }
}
