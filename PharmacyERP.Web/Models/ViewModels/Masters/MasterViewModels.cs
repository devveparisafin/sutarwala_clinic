using System.ComponentModel.DataAnnotations;

namespace PharmacyERP.Web.Models.ViewModels.Masters
{
    public class MedicineCategoryViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Category Name is required")]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class MedicineUnitViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Unit Name is required")]
        [StringLength(50)]
        public string Name { get; set; } = null!;

        public bool IsActive { get; set; } = true;
    }

    public class ManufacturerViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Manufacturer Name is required")]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? Phone { get; set; }

        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class GenericMedicineViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Generic Name is required")]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class RackViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Rack Name is required")]
        [StringLength(50)]
        public string Name { get; set; } = null!;

        public string? Location { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
