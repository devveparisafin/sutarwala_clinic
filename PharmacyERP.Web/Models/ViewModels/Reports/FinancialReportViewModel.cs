namespace PharmacyERP.Web.Models.ViewModels.Reports
{
    public class FinancialSummary
    {
        public decimal TotalSales { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal GrossProfit => TotalSales - TotalPurchases;
        
        public decimal OutputTax { get; set; } // Tax collected from sales
        public decimal InputTax { get; set; } // Tax paid on purchases
        public decimal NetGstPayable => OutputTax - InputTax;
    }

    public class FinancialReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public FinancialSummary Summary { get; set; } = new();
    }
}
