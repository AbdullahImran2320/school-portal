// DTOs/AcademicDtos.cs
using System.ComponentModel.DataAnnotations;

namespace SchoolPortal.API.DTOs
{
    public class SubjectDto
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
    }

    public class CreateSubjectDto
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string SubjectName { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "A valid ClassId is required")]
        public int ClassId { get; set; }
    }

    public class ExamDto
    {
        public int ExamId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
    }

    public class CreateExamDto
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string ExamName { get; set; } = string.Empty;

        [Required]
        public string Term { get; set; } = string.Empty;

        [Required]
        public string AcademicYear { get; set; } = string.Empty;
    }

    public class RecordResultDto
    {
        [Range(1, int.MaxValue)]
        public int StudentId { get; set; }

        [Range(1, int.MaxValue)]
        public int SubjectId { get; set; }

        [Range(1, int.MaxValue)]
        public int ExamId { get; set; }

        [Range(0, int.MaxValue)]
        public int MarksObtained { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "TotalMarks must be greater than 0")]
        public int TotalMarks { get; set; }
    }

    public class ExistingResultDto
    {
        public int StudentId { get; set; }
        public int MarksObtained { get; set; }
        public int TotalMarks { get; set; }
    }

    public class ResultDto
    {
        public int ResultId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int MarksObtained { get; set; }
        public int TotalMarks { get; set; }
        public double Percentage { get; set; }
        public string PassFail { get; set; } = string.Empty;
    }

    public class ReportCardDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ExamName { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public List<ResultDto> Subjects { get; set; } = new();
        public double OverallPercentage { get; set; }
        public string OverallResult { get; set; } = string.Empty; // Pass / Fail
    }
}