// Controllers/StudentFeeController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.Services;

namespace SchoolPortal.API.Controllers
{
    [ApiController]
    [Route("api/students/{studentId}/fee-summary")]
    // Doc says "any authenticated", but Pending accounts are supposed to have
    // zero access anywhere (see business rules) — scoping to the three real
    // roles instead of a bare [Authorize] so a Pending login can't read fees.
    [Authorize(Roles = "Admin,Accountant,Teacher")]
    public class StudentFeeController : ControllerBase
    {
        private readonly SchoolPortalDbContext _context;
        private readonly int _gracePeriodDay;
        private readonly decimal _lateFeeAmount;

        public StudentFeeController(SchoolPortalDbContext context, IConfiguration config)
        {
            _context = context;
            _gracePeriodDay = config.GetValue<int>("LateFeeSettings:GracePeriodDay");
            _lateFeeAmount = config.GetValue<decimal>("LateFeeSettings:LateFeeAmount");
        }

        [HttpGet]
        public async Task<IActionResult> GetSummary(int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return NotFound();

            var now = DateTime.Now;
            var ledgerEntities = await _context.FeeLedgers
                .Where(f => f.StudentId == studentId &&
                            FeeCalculator.IsApplicableMonth(student.AdmissionDate, f.MonthNumber, f.Year))
                .OrderBy(f => f.MonthNumber)
                .ToListAsync();

            var ledger = ledgerEntities.Select(f => new
            {
                f.MonthNumber,
                f.Year,
                f.DueAmount,
                f.DiscountAmount,
                f.PaidAmount,
                // ledger projection — add:
                f.LedgerId,
              
                LateFeeAmount = FeeCalculator.GetLateFee(f, now, _gracePeriodDay, _lateFeeAmount),
                ManualFineAmount = f.ManualFineAmount,
                Status = FeeCalculator.GetEffectiveStatus(f, now, _gracePeriodDay)
            });

            var charges = await _context.StudentCharges
                .Where(c => c.StudentId == studentId)
                .Select(c => new { c.ChargeType, c.DueAmount, c.DiscountAmount, c.PaidAmount,
                   
                    // charges projection — add:
                    c.ChargeId,
                    Status = c.Status.ToString() })
                .ToListAsync();

            return Ok(new { MonthlyLedger = ledger, OneOffCharges = charges });
        }

     
    }
}