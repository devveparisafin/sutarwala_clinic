using MongoDB.Driver;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Models.ViewModels;

namespace PharmacyERP.Web.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync();
    }

    public class DashboardService : IDashboardService
    {
        private readonly IBaseRepository<Sale> _saleRepository;
        private readonly IBaseRepository<Purchase> _purchaseRepository;
        private readonly IBaseRepository<Medicine> _medicineRepository;
        private readonly IBaseRepository<MedicineBatch> _batchRepo;
        private readonly ICustomerRepository _customerRepo;

        public DashboardService(
            IBaseRepository<Sale> saleRepository,
            IBaseRepository<Purchase> purchaseRepository,
            IBaseRepository<Medicine> medicineRepository,
            IBaseRepository<MedicineBatch> batchRepo,
            ICustomerRepository customerRepo)
        {
            _saleRepository = saleRepository;
            _purchaseRepository = purchaseRepository;
            _medicineRepository = medicineRepository;
            _batchRepo = batchRepo;
            _customerRepo = customerRepo;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            var today = DateTime.UtcNow.Date;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            var todaySales = await _saleRepository.FindAsync(x => x.SaleDate >= today && !x.IsDeleted);
            var todayPurchases = await _purchaseRepository.FindAsync(x => x.PurchaseDate >= today && !x.IsDeleted);
            var monthlySales = await _saleRepository.FindAsync(x => x.SaleDate >= startOfMonth && !x.IsDeleted);

            var medicines = await _medicineRepository.GetAllAsync();
            var batches = await _batchRepo.FindAsync(x => x.IsActive && !x.IsDeleted);

            var stockStats = medicines.Select(m => new StockAlertViewModel
            {
                Name = m.Name,
                CurrentStock = batches.Where(b => b.MedicineId == m.Id).Sum(b => b.CurrentQty),
                Threshold = m.LowStockThreshold
            }).ToList();

            var lowStockList = stockStats.Where(x => x.CurrentStock <= x.Threshold).OrderBy(x => x.CurrentStock).ToList();
            
            var expiryList = batches
                .Where(b => b.ExpiryDate <= DateTime.UtcNow.AddDays(30) && b.CurrentQty > 0)
                .Select(b => new BatchExpiryViewModel
                {
                    MedicineName = medicines.FirstOrDefault(m => m.Id == b.MedicineId)?.Name ?? "Unknown",
                    BatchNo = b.BatchNo,
                    ExpiryDate = b.ExpiryDate
                })
                .OrderBy(x => x.ExpiryDate)
                .ToList();

            var recentInvoices = (await _saleRepository.GetAllAsync()).OrderByDescending(x => x.SaleDate).Take(5).ToList();

            var monthLabels = new List<string>();
            var salesTrend = new List<decimal>();
            var purchaseTrend = new List<decimal>();

            for (int i = 5; i >= 0; i--)
            {
                var monthDate = DateTime.UtcNow.AddMonths(-i);
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
                
                monthLabels.Add(monthDate.ToString("MMM"));
                
                var monthSales = await _saleRepository.FindAsync(x => x.SaleDate >= monthStart && x.SaleDate <= monthEnd && !x.IsDeleted);
                var monthPurchases = await _purchaseRepository.FindAsync(x => x.PurchaseDate >= monthStart && x.PurchaseDate <= monthEnd && !x.IsDeleted);
                
                salesTrend.Add(monthSales.Sum(x => x.TotalAmount));
                purchaseTrend.Add(monthPurchases.Sum(x => x.TotalAmount));
            }

            var todaysReminders = await _customerRepo.GetTodaysRemindersAsync(today);

            var model = new DashboardViewModel
            {
                TotalSalesToday = todaySales.Sum(x => x.TotalAmount),
                TotalPurchaseToday = todayPurchases.Sum(x => x.TotalAmount),
                MonthlySales = monthlySales.Sum(x => x.TotalAmount),
                LowStockCount = lowStockList.Count,
                ExpiringSoonCount = expiryList.Count,
                LowStockMedicines = lowStockList.Take(5).ToList(),
                ExpiringSoonMedicines = expiryList.Take(5).ToList(),
                RecentInvoices = recentInvoices,
                
                MonthLabels = monthLabels,
                SalesData = salesTrend,
                PurchaseData = purchaseTrend,
                
                TodaysReminders = todaysReminders.Select(c => new CustomerReminderViewModel
                {
                    Id = c.Id!,
                    Name = c.Name,
                    MobileNumber = c.MobileNumber,
                    ReminderDate = c.ReminderDate!.Value,
                    ReminderFrequency = c.ReminderFrequency,
                    ReminderNote = c.ReminderNote
                }).ToList()
            };

            return model;
        }
    }
}
