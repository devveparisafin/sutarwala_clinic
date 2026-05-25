using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PharmacyERP.Web.Models.ViewModels
{
    public class MedicineViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Medicine Name is required")]
        [Display(Name = "Medicine Name")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Generic Name is required")]
        [Display(Name = "Generic Name")]
        public string GenericId { get; set; } = null!;

        [Required(ErrorMessage = "Manufacturer is required")]
        [Display(Name = "Manufacturer")]
        public string ManufacturerId { get; set; } = null!;

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public string CategoryId { get; set; } = null!;

        [Required(ErrorMessage = "Unit is required")]
        [Display(Name = "Unit")]
        public string UnitId { get; set; } = null!;

        public string? Barcode { get; set; }
        
        [Display(Name = "HSN Code")]
        public string? HSNCode { get; set; }
        
        public decimal GST { get; set; }
        
        public string? Description { get; set; }
        
        [Display(Name = "Medicine Image")]
        public IFormFile? ImageFile { get; set; }
        public string? ExistingImagePath { get; set; }
        
        [Display(Name = "Rack Location")]
        public string? RackId { get; set; }
        
        [Display(Name = "Specific Location")]
        public string? RackLocation { get; set; }
        
        [Required]
        [Display(Name = "Low Stock Threshold")]
        public int LowStockThreshold { get; set; } = 10;

        [Display(Name = "Support Loose Sale?")]
        public bool IsLooseSale { get; set; }

        [Display(Name = "Units Per Strip")]
        [Range(1, 1000, ErrorMessage = "Units per strip must be at least 1")]
        public int UnitsPerStrip { get; set; } = 1;

        [Display(Name = "Loose Unit Name")]
        public string? LooseUnitName { get; set; }

        [Display(Name = "Strip Name")]
        public string? StripName { get; set; }

        public bool IsActive { get; set; } = true;

        // SelectLists for Dropdowns
        public IEnumerable<SelectListItem>? Categories { get; set; }
        public IEnumerable<SelectListItem>? Manufacturers { get; set; }
        public IEnumerable<SelectListItem>? Units { get; set; }
        public IEnumerable<SelectListItem>? Generics { get; set; }
        public IEnumerable<SelectListItem>? Racks { get; set; }
    }
}
