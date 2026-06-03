using MongoDB.Bson;
using MongoDB.Driver;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Models.ViewModels.Reports;

namespace PharmacyERP.Web.Services
{
    public class ReportService : IReportService
    {
        private readonly IMongoCollection<Sale> _salesCollection;
        private readonly IMongoCollection<PurchaseMaster> _purchaseCollection;
        private readonly IMongoCollection<MedicineBatch> _batchCollection;
        private readonly IMongoCollection<Medicine> _medicineCollection;
        private readonly IMongoCollection<PurchaseDetail> _detailRepo;

        public ReportService(IMongoDbContext context, IBaseRepository<PurchaseDetail> detailRepo)
        {
            _salesCollection = context.GetCollection<Sale>("sales");
            _purchaseCollection = context.GetCollection<PurchaseMaster>("purchasemasters");
            _batchCollection = context.GetCollection<MedicineBatch>("medicinebatchs");
            _medicineCollection = context.GetCollection<Medicine>("medicines");
            _detailRepo = context.GetCollection<PurchaseDetail>("purchasedetails");
        }

        public async Task<SalesReportViewModel> GetSalesReportAsync(DateTime startDate, DateTime endDate)
        {
            // Normalize dates to UTC
            var startUtc = startDate.ToUniversalTime();
            var endUtc = endDate.ToUniversalTime().AddDays(1).AddTicks(-1);

            // MongoDB Aggregation Pipeline for Daily Sales
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument
                {
                    { "IsDeleted", false },
                    { "SaleDate", new BsonDocument
                        {
                            { "$gte", startUtc },
                            { "$lte", endUtc }
                        }
                    }
                }),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", new BsonDocument
                        {
                            { "$dateToString", new BsonDocument
                                {
                                    { "format", "%Y-%m-%d" },
                                    { "date", "$SaleDate" }
                                }
                            }
                        }
                    },
                    { "InvoiceCount", new BsonDocument("$sum", 1) },
                    { "TotalSubTotal", new BsonDocument("$sum", "$SubTotal") },
                    { "TotalTax", new BsonDocument("$sum", "$TaxAmount") },
                    { "TotalDiscount", new BsonDocument("$sum", "$DiscountAmount") },
                    { "TotalAmount", new BsonDocument("$sum", "$TotalAmount") }
                }),
                new BsonDocument("$sort", new BsonDocument("_id", 1))
            };

            var aggregate = await _salesCollection.AggregateAsync<BsonDocument>(pipeline);
            var results = await aggregate.ToListAsync();

            var report = new SalesReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                DailySales = new List<DailySalesItem>()
            };

            foreach (var doc in results)
            {
                report.DailySales.Add(new DailySalesItem
                {
                    Date = doc["_id"].AsString,
                    InvoiceCount = doc.Contains("InvoiceCount") ? Convert.ToInt32(doc["InvoiceCount"]) : 0,
                    TotalSubTotal = doc.Contains("TotalSubTotal") ? Convert.ToDecimal(doc["TotalSubTotal"]) : 0,
                    TotalTax = doc.Contains("TotalTax") ? Convert.ToDecimal(doc["TotalTax"]) : 0,
                    TotalDiscount = doc.Contains("TotalDiscount") ? Convert.ToDecimal(doc["TotalDiscount"]) : 0,
                    TotalAmount = doc.Contains("TotalAmount") ? Convert.ToDecimal(doc["TotalAmount"]) : 0
                });
            }

            return report;
        }

        public async Task<InventoryReportViewModel> GetInventoryReportAsync(string reportType)
        {
            // 1. Fetch all active stock batches
            var batches = await _batchCollection.Find(b => !b.IsDeleted && b.IsActive && b.CurrentQty > 0).ToListAsync();
            var batchNos = batches.Select(b => b.BatchNo).Distinct().ToList();

            // 2. Fetch corresponding purchase details in bulk (one query!)
            var purchaseDetailsList = await _detailRepo.Find(t => batchNos.Contains(t.BatchNo)).ToListAsync();
            var purchaseDetailsDict = purchaseDetailsList.GroupBy(x => x.BatchNo).ToDictionary(g => g.Key, g => g.FirstOrDefault());

            // 3. Fetch purchase master records in bulk (one query!)
            var masterIds = purchaseDetailsList.Select(pd => pd.PurchaseMasterId).Distinct().ToList();
            var purchaseMastersList = await _purchaseCollection.Find(t => masterIds.Contains(t.Id)).ToListAsync();
            var purchaseMastersDict = purchaseMastersList.ToDictionary(pm => pm.Id!);

            // 4. Fetch medicines in bulk and map them to dictionary for O(1) lookup
            var medicines = await _medicineCollection.Find(m => !m.IsDeleted).ToListAsync();
            var medicineDict = medicines.ToDictionary(m => m.Id!);

            var stockItems = new List<StockItem>();

            foreach (var batch in batches)
            {
                if (!purchaseDetailsDict.TryGetValue(batch.BatchNo, out var purchaseDetails) || purchaseDetails == null)
                    continue;

                purchaseMastersDict.TryGetValue(purchaseDetails.PurchaseMasterId, out var purchaseData);
                if (purchaseData == null)
                    continue;

                if (!medicineDict.TryGetValue(batch.MedicineId, out var med) || med == null)
                    continue;

                var purchaseRate = purchaseDetails.PurchaseRate / purchaseDetails.UnitsPerStrip;
                var gstAmount = (purchaseRate * purchaseDetails.GST) / 100;
                var unitPrice = (purchaseRate + gstAmount) - purchaseDetails.DiscountAmount;

                stockItems.Add(new StockItem
                {
                    MedicineName = med.Name,
                    Category = med.CategoryId, 
                    Manufacturer = med.ManufacturerId, 
                    BatchNumber = batch.BatchNo,
                    CurrentStock = batch.CurrentQty,
                    ExpiryDate = batch.ExpiryDate,
                    UnitPrice = unitPrice,
                    TotalValue = (batch.CurrentQty * unitPrice) - (purchaseData.DiscountAmount + purchaseData.OtherDiscount)
                });
            }

            // Apply Filters based on Report Type
            if (reportType == "LowStock")
            {
                stockItems = stockItems.Where(x => x.CurrentStock < 20).ToList();
            }
            else if (reportType == "Expiring")
            {
                // Expiring in next 90 days
                var threshold = DateTime.UtcNow.AddDays(90);
                stockItems = stockItems.Where(x => x.ExpiryDate <= threshold).ToList();
            }

            return new InventoryReportViewModel
            {
                ReportType = reportType,
                StockList = stockItems.OrderBy(x => x.MedicineName).ToList()
            };
        }

        public async Task<FinancialReportViewModel> GetFinancialReportAsync(DateTime startDate, DateTime endDate)
        {
            var startUtc = startDate.ToUniversalTime();
            var endUtc = endDate.ToUniversalTime().AddDays(1).AddTicks(-1);

            var salesMatch = Builders<Sale>.Filter.And(
                Builders<Sale>.Filter.Eq(x => x.IsDeleted, false),
                Builders<Sale>.Filter.Gte(x => x.SaleDate, startUtc),
                Builders<Sale>.Filter.Lte(x => x.SaleDate, endUtc)
            );

            var purchasesMatch = Builders<PurchaseMaster>.Filter.And(
                Builders<PurchaseMaster>.Filter.Eq(x => x.IsDeleted, false),
                Builders<PurchaseMaster>.Filter.Gte(x => x.PurchaseDate, startUtc),
                Builders<PurchaseMaster>.Filter.Lte(x => x.PurchaseDate, endUtc)
            );

            // Fetch numeric values in parallel using minimal projections to reduce network payload
            var salesTask = _salesCollection.Find(salesMatch)
                .Project(x => new Sale { TotalAmount = x.TotalAmount, TaxAmount = x.TaxAmount })
                .ToListAsync();

            var purchasesTask = _purchaseCollection.Find(purchasesMatch)
                .Project(x => new PurchaseMaster { TotalAmount = x.TotalAmount, TaxAmount = x.TaxAmount })
                .ToListAsync();

            await Task.WhenAll(salesTask, purchasesTask);

            var sales = salesTask.Result;
            var purchases = purchasesTask.Result;

            var summary = new FinancialSummary
            {
                TotalSales = sales.Sum(x => x.TotalAmount),
                TotalPurchases = purchases.Sum(x => x.TotalAmount),
                OutputTax = sales.Sum(x => x.TaxAmount),
                InputTax = purchases.Sum(x => x.TaxAmount)
            };

            return new FinancialReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                Summary = summary
            };
        }
    }
}
