// DTOs/PaymentDtos.cs
using System.ComponentModel.DataAnnotations;

namespace SchoolPortal.API.DTOs
{
    public class RecordPaymentDto
    {
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal AmountPaid { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = "Cash";

        [Required]
        public string CollectedBy { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Discount amount can't be negative")]
        public decimal DiscountAmount { get; set; } = 0;

        // Null = automatic configured fine; 0 = explicitly no fine; any other value = manual fine.
        [Range(0, double.MaxValue, ErrorMessage = "Fine amount can't be negative")]
        public decimal? FineAmount { get; set; }
    }

    public class PaymentResultDto
    {
        public int PaymentId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public decimal NewPaidTotal { get; set; }
        public decimal DueAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal LateFeeCharged { get; set; }
    }
}