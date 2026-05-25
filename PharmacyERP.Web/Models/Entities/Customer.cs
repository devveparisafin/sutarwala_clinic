using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace PharmacyERP.Web.Models.Entities
{
    public class Customer : BaseEntity
    {
        [Required]
        public string Name { get; set; } = null!;

        [Required]
        [RegularExpression(@"^\d{10,15}$", ErrorMessage = "Invalid Mobile Number")]
        public string MobileNumber { get; set; } = null!;

        public string? Email { get; set; }
        
        public string? Address { get; set; }
        
        public string? Gender { get; set; }
        
        public DateTime? DateOfBirth { get; set; }

        public string? Remarks { get; set; }
        
        public decimal CurrentBalance { get; set; }

        public DateTime? ReminderDate { get; set; }
        
        public string? ReminderFrequency { get; set; }
        
        public string? ReminderNote { get; set; }
    }
}
