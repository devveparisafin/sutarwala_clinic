namespace PharmacyERP.Web.Models.Entities
{
    public class Sale : BaseEntity
    {
        public string InvoiceNo { get; set; } = null!;
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;
        public string CustomerName { get; set; } = "Walk-in Customer";
        public string? CustomerPhone { get; set; }
        
        [MongoDB.Bson.Serialization.Attributes.BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string? CustomerId { get; set; }
        
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        
        public string PaymentMode { get; set; } = "Cash"; // Cash, Card, UPI, Mixed
        public string Status { get; set; } = "Paid"; // Paid, Hold, Cancelled
        
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }
        public string? TransactionGuid { get; set; }
    }
}
