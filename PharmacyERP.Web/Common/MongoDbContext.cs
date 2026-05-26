using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using PharmacyERP.Web.Configurations;
using PharmacyERP.Web.Interfaces;
using Microsoft.Extensions.Options;
using Serilog;

namespace PharmacyERP.Web.Common
{
    public class MongoDbContext : IMongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IOptions<MongoDbSettings> settings)
        {
            var mongoClientSettings = MongoClientSettings.FromConnectionString(settings.Value.ConnectionString);
            mongoClientSettings.ClusterConfigurator = cb => {
                cb.Subscribe<CommandStartedEvent>(e => {
                    Log.Debug("MongoCommand Started: {CommandName} - {Command}", e.CommandName, e.Command.ToJson());
                });
                cb.Subscribe<CommandSucceededEvent>(e => {
                    if (e.Duration.TotalMilliseconds > 100)
                    {
                        Log.Warning("SLOW QUERY DETECTED: {CommandName} executed in {Duration}ms", e.CommandName, e.Duration.TotalMilliseconds);
                    }
                });
            };

            var client = new MongoClient(mongoClientSettings);
            _database = client.GetDatabase(settings.Value.DatabaseName);
        }

        public IMongoDatabase Database => _database;

        public IMongoCollection<T> GetCollection<T>(string name)
        {
            return _database.GetCollection<T>(name);
        }
    }
}
