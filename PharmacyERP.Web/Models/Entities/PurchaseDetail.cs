namespace PharmacyERP.Web.Models.Entities
{
    public class PurchaseDetail : BaseEntity
    {
        public string PurchaseMasterId { get; set; } = null!;
        public string MedicineId { get; set; } = null!;
        
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
        
        public decimal GST { get; set; } // Percentage
        
        public decimal TotalPrice { get; set; } // ((Qty * Rate) - Discount) + Tax
        
        public int ReturnedQty { get; set; } = 0;
    }
}
