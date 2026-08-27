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
    public class VouchersController : ControllerBase
    {
        private readonly SchoolPortalDbContext _context;
        private readonly IConfiguration _config;
        private readonly int _gracePeriodDay;
        private readonly decimal _lateFeeAmount;

        public VouchersController(SchoolPortalDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _gracePeriodDay = config.GetValue<int>("LateFeeSettings:GracePeriodDay");
            _lateFeeAmount = config.GetValue<decimal>("LateFeeSettings:LateFeeAmount");
        }

        private async Task<FeeVoucherDto?> BuildVoucherAsync(Student student, int month, int year)
        {
            if (!FeeCalculator.IsApplicableMonth(student.AdmissionDate, month, year))
                return null;

            var ledger = await _context.FeeLedgers.FirstOrDefaultAsync(
                l => l.StudentId == student.StudentId && l.MonthNumber == month && l.Year == year);
            if (ledger == null) return null;

            var now = DateTime.Now;
            var lateFee = FeeCalculator.GetLateFee(ledger, now, _gracePeriodDay, _lateFeeAmount);
            // Never show a negative "amount due" on a printed voucher — an
            // overpaid or fully-paid month should read as 0, not a credit.
            var netMonthly = Math.Max((ledger.DueAmount - ledger.DiscountAmount + lateFee) - ledger.PaidAmount, 0);

            var outstandingCharges = await _context.StudentCharges
                .Where(c => c.StudentId == student.StudentId && c.Status != ChargeStatus.Paid)
                .Select(c => new VoucherChargeLineDto
                {
                    ChargeType = c.ChargeType,
                    Balance = (c.DueAmount - c.DiscountAmount) - c.PaidAmount
                })
                .ToListAsync();

            return new FeeVoucherDto
            {
                SchoolName = _config["SchoolSettings:SchoolName"] ?? "",
                CampusName = _config["SchoolSettings:CampusName"] ?? "",
                ChallanNumber = $"CH-{year}{month:D2}-{student.StudentId:D4}",
                IssueDate = now,
                DueDate = new DateTime(year, month, 1).AddDays(_gracePeriodDay - 1),
                StudentId = student.StudentId,
                StudentName = student.Name,
                BFormNumber = student.BFormNumber,
                ClassName = student.Class?.ClassName ?? "",
                FatherName = student.Parent?.FatherName ?? "",
                FatherMobile = student.Parent?.FatherMobile ?? "",
                VoucherMonth = month,
                VoucherYear = year,
                MonthlyFeeDue = ledger.DueAmount,
                DiscountAmount = ledger.DiscountAmount,
                LateFeeAmount = lateFee,
                MonthlyNetPayable = netMonthly,
                OutstandingCharges = outstandingCharges,
                TotalAmountDue = netMonthly + outstandingCharges.Sum(c => c.Balance)
            };
        }

        [HttpGet("students/{studentId}/voucher")]
        public async Task<ActionResult<FeeVoucherDto>> GetVoucher(int studentId, [FromQuery] int month, [FromQuery] int year)
        {
            var student = await _context.Students
                .Include(s => s.Class).Include(s => s.Parent)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null) return NotFound();

            var voucher = await BuildVoucherAsync(student, month, year);
            if (voucher == null) return NotFound(new { message = "No fee ledger found for this student/month/year." });
            return Ok(voucher);
        }

        // Bulk version — the one your friend will actually use monthly,
        // generating every student's voucher in one call to print for a class.
        [HttpGet("classes/{classId}/vouchers")]
        public async Task<ActionResult<List<FeeVoucherDto>>> GetClassVouchers(int classId, [FromQuery] int month, [FromQuery] int year)
        {
            var students = await _context.Students
                .Include(s => s.Class).Include(s => s.Parent)
                .Where(s => s.ClassId == classId && s.AdmissionStatus == AdmissionStatus.Admitted)
                .ToListAsync();

            var vouchers = new List<FeeVoucherDto>();
            foreach (var student in students)
            {
                var v = await BuildVoucherAsync(student, month, year);
                if (v != null) vouchers.Add(v);
            }
            return Ok(vouchers);
        }
    }
}