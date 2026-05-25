namespace PharmacyERP.Web.Models.Entities
{
    public class Rack : BaseEntity
    {
        public string Name { get; set; } = null!; // e.g. Rack A, Shelf 1
        public string? Location { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
