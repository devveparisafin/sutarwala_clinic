using System.ComponentModel.DataAnnotations;

namespace PharmacyERP.Web.Models.ViewModels.Masters
{
    public class QuickAddManufacturerViewModel
    {
        [Required(ErrorMessage = "Manufacturer Name is required")]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = null!;
        public string? Phone { get; set; }
    }

    public class QuickAddSupplierViewModel
    {
        [Required(ErrorMessage = "Supplier Name is required")]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class QuickAddGenericMedicineViewModel
    {
        [Required(ErrorMessage = "Generic Name is required")]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class QuickAddCategoryViewModel
    {
        [Required(ErrorMessage = "Category Name is required")]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }

    public class QuickAddUnitViewModel
    {
        [Required(ErrorMessage = "Unit Name is required")]
        [StringLength(50, MinimumLength = 1)]
        public string Name { get; set; } = null!;
    }

    public class QuickAddRackViewModel
    {
        [Required(ErrorMessage = "Rack Name is required")]
        [StringLength(50, MinimumLength = 1)]
        public string Name { get; set; } = null!;
    }

    public class QuickAddCustomerViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Mobile Number is required")]
        [RegularExpression(@"^\d{10,15}$", ErrorMessage = "Invalid Mobile Number")]
        public string Phone { get; set; } = null!;

        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string? Email { get; set; }
    }
}
