using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Models.ViewModels;
using PharmacyERP.Web.Helpers;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;

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
            var medicinesTask = _repository.GetAllAsync();
            
            // Cache-aside static master lists (10 minutes expiration) fetched in parallel
            var categoriesTask = _cache.GetOrCreateAsync("all_categories", entry => {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return _categoryRepo.GetAllAsync();
            });

            var manufacturersTask = _cache.GetOrCreateAsync("all_manufacturers", entry => {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return _manufacturerRepo.GetAllAsync();
            });

            var unitsTask = _cache.GetOrCreateAsync("all_units", entry => {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return _unitRepo.GetAllAsync();
            });

            var genericsTask = _cache.GetOrCreateAsync("all_generics", entry => {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return _genericRepo.GetAllAsync();
            });

            var racksTask = _cache.GetOrCreateAsync("all_racks", entry => {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return _rackRepo.GetAllAsync();
            });

            // Project only MedicineId and CurrentQty from active batches to avoid heavy JSON payload transfers
            var stockListTask = _batchRepo.Collection
                .Find(x => x.IsActive && !x.IsDeleted)
                .Project(x => new { x.MedicineId, x.CurrentQty })
                .ToListAsync();

            await Task.WhenAll(medicinesTask, categoriesTask, manufacturersTask, unitsTask, genericsTask, racksTask, stockListTask);

            var medicines = medicinesTask.Result;
            var categories = categoriesTask.Result ?? new List<MedicineCategory>();
            var manufacturers = manufacturersTask.Result ?? new List<Manufacturer>();
            var units = unitsTask.Result ?? new List<MedicineUnit>();
            var generics = genericsTask.Result ?? new List<GenericMedicine>();
            var racks = racksTask.Result ?? new List<Rack>();

            var stockDict = stockListTask.Result
                .GroupBy(x => x.MedicineId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.CurrentQty));

            // Convert lists to lookup dictionaries for O(1) matching (converts O(N*M) -> O(N))
            var categoryDict = categories.ToDictionary(c => c.Id!, c => c.Name);
            var manufacturerDict = manufacturers.ToDictionary(m => m.Id!, m => m.Name);
            var unitDict = units.ToDictionary(u => u.Id!, u => u.Name);
            var genericDict = generics.ToDictionary(g => g.Id!, g => g.Name);
            var rackDict = racks.ToDictionary(r => r.Id!, r => r.Name);

            var list = new List<MedicineListViewModel>();
            foreach (var m in medicines)
            {
                list.Add(new MedicineListViewModel
                {
                    Id = m.Id!,
                    Name = m.Name,
                    Barcode = m.Barcode,
                    RackName = m.RackId != null && rackDict.TryGetValue(m.RackId, out var rName) ? rName : "N/A",
                    RackLocation = m.RackLocation,
                    StockQuantity = stockDict.TryGetValue(m.Id!, out var qty) ? qty : 0,
                    IsActive = m.IsActive,
                    ImagePath = m.ImagePath,
                    CategoryName = m.CategoryId != null && categoryDict.TryGetValue(m.CategoryId, out var cName) ? cName : "N/A",
                    ManufacturerName = m.ManufacturerId != null && manufacturerDict.TryGetValue(m.ManufacturerId, out var mName) ? mName : "N/A",
                    UnitName = m.UnitId != null && unitDict.TryGetValue(m.UnitId, out var uName) ? uName : "N/A",
                    GenericName = m.GenericId != null && genericDict.TryGetValue(m.GenericId, out var gName) ? gName : "N/A",
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
