namespace PharmacyERP.Web.Models.Entities
{
    public class Payment : BaseEntity
    {
        public string SaleId { get; set; } = null!;
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; } = "Cash"; // Cash, Card, UPI
        public string? TransactionId { get; set; } // For Card/UPI
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public string? Remarks { get; set; }
    }
}
