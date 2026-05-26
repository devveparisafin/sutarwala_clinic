using MongoDB.Driver;
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

            // 0. Ensure MongoDB Unique and Search Indexes
            try
            {
                var database = context.Database;

                // 0. Unique and Search indexes for Sales
                var salesCollection = database.GetCollection<Sale>("sales");
                await salesCollection.Indexes.CreateOneAsync(new CreateIndexModel<Sale>(Builders<Sale>.IndexKeys.Ascending(x => x.InvoiceNo), new CreateIndexOptions { Unique = true }));
                await salesCollection.Indexes.CreateOneAsync(new CreateIndexModel<Sale>(Builders<Sale>.IndexKeys.Ascending(x => x.TransactionGuid), new CreateIndexOptions { Unique = true }));
                await salesCollection.Indexes.CreateOneAsync(new CreateIndexModel<Sale>(Builders<Sale>.IndexKeys.Descending(x => x.SaleDate)));

                // Unique and Search indexes for Purchases
                var purchaseCollection = database.GetCollection<PurchaseMaster>("purchasemasters");
                await purchaseCollection.Indexes.CreateOneAsync(new CreateIndexModel<PurchaseMaster>(Builders<PurchaseMaster>.IndexKeys.Combine(Builders<PurchaseMaster>.IndexKeys.Ascending(x => x.SupplierId), Builders<PurchaseMaster>.IndexKeys.Ascending(x => x.InvoiceNo)), new CreateIndexOptions { Unique = true }));
                await purchaseCollection.Indexes.CreateOneAsync(new CreateIndexModel<PurchaseMaster>(Builders<PurchaseMaster>.IndexKeys.Ascending(x => x.TransactionGuid), new CreateIndexOptions { Unique = true }));
                await purchaseCollection.Indexes.CreateOneAsync(new CreateIndexModel<PurchaseMaster>(Builders<PurchaseMaster>.IndexKeys.Descending(x => x.PurchaseDate)));

                // Optimized search indexes for Medicines
                var medicineCollection = database.GetCollection<Medicine>("medicines");
                await medicineCollection.Indexes.CreateOneAsync(new CreateIndexModel<Medicine>(Builders<Medicine>.IndexKeys.Ascending(x => x.Name)));
                await medicineCollection.Indexes.CreateOneAsync(new CreateIndexModel<Medicine>(Builders<Medicine>.IndexKeys.Ascending(x => x.Barcode)));
                await medicineCollection.Indexes.CreateOneAsync(new CreateIndexModel<Medicine>(Builders<Medicine>.IndexKeys.Ascending(x => x.GenericId)));
                await medicineCollection.Indexes.CreateOneAsync(new CreateIndexModel<Medicine>(Builders<Medicine>.IndexKeys.Ascending(x => x.ManufacturerId)));
                await medicineCollection.Indexes.CreateOneAsync(new CreateIndexModel<Medicine>(Builders<Medicine>.IndexKeys.Ascending(x => x.IsActive)));
                await medicineCollection.Indexes.CreateOneAsync(new CreateIndexModel<Medicine>(Builders<Medicine>.IndexKeys.Descending(x => x.CreatedAt)));

                // Search indexes for Supplier & Customer
                var supplierCollection = database.GetCollection<Supplier>("suppliers");
                await supplierCollection.Indexes.CreateOneAsync(new CreateIndexModel<Supplier>(Builders<Supplier>.IndexKeys.Ascending(x => x.Name)));
                await supplierCollection.Indexes.CreateOneAsync(new CreateIndexModel<Supplier>(Builders<Supplier>.IndexKeys.Ascending(x => x.Phone)));

                var customerCollection = database.GetCollection<Customer>("customers");
                await customerCollection.Indexes.CreateOneAsync(new CreateIndexModel<Customer>(Builders<Customer>.IndexKeys.Ascending(x => x.Name)));
                await customerCollection.Indexes.CreateOneAsync(new CreateIndexModel<Customer>(Builders<Customer>.IndexKeys.Ascending(x => x.MobileNumber)));

                // Generic, Manufacturer, Category, Unit, Rack
                var genericCollection = database.GetCollection<GenericMedicine>("genericmedicines");
                await genericCollection.Indexes.CreateOneAsync(new CreateIndexModel<GenericMedicine>(Builders<GenericMedicine>.IndexKeys.Ascending(x => x.Name)));

                var manufacturerCollection = database.GetCollection<Manufacturer>("manufacturers");
                await manufacturerCollection.Indexes.CreateOneAsync(new CreateIndexModel<Manufacturer>(Builders<Manufacturer>.IndexKeys.Ascending(x => x.Name)));

                var categoryCollection = database.GetCollection<MedicineCategory>("medicinecategorys");
                await categoryCollection.Indexes.CreateOneAsync(new CreateIndexModel<MedicineCategory>(Builders<MedicineCategory>.IndexKeys.Ascending(x => x.Name)));

                var unitCollection = database.GetCollection<MedicineUnit>("medicineunits");
                await unitCollection.Indexes.CreateOneAsync(new CreateIndexModel<MedicineUnit>(Builders<MedicineUnit>.IndexKeys.Ascending(x => x.Name)));

                var rackCollection = database.GetCollection<Rack>("racks");
                await rackCollection.Indexes.CreateOneAsync(new CreateIndexModel<Rack>(Builders<Rack>.IndexKeys.Ascending(x => x.Name)));

                // Compound stock batch indexes
                var batchCollection = database.GetCollection<MedicineBatch>("medicinebatchs");
                await batchCollection.Indexes.CreateOneAsync(new CreateIndexModel<MedicineBatch>(Builders<MedicineBatch>.IndexKeys.Combine(
                    Builders<MedicineBatch>.IndexKeys.Ascending(x => x.MedicineId),
                    Builders<MedicineBatch>.IndexKeys.Ascending(x => x.IsActive),
                    Builders<MedicineBatch>.IndexKeys.Ascending(x => x.IsDeleted)
                )));
                await batchCollection.Indexes.CreateOneAsync(new CreateIndexModel<MedicineBatch>(Builders<MedicineBatch>.IndexKeys.Combine(
                    Builders<MedicineBatch>.IndexKeys.Ascending(x => x.MedicineId),
                    Builders<MedicineBatch>.IndexKeys.Ascending(x => x.ExpiryDate)
                )));

                // Transactions
                var transCollection = database.GetCollection<StockTransaction>("stocktransactions");
                await transCollection.Indexes.CreateOneAsync(new CreateIndexModel<StockTransaction>(Builders<StockTransaction>.IndexKeys.Ascending(x => x.MedicineId)));
                await transCollection.Indexes.CreateOneAsync(new CreateIndexModel<StockTransaction>(Builders<StockTransaction>.IndexKeys.Ascending(x => x.BatchId)));
                await transCollection.Indexes.CreateOneAsync(new CreateIndexModel<StockTransaction>(Builders<StockTransaction>.IndexKeys.Ascending(x => x.ReferenceId)));
            }
            catch (Exception)
            {
                // Soft warning, do not block startup if pre-existing duplicate seed data is found
            }
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
