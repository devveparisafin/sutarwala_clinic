using PharmacyERP.Web.Models.ViewModels.Reports;

namespace PharmacyERP.Web.Interfaces
{
    public interface IReportService
    {
        Task<SalesReportViewModel> GetSalesReportAsync(DateTime startDate, DateTime endDate);
        Task<InventoryReportViewModel> GetInventoryReportAsync(string reportType);
        Task<FinancialReportViewModel> GetFinancialReportAsync(DateTime startDate, DateTime endDate);
    }
}
