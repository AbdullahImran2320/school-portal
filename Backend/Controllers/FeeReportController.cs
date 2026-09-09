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
            return Ok(await BuildDefaultersAsync());
        }

        // Extracted so both the GET endpoint and the Excel export share one
        // source of truth. Calling GetDefaulters() directly as a plain C#
        // method here would NOT have worked: it returns via Ok(...), which
        // stores the data in ActionResult<T>.Result, not .Value — so a
        // direct in-process call like (await GetDefaulters()).Value is
        // always null even though the same data serializes correctly over
        // HTTP. That's exactly the bug that made the export silently return
        // an empty file instead of the real list.
        private async Task<List<DefaulterDto>> BuildDefaultersAsync()
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

            return overdue
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
        }

        [HttpGet("reports/defaulters/export")]
        public async Task<IActionResult> ExportDefaulters()
        {
            var defaulters = await BuildDefaultersAsync();

            var bytes = ExcelExportService.BuildDefaultersWorkbook(defaulters);
            var fileName = $"Defaulters-{DateTime.Now:yyyy-MM-dd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // Grouped by when the money actually arrived (Payment.PaymentDate),
        // not by which month's fee it was for — a late payment collected in
        // October for August's tuition still counts as October's cash.
        // That's what a board/audit report actually wants: real cash flow
        // for the period, not a re-statement of the monthly billing cycle.
        [HttpGet("reports/collection-summary")]
        public async Task<ActionResult<CollectionSummaryDto>> GetCollectionSummary([FromQuery] int month, [FromQuery] int year)
        {
            if (month < 1 || month > 12)
                return BadRequest("Month must be between 1 and 12.");
            if (year < 2000 || year > 2100)
                return BadRequest("Year looks invalid.");

            return Ok(await BuildCollectionSummaryAsync(month, year));
        }

        // Same reasoning as BuildDefaultersAsync above — a shared method the
        // export can call directly, since calling GetCollectionSummary()
        // in-process would hit the same Ok(...)-wrapping problem.
        private async Task<CollectionSummaryDto> BuildCollectionSummaryAsync(int month, int year)
        {
            var monthStart = new DateTime(year, month, 1);
            var nextMonth = monthStart.AddMonths(1);

            var ledgerPayments = await _context.Payments
                .Where(p => p.Ledger != null && p.PaymentDate >= monthStart && p.PaymentDate < nextMonth)
                .Select(p => new { ClassName = p.Ledger!.Student.Class.ClassName, p.AmountPaid })
                .ToListAsync();

            var chargePayments = await _context.Payments
                .Where(p => p.Charge != null && p.PaymentDate >= monthStart && p.PaymentDate < nextMonth)
                .Select(p => new { ClassName = p.Charge!.Student.Class.ClassName, p.AmountPaid })
                .ToListAsync();

            var byClass = ledgerPayments
                .Select(p => (p.ClassName, Tuition: p.AmountPaid, Other: 0m))
                .Concat(chargePayments.Select(p => (p.ClassName, Tuition: 0m, Other: p.AmountPaid)))
                .GroupBy(p => p.ClassName)
                .Select(g => new ClassCollectionDto
                {
                    ClassName = g.Key,
                    TuitionCollected = g.Sum(x => x.Tuition),
                    OtherChargesCollected = g.Sum(x => x.Other),
                    Total = g.Sum(x => x.Tuition + x.Other),
                    PaymentsCount = g.Count()
                })
                .OrderByDescending(c => c.Total)
                .ToList();

            return new CollectionSummaryDto
            {
                Month = month,
                Year = year,
                ByClass = byClass,
                GrandTotal = byClass.Sum(c => c.Total),
                TotalPaymentsCount = byClass.Sum(c => c.PaymentsCount)
            };
        }

        [HttpGet("reports/collection-summary/export")]
        public async Task<IActionResult> ExportCollectionSummary([FromQuery] int month, [FromQuery] int year)
        {
            if (month < 1 || month > 12)
                return BadRequest("Month must be between 1 and 12.");
            if (year < 2000 || year > 2100)
                return BadRequest("Year looks invalid.");

            var summary = await BuildCollectionSummaryAsync(month, year);

            var bytes = ExcelExportService.BuildCollectionSummaryWorkbook(summary);
            var monthName = new DateTime(year, month, 1).ToString("yyyy-MM");
            var fileName = $"Collection-Summary-{monthName}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}