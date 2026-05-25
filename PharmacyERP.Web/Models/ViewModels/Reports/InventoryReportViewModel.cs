namespace PharmacyERP.Web.Models.ViewModels.Reports
{
    public class StockItem
    {
        public string MedicineName { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string Manufacturer { get; set; } = null!;
        public string BatchNumber { get; set; } = null!;
        public int CurrentStock { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class InventoryReportViewModel
    {
        public List<StockItem> StockList { get; set; } = new();
        public string ReportType { get; set; } = "All"; // All, LowStock, Expiring
        public decimal TotalInventoryValue => StockList.Sum(x => x.TotalValue);
    }
}
