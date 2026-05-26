using PharmacyERP.Web.Common;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Models.ViewModels;
using System.Collections.Concurrent;

namespace PharmacyERP.Web.Services
{
    public interface IPurchaseService
    {
        Task<bool> ProcessPurchaseAsync(PurchaseEntryViewModel model, string userId);
        Task<IEnumerable<PurchaseMaster>> GetAllPurchasesAsync();
        Task<PurchaseMaster> GetPurchaseByIdAsync(string id);
        Task<IEnumerable<PurchaseDetail>> GetPurchaseDetailsAsync(string masterId);
        Task<(bool Success, string Message)> DeletePurchaseAsync(string id, string userId);
        Task<PurchaseMaster?> GetPurchaseByInvoiceAsync(string invoiceNo);
        Task<bool> ProcessPurchaseReturnAsync(PurchaseReturnViewModel model, string userId);
    }

    public class PurchaseService : IPurchaseService
    {
        private static readonly ConcurrentDictionary<string, byte> _activePurchaseLocks = new();

        private readonly IBaseRepository<PurchaseMaster> _masterRepo;
        private readonly IBaseRepository<PurchaseDetail> _detailRepo;
        private readonly IBaseRepository<MedicineBatch> _batchRepo;
        private readonly IBaseRepository<Supplier> _supplierRepo;
        private readonly IStockService _stockService;

        public PurchaseService(
            IBaseRepository<PurchaseMaster> masterRepo,
            IBaseRepository<PurchaseDetail> detailRepo,
            IBaseRepository<MedicineBatch> batchRepo,
            IBaseRepository<Supplier> supplierRepo,
            IStockService stockService)
        {
            _masterRepo = masterRepo;
            _detailRepo = detailRepo;
            _batchRepo = batchRepo;
            _supplierRepo = supplierRepo;
            _stockService = stockService;
        }

        public async Task<bool> ProcessPurchaseAsync(PurchaseEntryViewModel model, string userId)
        {
            if (string.IsNullOrEmpty(model.TransactionGuid))
            {
                throw new Exception("Transaction GUID is required.");
            }

            // 0. Double-submission prevention at the Supplier invoice level
            var duplicateInvoice = (await _masterRepo.FindAsync(x => x.SupplierId == model.SupplierId && x.InvoiceNo == model.InvoiceNo && !x.IsDeleted)).FirstOrDefault();
            if (duplicateInvoice != null)
            {
                throw new Exception($"Invoice '{model.InvoiceNo}' has already been recorded for this supplier.");
            }

            if (!_activePurchaseLocks.TryAdd(model.TransactionGuid, 0))
            {
                throw new Exception("This purchase entry is already being processed. Please wait.");
            }

            try
            {
                // Check if this transaction has already been saved in the database
                var existingPurchases = await _masterRepo.FindAsync(x => x.TransactionGuid == model.TransactionGuid);
                if (existingPurchases.Any())
                {
                    throw new Exception("This purchase entry has already been saved.");
                }

                // 1. Create Purchase Master
                var master = new PurchaseMaster
                {
                    PurchaseDate = model.PurchaseDate,
                    SupplierId = model.SupplierId,
                    InvoiceNo = model.InvoiceNo,
                    SubTotal = model.SubTotal,
                    TaxAmount = model.TaxAmount,
                    DiscountAmount = model.DiscountAmount,
                    OtherDiscount = model.OtherDiscount,
                    TotalAmount = model.TotalAmount,
                    PaymentMode = model.PaymentMode,
                    Remarks = model.Remarks,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    TransactionGuid = model.TransactionGuid
                };
                await _masterRepo.CreateAsync(master);

                foreach (var item in model.Items)
                {
                    // 2. Create Purchase Detail
                    var detail = new PurchaseDetail
                    {
                        PurchaseMasterId = master.Id!,
                        MedicineId = item.MedicineId,
                        BatchNo = item.BatchNo,
                        ExpiryDate = item.ExpiryDate,
                        Qty = item.Qty,
                        FreeQty = item.FreeQty,
                        UnitsPerStrip = item.UnitsPerStrip,
                        PurchaseRate = item.PurchaseRate,
                        SaleRate = item.SaleRate,
                        MRP = item.MRP,
                        DiscountType = item.DiscountType,
                        DiscountValue = item.DiscountValue,
                        DiscountAmount = item.DiscountAmount,
                        GST = item.GST,
                        TotalPrice = item.TotalPrice,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _detailRepo.CreateAsync(detail);

                    // Calculate Total Units (including free qty)
                    int totalUnits = (item.Qty + item.FreeQty) * item.UnitsPerStrip;

                    // 3. Create/Update Medicine Batch Metadata (Price, Expiry, etc.)
                    // DO NOT update quantity here, StockService.RecordTransactionAsync will handle it.
                    var existingBatch = (await _batchRepo.FindAsync(x => x.MedicineId == item.MedicineId && x.BatchNo == item.BatchNo)).FirstOrDefault();
                    string batchId;
                    if (existingBatch != null)
                    {
                        existingBatch.PurchaseRate = item.PurchaseRate; // Update to latest strip rate
                        existingBatch.SaleRate = item.SaleRate;
                        existingBatch.MRP = item.MRP;
                        existingBatch.ExpiryDate = item.ExpiryDate;
                        await _batchRepo.UpdateAsync(existingBatch.Id!, existingBatch);
                        batchId = existingBatch.Id!;
                    }
                    else
                    {
                        var newBatch = new MedicineBatch
                        {
                            MedicineId = item.MedicineId,
                            BatchNo = item.BatchNo,
                            ExpiryDate = item.ExpiryDate,
                            PurchaseRate = item.PurchaseRate,
                            SaleRate = item.SaleRate,
                            MRP = item.MRP,
                            CurrentQty = 0, // Start with zero, RecordTransactionAsync will add totalUnits
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _batchRepo.CreateAsync(newBatch);
                        batchId = newBatch.Id!;
                    }

                    // 4. Record Stock Transaction
                    var transaction = new StockTransaction
                    {
                        MedicineId = item.MedicineId,
                        BatchId = batchId,
                        Type = TransactionType.Purchase,
                        Quantity = totalUnits,
                        ReferenceId = master.Id,
                        Remarks = $"Purchase Inv: {master.InvoiceNo}",
                        UserId = userId,
                        TransactionDate = DateTime.UtcNow
                    };
                    await _stockService.RecordTransactionAsync(transaction);
                }

                // 5. Update Supplier Balance
                var supplier = await _supplierRepo.GetByIdAsync(model.SupplierId);
                if (supplier != null)
                {
                    supplier.CurrentBalance += model.TotalAmount;
                    await _supplierRepo.UpdateAsync(supplier.Id!, supplier);
                }

                return true;
            }
            finally
            {
                _activePurchaseLocks.TryRemove(model.TransactionGuid, out _);
            }
        }

        public async Task<IEnumerable<PurchaseMaster>> GetAllPurchasesAsync() => await _masterRepo.GetAllAsync();

        public async Task<PurchaseMaster> GetPurchaseByIdAsync(string id) => await _masterRepo.GetByIdAsync(id);

        public async Task<IEnumerable<PurchaseDetail>> GetPurchaseDetailsAsync(string masterId) 
            => await _detailRepo.FindAsync(x => x.PurchaseMasterId == masterId);

        public async Task<(bool Success, string Message)> DeletePurchaseAsync(string id, string userId)
        {
            var master = await _masterRepo.GetByIdAsync(id);
            if (master == null)
                return (false, "Purchase record not found.");

            var details = await GetPurchaseDetailsAsync(id);

            // 1. Validate Stock Availability before making any changes
            foreach (var detail in details)
            {
                int totalUnits = (detail.Qty + detail.FreeQty) * detail.UnitsPerStrip;
                var batch = (await _batchRepo.FindAsync(x => x.MedicineId == detail.MedicineId && x.BatchNo == detail.BatchNo)).FirstOrDefault();
                
                if (batch == null)
                {
                    return (false, $"Stock batch '{detail.BatchNo}' not found.");
                }

                if (batch.CurrentQty < totalUnits)
                {
                    return (false, $"Cannot delete purchase because some stock from batch '{detail.BatchNo}' has already been sold or adjusted. Available: {batch.CurrentQty}, required to deduct: {totalUnits}.");
                }
            }

            // 2. Perform Stock Reversal and Soft Delete Details
            foreach (var detail in details)
            {
                int totalUnits = (detail.Qty + detail.FreeQty) * detail.UnitsPerStrip;
                var batch = (await _batchRepo.FindAsync(x => x.MedicineId == detail.MedicineId && x.BatchNo == detail.BatchNo)).FirstOrDefault();
                
                if (batch != null)
                {
                    batch.CurrentQty -= totalUnits;
                    await _batchRepo.UpdateAsync(batch.Id!, batch);
                }

                await _detailRepo.DeleteAsync(detail.Id!);
            }

            // 3. Delete Stock Transactions
            await _stockService.DeleteTransactionsByReferenceAsync(id);

            // 4. Update Supplier Balance
            var supplier = await _supplierRepo.GetByIdAsync(master.SupplierId);
            if (supplier != null)
            {
                supplier.CurrentBalance -= master.TotalAmount;
                await _supplierRepo.UpdateAsync(supplier.Id!, supplier);
            }

            // 5. Soft Delete Purchase Master
            await _masterRepo.DeleteAsync(id);

            return (true, "Purchase entry deleted successfully.");
        }

        public async Task<PurchaseMaster?> GetPurchaseByInvoiceAsync(string invoiceNo)
        {
            var purchases = await _masterRepo.FindAsync(x => x.InvoiceNo == invoiceNo);
            return purchases.FirstOrDefault();
        }

        public async Task<bool> ProcessPurchaseReturnAsync(PurchaseReturnViewModel model, string userId)
        {
            var master = await _masterRepo.GetByIdAsync(model.PurchaseId);
            if (master == null) throw new Exception("Purchase record not found.");

            decimal totalRefund = 0;
            bool anyReturn = false;

            foreach (var item in model.Items.Where(x => x.ReturnQty > 0))
            {
                var detail = await _detailRepo.GetByIdAsync(item.PurchaseDetailId);
                if (detail == null) continue;

                if (detail.ReturnedQty + item.ReturnQty > detail.Qty)
                    throw new Exception($"Cannot return more than purchased quantity for medicine ID {item.MedicineId}");

                // Load corresponding batch
                var batch = (await _batchRepo.FindAsync(x => x.MedicineId == item.MedicineId && x.BatchNo == item.BatchNo)).FirstOrDefault();
                if (batch == null)
                    throw new Exception($"Medicine batch '{item.BatchNo}' not found.");

                // Safeguard Check: Check remaining stock units (including units per strip scaling)
                int returnUnits = item.ReturnQty * detail.UnitsPerStrip;
                if (batch.CurrentQty < returnUnits)
                    throw new Exception($"Insufficient stock in batch '{item.BatchNo}' to complete this return. Available: {batch.CurrentQty} units, trying to return: {returnUnits} units.");

                // 1. Deduct stock from the batch
                batch.CurrentQty -= returnUnits;
                await _batchRepo.UpdateAsync(batch.Id!, batch);

                // 2. Record outward Stock Transaction
                var transaction = new StockTransaction
                {
                    MedicineId = item.MedicineId,
                    BatchId = batch.Id!,
                    Type = TransactionType.PurchaseReturn,
                    Quantity = -returnUnits, // negative for outward return
                    ReferenceId = master.Id,
                    Remarks = $"Purchase Return against Inv {master.InvoiceNo}",
                    UserId = userId,
                    TransactionDate = DateTime.UtcNow
                };
                await _stockService.RecordTransactionAsync(transaction);

                // 3. Update Detail Returned Quantity
                detail.ReturnedQty += item.ReturnQty;
                await _detailRepo.UpdateAsync(detail.Id!, detail);

                // Calculate refund prorated
                decimal refundAmount = (detail.TotalPrice / detail.Qty) * item.ReturnQty;
                totalRefund += refundAmount;
                anyReturn = true;
            }

            if (anyReturn)
            {
                // Update master totals and status
                var allDetails = await _detailRepo.FindAsync(x => x.PurchaseMasterId == master.Id);
                bool allReturned = allDetails.All(x => x.ReturnedQty == x.Qty);
                master.Status = allReturned ? "Returned" : "Partially Returned";
                master.TotalAmount -= totalRefund;
                await _masterRepo.UpdateAsync(master.Id!, master);

                // Update Supplier Balance (we owe them less now)
                var supplier = await _supplierRepo.GetByIdAsync(master.SupplierId);
                if (supplier != null)
                {
                    supplier.CurrentBalance -= totalRefund;
                    await _supplierRepo.UpdateAsync(supplier.Id!, supplier);
                }
            }

            return true;
        }
    }
}
