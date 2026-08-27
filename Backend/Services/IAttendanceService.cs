using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Services
{
    public interface IAttendanceService
    {
        Task MarkBulkAsync(BulkMarkAttendanceDto dto, string markedBy);
        Task<List<ClassAttendanceRowDto>> GetClassAttendanceForDateAsync(int classId, DateTime date);
        Task<StudentAttendanceSummaryDto?> GetStudentMonthlySummaryAsync(int studentId, int month, int year);
    }
}