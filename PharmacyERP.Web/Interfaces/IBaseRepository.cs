using System.Linq.Expressions;
using MongoDB.Driver;

namespace PharmacyERP.Web.Interfaces
{
    public interface IBaseRepository<T> where T : class
    {
        IMongoCollection<T> Collection { get; }
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T> GetByIdAsync(string id);
        Task CreateAsync(T entity);
        Task UpdateAsync(string id, T entity);
        Task DeleteAsync(string id);
        Task<long> CountAsync(Expression<Func<T, bool>> predicate);
    }
}
