using MongoDB.Driver;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;

namespace PharmacyERP.Web.Repositories
{
    public class CustomerPrescriptionRepository : BaseRepository<CustomerPrescription>, ICustomerPrescriptionRepository
    {
        public CustomerPrescriptionRepository(IMongoDbContext context) : base(context)
        {
            // Create Indexes
            var indexKeysDefinition = Builders<CustomerPrescription>.IndexKeys.Ascending(c => c.CustomerId);
            _collection.Indexes.CreateOne(new CreateIndexModel<CustomerPrescription>(indexKeysDefinition));
        }
    }
}
