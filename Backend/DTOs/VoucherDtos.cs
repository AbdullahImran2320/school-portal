// DTOs/VoucherDtos.cs
namespace SchoolPortal.API.DTOs
{
    public class VoucherChargeLineDto
    {
        public string ChargeType { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }

    public class FeeVoucherDto
    {
        public string SchoolName { get; set; } = string.Empty;
        public string CampusName { get; set; } = string.Empty;
        public string ChallanNumber { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }

        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string BFormNumber { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string FatherName { get; set; } = string.Empty;
        public string FatherMobile { get; set; } = string.Empty;

        public int VoucherMonth { get; set; }
        public int VoucherYear { get; set; }
        public decimal MonthlyFeeDue { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal LateFeeAmount { get; set; }
        public decimal MonthlyNetPayable { get; set; }

        public List<VoucherChargeLineDto> OutstandingCharges { get; set; } = new();
        public decimal TotalAmountDue { get; set; }

        // Always computed regardless of today's date, so one printed challan
        // stays valid and readable whether it's paid on day 1 or day 15 —
        // exactly like the two-column paper challan (by due date / after).
        public decimal TotalPaymentByDueDate { get; set; }
        public decimal TotalPaymentAfterDueDate { get; set; }

        // Bank/payment-terms block — admin-editable via Admin -> Challan Settings.
        public string AccountTitle { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string PaymentTermsLine1 { get; set; } = string.Empty;
        public string PaymentTermsLine2 { get; set; } = string.Empty;
    }

    public class PaidReceiptDto
    {
        public string SchoolName { get; set; } = string.Empty;
        public string CampusName { get; set; } = string.Empty;
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string CollectedBy { get; set; } = string.Empty;

        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string BFormNumber { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string FatherName { get; set; } = string.Empty;
        public string FatherMobile { get; set; } = string.Empty;

        public int VoucherMonth { get; set; }
        public int VoucherYear { get; set; }
        public decimal AmountPaid { get; set; }

        // Which single line this payment goes against on the printed receipt.
        // "Tuition" for a monthly-ledger payment; otherwise the charge's own
        // type string (e.g. "Admission Fee", "Registration Fee", "Security").
        // The frontend places AmountPaid next to whichever line this matches,
        // and leaves every other line on the receipt blank — matching how the
        // paper receipt is actually filled in by hand, one line at a time.
        public string PaidAgainst { get; set; } = string.Empty;

        public string AmountInWords { get; set; } = string.Empty;
    }
}
