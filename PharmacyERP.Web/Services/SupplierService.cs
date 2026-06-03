using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Models.ViewModels;

namespace PharmacyERP.Web.Services
{
    public interface ISupplierService : IBaseService<Supplier>
    {
        Task<bool> AddPaymentAsync(SupplierPayment payment);
        Task<SupplierLedgerViewModel> GetLedgerAsync(string supplierId);
        Task<(bool Success, string Message, string? Id)> QuickAddAsync(PharmacyERP.Web.Models.ViewModels.Masters.QuickAddSupplierViewModel model);
    }

    public class SupplierService : BaseService<Supplier>, ISupplierService
    {
        private readonly IBaseRepository<SupplierPayment> _paymentRepo;
        private readonly IBaseRepository<PurchaseMaster> _purchaseRepo;

        public SupplierService(
            IBaseRepository<Supplier> repository,
            IBaseRepository<SupplierPayment> paymentRepo,
            IBaseRepository<PurchaseMaster> purchaseRepo) : base(repository)
        {
            _paymentRepo = paymentRepo;
            _purchaseRepo = purchaseRepo;
        }

        public async Task<bool> AddPaymentAsync(SupplierPayment payment)
        {
            await _paymentRepo.CreateAsync(payment);
            
            var supplier = await _repository.GetByIdAsync(payment.SupplierId);
            if (supplier != null)
            {
                supplier.CurrentBalance -= payment.Amount; // Reducing what we owe
                await _repository.UpdateAsync(supplier.Id!, supplier);
                return true;
            }
            return false;
        }

        public async Task<SupplierLedgerViewModel> GetLedgerAsync(string supplierId)
        {
            var supplier = await _repository.GetByIdAsync(supplierId);
            if (supplier == null) return null!;

            // Fetch payments and purchases in parallel using direct indexed database queries
            var paymentsTask = _paymentRepo.FindAsync(x => x.SupplierId == supplierId && !x.IsDeleted);
            var purchasesTask = _purchaseRepo.FindAsync(x => x.SupplierId == supplierId && !x.IsDeleted);

            await Task.WhenAll(paymentsTask, purchasesTask);

            var payments = paymentsTask.Result.ToList();
            var purchases = purchasesTask.Result.ToList();

            var model = new SupplierLedgerViewModel
            {
                SupplierName = supplier.Name,
                CurrentBalance = supplier.CurrentBalance,
                Entries = new List<LedgerEntry>()
            };

            // Add opening balance as first entry
            decimal runningBalance = supplier.OpeningBalance;
            model.Entries.Add(new LedgerEntry
            {
                Date = supplier.CreatedAt,
                Description = "Opening Balance",
                Credit = supplier.OpeningBalance,
                Balance = runningBalance
            });

            var allEntries = new List<LedgerEntry>();

            // Add Payments to combined list
            foreach (var p in payments)
            {
                allEntries.Add(new LedgerEntry
                {
                    Date = p.PaymentDate,
                    Description = $"Payment ({p.PaymentMode})",
                    Reference = p.ReferenceNo ?? "N/A",
                    Debit = p.Amount
                });
            }

            // Add Purchases to combined list
            foreach (var p in purchases)
            {
                allEntries.Add(new LedgerEntry
                {
                    Date = p.PurchaseDate,
                    Description = "Purchase",
                    Reference = p.InvoiceNo,
                    Credit = p.TotalAmount,
                    ReferenceId = p.Id // Link to purchase details
                });
            }

            // Sort by date
            allEntries = allEntries.OrderBy(x => x.Date).ToList();

            // Calculate running balance
            foreach (var entry in allEntries)
            {
                if (entry.Debit > 0)
                    runningBalance -= entry.Debit;
                if (entry.Credit > 0)
                    runningBalance += entry.Credit;

                entry.Balance = runningBalance;
                model.Entries.Add(entry);
            }

            return model;
        }

        public async Task<(bool Success, string Message, string? Id)> QuickAddAsync(PharmacyERP.Web.Models.ViewModels.Masters.QuickAddSupplierViewModel model)
        {
            var cleanedName = model.Name.Trim();
            var cleanedPhone = model.Phone?.Trim();
            var cleanedEmail = model.Email?.Trim();

            var existingByName = (await _repository.FindAsync(x => x.Name.ToLower() == cleanedName.ToLower() && !x.IsDeleted)).FirstOrDefault();
            if (existingByName != null)
                return (false, "Supplier already exists", null);

            if (!string.IsNullOrEmpty(cleanedPhone))
            {
                var existingByPhone = (await _repository.FindAsync(x => x.Phone == cleanedPhone && !x.IsDeleted)).FirstOrDefault();
                if (existingByPhone != null)
                    return (false, "Supplier with this mobile number already exists", null);
            }

            if (!string.IsNullOrEmpty(cleanedEmail))
            {
                var existingByEmail = (await _repository.FindAsync(x => x.Email != null && x.Email.ToLower() == cleanedEmail.ToLower() && !x.IsDeleted)).FirstOrDefault();
                if (existingByEmail != null)
                    return (false, "Supplier with this email already exists", null);
            }

            var supplier = new Supplier
            {
                Name = cleanedName,
                Phone = cleanedPhone,
                Email = cleanedEmail,
                IsActive = true
            };
            await _repository.CreateAsync(supplier);
            return (true, "Supplier added successfully", supplier.Id);
        }
    }
}
