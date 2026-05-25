using PharmacyERP.Web.Models.Entities;

namespace PharmacyERP.Web.Models.ViewModels
{
    public class DashboardViewModel
    {
        public decimal TotalSalesToday { get; set; }
        public decimal TotalPurchaseToday { get; set; }
        public decimal MonthlySales { get; set; }
        public int LowStockCount { get; set; }
        public int ExpiringSoonCount { get; set; }
        
        public List<StockAlertViewModel> LowStockMedicines { get; set; } = new();
        public List<BatchExpiryViewModel> ExpiringSoonMedicines { get; set; } = new();
        public List<Sale> RecentInvoices { get; set; } = new();
        
        // Chart Data
        public List<decimal> SalesData { get; set; } = new();
        public List<decimal> PurchaseData { get; set; } = new();
        public List<string> MonthLabels { get; set; } = new();
        
        public List<CustomerReminderViewModel> TodaysReminders { get; set; } = new();
    }

    public class CustomerReminderViewModel
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string MobileNumber { get; set; } = null!;
        public DateTime ReminderDate { get; set; }
        public string? ReminderFrequency { get; set; }
        public string? ReminderNote { get; set; }
    }

    public class StockAlertViewModel
    {
        public string Name { get; set; } = null!;
        public int CurrentStock { get; set; }
        public int Threshold { get; set; }
    }

    public class BatchExpiryViewModel
    {
        public string MedicineName { get; set; } = null!;
        public string BatchNo { get; set; } = null!;
        public DateTime ExpiryDate { get; set; }
    }
}
