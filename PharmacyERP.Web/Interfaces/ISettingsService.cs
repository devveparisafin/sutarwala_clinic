using PharmacyERP.Web.Models.ViewModels;

namespace PharmacyERP.Web.Interfaces
{
    public interface ISettingsService
    {
        Task<SettingsViewModel> GetSettingsAsync();
        Task<bool> UpdateSettingsAsync(SettingsViewModel model, string webRootPath);
        Task<bool> TriggerBackupAsync();
    }
}
