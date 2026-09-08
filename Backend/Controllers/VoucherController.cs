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

        // chargeTypeOnly: when set, the challan shows ONLY that one line
        // (monthly tuition suppressed unless it IS the requested type) —
        // for printing a standalone collection notice for a single charge
        // (e.g. "Annual Charges") instead of the full combined monthly bill.
        private async Task<FeeVoucherDto?> BuildVoucherAsync(Student student, int month, int year, ChallanSettings? settings, string? chargeTypeOnly = null)
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
            var netMonthlyNoLateFee = Math.Max((ledger.DueAmount - ledger.DiscountAmount) - ledger.PaidAmount, 0);

            var includeMonthlyLine = chargeTypeOnly == null
                || string.Equals(chargeTypeOnly, "Monthly Fee", StringComparison.OrdinalIgnoreCase)
                || string.Equals(chargeTypeOnly, "Tuition Fee", StringComparison.OrdinalIgnoreCase);

            var chargesQuery = _context.StudentCharges
                .Where(c => c.StudentId == student.StudentId && c.Status != ChargeStatus.Paid);

            if (chargeTypeOnly != null)
                chargesQuery = chargesQuery.Where(c => c.ChargeType == chargeTypeOnly);

            var outstandingCharges = await chargesQuery
                .Select(c => new VoucherChargeLineDto
                {
                    ChargeType = c.ChargeType,
                    Balance = (c.DueAmount - c.DiscountAmount) - c.PaidAmount
                })
                .ToListAsync();

            var chargesTotal = outstandingCharges.Sum(c => c.Balance);
            var totalByDueDate = (includeMonthlyLine ? netMonthlyNoLateFee : 0) + chargesTotal;
            var totalAfterDueDate = totalByDueDate + (includeMonthlyLine ? lateFee : 0);

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
                MonthlyFeeDue = includeMonthlyLine ? ledger.DueAmount : 0,
                DiscountAmount = includeMonthlyLine ? ledger.DiscountAmount : 0,
                LateFeeAmount = includeMonthlyLine ? lateFee : 0,
                MonthlyNetPayable = includeMonthlyLine ? netMonthly : 0,
                OutstandingCharges = outstandingCharges,
                TotalAmountDue = (includeMonthlyLine ? netMonthly : 0) + chargesTotal,
                TotalPaymentByDueDate = totalByDueDate,
                TotalPaymentAfterDueDate = totalAfterDueDate,
                AccountTitle = settings?.AccountTitle ?? "",
                BankName = settings?.BankName ?? "",
                AccountNumber = settings?.AccountNumber ?? "",
                PaymentTermsLine1 = settings?.PaymentTermsLine1 ?? "",
                PaymentTermsLine2 = settings?.PaymentTermsLine2 ?? ""
            };
        }

        [HttpGet("students/{studentId}/voucher")]
        public async Task<ActionResult<FeeVoucherDto>> GetVoucher(int studentId, [FromQuery] int month, [FromQuery] int year, [FromQuery] string? chargeType = null)
        {
            var student = await _context.Students
                .Include(s => s.Class).Include(s => s.Parent)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null) return NotFound();

            var settings = await _context.ChallanSettings.FirstOrDefaultAsync();
            var voucher = await BuildVoucherAsync(student, month, year, settings, chargeType);
            if (voucher == null) return NotFound(new { message = "No fee ledger found for this student/month/year." });
            return Ok(voucher);
        }

        // Bulk version — the one your friend will actually use monthly,
        // generating every student's voucher in one call to print for a class.
        //
        // admissionDateFrom/To: only include students admitted in that window —
        // e.g. printing challans for just the latest admitted batch.
        // chargeType: see BuildVoucherAsync — restricts every challan in the
        // batch to a single line item instead of the full combined bill.
        [HttpGet("classes/{classId}/vouchers")]
        public async Task<ActionResult<List<FeeVoucherDto>>> GetClassVouchers(
            int classId, [FromQuery] int month, [FromQuery] int year,
            [FromQuery] string? chargeType = null,
            [FromQuery] DateTime? admissionDateFrom = null,
            [FromQuery] DateTime? admissionDateTo = null)
        {
            var query = _context.Students
                .Include(s => s.Class).Include(s => s.Parent)
                .Where(s => s.ClassId == classId && s.AdmissionStatus == AdmissionStatus.Admitted);

            if (admissionDateFrom.HasValue)
                query = query.Where(s => s.AdmissionDate >= admissionDateFrom.Value);
            if (admissionDateTo.HasValue)
                query = query.Where(s => s.AdmissionDate <= admissionDateTo.Value);

            var students = await query.ToListAsync();
            var settings = await _context.ChallanSettings.FirstOrDefaultAsync();

            var vouchers = new List<FeeVoucherDto>();
            foreach (var student in students)
            {
                var v = await BuildVoucherAsync(student, month, year, settings, chargeType);
                if (v != null) vouchers.Add(v);
            }
            return Ok(vouchers);
        }

        // Distinct charge type names currently in use for this class, so the
        // frontend's "specific charge" filter dropdown reflects real data
        // instead of a hardcoded guess.
        [HttpGet("classes/{classId}/charge-types")]
        public async Task<ActionResult<List<string>>> GetChargeTypesForClass(int classId)
        {
            var types = await _context.StudentCharges
                .Where(c => c.Student.ClassId == classId)
                .Select(c => c.ChargeType)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();
            return Ok(types);
        }

        // Paid receipts — one per actual payment transaction (against either
        // a monthly ledger or a one-off charge), not one per student. A
        // student who paid in two installments gets two receipts, matching
        // what really happened.
        [HttpGet("classes/{classId}/receipts")]
        public async Task<ActionResult<List<PaidReceiptDto>>> GetClassReceipts(int classId, [FromQuery] int month, [FromQuery] int year)
        {
            var ledgerPayments = await _context.Payments
                .Include(p => p.Ledger).ThenInclude(l => l!.Student).ThenInclude(s => s.Class)
                .Include(p => p.Ledger).ThenInclude(l => l!.Student).ThenInclude(s => s.Parent)
                .Where(p => p.Ledger != null
                         && p.Ledger.MonthNumber == month
                         && p.Ledger.Year == year
                         && p.Ledger.Student.ClassId == classId)
                .ToListAsync();

            // One-off charge payments (admission/registration/security/etc.)
            // don't carry a month/year of their own — they're matched into
            // this month's receipt run by when they were actually paid, so
            // they show up on the same monthly print run as everything else.
            var chargePayments = await _context.Payments
                .Include(p => p.Charge).ThenInclude(c => c!.Student).ThenInclude(s => s.Class)
                .Include(p => p.Charge).ThenInclude(c => c!.Student).ThenInclude(s => s.Parent)
                .Where(p => p.Charge != null
                         && p.Charge.Student.ClassId == classId
                         && p.PaymentDate.Month == month
                         && p.PaymentDate.Year == year)
                .ToListAsync();

            var schoolName = _config["SchoolSettings:SchoolName"] ?? "";
            var campusName = _config["SchoolSettings:CampusName"] ?? "";

            var receipts = new List<PaidReceiptDto>();

            receipts.AddRange(ledgerPayments.Select(p => new PaidReceiptDto
            {
                SchoolName = schoolName,
                CampusName = campusName,
                ReceiptNumber = p.ReceiptNumber,
                PaymentDate = p.PaymentDate,
                PaymentMethod = p.PaymentMethod,
                CollectedBy = p.CollectedBy,
                StudentId = p.Ledger!.Student.StudentId,
                StudentName = p.Ledger.Student.Name,
                BFormNumber = p.Ledger.Student.BFormNumber,
                ClassName = p.Ledger.Student.Class?.ClassName ?? "",
                FatherName = p.Ledger.Student.Parent?.FatherName ?? "",
                FatherMobile = p.Ledger.Student.Parent?.FatherMobile ?? "",
                VoucherMonth = p.Ledger.MonthNumber,
                VoucherYear = p.Ledger.Year,
                AmountPaid = p.AmountPaid,
                PaidAgainst = "Tuition Fee",
                AmountInWords = AmountInWords.Convert(p.AmountPaid)
            }));

            receipts.AddRange(chargePayments.Select(p => new PaidReceiptDto
            {
                SchoolName = schoolName,
                CampusName = campusName,
                ReceiptNumber = p.ReceiptNumber,
                PaymentDate = p.PaymentDate,
                PaymentMethod = p.PaymentMethod,
                CollectedBy = p.CollectedBy,
                StudentId = p.Charge!.Student.StudentId,
                StudentName = p.Charge.Student.Name,
                BFormNumber = p.Charge.Student.BFormNumber,
                ClassName = p.Charge.Student.Class?.ClassName ?? "",
                FatherName = p.Charge.Student.Parent?.FatherName ?? "",
                FatherMobile = p.Charge.Student.Parent?.FatherMobile ?? "",
                VoucherMonth = p.PaymentDate.Month,
                VoucherYear = p.PaymentDate.Year,
                AmountPaid = p.AmountPaid,
                PaidAgainst = p.Charge.ChargeType,
                AmountInWords = AmountInWords.Convert(p.AmountPaid)
            }));

            return Ok(receipts.OrderBy(r => r.PaymentDate).ToList());
        }
    }
}
