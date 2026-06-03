using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyERP.Web.Common;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PharmacyERP.Web.Controllers
{
    [Authorize]
    public class DoctorPrescriptionController : Controller
    {
        private readonly IBaseRepository<DoctorPrescription> _prescriptionRepo;
        private readonly IBaseRepository<Medicine> _medicineRepo;
        private readonly IBaseRepository<Rack> _rackRepo;
        private readonly IStockService _stockService;
        private readonly ISettingsService _settingsService;

        public DoctorPrescriptionController(
            IBaseRepository<DoctorPrescription> prescriptionRepo,
            IBaseRepository<Medicine> medicineRepo,
            IBaseRepository<Rack> rackRepo,
            IStockService stockService,
            ISettingsService settingsService)
        {
            _prescriptionRepo = prescriptionRepo;
            _medicineRepo = medicineRepo;
            _rackRepo = rackRepo;
            _stockService = stockService;
            _settingsService = settingsService;
        }

        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Index()
        {
            var doctorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var prescriptions = await _prescriptionRepo.FindAsync(x => x.DoctorId == doctorId && !x.IsDeleted);
            // Sort by CreatedAt descending
            var sorted = prescriptions.OrderByDescending(x => x.CreatedAt).ToList();
            return View(sorted);
        }

        [HttpGet]
        [Authorize(Roles = "Doctor")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Create([FromBody] DoctorPrescription model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.PatientName))
            {
                return Json(new { success = false, message = "Invalid prescription data. Patient name is required." });
            }

            if (model.Items == null || !model.Items.Any())
            {
                return Json(new { success = false, message = "Prescription must contain at least one medicine." });
            }

            try
            {
                var doctorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
                var doctorName = User.FindFirstValue("FullName") ?? User.Identity?.Name ?? "Doctor";

                model.DoctorId = doctorId;
                model.DoctorName = doctorName;
                model.Status = "Pending";
                model.CreatedAt = DateTime.UtcNow;

                await _prescriptionRepo.CreateAsync(model);

                return Json(new { success = true, message = "Prescription submitted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Cancel(string id)
        {
            var prescription = await _prescriptionRepo.GetByIdAsync(id);
            if (prescription == null) return NotFound();

            var doctorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (prescription.DoctorId != doctorId)
            {
                return Forbid();
            }

            if (prescription.Status != "Pending")
            {
                return Json(new { success = false, message = "Only pending prescriptions can be cancelled." });
            }

            prescription.Status = "Cancelled";
            await _prescriptionRepo.UpdateAsync(id, prescription);

            return Json(new { success = true, message = "Prescription cancelled successfully." });
        }

        [Authorize(Roles = "Admin,Pharmacist,Cashier")]
        public async Task<IActionResult> Queue()
        {
            var allPrescriptions = await _prescriptionRepo.FindAsync(x => x.Status == "Pending" && !x.IsDeleted);
            var sorted = allPrescriptions.OrderByDescending(x => x.CreatedAt).ToList();
            return View(sorted);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Pharmacist,Cashier")]
        public async Task<IActionResult> GetPrescriptionDetails(string id)
        {
            var prescription = await _prescriptionRepo.GetByIdAsync(id);
            if (prescription == null) return NotFound();

            var settings = await _settingsService.GetSettingsAsync();
            var defaultGst = settings?.DefaultGstPercentage ?? 18m;
            var racks = await _rackRepo.GetAllAsync();

            var results = new List<object>();

            foreach (var item in prescription.Items)
            {
                var medicine = await _medicineRepo.GetByIdAsync(item.MedicineId);
                if (medicine == null || !medicine.IsActive || medicine.IsDeleted)
                {
                    continue; // Skip inactive/deleted medicines
                }

                var stock = await _stockService.GetCurrentStockAsync(medicine.Id!);
                var batches = await _stockService.GetBatchesForSaleAsync(medicine.Id!, 1);
                var latestBatch = batches.FirstOrDefault();
                var rackName = racks.FirstOrDefault(r => r.Id == medicine.RackId)?.Name ?? "N/A";

                results.Add(new
                {
                    id = medicine.Id,
                    name = medicine.Name,
                    price = latestBatch?.SaleRate ?? 0,
                    gst = defaultGst,
                    stock = stock,
                    rack = rackName,
                    unitsPerStrip = medicine.UnitsPerStrip,
                    isLooseSale = medicine.IsLooseSale,
                    prescribedQty = item.Qty,
                    instructions = item.Instructions
                });
            }

            return Json(new
            {
                success = true,
                prescriptionId = prescription.Id,
                patientName = prescription.PatientName,
                patientPhone = prescription.PatientPhone,
                doctorName = prescription.DoctorName,
                remarks = prescription.Remarks,
                items = results
            });
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Pharmacist,Cashier")]
        public async Task<IActionResult> GetPendingCount()
        {
            var count = await _prescriptionRepo.CountAsync(x => x.Status == "Pending" && !x.IsDeleted);
            return Json(new { count });
        }
    }
}
