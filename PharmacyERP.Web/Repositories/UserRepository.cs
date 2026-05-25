using MongoDB.Driver;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;

namespace PharmacyERP.Web.Repositories
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
    }

    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(IMongoDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _collection.Find(x => x.Username == username && !x.IsDeleted).FirstOrDefaultAsync();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _collection.Find(x => x.Email == email && !x.IsDeleted).FirstOrDefaultAsync();
        }
    }
}
