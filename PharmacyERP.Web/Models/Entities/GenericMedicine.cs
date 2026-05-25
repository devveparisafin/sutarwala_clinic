namespace PharmacyERP.Web.Models.Entities
{
    public class GenericMedicine : BaseEntity
    {
        public string Name { get; set; } = null!; // e.g. Paracetamol
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
