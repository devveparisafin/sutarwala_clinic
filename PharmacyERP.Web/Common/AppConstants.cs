namespace PharmacyERP.Web.Common
{
    public static class AppConstants
    {
        public static class Roles
        {
            public const string Admin = "Admin";
            public const string Manager = "Manager";
            public const string Pharmacist = "Pharmacist";
            public const string Cashier = "Cashier";
        }

        public static class SessionKeys
        {
            public const string UserId = "UserId";
            public const string UserName = "UserName";
            public const string UserRole = "UserRole";
        }
    }
}
