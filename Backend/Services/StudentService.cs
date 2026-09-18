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

        // The globally-editable parts of the roll number format (prefix and
        // sequence padding). Defaults are returned rather than created here
        // so generation always works even before an admin has visited the
        // Roll Number Settings screen — SettingsController is what actually
        // persists a row the first time it's saved.
        private async Task<(string Prefix, int Digits)> GetRollNumberFormatAsync()
        {
            var settings = await _context.RollNumberSettings.FirstOrDefaultAsync();
            return settings == null ? ("R", 3) : (settings.Prefix, settings.SequenceDigits);
        }

        // {Prefix}{2-digit admission year}{ClassCode}{padded sequence},
        // e.g. prefix "F", admitted 2024, class code "PGA", sequence 1 ->
        // "F24PGA001". The admission year comes from the student's own
        // AdmissionDate, not the class's AcademicYear, since a student can
        // be admitted mid-year into a class/section that was itself opened
        // in an earlier year.
        private static string BuildRollNumberCode(string prefix, int digits, DateTime admissionDate, string classCode, int sequence)
        {
            var admissionYear = (admissionDate.Year % 100).ToString("D2");
            var paddedSequence = sequence.ToString().PadLeft(digits, '0');
            return $"{prefix}{admissionYear}{classCode}{paddedSequence}";
        }

        // Roll number settings are a singleton row, same pattern as
        // ChallanSettings — read here (and created with sane defaults on
        // first use) rather than requiring the Admin to visit the settings
        // screen before a single student can ever be created.
        private async Task<RollNumberSettings> GetOrCreateRollNumberSettingsAsync()
        {
            var settings = await _context.RollNumberSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new RollNumberSettings();
                _context.RollNumberSettings.Add(settings);
                await _context.SaveChangesAsync();
            }
            return settings;
        }

        // {Prefix}{2-digit admission year}{ClassCode}{padded sequence}, e.g.
        // "F24PGA001". The admission year comes from the student's own
        // AdmissionDate (not the class's AcademicYear) — see the design
        // discussion: "F24" means the year this particular student was
        // admitted, which is usually but not always the same as the class's
        // year.
        private static string BuildRollNumberCode(RollNumberSettings settings, DateTime admissionDate, string classCode, int sequence)
        {
            var yearSuffix = (admissionDate.Year % 100).ToString("D2");
            var paddedSequence = sequence.ToString().PadLeft(Math.Max(1, settings.SequenceDigits), '0');
            return $"{settings.Prefix}{yearSuffix}{classCode}{paddedSequence}";
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
            var schoolClass = await _context.Classes.FindAsync(dto.ClassId);
            if (schoolClass == null)
                throw new RollNumberGenerationException("Selected class was not found.");
            if (string.IsNullOrWhiteSpace(schoolClass.ClassCode))
                throw new RollNumberGenerationException(
                    $"'{schoolClass.ClassName}{(string.IsNullOrEmpty(schoolClass.Section) ? "" : " - " + schoolClass.Section)}' doesn't have a roll number code set yet. Set one in Manage Classes first.");

            int sequence;
            if (dto.RollNumberSequence.HasValue)
            {
                var taken = await _context.Students.AnyAsync(s =>
                    s.ClassId == dto.ClassId && s.RollNumberSequence == dto.RollNumberSequence.Value);
                if (taken) throw new DuplicateRollNumberException(dto.RollNumberSequence.Value);
                sequence = dto.RollNumberSequence.Value;
            }
            else
            {
                // Auto-assign the next position in this class's own
                // sequence. This is a simple "take the next one" counter,
                // not a retroactive re-sort by admission date — moving the
                // whole register every time an old admission date is
                // backfilled would be far more disruptive than useful in
                // practice.
                var maxSequence = await _context.Students
                    .Where(s => s.ClassId == dto.ClassId && s.RollNumberSequence != null)
                    .MaxAsync(s => (int?)s.RollNumberSequence) ?? 0;
                sequence = maxSequence + 1;
            }

            var settings = await GetOrCreateRollNumberSettingsAsync();
            var rollNumberCode = BuildRollNumberCode(settings, dto.AdmissionDate, schoolClass.ClassCode, sequence);

            var student = new Student
            {
                Name = dto.Name,
                RollNumber = rollNumberCode,
                RollNumberSequence = sequence,
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

            // A roll number is built from which class register the student
            // actually appears in day to day (its ClassCode and its own
            // position in that class), so moving classes clears both the
            // formatted code and the position outright rather than leaving
            // a number from their old class silently attached to the new
            // one. Reassigning afterward is a deliberate, separate action
            // via SetRollNumberAsync, not an incidental side effect someone
            // could miss while just fixing a typo in the student's name.
            if (student.ClassId != dto.ClassId)
            {
                student.RollNumber = null;
                student.RollNumberSequence = null;
            }
            student.ClassId = dto.ClassId;

            return await _repository.UpdateAsync(student);
        }

        public async Task<(bool Success, string? Error)> SetRollNumberAsync(int studentId, int desiredSequence)
        {
            if (desiredSequence < 1)
                return (false, "Roll number position must be a positive number.");

            var student = await _repository.GetByIdAsync(studentId);
            if (student == null) return (false, "Student not found.");

            if (student.Class == null || string.IsNullOrWhiteSpace(student.Class.ClassCode))
                return (false, "This student's class doesn't have a roll number code set yet. Set one in Manage Classes first.");

            // A reorder, not a plain assignment, same as before — but now
            // scoped to this student's own class/section instead of the
            // whole school: a formatted code like "F24C10A012" has no
            // meaning outside the class it was built for, so "shift by
            // one" only makes sense among the student's actual classmates.
            // Every classmate who already has a position is treated as one
            // continuous sequence, and moving this student to a new spot
            // shifts everyone between their old and new position by one —
            // the same behavior as dragging an item to a new position in
            // an ordered list, so positions never collide.
            var ordered = await _context.Students
                .Where(s => s.ClassId == student.ClassId && s.RollNumberSequence != null && s.StudentId != studentId)
                .OrderBy(s => s.RollNumberSequence)
                .ToListAsync();

            var insertIndex = Math.Clamp(desiredSequence - 1, 0, ordered.Count);
            ordered.Insert(insertIndex, student);

            var settings = await GetOrCreateRollNumberSettingsAsync();
            var classCode = student.Class.ClassCode;

            // Two-phase save inside one transaction. Students.RollNumber has
            // a UNIQUE index, and SQLite checks it after every single UPDATE
            // statement, so shifting positions one row at a time (e.g. two
            // students briefly both holding "F26NRA02") throws
            // "UNIQUE constraint failed: Students.RollNumber".
            // Phase 1 frees every affected roll number (NULLs are allowed by
            // the filtered index); phase 2 writes the final values, none of
            // which can collide because every code is now unused. If
            // anything fails, the transaction rolls back and nobody loses
            // their existing roll number.
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var s in ordered)
                    s.RollNumber = null;
                await _context.SaveChangesAsync();

                for (int i = 0; i < ordered.Count; i++)
                {
                    var sequence = i + 1;
                    ordered[i].RollNumberSequence = sequence;
                    ordered[i].RollNumber = BuildRollNumberCode(settings, ordered[i].AdmissionDate, classCode, sequence);
                }
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

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
            RollNumberSequence = s.RollNumberSequence,
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