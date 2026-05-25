namespace PharmacyERP.Web.Models.Entities
{
    public class Manufacturer : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
