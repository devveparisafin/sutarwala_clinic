namespace PharmacyERP.Web.Models.Entities
{
    public class Supplier : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? GSTIN { get; set; }
        
        public decimal OpeningBalance { get; set; }
        public decimal CurrentBalance { get; set; } // Positive means we owe them
        
        public bool IsActive { get; set; } = true;
    }
}
