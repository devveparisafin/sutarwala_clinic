using System.ComponentModel.DataAnnotations;

namespace PharmacyERP.Web.Models.ViewModels
{
    public class PurchaseSummaryViewModel
    {
        public string Id { get; set; } = null!;
        public string InvoiceNo { get; set; } = null!;
        public DateTime PurchaseDate { get; set; }
        public string SupplierName { get; set; } = null!;
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class PurchaseEntryViewModel
    {
        [Required]
        [Display(Name = "Purchase Date")]
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Supplier is required")]
        [Display(Name = "Supplier")]
        public string SupplierId { get; set; } = null!;

        [Required(ErrorMessage = "Invoice No is required")]
        [Display(Name = "Invoice Number")]
        public string InvoiceNo { get; set; } = null!;

        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal OtherDiscount { get; set; }
        public decimal TotalAmount { get; set; }

        public string? PaymentMode { get; set; }
        public string? Remarks { get; set; }
        public string? TransactionGuid { get; set; }

        public List<PurchaseItemViewModel> Items { get; set; } = new();
    }

    public class PurchaseItemViewModel
    {
        public string MedicineId { get; set; } = null!;
        public string MedicineName { get; set; } = null!;
        
        public string BatchNo { get; set; } = null!;
        public DateTime ExpiryDate { get; set; }
        
        public int Qty { get; set; }
        public int FreeQty { get; set; }
        public int UnitsPerStrip { get; set; } = 1;
        
        public decimal PurchaseRate { get; set; }
        public decimal SaleRate { get; set; }
        public decimal MRP { get; set; }
        
        public string DiscountType { get; set; } = "Percentage";
        public decimal DiscountValue { get; set; }
        public decimal DiscountAmount { get; set; }
        
        public decimal GST { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
