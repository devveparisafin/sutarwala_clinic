using System.ComponentModel.DataAnnotations;

namespace PharmacyERP.Web.Models.ViewModels
{
    public class SalesEntryViewModel
    {
        public string? CustomerName { get; set; } = "Walk-in Customer";
        public string? CustomerPhone { get; set; }
        
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        
        public string PaymentMode { get; set; } = "Cash";
        public string? TransactionId { get; set; }
        public string? TransactionGuid { get; set; }
        
        public List<SaleItemViewModel> Items { get; set; } = new();
    }

    public class SaleItemViewModel
    {
        public string MedicineId { get; set; } = null!;
        public string MedicineName { get; set; } = null!;
        public string BatchId { get; set; } = null!;
        public string BatchNo { get; set; } = null!;
        
        public int Qty { get; set; }
        public bool IsLoose { get; set; }
        public decimal Rate { get; set; }
        public decimal MRP { get; set; }
        public decimal GST { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
