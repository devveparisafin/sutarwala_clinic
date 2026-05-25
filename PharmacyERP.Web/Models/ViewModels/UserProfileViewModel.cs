namespace PharmacyERP.Web.Models.ViewModels
{
    public class UserProfileViewModel
    {
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string RoleName { get; set; } = null!;
        public DateTime? LastLogin { get; set; }
    }
}
