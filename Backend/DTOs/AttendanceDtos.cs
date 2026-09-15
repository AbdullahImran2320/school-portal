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

    public class AttendanceRegisterCellDto
    {
        public int Day { get; set; }
        // "Present" / "Absent" / "Leave" / "Late" / "NotMarked" — the system
        // has no holiday calendar, so a weekend or school holiday shows the
        // same as any other unmarked day rather than something distinct.
        public string Status { get; set; } = "NotMarked";
    }

    public class AttendanceRegisterRowDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int? RollNumber { get; set; }
        public List<AttendanceRegisterCellDto> Days { get; set; } = new();
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LeaveCount { get; set; }
        public int LateCount { get; set; }
    }

    public class AttendanceRegisterDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public int DaysInMonth { get; set; }
        public List<AttendanceRegisterRowDto> Students { get; set; } = new();
    }
}