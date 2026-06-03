using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Services;

namespace PharmacyERP.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly IMongoDbContext _mongoContext;

        public DashboardController(IDashboardService dashboardService, IMongoDbContext mongoContext)
        {
            _dashboardService = dashboardService;
            _mongoContext = mongoContext;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Doctor"))
            {
                return RedirectToAction("Index", "DoctorPrescription");
            }
            var data = await _dashboardService.GetDashboardDataAsync();
            return View(data);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ResetData()
        {
            var collectionsToDrop = new[] {
                "suppliers",
                "supplierpayments",
                "purchasemasters",
                "purchasedetails",
                "sales",
                "saledetails",
                "payments",
                "medicinebatchs",
                "stocktransactions",
                "customers",
                "customerpayments",

            };

            foreach (var coll in collectionsToDrop)
            {
                await _mongoContext.Database.DropCollectionAsync(coll);
            }

            return Content("Success: Data removed for suppliers, purchases, sales, and inventory.");
        }
    }
}
