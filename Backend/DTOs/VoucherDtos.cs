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
    }
}