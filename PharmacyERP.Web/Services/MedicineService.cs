using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Models.ViewModels;
using PharmacyERP.Web.Helpers;
using Microsoft.Extensions.Caching.Memory;

namespace PharmacyERP.Web.Services
{
    public interface IMedicineService : IBaseService<Medicine>
    {
        Task<IEnumerable<MedicineListViewModel>> GetMedicineListAsync();
        Task<MedicineViewModel> GetMedicineForEditAsync(string id);
        Task<bool> CreateMedicineAsync(MedicineViewModel model);
        Task<bool> UpdateMedicineAsync(MedicineViewModel model);
    }

    public class MedicineService : BaseService<Medicine>, IMedicineService
    {
        private readonly IBaseRepository<MedicineCategory> _categoryRepo;
        private readonly IBaseRepository<Manufacturer> _manufacturerRepo;
        private readonly IBaseRepository<MedicineUnit> _unitRepo;
        private readonly IBaseRepository<GenericMedicine> _genericRepo;
        private readonly IBaseRepository<Rack> _rackRepo;
        private readonly IStockService _stockService;
        private readonly IWebHostEnvironment _environment;
        private readonly IBaseRepository<MedicineBatch> _batchRepo;
        private readonly IMemoryCache _cache;

        public MedicineService(
            IBaseRepository<Medicine> repository,
            IBaseRepository<MedicineCategory> categoryRepo,
            IBaseRepository<Manufacturer> manufacturerRepo,
            IBaseRepository<MedicineUnit> unitRepo,
            IBaseRepository<GenericMedicine> genericRepo,
            IBaseRepository<Rack> rackRepo,
            IStockService stockService,
            IWebHostEnvironment environment,
            IBaseRepository<MedicineBatch> batchRepo,
            IMemoryCache cache) : base(repository)
        {
            _categoryRepo = categoryRepo;
            _manufacturerRepo = manufacturerRepo;
            _unitRepo = unitRepo;
            _genericRepo = genericRepo;
            _rackRepo = rackRepo;
            _stockService = stockService;
            _environment = environment;
            _batchRepo = batchRepo;
            _cache = cache;
        }

        public async Task<IEnumerable<MedicineListViewModel>> GetMedicineListAsync()
        {
            var medicines = await _repository.GetAllAsync();
            
            // Cache-aside static master lists (10 minutes expiration)
            var categories = await _cache.GetOrCreateAsync("all_categories", entry => {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return _categoryRepo.GetAllAsync();
            }) ?? new List<MedicineCategory>();

            var manufacturers = await _cache.GetOrCreateAsync("all_manufacturers", entry => {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return _manufacturerRepo.GetAllAsync();
            }) ?? new List<Manufacturer>();

            var units = await _cache.GetOrCreateAsync("all_units", entry => {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return _unitRepo.GetAllAsync();
            }) ?? new List<MedicineUnit>();

            var generics = await _cache.GetOrCreateAsync("all_generics", entry => {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return _genericRepo.GetAllAsync();
            }) ?? new List<GenericMedicine>();

            var racks = await _cache.GetOrCreateAsync("all_racks", entry => {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return _rackRepo.GetAllAsync();
            }) ?? new List<Rack>();

            // Bulk fetch all active batches to avoid N+1 query loop
            var allBatches = await _batchRepo.FindAsync(x => x.IsActive && !x.IsDeleted);
            var stockDict = allBatches.GroupBy(x => x.MedicineId).ToDictionary(g => g.Key, g => g.Sum(x => x.CurrentQty));

            var list = new List<MedicineListViewModel>();
            foreach (var m in medicines)
            {
                list.Add(new MedicineListViewModel
                {
                    Id = m.Id!,
                    Name = m.Name,
                    Barcode = m.Barcode,
                    RackName = racks.FirstOrDefault(r => r.Id == m.RackId)?.Name ?? "N/A",
                    RackLocation = m.RackLocation,
                    StockQuantity = stockDict.TryGetValue(m.Id!, out var qty) ? qty : 0,
                    IsActive = m.IsActive,
                    ImagePath = m.ImagePath,
                    CategoryName = categories.FirstOrDefault(c => c.Id == m.CategoryId)?.Name ?? "N/A",
                    ManufacturerName = manufacturers.FirstOrDefault(ma => ma.Id == m.ManufacturerId)?.Name ?? "N/A",
                    UnitName = units.FirstOrDefault(u => u.Id == m.UnitId)?.Name ?? "N/A",
                    GenericName = generics.FirstOrDefault(g => g.Id == m.GenericId)?.Name ?? "N/A",
                    IsLooseSale = m.IsLooseSale,
                    UnitsPerStrip = m.UnitsPerStrip,
                    LooseUnitName = m.LooseUnitName,
                    StripName = m.StripName
                });
            }
            return list;
        }

        public async Task<MedicineViewModel> GetMedicineForEditAsync(string id)
        {
            var m = await _repository.GetByIdAsync(id);
            if (m == null) return null!;

            return new MedicineViewModel
            {
                Id = m.Id,
                Name = m.Name,
                GenericId = m.GenericId,
                ManufacturerId = m.ManufacturerId,
                CategoryId = m.CategoryId,
                UnitId = m.UnitId,
                Barcode = m.Barcode,
                HSNCode = m.HSNCode,
                GST = m.GST,
                Description = m.Description,
                ExistingImagePath = m.ImagePath,
                RackId = m.RackId,
                RackLocation = m.RackLocation,
                LowStockThreshold = m.LowStockThreshold,
                IsActive = m.IsActive,
                IsLooseSale = m.IsLooseSale,
                UnitsPerStrip = m.UnitsPerStrip,
                LooseUnitName = m.LooseUnitName,
                StripName = m.StripName
            };
        }

        public async Task<bool> CreateMedicineAsync(MedicineViewModel model)
        {
            var medicine = new Medicine
            {
                Name = model.Name,
                GenericId = model.GenericId,
                ManufacturerId = model.ManufacturerId,
                CategoryId = model.CategoryId,
                UnitId = model.UnitId,
                Barcode = model.Barcode,
                HSNCode = model.HSNCode,
                GST = model.GST,
                Description = model.Description,
                RackId = model.RackId,
                RackLocation = model.RackLocation,
                LowStockThreshold = model.LowStockThreshold,
                IsActive = model.IsActive,
                IsLooseSale = model.IsLooseSale,
                UnitsPerStrip = model.UnitsPerStrip,
                LooseUnitName = model.LooseUnitName,
                StripName = model.StripName,
                CreatedAt = DateTime.UtcNow
            };

            if (model.ImageFile != null)
            {
                var uploads = Path.Combine(_environment.WebRootPath, "uploads", "medicines");
                medicine.ImagePath = await FileHelper.UploadFileAsync(model.ImageFile, uploads);
            }

            await _repository.CreateAsync(medicine);
            return true;
        }

        public async Task<bool> UpdateMedicineAsync(MedicineViewModel model)
        {
            var medicine = await _repository.GetByIdAsync(model.Id!);
            if (medicine == null) return false;

            medicine.Name = model.Name;
            medicine.GenericId = model.GenericId;
            medicine.ManufacturerId = model.ManufacturerId;
            medicine.CategoryId = model.CategoryId;
            medicine.UnitId = model.UnitId;
            medicine.Barcode = model.Barcode;
            medicine.HSNCode = model.HSNCode;
            medicine.GST = model.GST;
            medicine.Description = model.Description;
            medicine.RackId = model.RackId;
            medicine.RackLocation = model.RackLocation;
            medicine.LowStockThreshold = model.LowStockThreshold;
            medicine.IsActive = model.IsActive;
            medicine.IsLooseSale = model.IsLooseSale;
            medicine.UnitsPerStrip = model.UnitsPerStrip;
            medicine.LooseUnitName = model.LooseUnitName;
            medicine.StripName = model.StripName;
            medicine.UpdatedAt = DateTime.UtcNow;

            if (model.ImageFile != null)
            {
                var uploads = Path.Combine(_environment.WebRootPath, "uploads", "medicines");
                if (!string.IsNullOrEmpty(medicine.ImagePath))
                {
                    FileHelper.DeleteFile(Path.Combine(uploads, medicine.ImagePath));
                }
                medicine.ImagePath = await FileHelper.UploadFileAsync(model.ImageFile, uploads);
            }

            await _repository.UpdateAsync(medicine.Id!, medicine);
            return true;
        }
    }
}
