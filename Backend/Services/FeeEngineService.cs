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
            var discount = student?.MonthlyDiscountAmount ?? 0;

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