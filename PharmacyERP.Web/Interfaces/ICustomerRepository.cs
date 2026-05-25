using PharmacyERP.Web.Models.Entities;

namespace PharmacyERP.Web.Interfaces
{
    public interface ICustomerRepository : IBaseRepository<Customer>
    {
        Task<Customer?> GetByMobileAsync(string mobileNumber);
        Task<IEnumerable<Customer>> GetTodaysRemindersAsync(DateTime today);
    }
}
