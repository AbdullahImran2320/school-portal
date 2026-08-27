// Models/Attendance.cs
namespace SchoolPortal.API.Models
{
    public enum AttendanceStatus
    {
        Present,
        Absent,
        Leave,
        Late
    }

    public class Attendance
    {
        public int AttendanceId { get; set; }
        public DateTime Date { get; set; }
        public AttendanceStatus Status { get; set; }
        public string MarkedBy { get; set; } = string.Empty;

        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;
    }
}