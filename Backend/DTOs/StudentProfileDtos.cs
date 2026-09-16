// DTOs/StudentProfileDtos.cs
namespace SchoolPortal.API.DTOs
{
    public class RecentPaymentDto
    {
        public DateTime PaymentDate { get; set; }
        public decimal AmountPaid { get; set; }
        public string PaidAgainst { get; set; } = string.Empty;
        public string ReceiptNumber { get; set; } = string.Empty;
    }

    public class StudentProfileDto
    {
        public string SchoolName { get; set; } = string.Empty;
        public string CampusName { get; set; } = string.Empty;

        // Identity
        public int StudentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? RollNumber { get; set; }
        public string BFormNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public DateTime AdmissionDate { get; set; }
        public string AdmissionStatus { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public bool HasPhoto { get; set; }
        public string FatherName { get; set; } = string.Empty;
        public string FatherMobile { get; set; } = string.Empty;
        public string? MotherName { get; set; }
        public string? MotherMobile { get; set; }
        public decimal MonthlyDiscountAmount { get; set; }
        public string? DiscountReason { get; set; }

        // Fees — same figures a fee-grid row or a challan would show for
        // this student right now, not a separately-derived calculation.
        public decimal TotalOutstanding { get; set; }
        public int OverdueMonthsCount { get; set; }
        public List<RecentPaymentDto> RecentPayments { get; set; } = new();

        // Attendance — current calendar month only. A student's whole
        // attendance history belongs on the Attendance Register/Report
        // screens; this is a glance, not a replacement for those.
        public int AttendanceMonth { get; set; }
        public int AttendanceYear { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LeaveDays { get; set; }
        public int LateDays { get; set; }
        public double AttendancePercentage { get; set; }

        // Academics — most recent exam this student has any recorded
        // results for. Null fields mean no exam has been recorded yet.
        public string? LatestExamName { get; set; }
        public string? LatestExamTerm { get; set; }
        public double? LatestExamPercentage { get; set; }
        public string? LatestExamResult { get; set; }
    }
}
