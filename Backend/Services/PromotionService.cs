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
                // PromotionOrder now identifies a GRADE LEVEL, not a single row —
                // every section of "Class 1" shares the same PromotionOrder.
                // Group first, so "next class" means "next grade level", and a
                // grade can have any number of sections without breaking this.
                var allClasses = await _context.Classes.ToListAsync();
                var classesByOrder = allClasses
                    .GroupBy(c => c.PromotionOrder)
                    .ToDictionary(g => g.Key, g => g.ToList());

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

                    var nextOrder = student.Class.PromotionOrder + 1;
                    if (!classesByOrder.TryGetValue(nextOrder, out var nextGradeSections) || nextGradeSections.Count == 0)
                    {
                        // No next grade exists — this was the highest class (Class 10)
                        student.AdmissionStatus = AdmissionStatus.Graduated;
                        result.GraduatedCount++;
                        continue;
                    }

                    SchoolClass? targetClass;

                    if (nextGradeSections.Count == 1)
                    {
                        // Next grade isn't sectioned (or only has one section) —
                        // no ambiguity, everyone goes there.
                        targetClass = nextGradeSections[0];
                    }
                    else
                    {
                        // Multiple sections up there — only auto-promote if the
                        // student's current section name matches one exactly.
                        targetClass = nextGradeSections.FirstOrDefault(c =>
                            string.Equals(c.Section, student.Class.Section, StringComparison.OrdinalIgnoreCase));
                    }

                    if (targetClass == null)
                    {
                        // Ambiguous — leave this student exactly where they are
                        // and flag it for the Admin to resolve manually.
                        result.UnresolvedSections.Add(new UnresolvedPromotionDto
                        {
                            StudentId = student.StudentId,
                            StudentName = student.Name,
                            CurrentClassName = student.Class.ClassName,
                            CurrentSection = student.Class.Section,
                            AvailableSectionsInNextGrade = nextGradeSections.Select(c => c.Section).ToList()
                        });
                        continue;
                    }

                    student.ClassId = targetClass.ClassId;
                    await _feeEngineService.GenerateFeeRecordsForStudentAsync(student.StudentId, targetClass.ClassId, dto.ToAcademicYear);
                    result.PromotedCount++;
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
