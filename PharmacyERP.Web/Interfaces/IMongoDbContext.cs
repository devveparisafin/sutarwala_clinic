using MongoDB.Driver;
using PharmacyERP.Web.Configurations;
using Microsoft.Extensions.Options;

namespace PharmacyERP.Web.Interfaces
{
    public interface IMongoDbContext
    {
        IMongoDatabase Database { get; }
        IMongoCollection<T> GetCollection<T>(string name);
    }
}
