// DTOs/FeeGridDtos.cs
namespace SchoolPortal.API.DTOs
{
    public class MonthCellDto
    {
        public int MonthNumber { get; set; }
        public decimal DueAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal LateFeeAmount { get; set; }
        public decimal? ManualFineAmount { get; set; }
        public string Status { get; set; } = string.Empty; // Paid / Partial / Unpaid / Overdue
        public int LedgerId { get; set; }
    }

    public class StudentFeeRowDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public List<MonthCellDto> Months { get; set; } = new();
        public decimal TotalOutstanding { get; set; }
    }

    public class ClassFeeGridDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public List<StudentFeeRowDto> Students { get; set; } = new();
    }

    public class DefaulterDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string FatherMobile { get; set; } = string.Empty;
        public int OverdueMonthsCount { get; set; }
        public decimal TotalOutstanding { get; set; }
    }
}