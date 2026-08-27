// Models/Exam.cs
namespace SchoolPortal.API.Models
{
    public class Exam
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; } = string.Empty; // "Midterm", "Final"
        public string Term { get; set; } = string.Empty;     // "1st Term", "2nd Term"
        public string AcademicYear { get; set; } = string.Empty;
    }
}