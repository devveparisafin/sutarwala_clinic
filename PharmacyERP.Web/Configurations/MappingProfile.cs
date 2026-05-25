using AutoMapper;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Models.ViewModels;
using PharmacyERP.Web.Models.ViewModels.Masters;

namespace PharmacyERP.Web.Configurations
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Customer Mappings
            CreateMap<Customer, CustomerViewModel>().ReverseMap();
            CreateMap<CustomerPrescription, CustomerPrescriptionViewModel>().ReverseMap();
            // Settings Mapping
            CreateMap<Setting, SettingsViewModel>()
                .ForMember(dest => dest.StoreName, opt => opt.MapFrom(src => src.Store.StoreName))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Store.Address))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Store.Phone))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Store.Email))
                .ForMember(dest => dest.LogoPath, opt => opt.MapFrom(src => src.Store.LogoPath))
                
                .ForMember(dest => dest.DefaultGstPercentage, opt => opt.MapFrom(src => src.Gst.DefaultGstPercentage))
                .ForMember(dest => dest.GstInNumber, opt => opt.MapFrom(src => src.Gst.GstInNumber))
                
                .ForMember(dest => dest.InvoicePrefix, opt => opt.MapFrom(src => src.Invoice.InvoicePrefix))
                .ForMember(dest => dest.FooterText, opt => opt.MapFrom(src => src.Invoice.FooterText))
                .ForMember(dest => dest.TermsAndConditions, opt => opt.MapFrom(src => src.Invoice.TermsAndConditions))
                
                .ForMember(dest => dest.PaperSize, opt => opt.MapFrom(src => src.Printer.PaperSize))
                .ForMember(dest => dest.PrintLogo, opt => opt.MapFrom(src => src.Printer.PrintLogo))
                .ForMember(dest => dest.PrinterName, opt => opt.MapFrom(src => src.Printer.PrinterName))
                
                .ForMember(dest => dest.AutoBackupEnabled, opt => opt.MapFrom(src => src.Backup.AutoBackupEnabled))
                .ForMember(dest => dest.BackupPath, opt => opt.MapFrom(src => src.Backup.BackupPath))
                .ForMember(dest => dest.MongoDbConnectionString, opt => opt.MapFrom(src => src.Backup.MongoDbConnectionString))
                .ForMember(dest => dest.DatabaseName, opt => opt.MapFrom(src => src.Backup.DatabaseName))
                .ReverseMap()
                .ForPath(dest => dest.Store.StoreName, opt => opt.MapFrom(src => src.StoreName))
                .ForPath(dest => dest.Store.Address, opt => opt.MapFrom(src => src.Address))
                .ForPath(dest => dest.Store.Phone, opt => opt.MapFrom(src => src.Phone))
                .ForPath(dest => dest.Store.Email, opt => opt.MapFrom(src => src.Email))
                .ForPath(dest => dest.Store.LogoPath, opt => opt.MapFrom(src => src.LogoPath))
                
                .ForPath(dest => dest.Gst.DefaultGstPercentage, opt => opt.MapFrom(src => src.DefaultGstPercentage))
                .ForPath(dest => dest.Gst.GstInNumber, opt => opt.MapFrom(src => src.GstInNumber))
                
                .ForPath(dest => dest.Invoice.InvoicePrefix, opt => opt.MapFrom(src => src.InvoicePrefix))
                .ForPath(dest => dest.Invoice.FooterText, opt => opt.MapFrom(src => src.FooterText))
                .ForPath(dest => dest.Invoice.TermsAndConditions, opt => opt.MapFrom(src => src.TermsAndConditions))
                
                .ForPath(dest => dest.Printer.PaperSize, opt => opt.MapFrom(src => src.PaperSize))
                .ForPath(dest => dest.Printer.PrintLogo, opt => opt.MapFrom(src => src.PrintLogo))
                .ForPath(dest => dest.Printer.PrinterName, opt => opt.MapFrom(src => src.PrinterName))
                
                .ForPath(dest => dest.Backup.AutoBackupEnabled, opt => opt.MapFrom(src => src.AutoBackupEnabled))
                .ForPath(dest => dest.Backup.BackupPath, opt => opt.MapFrom(src => src.BackupPath))
                .ForPath(dest => dest.Backup.MongoDbConnectionString, opt => opt.MapFrom(src => src.MongoDbConnectionString))
                .ForPath(dest => dest.Backup.DatabaseName, opt => opt.MapFrom(src => src.DatabaseName));

            // Master Mappings
            CreateMap<MedicineCategory, MedicineCategoryViewModel>().ReverseMap();
            CreateMap<MedicineUnit, MedicineUnitViewModel>().ReverseMap();
            CreateMap<Manufacturer, ManufacturerViewModel>().ReverseMap();
            CreateMap<GenericMedicine, GenericMedicineViewModel>().ReverseMap();
            CreateMap<Rack, RackViewModel>().ReverseMap();
        }
    }
}
