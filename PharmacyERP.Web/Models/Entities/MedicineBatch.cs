namespace PharmacyERP.Web.Models.Entities
{
    public class MedicineBatch : BaseEntity
    {
        public string MedicineId { get; set; } = null!;
        public string BatchNo { get; set; } = null!;
        public DateTime ExpiryDate { get; set; }
        
        public decimal PurchaseRate { get; set; }
        public decimal SaleRate { get; set; }
        public decimal MRP { get; set; }
        
        public int CurrentQty { get; set; } // Current stock in this batch
        public bool IsActive { get; set; } = true;
    }
}
