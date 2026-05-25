namespace PharmacyERP.Web.Models.Entities
{
    public class Purchase : BaseEntity
    {
        public string SupplierName { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    }
}
