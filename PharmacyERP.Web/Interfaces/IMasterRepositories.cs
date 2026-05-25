using PharmacyERP.Web.Models.Entities;

namespace PharmacyERP.Web.Interfaces
{
    public interface IMedicineCategoryRepository : IBaseRepository<MedicineCategory> { }
    public interface IMedicineUnitRepository : IBaseRepository<MedicineUnit> { }
    public interface IManufacturerRepository : IBaseRepository<Manufacturer> { }
    public interface IGenericMedicineRepository : IBaseRepository<GenericMedicine> { }
    public interface IRackRepository : IBaseRepository<Rack> { }
}
