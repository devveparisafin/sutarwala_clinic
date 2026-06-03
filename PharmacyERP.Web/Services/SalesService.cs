using PharmacyERP.Web.Common;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Models.ViewModels;
using System.Collections.Concurrent;

namespace PharmacyERP.Web.Services
{
    public interface ISalesService
    {
        Task<string> ProcessSaleAsync(SalesEntryViewModel model, string userId);
        Task<IEnumerable<Sale>> GetAllSalesAsync();
        Task<Sale> GetSaleByIdAsync(string id);
        Task<IEnumerable<SaleDetail>> GetSaleDetailsAsync(string saleId);
        Task<bool> ProcessSaleReturnAsync(SaleReturnViewModel model, string userId);
        Task<Sale?> GetSaleByInvoiceAsync(string invoiceNo);
    }

    public class SalesService : ISalesService
    {
        private static readonly ConcurrentDictionary<string, byte> _activeSalesLocks = new();

        private readonly IBaseRepository<Sale> _saleRepo;
        private readonly IBaseRepository<SaleDetail> _detailRepo;
        private readonly IBaseRepository<Payment> _paymentRepo;
        private readonly IBaseRepository<MedicineBatch> _batchRepo;
        private readonly IBaseRepository<Medicine> _medicineRepo;
        private readonly ICustomerRepository _customerRepo;
        private readonly IStockService _stockService;

        public SalesService(
            IBaseRepository<Sale> saleRepo,
            IBaseRepository<SaleDetail> detailRepo,
            IBaseRepository<Payment> paymentRepo,
            IBaseRepository<MedicineBatch> batchRepo,
            IBaseRepository<Medicine> medicineRepo,
            ICustomerRepository customerRepo,
            IStockService stockService)
        {
            _saleRepo = saleRepo;
            _detailRepo = detailRepo;
            _paymentRepo = paymentRepo;
            _batchRepo = batchRepo;
            _medicineRepo = medicineRepo;
            _customerRepo = customerRepo;
            _stockService = stockService;
        }

        public async Task<string> ProcessSaleAsync(SalesEntryViewModel model, string userId)
        {
            if (string.IsNullOrEmpty(model.TransactionGuid))
            {
                throw new Exception("Transaction GUID is required.");
            }

            if (!_activeSalesLocks.TryAdd(model.TransactionGuid, 0))
            {
                throw new Exception("This sale transaction is already being processed. Please wait.");
            }

            try
            {
                // Check database if it has already been saved
                var existingSales = await _saleRepo.FindAsync(x => x.TransactionGuid == model.TransactionGuid);
                if (existingSales.Any())
                {
                    throw new Exception("This sale transaction has already been saved.");
                }

                // 0. Handle Customer (Auto-create by Mobile Number)
                string? customerId = null;
                if (!string.IsNullOrEmpty(model.CustomerPhone))
                {
                    var customer = await _customerRepo.GetByMobileAsync(model.CustomerPhone);
                    
                    if (customer == null)
                    {
                        customer = new Customer
                        {
                            Name = model.CustomerName ?? "Customer",
                            MobileNumber = model.CustomerPhone,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _customerRepo.CreateAsync(customer);
                    }
                    customerId = customer.Id;
                }

                // 1. Create Sale Master
                var sale = new Sale
                {
                    InvoiceNo = $"INV-{DateTime.Now:yyyyMMddHHmmss}",
                    SaleDate = DateTime.UtcNow,
                    CustomerName = model.CustomerName ?? "Walk-in Customer",
                    CustomerPhone = model.CustomerPhone,
                    CustomerId = customerId,
                    SubTotal = model.SubTotal,
                    TaxAmount = model.TaxAmount,
                    DiscountAmount = model.DiscountAmount,
                    TotalAmount = model.TotalAmount,
                    PaymentMode = model.PaymentMode,
                    Status = model.PaymentMode == "Credit" ? "Unpaid" : "Paid",
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    TransactionGuid = model.TransactionGuid,
                    PrescriptionId = model.PrescriptionId
                };
                await _saleRepo.CreateAsync(sale);

                // Update Customer Balance if Credit sale
                if (model.PaymentMode == "Credit" && !string.IsNullOrEmpty(customerId))
                {
                    var customer = await _customerRepo.GetByIdAsync(customerId);
                    if (customer != null)
                    {
                        customer.CurrentBalance += model.TotalAmount;
                        await _customerRepo.UpdateAsync(customer.Id!, customer);
                    }
                }

                // 2. Process Items and FIFO Deduction
                foreach (var item in model.Items)
                {
                    var medicine = await _medicineRepo.GetByIdAsync(item.MedicineId);
                    if (medicine == null) throw new Exception($"Medicine not found: {item.MedicineId}");

                    // Calculate total units to deduct
                    int totalUnits = item.IsLoose ? item.Qty : (item.Qty * medicine.UnitsPerStrip);
                    
                    // Deduct stock and get which batches were used
                    var deductions = await _stockService.DeductStockAsync(
                        item.MedicineId, 
                        totalUnits, 
                        sale.Id!, 
                        $"Sale Inv: {sale.InvoiceNo}", 
                        userId);

                    // Create a Sale Detail record for each batch used (for accurate batch/expiry tracking)
                    foreach (var d in deductions)
                    {
                        // Calculate pro-rated price for this batch portion
                        decimal ratio = (decimal)d.UnitsDeducted / totalUnits;
                        
                        var detail = new SaleDetail
                        {
                            SaleId = sale.Id!,
                            MedicineId = item.MedicineId,
                            BatchId = d.BatchId,
                            Qty = d.UnitsDeducted, // Storing units for precision in split batches
                            IsLoose = true, // Force to loose if split, or keep as is? 
                            Rate = item.Rate,
                            GST = item.GST,
                            TotalPrice = Math.Round(item.TotalPrice * ratio, 2),
                            CreatedAt = DateTime.UtcNow
                        };
                        await _detailRepo.CreateAsync(detail);
                    }
                }

                // 3. Record Payment (Skip if Credit sale)
                if (model.PaymentMode != "Credit")
                {
                    var payment = new Payment
                    {
                        SaleId = sale.Id!,
                        Amount = sale.TotalAmount,
                        PaymentMode = model.PaymentMode,
                        TransactionId = model.TransactionId,
                        PaymentDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _paymentRepo.CreateAsync(payment);
                }

                return sale.Id!;
            }
            finally
            {
                _activeSalesLocks.TryRemove(model.TransactionGuid, out _);
            }
        }

        public async Task<IEnumerable<Sale>> GetAllSalesAsync() => await _saleRepo.GetAllAsync();

        public async Task<Sale> GetSaleByIdAsync(string id) => await _saleRepo.GetByIdAsync(id);

        public async Task<IEnumerable<SaleDetail>> GetSaleDetailsAsync(string saleId)
            => await _detailRepo.FindAsync(x => x.SaleId == saleId);

        public async Task<Sale?> GetSaleByInvoiceAsync(string invoiceNo)
        {
            var sales = await _saleRepo.FindAsync(x => x.InvoiceNo == invoiceNo);
            return sales.FirstOrDefault();
        }

        public async Task<bool> ProcessSaleReturnAsync(SaleReturnViewModel model, string userId)
        {
            var sale = await _saleRepo.GetByIdAsync(model.SaleId);
            if (sale == null) throw new Exception("Sale not found");

            decimal totalRefund = 0;
            bool anyReturn = false;

            foreach (var item in model.Items.Where(x => x.ReturnQty > 0))
            {
                var detail = await _detailRepo.GetByIdAsync(item.SaleDetailId);
                if (detail == null) continue;

                if (detail.ReturnedQty + item.ReturnQty > detail.Qty)
                    throw new Exception($"Cannot return more than sold quantity for medicine ID {item.MedicineId}");

                // Update detail
                detail.ReturnedQty += item.ReturnQty;
                await _detailRepo.UpdateAsync(detail.Id!, detail);

                // Revert stock
                var transaction = new StockTransaction
                {
                    MedicineId = item.MedicineId,
                    BatchId = item.BatchId,
                    Type = TransactionType.SalesReturn,
                    Quantity = item.ReturnQty, // positive for inward
                    ReferenceId = sale.InvoiceNo,
                    Remarks = $"Return against Invoice {sale.InvoiceNo}",
                    UserId = userId,
                    TransactionDate = DateTime.UtcNow
                };
                await _stockService.RecordTransactionAsync(transaction);

                // Calculate refund for this item based on original rate + tax
                decimal refundAmount = (detail.TotalPrice / detail.Qty) * item.ReturnQty;
                totalRefund += refundAmount;
                anyReturn = true;
            }

            if (anyReturn)
            {
                // Update Sale status to partially or fully returned
                var allDetails = await _detailRepo.FindAsync(x => x.SaleId == sale.Id);
                bool allReturned = allDetails.All(x => x.ReturnedQty == x.Qty);
                sale.Status = allReturned ? "Returned" : "Partially Returned";
                
                sale.TotalAmount -= totalRefund; // Adjust total after return
                await _saleRepo.UpdateAsync(sale.Id!, sale);

                // Handle Customer Balance
                if (sale.PaymentMode == "Credit" && !string.IsNullOrEmpty(sale.CustomerId))
                {
                    var customer = await _customerRepo.GetByIdAsync(sale.CustomerId);
                    if (customer != null)
                    {
                        customer.CurrentBalance -= totalRefund;
                        await _customerRepo.UpdateAsync(customer.Id!, customer);
                    }
                }
                else
                {
                    // Create negative payment record for cash/bank return
                    var payment = new Payment
                    {
                        SaleId = sale.Id!,
                        Amount = -totalRefund,
                        PaymentMode = sale.PaymentMode,
                        TransactionId = "REFUND",
                        PaymentDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _paymentRepo.CreateAsync(payment);
                }
            }

            return true;
        }
    }
}
