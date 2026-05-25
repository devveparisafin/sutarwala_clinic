using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PharmacyERP.Web.Models.ViewModels
{
    public class CustomerViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Mobile Number is required")]
        [RegularExpression(@"^\d{10,15}$", ErrorMessage = "Invalid Mobile Number")]
        public string MobileNumber { get; set; } = null!;

        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string? Email { get; set; }

        public string? Address { get; set; }

        public string? Gender { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public string? Remarks { get; set; }

        [Display(Name = "Reminder Date")]
        [DataType(DataType.Date)]
        public DateTime? ReminderDate { get; set; }
        
        [Display(Name = "Reminder Frequency")]
        public string? ReminderFrequency { get; set; }
        
        [Display(Name = "Reminder Note")]
        public string? ReminderNote { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
