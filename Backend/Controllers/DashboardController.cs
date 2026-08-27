using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Admin,Accountant,Teacher")]
public class DashboardController : ControllerBase
{
    private readonly SchoolPortalDbContext _context;

    public DashboardController(SchoolPortalDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var academicYear = today.Year.ToString();

        // "Active" means currently admitted. Withdrawn, applied, rejected and
        // graduated students are intentionally excluded from the KPI.
        var activeStudents = _context.Students
            .Where(s => s.AdmissionStatus == AdmissionStatus.Admitted);

        var totalActiveStudents = await activeStudents.CountAsync();

        // A monthly fee ledger represents the generated monthly challan for a
        // student. Count only admitted students for the current month/year.
        var feeChallansGenerated = await _context.FeeLedgers
            .Where(l =>
                l.Year == today.Year &&
                l.MonthNumber == today.Month &&
                l.Student.AdmissionStatus == AdmissionStatus.Admitted &&
                (l.Student.AdmissionDate.Year < today.Year ||
                 (l.Student.AdmissionDate.Year == today.Year && l.Student.AdmissionDate.Month <= today.Month)))
            .CountAsync();

        // Payments are the source of truth for money actually collected.
        // SQLite/EF Core cannot translate Sum() directly over a decimal column.
        // Fetch only the matching decimal values, then aggregate in .NET.
        var paymentAmounts = await _context.Payments
            .Where(p => p.PaymentDate >= monthStart && p.PaymentDate < nextMonth)
            .Select(p => p.AmountPaid)
            .ToListAsync();

        var feeAmountCollected = paymentAmounts.Sum();

        // Load only the small amount of data needed for today's class summary.
        var classes = await _context.Classes
            .Where(c => c.AcademicYear == academicYear)
            .OrderBy(c => c.PromotionOrder)
            .Select(c => new
            {
                c.ClassId,
                c.ClassName,
                TotalStudents = c.Students.Count(s =>
                    s.AdmissionStatus == AdmissionStatus.Admitted)
            })
            .ToListAsync();

        var attendanceRows = await _context.Attendances
            .Where(a => a.Date == today &&
                        a.Student.AdmissionStatus == AdmissionStatus.Admitted)
            .Select(a => new
            {
                a.Student.ClassId,
                a.Status
            })
            .ToListAsync();

        var attendance = classes.Select(c =>
        {
            var records = attendanceRows
                .Where(a => a.ClassId == c.ClassId)
                .ToList();

            var present = records.Count(a => a.Status == AttendanceStatus.Present);
            var absent = records.Count(a => a.Status == AttendanceStatus.Absent);

            // The existing portal also supports Leave and Late. Keep those
            // records visible as "Other" rather than silently relabelling them.
            var otherMarked = records.Count(a =>
                a.Status == AttendanceStatus.Leave ||
                a.Status == AttendanceStatus.Late);

            var unmarked = Math.Max(c.TotalStudents - records.Count, 0);

            return new ClassAttendanceSummaryDto
            {
                ClassId = c.ClassId,
                ClassName = c.ClassName,
                TotalStudents = c.TotalStudents,
                Present = present,
                Absent = absent,
                Unmarked = unmarked,
                OtherMarked = otherMarked
            };
        }).ToList();

        return Ok(new DashboardSummaryDto
        {
            TotalActiveStudents = totalActiveStudents,
            FeeChallansGeneratedThisMonth = feeChallansGenerated,
            FeeAmountCollectedThisMonth = feeAmountCollected,
            AttendanceDate = today,
            Attendance = attendance
        });
    }
}
