using PharmacyERP.Web.Common;

namespace PharmacyERP.Web.Models.Entities
{
    public class StockTransaction : BaseEntity
    {
        public string MedicineId { get; set; } = null!;
        public string BatchId { get; set; } = null!;
        public TransactionType Type { get; set; }
        public int Quantity { get; set; } // Positive for inward, negative for outward
        public string? ReferenceId { get; set; } // Invoice ID or Purchase ID
        public string? Remarks { get; set; }
        public string? UserId { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    }
}
