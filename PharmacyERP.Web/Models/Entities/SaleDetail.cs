namespace PharmacyERP.Web.Models.Entities
{
    public class SaleDetail : BaseEntity
    {
        public string SaleId { get; set; } = null!;
        public string MedicineId { get; set; } = null!;
        public string BatchId { get; set; } = null!;
        
        public int Qty { get; set; }
        public bool IsLoose { get; set; }
        public decimal Rate { get; set; } // Sale Rate at time of sale
        public decimal MRP { get; set; }
        public decimal GST { get; set; } // GST percentage
        
        public decimal TotalPrice { get; set; } // (Qty * Rate) + Tax
        
        public int ReturnedQty { get; set; } = 0;
    }
}
