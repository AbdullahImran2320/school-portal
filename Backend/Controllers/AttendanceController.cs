using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public AttendanceController(IAttendanceService attendanceService) => _attendanceService = attendanceService;

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
    }
}