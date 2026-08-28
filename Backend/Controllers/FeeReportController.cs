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
    [Route("api")]
    [Authorize(Roles = "Admin,Accountant")]
    public class FeeReportsController : ControllerBase
    {
        private readonly SchoolPortalDbContext _context;
        private readonly int _gracePeriodDay;
        private readonly decimal _lateFeeAmount;

        public FeeReportsController(SchoolPortalDbContext context, IConfiguration config)
        {
            _context = context;
            _gracePeriodDay = config.GetValue<int>("LateFeeSettings:GracePeriodDay");
            _lateFeeAmount = config.GetValue<decimal>("LateFeeSettings:LateFeeAmount");
        }


        [HttpGet("classes/{classId}/fee-grid")]
        public async Task<ActionResult<ClassFeeGridDto>> GetClassFeeGrid(int classId, [FromQuery] int? year)
        {

            var schoolClass = await _context.Classes.FindAsync(classId);
            if (schoolClass == null) return NotFound();

            var now = DateTime.Now;
            var targetYear = year ?? now.Year;

            var students = await _context.Students
                .Where(s => s.ClassId == classId && s.AdmissionStatus == AdmissionStatus.Admitted)
                .Include(s => s.Class)
                .ToListAsync();

            var studentIds = students.Select(s => s.StudentId).ToList();
            var allLedgers = await _context.FeeLedgers
                .Where(l => studentIds.Contains(l.StudentId) && l.Year == targetYear)
                .ToListAsync();

            var rows = students.Select(s =>
            {
                var months = allLedgers
                    .Where(l => l.StudentId == s.StudentId &&
                                FeeCalculator.IsApplicableMonth(s.AdmissionDate, l.MonthNumber, l.Year))
                    .OrderBy(l => l.MonthNumber)
                  .Select(l => new MonthCellDto
                  {
                      MonthNumber = l.MonthNumber,
                      DueAmount = l.DueAmount,
                      DiscountAmount = l.DiscountAmount,
                      PaidAmount = l.PaidAmount,
                      LedgerId = l.LedgerId,
                      LateFeeAmount = FeeCalculator.GetLateFee(l, now, _gracePeriodDay, _lateFeeAmount),
                      ManualFineAmount = l.ManualFineAmount,
                      Status = FeeCalculator.GetEffectiveStatus(l, now, _gracePeriodDay)
                  })
                    .ToList();

                return new StudentFeeRowDto
                {
                    StudentId = s.StudentId,
                    StudentName = s.Name,
                    Months = months,
                    TotalOutstanding = months.Sum(m => (m.DueAmount - m.DiscountAmount + m.LateFeeAmount) - m.PaidAmount)
                };
            }).ToList();

            return Ok(new ClassFeeGridDto
            {
                ClassId = classId,
                ClassName = schoolClass.ClassName,
                Students = rows
            });
        }

        [HttpGet("reports/defaulters")]
        public async Task<ActionResult<List<DefaulterDto>>> GetDefaulters()
        {
            var now = DateTime.Now;

            var ledgers = await _context.FeeLedgers
                .Include(l => l.Student).ThenInclude(s => s.Class)
                .Include(l => l.Student).ThenInclude(s => s.Parent)
                .Where(l => l.Status != LedgerStatus.Paid &&
                            l.Student.AdmissionStatus == AdmissionStatus.Admitted)
                .ToListAsync();

            var overdue = ledgers.Where(l =>
                FeeCalculator.IsApplicableMonth(l.Student.AdmissionDate, l.MonthNumber, l.Year) &&
                FeeCalculator.GetEffectiveStatus(l, now, _gracePeriodDay) == "Overdue");

            var grouped = overdue
                .GroupBy(l => l.Student)
                .Select(g => new DefaulterDto
                {
                    StudentId = g.Key.StudentId,
                    StudentName = g.Key.Name,
                    ClassName = g.Key.Class.ClassName,
                    FatherMobile = g.Key.Parent.FatherMobile,
                    OverdueMonthsCount = g.Count(),
                    // Same formula as the fee-grid: due minus discount plus
                    // late fee minus paid, floored at 0 — this used to skip
                    // discount and late fee entirely, so it disagreed with
                    // the fee-grid's total for the same student.
                    TotalOutstanding = g.Sum(l => Math.Max(
                        (l.DueAmount - l.DiscountAmount + FeeCalculator.GetLateFee(l, now, _gracePeriodDay, _lateFeeAmount)) - l.PaidAmount,
                        0))
                })
                .OrderByDescending(d => d.OverdueMonthsCount)
                .ToList();

            return Ok(grouped);
        }
    }
}