using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PharmacyERP.Web.Models.ViewModels
{
    public class CustomerPrescriptionViewModel
    {
        public string? Id { get; set; }

        [Required]
        public string CustomerId { get; set; } = null!;

        [Display(Name = "Doctor Name")]
        public string? DoctorName { get; set; }

        public DateTime PrescriptionDate { get; set; } = DateTime.UtcNow;

        public string? ImagePath { get; set; }

        [Display(Name = "Upload Prescription")]
        public IFormFile? ImageFile { get; set; }

        public string? Remarks { get; set; }
    }
}
