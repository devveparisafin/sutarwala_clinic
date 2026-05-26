using System.Collections.Generic;

namespace PharmacyERP.Web.Models.ViewModels
{
    public class PurchaseReturnViewModel
    {
        public string PurchaseId { get; set; } = null!;
        public string InvoiceNo { get; set; } = null!;
        public List<PurchaseReturnItemViewModel> Items { get; set; } = new();
    }

    public class PurchaseReturnItemViewModel
    {
        public string PurchaseDetailId { get; set; } = null!;
        public string MedicineId { get; set; } = null!;
        public string BatchNo { get; set; } = null!;
        public int ReturnQty { get; set; }
        public decimal RefundAmount { get; set; }
    }
}
