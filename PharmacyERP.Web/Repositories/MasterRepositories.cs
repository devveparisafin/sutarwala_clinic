using MongoDB.Driver;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;

namespace PharmacyERP.Web.Repositories
{
    public class MedicineCategoryRepository : BaseRepository<MedicineCategory>, IMedicineCategoryRepository
    {
        public MedicineCategoryRepository(IMongoDbContext context) : base(context)
        {
            var indexKeys = Builders<MedicineCategory>.IndexKeys.Ascending(x => x.Name);
            _collection.Indexes.CreateOne(new CreateIndexModel<MedicineCategory>(indexKeys, new CreateIndexOptions { Unique = true }));
        }
    }

    public class MedicineUnitRepository : BaseRepository<MedicineUnit>, IMedicineUnitRepository
    {
        public MedicineUnitRepository(IMongoDbContext context) : base(context)
        {
            var indexKeys = Builders<MedicineUnit>.IndexKeys.Ascending(x => x.Name);
            _collection.Indexes.CreateOne(new CreateIndexModel<MedicineUnit>(indexKeys, new CreateIndexOptions { Unique = true }));
        }
    }

    public class ManufacturerRepository : BaseRepository<Manufacturer>, IManufacturerRepository
    {
        public ManufacturerRepository(IMongoDbContext context) : base(context)
        {
            var indexKeys = Builders<Manufacturer>.IndexKeys.Ascending(x => x.Name);
            _collection.Indexes.CreateOne(new CreateIndexModel<Manufacturer>(indexKeys, new CreateIndexOptions { Unique = true }));
        }
    }

    public class GenericMedicineRepository : BaseRepository<GenericMedicine>, IGenericMedicineRepository
    {
        public GenericMedicineRepository(IMongoDbContext context) : base(context)
        {
            var indexKeys = Builders<GenericMedicine>.IndexKeys.Ascending(x => x.Name);
            _collection.Indexes.CreateOne(new CreateIndexModel<GenericMedicine>(indexKeys, new CreateIndexOptions { Unique = true }));
        }
    }

    public class RackRepository : BaseRepository<Rack>, IRackRepository
    {
        public RackRepository(IMongoDbContext context) : base(context)
        {
            var indexKeys = Builders<Rack>.IndexKeys.Ascending(x => x.Name);
            _collection.Indexes.CreateOne(new CreateIndexModel<Rack>(indexKeys, new CreateIndexOptions { Unique = true }));
        }
    }
}
