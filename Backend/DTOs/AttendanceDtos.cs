// DTOs/AttendanceDtos.cs
using System.ComponentModel.DataAnnotations;

namespace SchoolPortal.API.DTOs
{
   
    public class MarkAttendanceEntryDto
    {
        [Range(1, int.MaxValue)]
        public int StudentId { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;
    }

    public class BulkMarkAttendanceDto
    {
        [Range(1, int.MaxValue)]
        public int ClassId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required, MinLength(1, ErrorMessage = "At least one attendance entry is required")]
        public List<MarkAttendanceEntryDto> Entries { get; set; } = new();
    }

    public class ClassAttendanceRowDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Status { get; set; } = "NotMarked";
    }

    public class StudentAttendanceSummaryDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int TotalMarkedDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LeaveDays { get; set; }
        public int LateDays { get; set; }
        public double AttendancePercentage { get; set; }
    }
}