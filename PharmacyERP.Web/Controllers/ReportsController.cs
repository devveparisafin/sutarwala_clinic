using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmacyERP.Web.Interfaces;
using System;

namespace PharmacyERP.Web.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Sales(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today.AddDays(-30);
            var end = endDate ?? DateTime.Today;

            var report = await _reportService.GetSalesReportAsync(start, end);
            return View(report);
        }

        public async Task<IActionResult> Inventory(string type = "All")
        {
            var report = await _reportService.GetInventoryReportAsync(type);
            return View(report);
        }

        public async Task<IActionResult> Financial(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var end = endDate ?? DateTime.Today;

            var report = await _reportService.GetFinancialReportAsync(start, end);
            return View(report);
        }
    }
}
