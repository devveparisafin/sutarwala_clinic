namespace PharmacyERP.Web.Models.ViewModels
{
    public class SupplierLedgerViewModel
    {
        public string SupplierName { get; set; } = null!;
        public decimal CurrentBalance { get; set; }
        public List<LedgerEntry> Entries { get; set; } = new();
    }

    public class LedgerEntry
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = null!;
        public string Reference { get; set; } = null!;
        public decimal Debit { get; set; }  // Payments we made
        public decimal Credit { get; set; } // Purchases we made
        public decimal Balance { get; set; }
        public string? ReferenceId { get; set; }
    }
}
