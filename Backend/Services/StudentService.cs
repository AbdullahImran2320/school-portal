using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;
using SchoolPortal.API.Repositories;

namespace SchoolPortal.API.Services
{

    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _repository;
        private readonly IFeeEngineService _feeEngineService;
        private readonly SchoolPortalDbContext _context;

        public StudentService(IStudentRepository repository, IFeeEngineService feeEngineService, SchoolPortalDbContext context)
        {
            _repository = repository;
            _feeEngineService = feeEngineService;
            _context = context;
        }

        private async Task<string> GetAcademicYearForClassAsync(int classId)
        {
            var schoolClass = await _context.Classes.FindAsync(classId);
            // Falls back to the current calendar year only if the class
            // record is somehow missing — the class's own AcademicYear is
            // the real source of truth, not a hardcoded literal.
            return schoolClass?.AcademicYear ?? DateTime.Now.Year.ToString();
        }
        public async Task<List<StudentDto>> GetAllStudentsAsync()
        {
            var students = await _repository.GetAllAsync();
            return students.Select(MapToDto).ToList();
        }

        public async Task<StudentDto?> GetStudentByIdAsync(int id)
        {
            var student = await _repository.GetByIdAsync(id);
            return student == null ? null : MapToDto(student);
        }

        public async Task<StudentDto> CreateStudentAsync(CreateStudentDto dto)
        {
            int rollNumber;
            if (dto.RollNumber.HasValue)
            {
                var taken = await _context.Students.AnyAsync(s => s.RollNumber == dto.RollNumber.Value);
                if (taken) throw new DuplicateRollNumberException(dto.RollNumber.Value);
                rollNumber = dto.RollNumber.Value;
            }
            else
            {
                // Auto-assign the next number in the school-wide sequence.
                // This is a simple "take the next one" counter, not a
                // retroactive re-sort by admission date — moving the whole
                // register every time an old admission date is backfilled
                // would be far more disruptive than useful in practice.
                var maxRoll = await _context.Students
                    .Where(s => s.RollNumber != null)
                    .MaxAsync(s => (int?)s.RollNumber) ?? 0;
                rollNumber = maxRoll + 1;
            }

            var student = new Student
            {
                Name = dto.Name,
                RollNumber = rollNumber,
                BFormNumber = dto.BFormNumber,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
                AdmissionDate = dto.AdmissionDate,
                AdmissionStatus = Enum.Parse<AdmissionStatus>(dto.AdmissionStatus),
                ClassId = dto.ClassId,
                ParentId = dto.ParentId
            };

            var created = await _repository.AddAsync(student);
            var academicYear = await GetAcademicYearForClassAsync(created.ClassId);
            await _feeEngineService.GenerateFeeRecordsForStudentAsync(created.StudentId, created.ClassId, academicYear);

            var full = await _repository.GetByIdAsync(created.StudentId);
            return MapToDto(full!);
           
        }

        public async Task<bool> UpdateStudentAsync(int id, UpdateStudentDto dto)
        {
            var student = await _repository.GetByIdAsync(id);
            if (student == null) return false;

            student.Name = dto.Name;
            student.BFormNumber = dto.BFormNumber;
            student.DateOfBirth = dto.DateOfBirth;
            student.Gender = dto.Gender;
            student.AdmissionStatus = Enum.Parse<AdmissionStatus>(dto.AdmissionStatus);

            // A roll number is unique school-wide, but still tied to which
            // class register the student actually appears in day to day —
            // moving classes clears it outright rather than leaving a
            // number from their old class silently attached to the new
            // one. Reassigning afterward is a deliberate, separate action
            // via SetRollNumberAsync, not an incidental side effect someone
            // could miss while just fixing a typo in the student's name.
            if (student.ClassId != dto.ClassId)
            {
                student.RollNumber = null;
            }
            student.ClassId = dto.ClassId;

            return await _repository.UpdateAsync(student);
        }

        public async Task<(bool Success, string? Error)> SetRollNumberAsync(int studentId, int rollNumber)
        {
            var student = await _repository.GetByIdAsync(studentId);
            if (student == null) return (false, "Student not found.");

            var takenByOther = await _context.Students
                .AnyAsync(s => s.RollNumber == rollNumber && s.StudentId != studentId);
            if (takenByOther)
                return (false, $"Roll number {rollNumber} is already assigned to another student.");

            student.RollNumber = rollNumber;
            await _repository.UpdateAsync(student);
            return (true, null);
        }

        public async Task<bool> DeleteStudentAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        private static StudentDto MapToDto(Student s) => new()
        {
            StudentId = s.StudentId,
            Name = s.Name,
            RollNumber = s.RollNumber,
            BFormNumber = s.BFormNumber,
            DateOfBirth = s.DateOfBirth,
            Gender = s.Gender,
            AdmissionDate = s.AdmissionDate,
            AdmissionStatus = s.AdmissionStatus.ToString(),
            ClassId = s.ClassId,
            ClassName = s.Class?.ClassName ?? "",
            Section = s.Class?.Section ?? "",
            HasPhoto = !string.IsNullOrEmpty(s.PhotoFileName),
            ParentId = s.ParentId,
            FatherName = s.Parent?.FatherName ?? "",
            FatherMobile = s.Parent?.FatherMobile ?? "",
            MotherName = s.Parent?.MotherName,
            MotherMobile = s.Parent?.MotherMobile
        };

        public async Task<bool> SetDiscountAsync(int studentId, decimal amount, string? reason, bool applyToRemainingMonths)
        {
            var student = await _repository.GetByIdAsync(studentId);
            if (student == null) return false;

            // Same defense-in-depth as FeeEngineService: this method takes a
            // raw decimal, so it doesn't inherit SetDiscountDto's [Range]
            // validation from whatever calls it. Clamped here so a negative
            // discount can never be stored regardless of caller.
            amount = Math.Max(0, amount);

            student.MonthlyDiscountAmount = amount;
            student.DiscountReason = reason;
            await _repository.UpdateAsync(student);

            if (applyToRemainingMonths)
            {
                // Applies to every genuinely unpaid month in the year, past
                // or future — not just "this month onward". A month that's
                // overdue is still stored as Unpaid (this system never
                // actually sets LedgerStatus.Overdue; "overdue" is purely a
                // computed display label based on the due date), so
                // excluding anything before "today" was quietly skipping
                // already-passed months a concession should still cover —
                // exactly the July-vs-September inconsistency this fixes.
                //
                // Paid and Partial months are still never touched — that
                // would retroactively alter money that's already been
                // collected and recorded, which is a different and much
                // riskier kind of change than backfilling an unpaid month.
                var now = DateTime.Now;
                var candidateMonths = await _context.FeeLedgers.Where(l =>
                    l.StudentId == studentId &&
                    l.Year == now.Year &&
                    l.Status == LedgerStatus.Unpaid)
                    .ToListAsync();

                var unpaidFutureMonths = candidateMonths
                    .Where(l => FeeCalculator.IsApplicableMonth(student.AdmissionDate, l.MonthNumber, l.Year))
                    .ToList();

                foreach (var l in unpaidFutureMonths)
                {
                    l.DiscountAmount = amount;
                }
                await _context.SaveChangesAsync();
            }

            return true;
        }
    }
}