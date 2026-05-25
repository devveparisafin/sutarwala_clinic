using PharmacyERP.Web.Common;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Models.ViewModels;

namespace PharmacyERP.Web.Services
{
    public interface IPurchaseService
    {
        Task<bool> ProcessPurchaseAsync(PurchaseEntryViewModel model, string userId);
        Task<IEnumerable<PurchaseMaster>> GetAllPurchasesAsync();
        Task<PurchaseMaster> GetPurchaseByIdAsync(string id);
        Task<IEnumerable<PurchaseDetail>> GetPurchaseDetailsAsync(string masterId);
    }

    public class PurchaseService : IPurchaseService
    {
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
                CreatedAt = DateTime.UtcNow
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

        public async Task<IEnumerable<PurchaseMaster>> GetAllPurchasesAsync() => await _masterRepo.GetAllAsync();

        public async Task<PurchaseMaster> GetPurchaseByIdAsync(string id) => await _masterRepo.GetByIdAsync(id);

        public async Task<IEnumerable<PurchaseDetail>> GetPurchaseDetailsAsync(string masterId) 
            => await _detailRepo.FindAsync(x => x.PurchaseMasterId == masterId);
    }
}
