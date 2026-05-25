using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace PharmacyERP.Web.Models.Entities
{
    public class CustomerPrescription : BaseEntity
    {
        [Required]
        [BsonRepresentation(BsonType.ObjectId)]
        public string CustomerId { get; set; } = null!;

        public string? DoctorName { get; set; }

        public DateTime PrescriptionDate { get; set; } = DateTime.UtcNow;

        [Required]
        public string ImagePath { get; set; } = null!; // Path in wwwroot/uploads

        public string? Remarks { get; set; }
    }
}
