using MongoDB.Bson;
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

            // Parallel executions for top widget stats using projections to save bandwidth
            var todaySalesTask = _saleRepository.Collection
                .Find(x => x.SaleDate >= today && !x.IsDeleted)
                .Project(x => new Sale { TotalAmount = x.TotalAmount })
                .ToListAsync();

            var todayPurchasesTask = _purchaseRepository.Collection
                .Find(x => x.PurchaseDate >= today && !x.IsDeleted)
                .Project(x => new PurchaseMaster { TotalAmount = x.TotalAmount })
                .ToListAsync();

            var monthlySalesTask = _saleRepository.Collection
                .Find(x => x.SaleDate >= startOfMonth && !x.IsDeleted)
                .Project(x => new Sale { TotalAmount = x.TotalAmount })
                .ToListAsync();

            // Direct database-side sorting and limiting for 5 recent invoices
            var recentInvoicesTask = _saleRepository.Collection
                .Find(x => !x.IsDeleted)
                .SortByDescending(x => x.SaleDate)
                .Limit(5)
                .ToListAsync();

            var todaysRemindersTask = _customerRepo.GetTodaysRemindersAsync(today);

            // 1. Server-side low-stock calculation via Aggregation pipeline (crush OOM risks)
            var stockAlertsPipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("IsDeleted", false)),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "medicinebatchs" },
                    { "let", new BsonDocument("medId", "$_id") },
                    { "pipeline", new BsonArray
                        {
                            new BsonDocument("$match", new BsonDocument
                            {
                                { "$expr", new BsonDocument("$and", new BsonArray
                                    {
                                        new BsonDocument("$eq", new BsonArray { "$MedicineId", "$$medId" }),
                                        new BsonDocument("$eq", new BsonArray { "$IsActive", true }),
                                        new BsonDocument("$eq", new BsonArray { "$IsDeleted", false })
                                    })
                                }
                            })
                        }
                    },
                    { "as", "batches" }
                }),
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 }, // Suppress _id to prevent C# MongoDB deserialization error
                    { "Name", "$Name" },
                    { "Threshold", "$LowStockThreshold" },
                    { "CurrentStock", new BsonDocument("$sum", "$batches.CurrentQty") }
                }),
                new BsonDocument("$match", new BsonDocument("$expr", new BsonDocument("$lte", new BsonArray { "$CurrentStock", "$Threshold" }))),
                new BsonDocument("$sort", new BsonDocument("CurrentStock", 1))
            };

            var lowStockTask = _medicineRepository.Collection
                .Aggregate<StockAlertViewModel>(stockAlertsPipeline)
                .ToListAsync();

            // 2. Server-side expiry calculation via Aggregation pipeline
            var expiryPipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument
                {
                    { "IsActive", true },
                    { "IsDeleted", false },
                    { "CurrentQty", new BsonDocument("$gt", 0) },
                    { "ExpiryDate", new BsonDocument("$lte", DateTime.UtcNow.AddDays(30)) }
                }),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "medicines" },
                    { "localField", "MedicineId" },
                    { "foreignField", "_id" },
                    { "as", "medicine" }
                }),
                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$medicine" },
                    { "preserveNullAndEmptyArrays", true }
                }),
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 }, // Suppress _id to prevent C# MongoDB deserialization error
                    { "MedicineName", new BsonDocument("$ifNull", new BsonArray { "$medicine.Name", "Unknown" }) },
                    { "BatchNo", "$BatchNo" },
                    { "ExpiryDate", "$ExpiryDate" }
                }),
                new BsonDocument("$sort", new BsonDocument("ExpiryDate", 1))
            };

            var expiryTask = _batchRepo.Collection
                .Aggregate<BatchExpiryViewModel>(expiryPipeline)
                .ToListAsync();

            // 3. Parallel monthly sales and monthly purchases trends queries
            var monthLabels = new List<string>();
            var salesTrendTasks = new List<Task<List<Sale>>>();
            var purchaseTrendTasks = new List<Task<List<PurchaseMaster>>>();

            for (int i = 5; i >= 0; i--)
            {
                var monthDate = DateTime.UtcNow.AddMonths(-i);
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

                monthLabels.Add(monthDate.ToString("MMM"));

                salesTrendTasks.Add(_saleRepository.Collection
                    .Find(x => x.SaleDate >= monthStart && x.SaleDate <= monthEnd && !x.IsDeleted)
                    .Project(x => new Sale { TotalAmount = x.TotalAmount })
                    .ToListAsync());

                purchaseTrendTasks.Add(_purchaseRepository.Collection
                    .Find(x => x.PurchaseDate >= monthStart && x.PurchaseDate <= monthEnd && !x.IsDeleted)
                    .Project(x => new PurchaseMaster { TotalAmount = x.TotalAmount })
                    .ToListAsync());
            }

            // Fire all concurrent async operations in parallel
            var allTasks = new List<Task>
            {
                todaySalesTask,
                todayPurchasesTask,
                monthlySalesTask,
                recentInvoicesTask,
                todaysRemindersTask,
                lowStockTask,
                expiryTask
            };
            allTasks.AddRange(salesTrendTasks);
            allTasks.AddRange(purchaseTrendTasks);

            await Task.WhenAll(allTasks);

            // Compute trends from parallel task results
            var salesTrend = salesTrendTasks.Select(t => t.Result.Sum(x => x.TotalAmount)).ToList();
            var purchaseTrend = purchaseTrendTasks.Select(t => t.Result.Sum(x => x.TotalAmount)).ToList();

            var lowStockList = lowStockTask.Result;
            var expiryList = expiryTask.Result;

            var model = new DashboardViewModel
            {
                TotalSalesToday = todaySalesTask.Result.Sum(x => x.TotalAmount),
                TotalPurchaseToday = todayPurchasesTask.Result.Sum(x => x.TotalAmount),
                MonthlySales = monthlySalesTask.Result.Sum(x => x.TotalAmount),
                LowStockCount = lowStockList.Count,
                ExpiringSoonCount = expiryList.Count,
                LowStockMedicines = lowStockList.Take(5).ToList(),
                ExpiringSoonMedicines = expiryList.Take(5).ToList(),
                RecentInvoices = recentInvoicesTask.Result,

                MonthLabels = monthLabels,
                SalesData = salesTrend,
                PurchaseData = purchaseTrend,

                TodaysReminders = todaysRemindersTask.Result.Select(c => new CustomerReminderViewModel
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
