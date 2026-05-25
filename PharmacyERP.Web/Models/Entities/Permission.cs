namespace PharmacyERP.Web.Models.Entities
{
    public class Permission : BaseEntity
    {
        public string Name { get; set; } = null!; // e.g., "ManageUsers", "DispenseMedicine"
        public string? Description { get; set; }
        public string Module { get; set; } = null!; // e.g., "Authentication", "Inventory"
    }
}
