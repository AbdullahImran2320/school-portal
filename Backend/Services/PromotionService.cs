// Services/IPromotionService.cs + PromotionService.cs
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Services
{
    public interface IPromotionService
    {
        Task<PromotionResultDto> PromoteAllAsync(PromoteClassesDto dto);
    }

    public class PromotionService : IPromotionService
    {
        private readonly SchoolPortalDbContext _context;
        private readonly IFeeEngineService _feeEngineService;

        public PromotionService(SchoolPortalDbContext context, IFeeEngineService feeEngineService)
        {
            _context = context;
            _feeEngineService = feeEngineService;
        }

        public async Task<PromotionResultDto> PromoteAllAsync(PromoteClassesDto dto)
        {
            var result = new PromotionResultDto();
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var classByOrder = await _context.Classes.ToDictionaryAsync(c => c.PromotionOrder);
                var students = await _context.Students
                    .Include(s => s.Class)
                    .Where(s => s.AdmissionStatus == AdmissionStatus.Admitted)
                    .ToListAsync();

                // Running this twice for the same target year would otherwise
                // silently duplicate every student's fee ledgers/charges, and
                // push already-promoted students up a second grade level on
                // top of the first run. Detect who's already been processed
                // for ToAcademicYear (by their fee records existing) and skip
                // them entirely — this is a once-a-year bulk action that's
                // easy to accidentally trigger twice.
                var targetYearNum = int.Parse(dto.ToAcademicYear);
                var alreadyProcessedIds = (await _context.FeeLedgers
                        .Where(l => l.Year == targetYearNum)
                        .Select(l => l.StudentId)
                        .Distinct()
                        .ToListAsync())
                    .Concat(await _context.StudentCharges
                        .Where(c => c.AcademicYear == dto.ToAcademicYear)
                        .Select(c => c.StudentId)
                        .Distinct()
                        .ToListAsync())
                    .ToHashSet();

                foreach (var student in students)
                {
                    if (alreadyProcessedIds.Contains(student.StudentId))
                    {
                        result.AlreadyProcessedCount++;
                        continue;
                    }

                    if (dto.HoldBackStudentIds.Contains(student.StudentId))
                    {
                        // Repeater: stays in the same class, still gets a fresh year's ledger
                        await _feeEngineService.GenerateFeeRecordsForStudentAsync(student.StudentId, student.ClassId, dto.ToAcademicYear);
                        result.HeldBackCount++;
                        continue;
                    }

                    if (classByOrder.TryGetValue(student.Class.PromotionOrder + 1, out var nextClass))
                    {
                        student.ClassId = nextClass.ClassId;
                        await _feeEngineService.GenerateFeeRecordsForStudentAsync(student.StudentId, nextClass.ClassId, dto.ToAcademicYear);
                        result.PromotedCount++;
                    }
                    else
                    {
                        // No next class exists — this was the highest class (Class 10)
                        student.AdmissionStatus = AdmissionStatus.Graduated;
                        result.GraduatedCount++;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                result.Errors.Add(ex.Message);
            }

            return result;
        }
    }
}