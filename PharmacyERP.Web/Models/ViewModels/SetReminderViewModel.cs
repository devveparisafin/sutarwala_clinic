using System;

namespace PharmacyERP.Web.Models.ViewModels
{
    public class SetReminderViewModel
    {
        public string CustomerId { get; set; } = null!;
        public DateTime? ReminderDate { get; set; }
        public string? ReminderFrequency { get; set; }
        public string? ReminderNote { get; set; }
    }
}
