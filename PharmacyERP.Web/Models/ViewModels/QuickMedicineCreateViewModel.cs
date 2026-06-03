using System.ComponentModel.DataAnnotations;

namespace PharmacyERP.Web.Models.ViewModels
{
    public class QuickMedicineCreateViewModel
    {
        [Required(ErrorMessage = "Medicine Name is required")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Generic Name is required")]
        public string GenericId { get; set; } = null!;

        [Required(ErrorMessage = "Manufacturer is required")]
        public string ManufacturerId { get; set; } = null!;

        [Required(ErrorMessage = "Category is required")]
        public string CategoryId { get; set; } = null!;

        [Required(ErrorMessage = "Unit is required")]
        public string UnitId { get; set; } = null!;

        public string? RackId { get; set; }

        public decimal GST { get; set; }

        public int UnitsPerStrip { get; set; } = 1;

        public bool IsLooseSale { get; set; }

        public string? LooseUnitName { get; set; }
    }
}
