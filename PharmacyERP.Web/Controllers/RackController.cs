using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.ViewModels.Masters;

namespace PharmacyERP.Web.Controllers
{
    [Authorize]
    public class RackController : Controller
    {
        private readonly IRackService _service;

        public RackController(IRackService service)
        {
            _service = service;
        }

        public IActionResult Index() => View();

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
        public async Task<IActionResult> Upsert(RackViewModel model)
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

        [HttpPost]
        public async Task<IActionResult> QuickAdd([FromBody] QuickAddRackViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data entered." });

            var (success, message, id) = await _service.QuickAddAsync(model);
            return Json(new { success = success, message = message, id = id, text = model.Name.Trim() });
        }
    }
}
