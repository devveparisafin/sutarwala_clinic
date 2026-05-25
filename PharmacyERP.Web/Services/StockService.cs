using PharmacyERP.Web.Common;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;

namespace PharmacyERP.Web.Services
{
    public interface IStockService
    {
        Task RecordTransactionAsync(StockTransaction transaction);
        Task<int> GetCurrentStockAsync(string medicineId);
        Task<List<MedicineBatch>> GetBatchesForSaleAsync(string medicineId, int requestedQty);
        Task AdjustStockAsync(InventoryAdjustment adjustment);
        Task<IEnumerable<dynamic>> GetStockAggregationAsync();
        Task<IEnumerable<MedicineBatch>> GetBatchesByIdsAsync(IEnumerable<string> batchIds);
        Task<IEnumerable<StockTransaction>> GetTransactionsByReferenceAsync(string referenceId);
        Task<List<DeductionResult>> DeductStockAsync(string medicineId, int totalUnits, string referenceId, string remarks, string userId, TransactionType type = TransactionType.Sale);
    }

    public class DeductionResult
    {
        public string BatchId { get; set; } = null!;
        public int UnitsDeducted { get; set; }
    }

    public class StockService : IStockService
    {
        private readonly IBaseRepository<StockTransaction> _transactionRepo;
        private readonly IBaseRepository<MedicineBatch> _batchRepo;

        public StockService(
            IBaseRepository<StockTransaction> transactionRepo,
            IBaseRepository<MedicineBatch> batchRepo)
        {
            _transactionRepo = transactionRepo;
            _batchRepo = batchRepo;
        }

        public async Task RecordTransactionAsync(StockTransaction transaction)
        {
            await _transactionRepo.CreateAsync(transaction);
            
            // Update Batch Quantity
            var batch = await _batchRepo.GetByIdAsync(transaction.BatchId);
            if (batch != null)
            {
                batch.CurrentQty += transaction.Quantity;
                await _batchRepo.UpdateAsync(batch.Id!, batch);
            }
        }

        public async Task<int> GetCurrentStockAsync(string medicineId)
        {
            var batches = await _batchRepo.FindAsync(x => x.MedicineId == medicineId && x.IsActive && !x.IsDeleted);
            return batches.Sum(x => x.CurrentQty);
        }

        public async Task<List<MedicineBatch>> GetBatchesForSaleAsync(string medicineId, int requestedQty)
        {
            // FIFO: Sort by Expiry Date (earliest first)
            var availableBatches = (await _batchRepo.FindAsync(x => 
                x.MedicineId == medicineId && 
                x.CurrentQty > 0 && 
                x.IsActive && 
                !x.IsDeleted &&
                x.ExpiryDate > DateTime.UtcNow))
                .OrderBy(x => x.ExpiryDate)
                .ToList();

            var selectedBatches = new List<MedicineBatch>();
            int remainingToPick = requestedQty;

            foreach (var batch in availableBatches)
            {
                if (remainingToPick <= 0) break;

                int pickFromThisBatch = Math.Min(batch.CurrentQty, remainingToPick);
                batch.CurrentQty -= pickFromThisBatch; // Local update for the returned object
                selectedBatches.Add(batch);
                remainingToPick -= pickFromThisBatch;
            }

            if (remainingToPick > 0)
            {
                throw new Exception("Insufficient stock available for the requested quantity.");
            }

            return selectedBatches;
        }

        public async Task<List<DeductionResult>> DeductStockAsync(string medicineId, int totalUnits, string referenceId, string remarks, string userId, TransactionType type = TransactionType.Sale)
        {
            // 1. Fetch available batches sorted by expiry (FIFO)
            var availableBatches = (await _batchRepo.FindAsync(x => 
                x.MedicineId == medicineId && 
                x.CurrentQty > 0 && 
                x.IsActive && 
                !x.IsDeleted &&
                x.ExpiryDate > DateTime.UtcNow))
                .OrderBy(x => x.ExpiryDate)
                .ToList();

            int remainingToDeduct = totalUnits;
            var results = new List<DeductionResult>();

            foreach (var batch in availableBatches)
            {
                if (remainingToDeduct <= 0) break;

                int qtyFromThisBatch = Math.Min(remainingToDeduct, batch.CurrentQty);
                if (qtyFromThisBatch <= 0) continue;

                // 2. Record Transaction (Negative quantity for deduction)
                var transaction = new StockTransaction
                {
                    MedicineId = medicineId,
                    BatchId = batch.Id!,
                    Type = type,
                    Quantity = -qtyFromThisBatch,
                    ReferenceId = referenceId,
                    Remarks = remarks,
                    UserId = userId,
                    TransactionDate = DateTime.UtcNow
                };

                await RecordTransactionAsync(transaction);
                
                results.Add(new DeductionResult { BatchId = batch.Id!, UnitsDeducted = qtyFromThisBatch });
                remainingToDeduct -= qtyFromThisBatch;
            }

            if (remainingToDeduct > 0)
            {
                throw new Exception($"Insufficient stock for medicine ID {medicineId}. Missing {remainingToDeduct} units.");
            }

            return results;
        }

        public async Task<IEnumerable<dynamic>> GetStockAggregationAsync()
        {
            var batches = await _batchRepo.FindAsync(x => x.IsActive && !x.IsDeleted && x.CurrentQty > 0);
            
            return batches
                .GroupBy(x => x.MedicineId)
                .Select(g => new
                {
                    MedicineId = g.Key,
                    TotalStock = g.Sum(x => x.CurrentQty),
                    BatchCount = g.Count(),
                    EarliestExpiry = g.Min(x => x.ExpiryDate)
                });
        }

        public async Task AdjustStockAsync(InventoryAdjustment adjustment)
        {
            var transaction = new StockTransaction
            {
                MedicineId = adjustment.MedicineId,
                BatchId = adjustment.BatchId,
                Type = TransactionType.Adjustment,
                Quantity = adjustment.AdjustedQty,
                Remarks = adjustment.Reason,
                TransactionDate = DateTime.UtcNow
            };

            await RecordTransactionAsync(transaction);
        }

        public async Task<IEnumerable<MedicineBatch>> GetBatchesByIdsAsync(IEnumerable<string> batchIds)
        {
            return await _batchRepo.FindAsync(x => batchIds.Contains(x.Id));
        }

        public async Task<IEnumerable<StockTransaction>> GetTransactionsByReferenceAsync(string referenceId)
        {
            return await _transactionRepo.FindAsync(x => x.ReferenceId == referenceId);
        }
    }
}
