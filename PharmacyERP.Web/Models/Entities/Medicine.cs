namespace PharmacyERP.Web.Models.Entities
{
    public class Medicine : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string GenericId { get; set; } = null!;
        public string ManufacturerId { get; set; } = null!;
        public string CategoryId { get; set; } = null!;
        public string UnitId { get; set; } = null!;
        
        public string? Barcode { get; set; }
        public string? HSNCode { get; set; }
        public decimal GST { get; set; } // Percentage e.g. 12.0
        public string? Description { get; set; }
        public string? ImagePath { get; set; }
        public string? RackId { get; set; }
        public string? RackLocation { get; set; }
        
        public int LowStockThreshold { get; set; }
        
        public bool IsLooseSale { get; set; }
        public int UnitsPerStrip { get; set; } = 1;
        public string? LooseUnitName { get; set; }
        public string? StripName { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
