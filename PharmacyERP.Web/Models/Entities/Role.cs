namespace PharmacyERP.Web.Models.Entities
{
    public class Role : BaseEntity
    {
        public string Name { get; set; } = null!; // Admin, Pharmacist, Cashier
        public string? Description { get; set; }
        public List<string> PermissionIds { get; set; } = new();
    }
}
