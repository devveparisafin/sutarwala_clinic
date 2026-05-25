using MongoDB.Driver;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;

namespace PharmacyERP.Web.Repositories
{
    public class SettingRepository : BaseRepository<Setting>, ISettingRepository
    {
        public SettingRepository(IMongoDbContext context) : base(context)
        {
        }

        public async Task<Setting?> GetMainSettingAsync()
        {
            return await _collection.Find(x => !x.IsDeleted).FirstOrDefaultAsync();
        }
    }
}
