using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Models.ViewModels;
using PharmacyERP.Web.Services;

namespace PharmacyERP.Web.Controllers
{
    [Authorize]
    public class MedicinesController : Controller
    {
        private readonly IMedicineService _medicineService;
        private readonly IBaseRepository<MedicineCategory> _categoryRepo;
        private readonly IBaseRepository<Manufacturer> _manufacturerRepo;
        private readonly IBaseRepository<MedicineUnit> _unitRepo;
        private readonly IBaseRepository<GenericMedicine> _genericRepo;
        private readonly IBaseRepository<Rack> _rackRepo;

        public MedicinesController(
            IMedicineService medicineService,
            IBaseRepository<MedicineCategory> categoryRepo,
            IBaseRepository<Manufacturer> manufacturerRepo,
            IBaseRepository<MedicineUnit> unitRepo,
            IBaseRepository<GenericMedicine> genericRepo,
            IBaseRepository<Rack> rackRepo)
        {
            _medicineService = medicineService;
            _categoryRepo = categoryRepo;
            _manufacturerRepo = manufacturerRepo;
            _unitRepo = unitRepo;
            _genericRepo = genericRepo;
            _rackRepo = rackRepo;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _medicineService.GetMedicineListAsync();
            return View(list);
        }

        private async Task PopulateMasterDropdownsAsync(MedicineViewModel model)
        {
            var categoriesTask = _categoryRepo.FindAsync(x => x.IsActive);
            var manufacturersTask = _manufacturerRepo.FindAsync(x => x.IsActive);
            var unitsTask = _unitRepo.FindAsync(x => x.IsActive);
            var genericsTask = _genericRepo.FindAsync(x => x.IsActive);
            var racksTask = _rackRepo.FindAsync(x => x.IsActive);

            await Task.WhenAll(categoriesTask, manufacturersTask, unitsTask, genericsTask, racksTask);

            model.Categories = categoriesTask.Result.Select(x => new SelectListItem(x.Name, x.Id));
            model.Manufacturers = manufacturersTask.Result.Select(x => new SelectListItem(x.Name, x.Id));
            model.Units = unitsTask.Result.Select(x => new SelectListItem(x.Name, x.Id));
            model.Generics = genericsTask.Result.Select(x => new SelectListItem(x.Name, x.Id));
            model.Racks = racksTask.Result.Select(x => new SelectListItem(x.Name, x.Id));
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new MedicineViewModel();
            await PopulateMasterDropdownsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MedicineViewModel model)
        {
            if (ModelState.IsValid)
            {
                await _medicineService.CreateMedicineAsync(model);
                TempData["SuccessMessage"] = "Medicine added successfully.";
                return RedirectToAction(nameof(Create));
            }
            
            await PopulateMasterDropdownsAsync(model);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var model = await _medicineService.GetMedicineForEditAsync(id);
            if (model == null) return NotFound();

            await PopulateMasterDropdownsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MedicineViewModel model)
        {
            if (ModelState.IsValid)
            {
                await _medicineService.UpdateMedicineAsync(model);
                TempData["SuccessMessage"] = "Medicine updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            
            await PopulateMasterDropdownsAsync(model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _medicineService.DeleteAsync(id);
            return Json(new { success = true, message = "Medicine deleted successfully." });
        }
    }
}
