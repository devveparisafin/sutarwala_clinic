using Serilog;
using PharmacyERP.Web.Configurations;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Common;
using PharmacyERP.Web.Repositories;
using PharmacyERP.Web.Services;
using PharmacyERP.Web.Middleware;
using FluentValidation.AspNetCore;
using FluentValidation;
using System.Reflection;
using PharmacyERP.Web.Models.Entities;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Set default culture to India (Rupees symbol, Indian date format)
var cultureInfo = new CultureInfo("en-IN");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// 1. Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Add MongoDB Settings
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

// 3. Register Core Services (DI)
builder.Services.AddSingleton<IMongoDbContext, MongoDbContext>();
// Core Data Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBaseRepository<Role>, BaseRepository<Role>>();
builder.Services.AddScoped<IBaseRepository<MedicineCategory>, BaseRepository<MedicineCategory>>();
builder.Services.AddScoped<IBaseRepository<Manufacturer>, BaseRepository<Manufacturer>>();
builder.Services.AddScoped<IBaseRepository<MedicineUnit>, BaseRepository<MedicineUnit>>();
builder.Services.AddScoped<IBaseRepository<GenericMedicine>, BaseRepository<GenericMedicine>>();
builder.Services.AddScoped<IBaseRepository<Medicine>, BaseRepository<Medicine>>();
builder.Services.AddScoped<IBaseRepository<Sale>, BaseRepository<Sale>>();
builder.Services.AddScoped<IBaseRepository<Purchase>, BaseRepository<Purchase>>();
builder.Services.AddScoped<IBaseRepository<Supplier>, BaseRepository<Supplier>>();
builder.Services.AddScoped<IBaseRepository<SupplierPayment>, BaseRepository<SupplierPayment>>();
builder.Services.AddScoped<IBaseRepository<MedicineBatch>, BaseRepository<MedicineBatch>>();
builder.Services.AddScoped<IBaseRepository<StockTransaction>, BaseRepository<StockTransaction>>();
builder.Services.AddScoped<IBaseRepository<InventoryAdjustment>, BaseRepository<InventoryAdjustment>>();
builder.Services.AddScoped<IBaseRepository<PurchaseMaster>, BaseRepository<PurchaseMaster>>();
builder.Services.AddScoped<IBaseRepository<PurchaseDetail>, BaseRepository<PurchaseDetail>>();
builder.Services.AddScoped<IBaseRepository<SaleDetail>, BaseRepository<SaleDetail>>();
builder.Services.AddScoped<IBaseRepository<Payment>, BaseRepository<Payment>>();
builder.Services.AddScoped<IBaseRepository<Customer>, BaseRepository<Customer>>();
builder.Services.AddScoped<IBaseRepository<CustomerPayment>, BaseRepository<CustomerPayment>>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerPrescriptionRepository, CustomerPrescriptionRepository>();
builder.Services.AddScoped<ISettingRepository, SettingRepository>();
builder.Services.AddScoped<IMedicineCategoryRepository, MedicineCategoryRepository>();
builder.Services.AddScoped<IMedicineUnitRepository, MedicineUnitRepository>();
builder.Services.AddScoped<IManufacturerRepository, ManufacturerRepository>();
builder.Services.AddScoped<IGenericMedicineRepository, GenericMedicineRepository>();
builder.Services.AddScoped<IBaseRepository<Rack>, BaseRepository<Rack>>();
builder.Services.AddScoped<IRackRepository, RackRepository>();
// Core Business Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IMedicineService, MedicineService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IPurchaseService, PurchaseService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IMedicineCategoryService, MedicineCategoryService>();
builder.Services.AddScoped<IMedicineUnitService, MedicineUnitService>();
builder.Services.AddScoped<IManufacturerService, ManufacturerService>();
builder.Services.AddScoped<IGenericMedicineService, GenericMedicineService>();
builder.Services.AddScoped<IRackService, RackService>();

// 4. Add AutoMapper
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

// 5. Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// 6. Add Session Support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 7. Add Authentication & Authorization
builder.Services.AddAuthentication("PharmacyAuth")
    .AddCookie("PharmacyAuth", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
    });

// 8. Add MVC with NewtonsoftJson
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbSeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 9. Custom Exception Middleware
app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 10. Use Session & Authentication
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

try
{
    Log.Information("Starting Pharmacy ERP Web Application...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
