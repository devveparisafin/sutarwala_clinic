namespace PharmacyERP.Web.Models.ViewModels
{
    public class MedicineListViewModel
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string GenericName { get; set; } = null!;
        public string ManufacturerName { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public string UnitName { get; set; } = null!;
        public string? Barcode { get; set; }
        public int StockQuantity { get; set; }
        public string? RackName { get; set; }
        public string? RackLocation { get; set; }
        public bool IsActive { get; set; }
        public string? ImagePath { get; set; }

        public bool IsLooseSale { get; set; }
        public int UnitsPerStrip { get; set; }
        public string? LooseUnitName { get; set; }
        public string? StripName { get; set; }
    }
}
