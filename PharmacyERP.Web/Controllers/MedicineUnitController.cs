using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.ViewModels.Masters;

namespace PharmacyERP.Web.Controllers
{
    [Authorize]
    public class MedicineUnitController : Controller
    {
        private readonly IMedicineUnitService _service;

        public MedicineUnitController(IMedicineUnitService service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Json(new { data });
        }

        [HttpGet]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null) return NotFound();
            return Json(data);
        }

        [HttpPost]
        public async Task<IActionResult> Upsert(MedicineUnitViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool success;
                if (string.IsNullOrEmpty(model.Id))
                    success = await _service.CreateAsync(model);
                else
                    success = await _service.UpdateAsync(model);

                if (success) return Json(new { success = true, message = "Saved successfully" });
            }
            return Json(new { success = false, message = "Error while saving" });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(string id)
        {
            var success = await _service.DeleteAsync(id);
            if (success) return Json(new { success = true, message = "Deleted successfully" });
            return Json(new { success = false, message = "Error while deleting" });
        }
    }
}
