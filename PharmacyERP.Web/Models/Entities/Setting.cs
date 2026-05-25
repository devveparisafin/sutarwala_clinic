namespace PharmacyERP.Web.Models.Entities
{
    public class StoreSettings
    {
        public string StoreName { get; set; } = "Pharmacy ERP";
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? LogoPath { get; set; }
    }

    public class GstSettings
    {
        public decimal DefaultGstPercentage { get; set; } = 18m;
        public string? GstInNumber { get; set; }
    }

    public class InvoiceSettings
    {
        public string InvoicePrefix { get; set; } = "INV-";
        public string? FooterText { get; set; }
        public string? TermsAndConditions { get; set; }
    }

    public class PrinterSettings
    {
        public string PaperSize { get; set; } = "80mm"; // 58mm or 80mm
        public bool PrintLogo { get; set; } = true;
        public string? PrinterName { get; set; }
    }

    public class BackupSettings
    {
        public bool AutoBackupEnabled { get; set; } = false;
        public string BackupPath { get; set; } = "C:\\PharmacyBackups";
        public string MongoDbConnectionString { get; set; } = "mongodb://localhost:27017";
        public string DatabaseName { get; set; } = "PharmacyERP";
    }

    public class Setting : BaseEntity
    {
        public string AppVersion { get; set; } = "1.0.0";
        public StoreSettings Store { get; set; } = new();
        public GstSettings Gst { get; set; } = new();
        public InvoiceSettings Invoice { get; set; } = new();
        public PrinterSettings Printer { get; set; } = new();
        public BackupSettings Backup { get; set; } = new();
    }
}
