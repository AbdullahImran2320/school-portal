// Models/StudentCharge.cs
namespace SchoolPortal.API.Models
{
    public enum ChargeStatus
    {
        Unpaid,
        Partial,
        Paid
    }

    public class StudentCharge
    {
        public int ChargeId { get; set; }
        public string ChargeType { get; set; } = string.Empty; // "Book Price", "Uniform Price", etc.
        public decimal DueAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public ChargeStatus Status { get; set; } = ChargeStatus.Unpaid;
        public string AcademicYear { get; set; } = string.Empty;
  
        public decimal DiscountAmount { get; set; } = 0;

        // Foreign key
        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;
    }
}