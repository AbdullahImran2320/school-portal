// Models/FeeLedger.cs
namespace SchoolPortal.API.Models
{
    public enum LedgerStatus
    {
        Unpaid,
        Partial,
        Paid,
        Overdue
    }

    public class FeeLedger
    {
        public int LedgerId { get; set; }
        public int MonthNumber { get; set; }  // 1 = January ... 12 = December
        public int Year { get; set; }
        public decimal DueAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public LedgerStatus Status { get; set; } = LedgerStatus.Unpaid;
        public decimal DiscountAmount { get; set; } = 0;
        // Null means use the configured automatic late fee; a value (including 0)
        // is a manual override for this particular monthly fee.
        public decimal? ManualFineAmount { get; set; }

        // Foreign key
        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;
    }
}