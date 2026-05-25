using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;

namespace PharmacyERP.Web.Controllers
{
    [Authorize]
    public class InventorySettingsController : Controller
    {
        private readonly IBaseRepository<MedicineCategory> _categoryRepo;
        private readonly IBaseRepository<Manufacturer> _manufacturerRepo;
        private readonly IBaseRepository<MedicineUnit> _unitRepo;
        private readonly IBaseRepository<GenericMedicine> _genericRepo;

        public InventorySettingsController(
            IBaseRepository<MedicineCategory> categoryRepo,
            IBaseRepository<Manufacturer> manufacturerRepo,
            IBaseRepository<MedicineUnit> unitRepo,
            IBaseRepository<GenericMedicine> genericRepo)
        {
            _categoryRepo = categoryRepo;
            _manufacturerRepo = manufacturerRepo;
            _unitRepo = unitRepo;
            _genericRepo = genericRepo;
        }

        public IActionResult Index() => View();

        #region Categories
        [HttpGet]
        public async Task<IActionResult> GetCategories() => Json(await _categoryRepo.GetAllAsync());

        [HttpPost]
        public async Task<IActionResult> AddCategory(string name)
        {
            await _categoryRepo.CreateAsync(new MedicineCategory { Name = name });
            return Json(new { success = true });
        }
        #endregion

        #region Manufacturers
        [HttpGet]
        public async Task<IActionResult> GetManufacturers() => Json(await _manufacturerRepo.GetAllAsync());

        [HttpPost]
        public async Task<IActionResult> AddManufacturer(string name)
        {
            await _manufacturerRepo.CreateAsync(new Manufacturer { Name = name });
            return Json(new { success = true });
        }
        #endregion

        #region Units
        [HttpGet]
        public async Task<IActionResult> GetUnits() => Json(await _unitRepo.GetAllAsync());

        [HttpPost]
        public async Task<IActionResult> AddUnit(string name)
        {
            await _unitRepo.CreateAsync(new MedicineUnit { Name = name });
            return Json(new { success = true });
        }
        #endregion

        #region Generics
        [HttpGet]
        public async Task<IActionResult> GetGenerics() => Json(await _genericRepo.GetAllAsync());

        [HttpPost]
        public async Task<IActionResult> AddGeneric(string name)
        {
            await _genericRepo.CreateAsync(new GenericMedicine { Name = name });
            return Json(new { success = true });
        }
        #endregion
    }
}
