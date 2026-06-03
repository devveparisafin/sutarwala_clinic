using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Models.ViewModels;
using PharmacyERP.Web.Services;
using System.Security.Claims;
using System.IO;
using System.Text;
using System.Globalization;

namespace PharmacyERP.Web.Controllers
{
    [Authorize]
    public class PurchasesController : Controller
    {
        private readonly IPurchaseService _purchaseService;
        private readonly ISupplierService _supplierService;
        private readonly IMedicineService _medicineService;
        private readonly IBaseRepository<Medicine> _medicineRepo;
        private readonly IBaseRepository<MedicineCategory> _categoryRepo;
        private readonly IBaseRepository<Manufacturer> _manufacturerRepo;
        private readonly IBaseRepository<MedicineUnit> _unitRepo;
        private readonly IBaseRepository<GenericMedicine> _genericRepo;
        private readonly IBaseRepository<Rack> _rackRepo;

        public PurchasesController(
            IPurchaseService purchaseService,
            ISupplierService supplierService,
            IMedicineService medicineService,
            IBaseRepository<Medicine> medicineRepo,
            IBaseRepository<MedicineCategory> categoryRepo,
            IBaseRepository<Manufacturer> manufacturerRepo,
            IBaseRepository<MedicineUnit> unitRepo,
            IBaseRepository<GenericMedicine> genericRepo,
            IBaseRepository<Rack> rackRepo)
        {
            _purchaseService = purchaseService;
            _supplierService = supplierService;
            _medicineService = medicineService;
            _medicineRepo = medicineRepo;
            _categoryRepo = categoryRepo;
            _manufacturerRepo = manufacturerRepo;
            _unitRepo = unitRepo;
            _genericRepo = genericRepo;
            _rackRepo = rackRepo;
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

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
                var result = await _purchaseService.ProcessPurchaseAsync(model, userId);
                
                if (result)
                    return Json(new { success = true, message = "Purchase recorded successfully.", redirectUrl = Url.Action("Index") });
                
                return Json(new { success = false, message = "Failed to process purchase." });
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
            var results = await _medicineRepo.FindAsync(x => x.Name.ToLower().Contains(term.ToLower()) && x.IsActive && !x.IsDeleted);
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

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return Json(new { success = false, message = "Invalid purchase ID." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
            var (success, message) = await _purchaseService.DeletePurchaseAsync(id, userId);

            return Json(new { success = success, message = message });
        }

        [HttpGet]
        public IActionResult Return()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoiceForReturn(string invoiceNo)
        {
            if (string.IsNullOrEmpty(invoiceNo))
                return Json(new { success = false, message = "Invoice number required" });

            var purchaseObj = await _purchaseService.GetPurchaseByInvoiceAsync(invoiceNo);
            if (purchaseObj == null)
                return Json(new { success = false, message = "Purchase invoice not found" });

            if (purchaseObj.Status == "Returned")
                return Json(new { success = false, message = "Invoice is already fully returned" });

            var details = await _purchaseService.GetPurchaseDetailsAsync(purchaseObj.Id!);
            var medicineIds = details.Select(d => d.MedicineId).Distinct();
            var medicines = (await _medicineRepo.FindAsync(m => medicineIds.Contains(m.Id))).ToDictionary(m => m.Id!, m => m.Name);

            var resultDetails = details.Select(d => new {
                PurchaseDetailId = d.Id,
                MedicineId = d.MedicineId,
                BatchNo = d.BatchNo,
                MedicineName = medicines.ContainsKey(d.MedicineId) ? medicines[d.MedicineId] : "Unknown",
                PurchasedQty = d.Qty,
                ReturnedQty = d.ReturnedQty,
                AvailableToReturn = d.Qty - d.ReturnedQty,
                Rate = d.PurchaseRate,
                GST = d.GST,
                TotalPrice = d.TotalPrice,
                UnitRefund = Math.Round(d.TotalPrice / (d.Qty == 0 ? 1 : d.Qty), 2)
            });

            return Json(new {
                success = true,
                purchaseId = purchaseObj.Id,
                invoiceNo = purchaseObj.InvoiceNo,
                date = purchaseObj.PurchaseDate.ToString("yyyy-MM-dd HH:mm"),
                items = resultDetails
            });
        }

        [HttpPost]
        public async Task<IActionResult> ProcessReturn([FromBody] PurchaseReturnViewModel model)
        {
            if (model == null || !model.Items.Any(x => x.ReturnQty > 0))
                return Json(new { success = false, message = "No items to return." });

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
                await _purchaseService.ProcessPurchaseReturnAsync(model, userId);
                return Json(new { success = true, message = "Purchase return processed successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Import()
        {
            ViewBag.Suppliers = (await _supplierService.FindAsync(x => x.IsActive)).Select(x => new SelectListItem(x.Name, x.Id));
            return View();
        }

        [HttpGet]
        public IActionResult DownloadSampleCsv()
        {
            var csv = "MedicineName,BatchNo,ExpiryDate,Qty,FreeQty,UnitsPerStrip,PurchaseRate,Discount,SaleRate,MRP,GST\n" +
                      "Paracetamol 650mg,BAT123,12/26,10,1,10,15.50,0,18.00,20.00,12\n" +
                      "Ibuprofen 400mg,BAT456,06/27,5,0,10,22.00,5,25.00,28.50,18";
            var bytes = Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", "PurchaseImportSample.csv");
        }

        [HttpPost]
        public async Task<IActionResult> ParseImport(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Please upload a valid CSV file." });

            try
            {
                var parsedRows = new List<CsvPurchaseRow>();
                using (var stream = file.OpenReadStream())
                using (var reader = new StreamReader(stream))
                {
                    var headerLine = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(headerLine))
                        return Json(new { success = false, message = "The CSV file is empty." });

                    var headers = headerLine.Split(',').Select(h => h.Trim().ToLowerInvariant()).ToList();

                    int nameIdx = headers.IndexOf("medicinename");
                    int batchIdx = headers.IndexOf("batchno");
                    int expiryIdx = headers.IndexOf("expirydate");
                    int qtyIdx = headers.IndexOf("qty");
                    int freeQtyIdx = headers.IndexOf("freeqty");
                    int unitsIdx = headers.IndexOf("unitsperstrip");
                    int prateIdx = headers.IndexOf("purchaserate");
                    int discIdx = headers.IndexOf("discount");
                    int srateIdx = headers.IndexOf("salerate");
                    int mrpIdx = headers.IndexOf("mrp");
                    int gstIdx = headers.IndexOf("gst");

                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var parts = line.Split(',').Select(p => p.Trim()).ToArray();
                        if (parts.Length < headers.Count) continue;

                        var row = new CsvPurchaseRow
                        {
                            MedicineName = nameIdx != -1 && nameIdx < parts.Length ? parts[nameIdx] : "",
                            BatchNo = batchIdx != -1 && batchIdx < parts.Length ? parts[batchIdx] : "",
                            ExpiryRaw = expiryIdx != -1 && expiryIdx < parts.Length ? parts[expiryIdx] : "",
                            Qty = qtyIdx != -1 && qtyIdx < parts.Length && int.TryParse(parts[qtyIdx], out var q) ? q : 0,
                            FreeQty = freeQtyIdx != -1 && freeQtyIdx < parts.Length && int.TryParse(parts[freeQtyIdx], out var f) ? f : 0,
                            UnitsPerStrip = unitsIdx != -1 && unitsIdx < parts.Length && int.TryParse(parts[unitsIdx], out var u) ? u : 1,
                            PurchaseRate = prateIdx != -1 && prateIdx < parts.Length && decimal.TryParse(parts[prateIdx], out var pr) ? pr : 0,
                            Discount = discIdx != -1 && discIdx < parts.Length && decimal.TryParse(parts[discIdx], out var disc) ? disc : 0,
                            SaleRate = srateIdx != -1 && srateIdx < parts.Length && decimal.TryParse(parts[srateIdx], out var sr) ? sr : 0,
                            MRP = mrpIdx != -1 && mrpIdx < parts.Length && decimal.TryParse(parts[mrpIdx], out var mrp) ? mrp : 0,
                            GST = gstIdx != -1 && gstIdx < parts.Length && decimal.TryParse(parts[gstIdx], out var gst) ? gst : 0
                        };

                        if (!string.IsNullOrEmpty(row.MedicineName))
                        {
                            parsedRows.Add(row);
                        }
                    }
                }

                // Check which medicines exist in the database
                var distinctNames = parsedRows.Select(x => x.MedicineName.Trim()).Distinct().ToList();

                // Fetch active, non-deleted medicines matching these names (case-insensitive)
                var existingMedicines = await _medicineRepo.FindAsync(x => x.IsActive && !x.IsDeleted);
                var existingDict = existingMedicines
                    .Where(m => distinctNames.Any(n => string.Equals(m.Name, n, StringComparison.OrdinalIgnoreCase)))
                    .ToDictionary(m => m.Name.Trim().ToLowerInvariant(), m => m);

                var itemsDto = new List<ParsedPurchaseItemDto>();
                var missingMedicines = new List<string>();

                foreach (var row in parsedRows)
                {
                    var nameKey = row.MedicineName.Trim().ToLowerInvariant();
                    bool exists = existingDict.TryGetValue(nameKey, out var medicine);

                    var expiryDateObj = ParseExpiryDate(row.ExpiryRaw);

                    var item = new ParsedPurchaseItemDto
                    {
                        MedicineId = exists ? medicine!.Id : null,
                        MedicineName = row.MedicineName,
                        BatchNo = row.BatchNo,
                        ExpiryRaw = row.ExpiryRaw,
                        ExpiryDate = expiryDateObj?.ToString("yyyy-MM-dd"),
                        Qty = row.Qty,
                        FreeQty = row.FreeQty,
                        UnitsPerStrip = exists ? medicine!.UnitsPerStrip : row.UnitsPerStrip,
                        PurchaseRate = row.PurchaseRate,
                        Discount = row.Discount,
                        SaleRate = row.SaleRate,
                        MRP = row.MRP,
                        GST = exists ? medicine!.GST : row.GST,
                        Exists = exists
                    };

                    itemsDto.Add(item);

                    if (!exists && !missingMedicines.Contains(row.MedicineName))
                    {
                        missingMedicines.Add(row.MedicineName);
                    }
                }

                return Json(new { success = true, items = itemsDto, missingMedicines = missingMedicines });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error parsing CSV: " + ex.Message });
            }
        }

        private DateTime? ParseExpiryDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            if (raw.Contains('/'))
            {
                var parts = raw.Split('/');
                if (parts.Length == 2 && int.TryParse(parts[0], out var m) && int.TryParse(parts[1], out var y))
                {
                    if (parts[1].Length == 2) y += 2000;
                    if (m >= 1 && m <= 12)
                    {
                        return new DateTime(y, m, DateTime.DaysInMonth(y, m));
                    }
                }
            }

            if (DateTime.TryParse(raw, out var dt))
            {
                return dt;
            }

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> GetImportMasterData()
        {
            var categories = (await _categoryRepo.FindAsync(x => x.IsActive)).Select(x => new { id = x.Id, name = x.Name }).OrderBy(x => x.name);
            var manufacturers = (await _manufacturerRepo.FindAsync(x => x.IsActive)).Select(x => new { id = x.Id, name = x.Name }).OrderBy(x => x.name);
            var units = (await _unitRepo.FindAsync(x => x.IsActive)).Select(x => new { id = x.Id, name = x.Name }).OrderBy(x => x.name);
            var generics = (await _genericRepo.FindAsync(x => x.IsActive)).Select(x => new { id = x.Id, name = x.Name }).OrderBy(x => x.name);
            var racks = (await _rackRepo.FindAsync(x => x.IsActive)).Select(x => new { id = x.Id, name = x.Name }).OrderBy(x => x.name);

            return Json(new
            {
                categories,
                manufacturers,
                units,
                generics,
                racks
            });
        }

        [HttpPost]
        public async Task<IActionResult> QuickCreateMedicine([FromBody] QuickMedicineCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            try
            {
                var medicine = new Medicine
                {
                    Name = model.Name,
                    GenericId = model.GenericId,
                    ManufacturerId = model.ManufacturerId,
                    CategoryId = model.CategoryId,
                    UnitId = model.UnitId,
                    RackId = model.RackId,
                    GST = model.GST,
                    UnitsPerStrip = model.UnitsPerStrip,
                    IsLooseSale = model.IsLooseSale,
                    LooseUnitName = model.LooseUnitName,
                    LowStockThreshold = 10,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _medicineRepo.CreateAsync(medicine);

                return Json(new { success = true, medicineId = medicine.Id, name = medicine.Name });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class CsvPurchaseRow
        {
            public string MedicineName { get; set; } = "";
            public string BatchNo { get; set; } = "";
            public string ExpiryRaw { get; set; } = "";
            public int Qty { get; set; }
            public int FreeQty { get; set; }
            public int UnitsPerStrip { get; set; } = 1;
            public decimal PurchaseRate { get; set; }
            public decimal Discount { get; set; }
            public decimal SaleRate { get; set; }
            public decimal MRP { get; set; }
            public decimal GST { get; set; }
        }

        public class ParsedPurchaseItemDto
        {
            public string? MedicineId { get; set; }
            public string MedicineName { get; set; } = null!;
            public string BatchNo { get; set; } = null!;
            public string ExpiryRaw { get; set; } = null!;
            public string? ExpiryDate { get; set; }
            public int Qty { get; set; }
            public int FreeQty { get; set; }
            public int UnitsPerStrip { get; set; }
            public decimal PurchaseRate { get; set; }
            public decimal Discount { get; set; }
            public decimal SaleRate { get; set; }
            public decimal MRP { get; set; }
            public decimal GST { get; set; }
            public bool Exists { get; set; }
        }
    }
}
