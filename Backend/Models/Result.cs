// Models/Result.cs
namespace SchoolPortal.API.Models
{
    public class Result
    {
        public int ResultId { get; set; }
        public int MarksObtained { get; set; }
        public int TotalMarks { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public int SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;

        public int ExamId { get; set; }
        public Exam Exam { get; set; } = null!;
    }
}