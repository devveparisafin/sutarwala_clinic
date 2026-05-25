namespace PharmacyERP.Web.Models.Entities
{
    public class SupplierPayment : BaseEntity
    {
        public string SupplierId { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public string? PaymentMode { get; set; } // Cash, Cheque, Online
        public string? ReferenceNo { get; set; }
        public string? Remarks { get; set; }
    }
}
