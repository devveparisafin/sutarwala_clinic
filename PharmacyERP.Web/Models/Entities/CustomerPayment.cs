using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace PharmacyERP.Web.Models.Entities
{
    public class CustomerPayment : BaseEntity
    {
        [Required]
        [BsonRepresentation(BsonType.ObjectId)]
        public string CustomerId { get; set; } = null!;

        public decimal Amount { get; set; }
        
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        
        public string PaymentMode { get; set; } = "Cash"; // Cash, UPI, Card
        
        public string? ReferenceNo { get; set; }
        
        public string? Remarks { get; set; }
    }
}
