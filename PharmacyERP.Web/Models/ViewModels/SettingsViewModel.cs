using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PharmacyERP.Web.Models.ViewModels
{
    public class SettingsViewModel
    {
        public string? Id { get; set; }

        // Store Settings
        [Required(ErrorMessage = "Store Name is required")]
        [Display(Name = "Store Name")]
        public string StoreName { get; set; } = null!;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        
        [EmailAddress(ErrorMessage = "Invalid Email")]
        public string? Email { get; set; }
        
        public string? LogoPath { get; set; }
        public IFormFile? LogoFile { get; set; }

        // GST Settings
        [Display(Name = "Default GST Percentage")]
        [Range(0, 100, ErrorMessage = "GST must be between 0 and 100")]
        public decimal DefaultGstPercentage { get; set; }
        
        [Display(Name = "GSTIN Number")]
        public string? GstInNumber { get; set; }

        // Invoice Settings
        [Display(Name = "Invoice Prefix")]
        public string InvoicePrefix { get; set; } = "INV-";
        
        [Display(Name = "Footer Text")]
        public string? FooterText { get; set; }
        
        [Display(Name = "Terms & Conditions")]
        public string? TermsAndConditions { get; set; }

        // Printer Settings
        [Display(Name = "Paper Size")]
        public string PaperSize { get; set; } = "80mm";
        
        [Display(Name = "Print Logo on Invoice")]
        public bool PrintLogo { get; set; }
        
        [Display(Name = "Target Printer Name (Optional)")]
        public string? PrinterName { get; set; }

        // Backup Settings
        [Display(Name = "Enable Automated Backups")]
        public bool AutoBackupEnabled { get; set; }
        
        [Display(Name = "Backup Path")]
        [Required(ErrorMessage = "Backup Path is required")]
        public string BackupPath { get; set; } = null!;

        [Display(Name = "MongoDB Connection String")]
        public string MongoDbConnectionString { get; set; } = "mongodb://localhost:27017";

        [Display(Name = "Database Name")]
        public string DatabaseName { get; set; } = "PharmacyERP";
    }
}
