using AutoMapper;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Models.ViewModels.Masters;

namespace PharmacyERP.Web.Services
{
    public class MedicineCategoryService : IMedicineCategoryService
    {
        private readonly IMedicineCategoryRepository _repo;
        private readonly IMapper _mapper;

        public MedicineCategoryService(IMedicineCategoryRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<MedicineCategoryViewModel>> GetAllAsync()
        {
            var entities = await _repo.GetAllAsync();
            return _mapper.Map<IEnumerable<MedicineCategoryViewModel>>(entities);
        }

        public async Task<MedicineCategoryViewModel?> GetByIdAsync(string id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return _mapper.Map<MedicineCategoryViewModel>(entity);
        }

        public async Task<bool> CreateAsync(MedicineCategoryViewModel model)
        {
            var entity = _mapper.Map<MedicineCategory>(model);
            await _repo.CreateAsync(entity);
            return true;
        }

        public async Task<bool> UpdateAsync(MedicineCategoryViewModel model)
        {
            if (string.IsNullOrEmpty(model.Id)) return false;
            var entity = _mapper.Map<MedicineCategory>(model);
            await _repo.UpdateAsync(model.Id, entity);
            return true;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            await _repo.DeleteAsync(id);
            return true;
        }

        public async Task<(bool Success, string Message, string? Id)> QuickAddAsync(QuickAddCategoryViewModel model)
        {
            var cleanedName = model.Name.Trim();
            var existing = (await _repo.FindAsync(x => x.Name.ToLower() == cleanedName.ToLower() && !x.IsDeleted)).FirstOrDefault();
            if (existing != null)
                return (false, "Medicine category already exists", null);

            var category = new MedicineCategory
            {
                Name = cleanedName,
                Description = model.Description?.Trim(),
                IsActive = true
            };
            await _repo.CreateAsync(category);
            return (true, "Medicine category added successfully", category.Id);
        }
    }

    public class MedicineUnitService : IMedicineUnitService
    {
        private readonly IMedicineUnitRepository _repo;
        private readonly IMapper _mapper;

        public MedicineUnitService(IMedicineUnitRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<MedicineUnitViewModel>> GetAllAsync()
        {
            var entities = await _repo.GetAllAsync();
            return _mapper.Map<IEnumerable<MedicineUnitViewModel>>(entities);
        }

        public async Task<MedicineUnitViewModel?> GetByIdAsync(string id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return _mapper.Map<MedicineUnitViewModel>(entity);
        }

        public async Task<bool> CreateAsync(MedicineUnitViewModel model)
        {
            var entity = _mapper.Map<MedicineUnit>(model);
            await _repo.CreateAsync(entity);
            return true;
        }

        public async Task<bool> UpdateAsync(MedicineUnitViewModel model)
        {
            if (string.IsNullOrEmpty(model.Id)) return false;
            var entity = _mapper.Map<MedicineUnit>(model);
            await _repo.UpdateAsync(model.Id, entity);
            return true;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            await _repo.DeleteAsync(id);
            return true;
        }

        public async Task<(bool Success, string Message, string? Id)> QuickAddAsync(QuickAddUnitViewModel model)
        {
            var cleanedName = model.Name.Trim();
            var existing = (await _repo.FindAsync(x => x.Name.ToLower() == cleanedName.ToLower() && !x.IsDeleted)).FirstOrDefault();
            if (existing != null)
                return (false, "Medicine unit already exists", null);

            var unit = new MedicineUnit
            {
                Name = cleanedName,
                IsActive = true
            };
            await _repo.CreateAsync(unit);
            return (true, "Medicine unit added successfully", unit.Id);
        }
    }

    public class ManufacturerService : IManufacturerService
    {
        private readonly IManufacturerRepository _repo;
        private readonly IMapper _mapper;

        public ManufacturerService(IManufacturerRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ManufacturerViewModel>> GetAllAsync()
        {
            var entities = await _repo.GetAllAsync();
            return _mapper.Map<IEnumerable<ManufacturerViewModel>>(entities);
        }

        public async Task<ManufacturerViewModel?> GetByIdAsync(string id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return _mapper.Map<ManufacturerViewModel>(entity);
        }

        public async Task<bool> CreateAsync(ManufacturerViewModel model)
        {
            var entity = _mapper.Map<Manufacturer>(model);
            await _repo.CreateAsync(entity);
            return true;
        }

        public async Task<bool> UpdateAsync(ManufacturerViewModel model)
        {
            if (string.IsNullOrEmpty(model.Id)) return false;
            var entity = _mapper.Map<Manufacturer>(model);
            await _repo.UpdateAsync(model.Id, entity);
            return true;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            await _repo.DeleteAsync(id);
            return true;
        }

        public async Task<(bool Success, string Message, string? Id)> QuickAddAsync(QuickAddManufacturerViewModel model)
        {
            var cleanedName = model.Name.Trim();
            var existing = (await _repo.FindAsync(x => x.Name.ToLower() == cleanedName.ToLower() && !x.IsDeleted)).FirstOrDefault();
            if (existing != null)
                return (false, "Manufacturer already exists", null);

            var manufacturer = new Manufacturer
            {
                Name = cleanedName,
                Phone = model.Phone?.Trim(),
                IsActive = true
            };
            await _repo.CreateAsync(manufacturer);
            return (true, "Manufacturer added successfully", manufacturer.Id);
        }
    }

    public class GenericMedicineService : IGenericMedicineService
    {
        private readonly IGenericMedicineRepository _repo;
        private readonly IMapper _mapper;

        public GenericMedicineService(IGenericMedicineRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<GenericMedicineViewModel>> GetAllAsync()
        {
            var entities = await _repo.GetAllAsync();
            return _mapper.Map<IEnumerable<GenericMedicineViewModel>>(entities);
        }

        public async Task<GenericMedicineViewModel?> GetByIdAsync(string id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return _mapper.Map<GenericMedicineViewModel>(entity);
        }

        public async Task<bool> CreateAsync(GenericMedicineViewModel model)
        {
            var entity = _mapper.Map<GenericMedicine>(model);
            await _repo.CreateAsync(entity);
            return true;
        }

        public async Task<bool> UpdateAsync(GenericMedicineViewModel model)
        {
            if (string.IsNullOrEmpty(model.Id)) return false;
            var entity = _mapper.Map<GenericMedicine>(model);
            await _repo.UpdateAsync(model.Id, entity);
            return true;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            await _repo.DeleteAsync(id);
            return true;
        }

        public async Task<(bool Success, string Message, string? Id)> QuickAddAsync(QuickAddGenericMedicineViewModel model)
        {
            var cleanedName = model.Name.Trim();
            var existing = (await _repo.FindAsync(x => x.Name.ToLower() == cleanedName.ToLower() && !x.IsDeleted)).FirstOrDefault();
            if (existing != null)
                return (false, "Generic medicine already exists", null);

            var generic = new GenericMedicine
            {
                Name = cleanedName,
                Description = model.Description?.Trim(),
                IsActive = true
            };
            await _repo.CreateAsync(generic);
            return (true, "Generic medicine added successfully", generic.Id);
        }
    }

    public class RackService : IRackService
    {
        private readonly IRackRepository _repo;
        private readonly IMapper _mapper;

        public RackService(IRackRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<RackViewModel>> GetAllAsync()
        {
            var entities = await _repo.GetAllAsync();
            return _mapper.Map<IEnumerable<RackViewModel>>(entities);
        }

        public async Task<RackViewModel?> GetByIdAsync(string id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return _mapper.Map<RackViewModel>(entity);
        }

        public async Task<bool> CreateAsync(RackViewModel model)
        {
            var entity = _mapper.Map<Rack>(model);
            await _repo.CreateAsync(entity);
            return true;
        }

        public async Task<bool> UpdateAsync(RackViewModel model)
        {
            if (string.IsNullOrEmpty(model.Id)) return false;
            var entity = _mapper.Map<Rack>(model);
            await _repo.UpdateAsync(model.Id, entity);
            return true;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            await _repo.DeleteAsync(id);
            return true;
        }
    }
}
