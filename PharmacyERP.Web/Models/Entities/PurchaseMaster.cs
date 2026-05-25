namespace PharmacyERP.Web.Models.Entities
{
    public class PurchaseMaster : BaseEntity
    {
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        public string SupplierId { get; set; } = null!;
        public string InvoiceNo { get; set; } = null!;
        
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal OtherDiscount { get; set; }
        public decimal TotalAmount { get; set; }
        
        public string? PaymentMode { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }
    }
}
