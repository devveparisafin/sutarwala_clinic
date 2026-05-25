namespace PharmacyERP.Web.Models.ViewModels
{
    public class CustomerLedgerViewModel
    {
        public string CustomerName { get; set; } = null!;
        public string MobileNumber { get; set; } = null!;
        public decimal CurrentBalance { get; set; }
        public List<CustomerLedgerEntry> Entries { get; set; } = new();
    }

    public class CustomerLedgerEntry
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = null!;
        public string Reference { get; set; } = null!;
        public decimal Debit { get; set; }  // Sales (increases balance)
        public decimal Credit { get; set; } // Payments (decreases balance)
        public decimal Balance { get; set; }
        public string? ReferenceId { get; set; }
    }
}
