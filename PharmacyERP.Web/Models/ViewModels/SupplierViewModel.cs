using System.ComponentModel.DataAnnotations;

namespace PharmacyERP.Web.Models.ViewModels
{
    public class SupplierViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Supplier Name is required")]
        public string Name { get; set; } = null!;

        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }

        [Phone]
        public string? Phone { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? Address { get; set; }

        [Display(Name = "GSTIN Number")]
        public string? GSTIN { get; set; }

        [Display(Name = "Opening Balance")]
        public decimal OpeningBalance { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
