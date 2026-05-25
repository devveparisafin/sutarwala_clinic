using MongoDB.Driver;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;

namespace PharmacyERP.Web.Repositories
{
    public class CustomerRepository : BaseRepository<Customer>, ICustomerRepository
    {
        public CustomerRepository(IMongoDbContext context) : base(context)
        {
            // Create Indexes
            var indexKeysDefinition = Builders<Customer>.IndexKeys.Ascending(c => c.MobileNumber);
            _collection.Indexes.CreateOne(new CreateIndexModel<Customer>(indexKeysDefinition, new CreateIndexOptions { Unique = true }));
            
            var nameIndexKeys = Builders<Customer>.IndexKeys.Ascending(c => c.Name);
            _collection.Indexes.CreateOne(new CreateIndexModel<Customer>(nameIndexKeys));
        }

        public async Task<Customer?> GetByMobileAsync(string mobileNumber)
        {
            return await _collection.Find(x => x.MobileNumber == mobileNumber && !x.IsDeleted).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Customer>> GetTodaysRemindersAsync(DateTime today)
        {
            // We want to match only the date part. Since MongoDB dates include time,
            // we search for dates between start of today and end of today.
            var startOfDay = today.Date;
            var endOfDay = today.Date.AddDays(1).AddTicks(-1);

            return await _collection
                .Find(x => x.ReminderDate != null
                        && x.ReminderDate >= startOfDay
                        && x.ReminderDate <= endOfDay
                        && !x.IsDeleted)
                .ToListAsync();
        }
    }
}
