
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Services { 

    public class AttendanceService : IAttendanceService
    {
        private readonly SchoolPortalDbContext _context;
        public AttendanceService(SchoolPortalDbContext context) => _context = context;

        public async Task MarkBulkAsync(BulkMarkAttendanceDto dto, string markedBy)
        {
            var dateOnly = dto.Date.Date;
            var studentIds = dto.Entries.Select(e => e.StudentId).ToList();

            var existing = await _context.Attendances
                .Where(a => a.Date == dateOnly && studentIds.Contains(a.StudentId))
                .ToDictionaryAsync(a => a.StudentId);

            foreach (var entry in dto.Entries)
            {
                var status = Enum.Parse<AttendanceStatus>(entry.Status);

                if (existing.TryGetValue(entry.StudentId, out var record))
                {
                    // Correcting an earlier mistake same day — update, don't duplicate
                    record.Status = status;
                    record.MarkedBy = markedBy;
                }
                else
                {
                    _context.Attendances.Add(new Attendance
                    {
                        StudentId = entry.StudentId,
                        Date = dateOnly,
                        Status = status,
                        MarkedBy = markedBy
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<ClassAttendanceRowDto>> GetClassAttendanceForDateAsync(int classId, DateTime date)
        {
            var dateOnly = date.Date;
            var students = await _context.Students
                .Where(s => s.ClassId == classId && s.AdmissionStatus == AdmissionStatus.Admitted)
                .ToListAsync();
            var marks = await _context.Attendances
                .Where(a => a.Date == dateOnly && students.Select(s => s.StudentId).Contains(a.StudentId))
                .ToDictionaryAsync(a => a.StudentId);

            return students.Select(s => new ClassAttendanceRowDto
            {
                StudentId = s.StudentId,
                StudentName = s.Name,
                Status = marks.TryGetValue(s.StudentId, out var mark) ? mark.Status.ToString() : "NotMarked"
            }).ToList();
        }

        public async Task<StudentAttendanceSummaryDto?> GetStudentMonthlySummaryAsync(int studentId, int month, int year)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return null;

            var records = await _context.Attendances
                .Where(a => a.StudentId == studentId && a.Date.Month == month && a.Date.Year == year)
                .ToListAsync();

            int present = records.Count(r => r.Status == AttendanceStatus.Present);
            int absent = records.Count(r => r.Status == AttendanceStatus.Absent);
            int leave = records.Count(r => r.Status == AttendanceStatus.Leave);
            int late = records.Count(r => r.Status == AttendanceStatus.Late);
            int total = records.Count;

            // Late counts as attended for percentage purposes — only
            // Absent and Leave count against the student.
            double percentage = total > 0 ? Math.Round((present + late) * 100.0 / total, 1) : 0;

            return new StudentAttendanceSummaryDto
            {
                StudentId = studentId,
                StudentName = student.Name,
                TotalMarkedDays = total,
                PresentDays = present,
                AbsentDays = absent,
                LeaveDays = leave,
                LateDays = late,
                AttendancePercentage = percentage
            };
        }
    }
}