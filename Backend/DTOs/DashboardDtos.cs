namespace SchoolPortal.API.DTOs;

public sealed class DashboardSummaryDto
{
    public int TotalActiveStudents { get; set; }
    public int FeeChallansGeneratedThisMonth { get; set; }
    public decimal FeeAmountCollectedThisMonth { get; set; }
    public DateTime AttendanceDate { get; set; }
    public List<ClassAttendanceSummaryDto> Attendance { get; set; } = new();
}

public sealed class ClassAttendanceSummaryDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Unmarked { get; set; }
    public int OtherMarked { get; set; }
}
