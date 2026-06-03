using AutoMapper;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Models.ViewModels;

namespace PharmacyERP.Web.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepo;
        private readonly ICustomerPrescriptionRepository _prescriptionRepo;
        private readonly IBaseRepository<Sale> _saleRepo;
        private readonly IBaseRepository<CustomerPayment> _paymentRepo;
        private readonly IMapper _mapper;

        public CustomerService(
            ICustomerRepository customerRepo,
            ICustomerPrescriptionRepository prescriptionRepo,
            IBaseRepository<Sale> saleRepo,
            IBaseRepository<CustomerPayment> paymentRepo,
            IMapper mapper)
        {
            _customerRepo = customerRepo;
            _prescriptionRepo = prescriptionRepo;
            _saleRepo = saleRepo;
            _paymentRepo = paymentRepo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CustomerViewModel>> GetAllCustomersAsync()
        {
            var customers = await _customerRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<CustomerViewModel>>(customers.OrderByDescending(c => c.CreatedAt));
        }

        public async Task<CustomerViewModel?> GetCustomerByIdAsync(string id)
        {
            var customer = await _customerRepo.GetByIdAsync(id);
            if (customer == null) return null;
            return _mapper.Map<CustomerViewModel>(customer);
        }

        public async Task<CustomerViewModel> CreateCustomerAsync(CustomerViewModel model)
        {
            var customer = _mapper.Map<Customer>(model);
            await _customerRepo.CreateAsync(customer);
            return _mapper.Map<CustomerViewModel>(customer);
        }

        public async Task<bool> UpdateCustomerAsync(CustomerViewModel model)
        {
            var existingCustomer = await _customerRepo.GetByIdAsync(model.Id!);
            if (existingCustomer == null) return false;

            _mapper.Map(model, existingCustomer);
            existingCustomer.UpdatedAt = DateTime.UtcNow;

            await _customerRepo.UpdateAsync(existingCustomer.Id!, existingCustomer);
            return true;
        }

        public async Task<bool> DeleteCustomerAsync(string id)
        {
            await _customerRepo.DeleteAsync(id);
            return true;
        }

        public async Task<CustomerHistoryViewModel> GetCustomerHistoryAsync(string customerId)
        {
            var customer = await _customerRepo.GetByIdAsync(customerId);
            if (customer == null) return new CustomerHistoryViewModel();

            var prescriptionsTask = _prescriptionRepo.FindAsync(p => p.CustomerId == customerId);
            var salesTask = _saleRepo.FindAsync(s => s.CustomerId == customerId || s.CustomerPhone == customer.MobileNumber);

            await Task.WhenAll(prescriptionsTask, salesTask);

            var prescriptions = prescriptionsTask.Result;
            var sales = salesTask.Result;

            return new CustomerHistoryViewModel
            {
                Customer = _mapper.Map<CustomerViewModel>(customer),
                Prescriptions = _mapper.Map<List<CustomerPrescriptionViewModel>>(prescriptions.OrderByDescending(p => p.PrescriptionDate)),
                PurchaseHistory = sales.OrderByDescending(s => s.SaleDate).ToList()
            };
        }

        public async Task<CustomerPrescriptionViewModel> AddPrescriptionAsync(CustomerPrescriptionViewModel model, string webRootPath)
        {
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(webRootPath, "uploads", "prescriptions");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(fileStream);
                }

                model.ImagePath = "/uploads/prescriptions/" + uniqueFileName;
            }

            var prescription = _mapper.Map<CustomerPrescription>(model);
            await _prescriptionRepo.CreateAsync(prescription);

            return _mapper.Map<CustomerPrescriptionViewModel>(prescription);
        }

        public async Task<bool> DeletePrescriptionAsync(string prescriptionId)
        {
            await _prescriptionRepo.DeleteAsync(prescriptionId);
            return true;
        }

        public async Task<IEnumerable<CustomerViewModel>> SearchCustomersAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<CustomerViewModel>();

            var customers = await _customerRepo.FindAsync(c => 
                c.Name.ToLower().Contains(query.ToLower()) || 
                c.MobileNumber.Contains(query));
                
            return _mapper.Map<IEnumerable<CustomerViewModel>>(customers);
        }

        public async Task<bool> AcknowledgeReminderAsync(string customerId)
        {
            var customer = await _customerRepo.GetByIdAsync(customerId);
            if (customer == null || customer.ReminderDate == null) return false;

            if (customer.ReminderFrequency == "Monthly")
            {
                customer.ReminderDate = customer.ReminderDate.Value.AddMonths(1);
            }
            else // Default or Weekly
            {
                customer.ReminderDate = customer.ReminderDate.Value.AddDays(7);
            }

            customer.UpdatedAt = DateTime.UtcNow;
            await _customerRepo.UpdateAsync(customer.Id!, customer);
            
            return true;
        }

        public async Task<bool> SetReminderAsync(string customerId, DateTime? reminderDate, string? frequency, string? note)
        {
            var customer = await _customerRepo.GetByIdAsync(customerId);
            if (customer == null) return false;

            customer.ReminderDate = reminderDate;
            customer.ReminderFrequency = frequency;
            customer.ReminderNote = note;
            customer.UpdatedAt = DateTime.UtcNow;

            await _customerRepo.UpdateAsync(customer.Id!, customer);
            return true;
        }

        public async Task<IEnumerable<CustomerPrescriptionViewModel>> GetCustomerPrescriptionsAsync(string customerId)
        {
            var prescriptions = await _prescriptionRepo.FindAsync(p => p.CustomerId == customerId);
            return _mapper.Map<IEnumerable<CustomerPrescriptionViewModel>>(prescriptions.OrderByDescending(p => p.PrescriptionDate));
        }

        public async Task<CustomerLedgerViewModel?> GetLedgerAsync(string customerId)
        {
            var customer = await _customerRepo.GetByIdAsync(customerId);
            if (customer == null) return null;

            var salesTask = _saleRepo.FindAsync(s => s.CustomerId == customerId && s.PaymentMode == "Credit" && !s.IsDeleted);
            var paymentsTask = _paymentRepo.FindAsync(p => p.CustomerId == customerId && !p.IsDeleted);

            await Task.WhenAll(salesTask, paymentsTask);

            var sales = salesTask.Result;
            var payments = paymentsTask.Result;

            var entries = new List<CustomerLedgerEntry>();

            foreach (var sale in sales)
            {
                entries.Add(new CustomerLedgerEntry
                {
                    Date = sale.SaleDate,
                    Description = "Credit Sale - " + sale.InvoiceNo,
                    Reference = sale.InvoiceNo,
                    Debit = sale.TotalAmount,
                    Credit = 0,
                    ReferenceId = sale.Id
                });
            }

            foreach (var payment in payments)
            {
                entries.Add(new CustomerLedgerEntry
                {
                    Date = payment.PaymentDate,
                    Description = "Payment - " + (payment.PaymentMode ?? "Cash"),
                    Reference = payment.ReferenceNo ?? "PAY",
                    Debit = 0,
                    Credit = payment.Amount,
                    ReferenceId = payment.Id
                });
            }

            var sortedEntries = entries.OrderBy(e => e.Date).ToList();
            decimal runningBalance = 0;
            foreach (var entry in sortedEntries)
            {
                runningBalance += entry.Debit - entry.Credit;
                entry.Balance = runningBalance;
            }

            return new CustomerLedgerViewModel
            {
                CustomerName = customer.Name,
                MobileNumber = customer.MobileNumber,
                CurrentBalance = customer.CurrentBalance,
                Entries = sortedEntries
            };
        }

        public async Task<bool> AddPaymentAsync(CustomerPayment payment)
        {
            var customer = await _customerRepo.GetByIdAsync(payment.CustomerId);
            if (customer == null) return false;

            payment.CreatedAt = DateTime.UtcNow;
            await _paymentRepo.CreateAsync(payment);

            customer.CurrentBalance -= payment.Amount;
            customer.UpdatedAt = DateTime.UtcNow;
            await _customerRepo.UpdateAsync(customer.Id!, customer);

            return true;
        }

        public async Task<(bool Success, string Message, string? Id)> QuickAddAsync(PharmacyERP.Web.Models.ViewModels.Masters.QuickAddCustomerViewModel model)
        {
            var cleanedName = model.Name.Trim();
            var cleanedPhone = model.Phone.Trim();
            var cleanedEmail = model.Email?.Trim();

            var existing = (await _customerRepo.FindAsync(x => x.MobileNumber == cleanedPhone && !x.IsDeleted)).FirstOrDefault();
            if (existing != null)
                return (false, "Customer with this mobile number already exists", null);

            var customer = new Customer
            {
                Name = cleanedName,
                MobileNumber = cleanedPhone,
                Email = cleanedEmail,
                CreatedAt = DateTime.UtcNow
            };
            await _customerRepo.CreateAsync(customer);
            return (true, "Customer added successfully", customer.Id);
        }
    }
}
