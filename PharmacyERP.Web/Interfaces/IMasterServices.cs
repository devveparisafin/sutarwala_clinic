using PharmacyERP.Web.Models.ViewModels.Masters;

namespace PharmacyERP.Web.Interfaces
{
    public interface IMedicineCategoryService
    {
        Task<IEnumerable<MedicineCategoryViewModel>> GetAllAsync();
        Task<MedicineCategoryViewModel?> GetByIdAsync(string id);
        Task<bool> CreateAsync(MedicineCategoryViewModel model);
        Task<bool> UpdateAsync(MedicineCategoryViewModel model);
        Task<bool> DeleteAsync(string id);
        Task<(bool Success, string Message, string? Id)> QuickAddAsync(QuickAddCategoryViewModel model);
    }

    public interface IMedicineUnitService
    {
        Task<IEnumerable<MedicineUnitViewModel>> GetAllAsync();
        Task<MedicineUnitViewModel?> GetByIdAsync(string id);
        Task<bool> CreateAsync(MedicineUnitViewModel model);
        Task<bool> UpdateAsync(MedicineUnitViewModel model);
        Task<bool> DeleteAsync(string id);
        Task<(bool Success, string Message, string? Id)> QuickAddAsync(QuickAddUnitViewModel model);
    }

    public interface IManufacturerService
    {
        Task<IEnumerable<ManufacturerViewModel>> GetAllAsync();
        Task<ManufacturerViewModel?> GetByIdAsync(string id);
        Task<bool> CreateAsync(ManufacturerViewModel model);
        Task<bool> UpdateAsync(ManufacturerViewModel model);
        Task<bool> DeleteAsync(string id);
        Task<(bool Success, string Message, string? Id)> QuickAddAsync(QuickAddManufacturerViewModel model);
    }

    public interface IGenericMedicineService
    {
        Task<IEnumerable<GenericMedicineViewModel>> GetAllAsync();
        Task<GenericMedicineViewModel?> GetByIdAsync(string id);
        Task<bool> CreateAsync(GenericMedicineViewModel model);
        Task<bool> UpdateAsync(GenericMedicineViewModel model);
        Task<bool> DeleteAsync(string id);
        Task<(bool Success, string Message, string? Id)> QuickAddAsync(QuickAddGenericMedicineViewModel model);
    }

    public interface IRackService
    {
        Task<IEnumerable<RackViewModel>> GetAllAsync();
        Task<RackViewModel?> GetByIdAsync(string id);
        Task<bool> CreateAsync(RackViewModel model);
        Task<bool> UpdateAsync(RackViewModel model);
        Task<bool> DeleteAsync(string id);
    }
}
