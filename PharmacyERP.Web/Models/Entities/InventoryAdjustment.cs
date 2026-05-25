namespace PharmacyERP.Web.Models.Entities
{
    public class InventoryAdjustment : BaseEntity
    {
        public string MedicineId { get; set; } = null!;
        public string BatchId { get; set; } = null!;
        public int AdjustedQty { get; set; }
        public string Reason { get; set; } = null!; // Damage, Stock Correction, etc.
        public DateTime AdjustmentDate { get; set; } = DateTime.UtcNow;
    }
}
