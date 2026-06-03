using PharmacyERP.Web.Models.ViewModels;
using PharmacyERP.Web.Models.Entities;

namespace PharmacyERP.Web.Interfaces
{
    public interface ICustomerService
    {
        Task<IEnumerable<CustomerViewModel>> GetAllCustomersAsync();
        Task<CustomerViewModel?> GetCustomerByIdAsync(string id);
        Task<CustomerViewModel> CreateCustomerAsync(CustomerViewModel model);
        Task<bool> UpdateCustomerAsync(CustomerViewModel model);
        Task<bool> DeleteCustomerAsync(string id);

        Task<CustomerHistoryViewModel> GetCustomerHistoryAsync(string customerId);
        
        Task<CustomerPrescriptionViewModel> AddPrescriptionAsync(CustomerPrescriptionViewModel model, string webRootPath);
        Task<bool> DeletePrescriptionAsync(string prescriptionId);
        
        Task<IEnumerable<CustomerViewModel>> SearchCustomersAsync(string query);
        
        Task<CustomerLedgerViewModel?> GetLedgerAsync(string customerId);
        Task<bool> AddPaymentAsync(CustomerPayment payment);
        
        Task<bool> AcknowledgeReminderAsync(string customerId);
        Task<bool> SetReminderAsync(string customerId, DateTime? reminderDate, string? frequency, string? note);
        Task<IEnumerable<CustomerPrescriptionViewModel>> GetCustomerPrescriptionsAsync(string customerId);
        Task<(bool Success, string Message, string? Id)> QuickAddAsync(PharmacyERP.Web.Models.ViewModels.Masters.QuickAddCustomerViewModel model);
    }
}
