namespace PharmacyERP.Web.Models.Entities
{
    public class MedicineCategory : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
