// Models/Payment.cs
namespace SchoolPortal.API.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string PaymentMethod { get; set; } = "Cash"; // Cash, Bank Transfer, etc.
        public string ReceiptNumber { get; set; } = string.Empty;
        public string CollectedBy { get; set; } = string.Empty;

        // Exactly one of these two will be set — a payment is either
        // against a monthly ledger row or a one-off charge, never both.
        public int? LedgerId { get; set; }
        public FeeLedger? Ledger { get; set; }

        public int? ChargeId { get; set; }
        public StudentCharge? Charge { get; set; }
    }
}