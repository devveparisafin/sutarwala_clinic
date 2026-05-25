namespace PharmacyERP.Web.Models.ViewModels.Reports
{
    public class DailySalesItem
    {
        public string Date { get; set; } = null!;
        public int InvoiceCount { get; set; }
        public decimal TotalSubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class SalesReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<DailySalesItem> DailySales { get; set; } = new();
        public decimal GrandTotal => DailySales.Sum(x => x.TotalAmount);
    }
}
