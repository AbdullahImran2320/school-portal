// Models/Student.cs
namespace SchoolPortal.API.Models
{
    public enum AdmissionStatus
    {
        Applied,
        Admitted,
        Withdrawn,
        Rejected,
        Graduated
    }

    public class Student
    {
        public int StudentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BFormNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public DateTime AdmissionDate { get; set; }
        public AdmissionStatus AdmissionStatus { get; set; } = AdmissionStatus.Applied;
        public decimal MonthlyDiscountAmount { get; set; } = 0;
        public string? DiscountReason { get; set; } // "Sibling Discount", "Staff Scholarship", etc.

        // Foreign keys
        public int ClassId { get; set; }
        public SchoolClass Class { get; set; } = null!;

        public int ParentId { get; set; }
        public Parent Parent { get; set; } = null!;
      
   
    }
}