using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;
using SchoolPortal.API.Services;

namespace SchoolPortal.API.Controllers
{
    [ApiController]
    [Route("api/attendance")]
    [Authorize(Roles = "Admin,Accountant,Teacher")]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;
        private readonly SchoolPortalDbContext _context;
        public AttendanceController(IAttendanceService attendanceService, SchoolPortalDbContext context)
        {
            _attendanceService = attendanceService;
            _context = context;
        }

        [Authorize(Roles = "Admin,Teacher")]
        [HttpPost("mark")]
        public async Task<IActionResult> MarkBulk(BulkMarkAttendanceDto dto)
        {
            var invalidStatus = dto.Entries.FirstOrDefault(e => !Enum.TryParse<AttendanceStatus>(e.Status, out _));
            if (invalidStatus != null)
                return BadRequest(new { message = $"Invalid attendance status for student {invalidStatus.StudentId}. Must be Present, Absent, Leave, or Late." });
            var markedBy = User.Identity?.Name ?? "Unknown";
            await _attendanceService.MarkBulkAsync(dto, markedBy);
            return Ok(new { message = $"Attendance marked for {dto.Entries.Count} students" });
        }

        [HttpGet("class/{classId}")]
        public async Task<ActionResult<List<ClassAttendanceRowDto>>> GetClassAttendance(int classId, [FromQuery] DateTime date)
        {
            return Ok(await _attendanceService.GetClassAttendanceForDateAsync(classId, date));
        }

        [HttpGet("students/{studentId}/summary")]
        public async Task<ActionResult<StudentAttendanceSummaryDto>> GetSummary(int studentId, [FromQuery] int month, [FromQuery] int year)
        {
            var summary = await _attendanceService.GetStudentMonthlySummaryAsync(studentId, month, year);
            if (summary == null) return NotFound();
            return Ok(summary);
        }

        [HttpGet("class/{classId}/register")]
        public async Task<ActionResult<AttendanceRegisterDto>> GetClassRegister(int classId, [FromQuery] int month, [FromQuery] int year)
        {
            if (month < 1 || month > 12) return BadRequest("Month must be between 1 and 12.");
            if (year < 2000 || year > 2100) return BadRequest("Year looks invalid.");

            var register = await BuildRegisterAsync(classId, month, year);
            if (register == null) return NotFound();
            return Ok(register);
        }

        [HttpGet("class/{classId}/register/export")]
        public async Task<IActionResult> ExportClassRegister(int classId, [FromQuery] int month, [FromQuery] int year)
        {
            if (month < 1 || month > 12) return BadRequest("Month must be between 1 and 12.");
            if (year < 2000 || year > 2100) return BadRequest("Year looks invalid.");

            var register = await BuildRegisterAsync(classId, month, year);
            if (register == null) return NotFound();

            var bytes = ExcelExportService.BuildAttendanceRegisterWorkbook(register);
            var monthTag = new DateTime(year, month, 1).ToString("yyyy-MM");
            var fileName = $"Attendance-{register.ClassName.Replace(' ', '-')}-{monthTag}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // Shared by the JSON endpoint and the Excel export — same reasoning
        // as the fee reports: whole-month attendance for every admitted
        // student in a class, one row per student, one column per calendar
        // day. There's no holiday calendar in this system, so weekends and
        // school holidays show the same as any other day nobody marked.
        private async Task<AttendanceRegisterDto?> BuildRegisterAsync(int classId, int month, int year)
        {
            var schoolClass = await _context.Classes.FindAsync(classId);
            if (schoolClass == null) return null;

            var students = await _context.Students
                .Where(s => s.ClassId == classId && s.AdmissionStatus == AdmissionStatus.Admitted)
                .OrderBy(s => s.RollNumberSequence ?? int.MaxValue)
                .ThenBy(s => s.Name)
                .ToListAsync();

            var studentIds = students.Select(s => s.StudentId).ToList();
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var records = await _context.Attendances
                .Where(a => studentIds.Contains(a.StudentId) && a.Date >= monthStart && a.Date < monthEnd)
                .ToListAsync();

            var rows = students.Select(s =>
            {
                var byDay = records
                    .Where(a => a.StudentId == s.StudentId)
                    .ToDictionary(a => a.Date.Day, a => a.Status.ToString());

                var days = Enumerable.Range(1, daysInMonth)
                    .Select(d => new AttendanceRegisterCellDto
                    {
                        Day = d,
                        Status = byDay.GetValueOrDefault(d, "NotMarked")
                    })
                    .ToList();

                return new AttendanceRegisterRowDto
                {
                    StudentId = s.StudentId,
                    StudentName = s.Name,
                    RollNumber = s.RollNumber,
                    Days = days,
                    PresentCount = days.Count(d => d.Status == "Present"),
                    AbsentCount = days.Count(d => d.Status == "Absent"),
                    LeaveCount = days.Count(d => d.Status == "Leave"),
                    LateCount = days.Count(d => d.Status == "Late")
                };
            }).ToList();

            return new AttendanceRegisterDto
            {
                ClassId = classId,
                ClassName = schoolClass.ClassName + (string.IsNullOrEmpty(schoolClass.Section) ? "" : " - " + schoolClass.Section),
                Month = month,
                Year = year,
                DaysInMonth = daysInMonth,
                Students = rows
            };
        }
    }
}