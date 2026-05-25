using PharmacyERP.Web.Models.Entities;

namespace PharmacyERP.Web.Models.ViewModels
{
    public class CustomerHistoryViewModel
    {
        public CustomerViewModel Customer { get; set; } = new();
        public List<CustomerPrescriptionViewModel> Prescriptions { get; set; } = new();
        public List<Sale> PurchaseHistory { get; set; } = new();
    }
}
