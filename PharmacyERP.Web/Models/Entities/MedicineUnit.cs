namespace PharmacyERP.Web.Models.Entities
{
    public class MedicineUnit : BaseEntity
    {
        public string Name { get; set; } = null!; // Tablet, Strip, Bottle, ml, mg
        public bool IsActive { get; set; } = true;
    }
}
