using PharmacyERP.Web.Common;
using PharmacyERP.Web.Helpers;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Repositories;

namespace PharmacyERP.Web.Common
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<IMongoDbContext>();
            var userRepository = serviceProvider.GetRequiredService<IUserRepository>();
            var roleRepository = serviceProvider.GetRequiredService<IBaseRepository<Role>>();

            // 1. Seed Roles
            var roles = new List<string> { AppConstants.Roles.Admin, AppConstants.Roles.Pharmacist, AppConstants.Roles.Cashier };
            foreach (var roleName in roles)
            {
                var existingRole = await roleRepository.FindAsync(x => x.Name == roleName);
                if (!existingRole.Any())
                {
                    await roleRepository.CreateAsync(new Role { Name = roleName, Description = $"{roleName} role" });
                }
            }

            // 2. Seed Admin User
            var adminRole = (await roleRepository.FindAsync(x => x.Name == AppConstants.Roles.Admin)).FirstOrDefault();
            if (adminRole != null)
            {
                var existingAdmin = await userRepository.GetByUsernameAsync("admin");
                if (existingAdmin == null)
                {
                    var adminUser = new User
                    {
                        Username = "admin",
                        Email = "admin@pharmacy.com",
                        FullName = "System Administrator",
                        PasswordHash = PasswordHasher.HashPassword("Admin@123"),
                        RoleId = adminRole.Id!,
                        IsActive = true
                    };
                    await userRepository.CreateAsync(adminUser);
                }
            }

            // 3. Seed Master Data
            var categoryRepo = serviceProvider.GetRequiredService<IBaseRepository<MedicineCategory>>();
            var manufacturerRepo = serviceProvider.GetRequiredService<IBaseRepository<Manufacturer>>();
            var unitRepo = serviceProvider.GetRequiredService<IBaseRepository<MedicineUnit>>();
            var genericRepo = serviceProvider.GetRequiredService<IBaseRepository<GenericMedicine>>();

            if (!(await categoryRepo.GetAllAsync()).Any())
            {
                await categoryRepo.CreateAsync(new MedicineCategory { Name = "Tablets" });
                await categoryRepo.CreateAsync(new MedicineCategory { Name = "Syrups" });
            }

            if (!(await manufacturerRepo.GetAllAsync()).Any())
            {
                await manufacturerRepo.CreateAsync(new Manufacturer { Name = "Pfizer" });
                await manufacturerRepo.CreateAsync(new Manufacturer { Name = "Novartis" });
            }

            if (!(await unitRepo.GetAllAsync()).Any())
            {
                await unitRepo.CreateAsync(new MedicineUnit { Name = "Strip" });
                await unitRepo.CreateAsync(new MedicineUnit { Name = "Bottle" });
            }

            if (!(await genericRepo.GetAllAsync()).Any())
            {
                await genericRepo.CreateAsync(new GenericMedicine { Name = "Paracetamol" });
                await genericRepo.CreateAsync(new GenericMedicine { Name = "Amoxicillin" });
            }
        }
    }
}
