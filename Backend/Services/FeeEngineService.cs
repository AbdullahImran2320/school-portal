// Services/IFeeEngineService.cs + FeeEngineService.cs
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Services
{
    public class FeeEngineService : IFeeEngineService
    {
        private readonly SchoolPortalDbContext _context;
        public FeeEngineService(SchoolPortalDbContext context) => _context = context;

        public async Task GenerateFeeRecordsForStudentAsync(int studentId, int classId, string academicYear)
        {
            var student = await _context.Students.FindAsync(studentId);
            // Defense in depth: the API-facing DTO for setting a concession
            // already rejects negative values, but this method takes a raw
            // decimal, not that DTO — any future caller that doesn't go
            // through the validated endpoint could otherwise store a
            // negative discount here. A negative DiscountAmount is a real
            // problem, not a harmless edge case: it gets SUBTRACTED in every
            // total-due calculation, so a negative value silently ADDS to
            // what a student owes instead of reducing it.
            var discount = Math.Max(0, student?.MonthlyDiscountAmount ?? 0);

            var components = await _context.FeeComponents
                .Where(c => c.ClassId == classId && c.AcademicYear == academicYear)
                .ToListAsync();

            var monthly = components.FirstOrDefault(c => c.Frequency == FeeFrequency.Monthly);
            if (monthly != null)
            {
                var startYear = int.Parse(academicYear);
                var firstMonth = student?.AdmissionDate.Year == startYear
                    ? student.AdmissionDate.Month
                    : 1;

                for (int month = firstMonth; month <= 12; month++)
                {
                    _context.FeeLedgers.Add(new FeeLedger
                    {
                        StudentId = studentId,
                        MonthNumber = month,
                        Year = startYear,
                        DueAmount = monthly.Amount,
                        DiscountAmount = discount,
                        PaidAmount = 0,
                        Status = LedgerStatus.Unpaid
                    });
                }
            }

            // one-off charges loop stays the same — discounts apply to the monthly fee only for now
            foreach (var oneOff in components.Where(c => c.Frequency != FeeFrequency.Monthly))
            {
                _context.StudentCharges.Add(new StudentCharge
                {
                    StudentId = studentId,
                    ChargeType = oneOff.ComponentName,
                    DueAmount = oneOff.Amount,
                    PaidAmount = 0,
                    Status = ChargeStatus.Unpaid,
                    AcademicYear = academicYear
                });
            }

            await _context.SaveChangesAsync();
        }
    }
}