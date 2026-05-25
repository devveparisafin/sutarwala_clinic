namespace PharmacyERP.Web.Models.ViewModels
{
    public class SaleReturnViewModel
    {
        public string SaleId { get; set; } = null!;
        public string InvoiceNo { get; set; } = null!;
        public List<SaleReturnItemViewModel> Items { get; set; } = new();
    }

    public class SaleReturnItemViewModel
    {
        public string SaleDetailId { get; set; } = null!;
        public string MedicineId { get; set; } = null!;
        public string BatchId { get; set; } = null!;
        public int ReturnQty { get; set; }
        public decimal RefundAmount { get; set; }
    }
}
