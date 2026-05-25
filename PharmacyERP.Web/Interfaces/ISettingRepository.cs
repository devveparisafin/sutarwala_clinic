using PharmacyERP.Web.Models.Entities;

namespace PharmacyERP.Web.Interfaces
{
    public interface ISettingRepository : IBaseRepository<Setting>
    {
        Task<Setting?> GetMainSettingAsync();
    }
}
