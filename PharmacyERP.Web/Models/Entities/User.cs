namespace PharmacyERP.Web.Models.Entities
{
    public class User : BaseEntity
    {
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string RoleId { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public DateTime? LastLogin { get; set; }
    }
}
