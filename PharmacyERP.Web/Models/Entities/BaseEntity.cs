using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PharmacyERP.Web.Models.Entities
{
    [BsonIgnoreExtraElements(Inherited = true)]
    public abstract class BaseEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
