// Services/RollNumberRebuilder.cs
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;

namespace SchoolPortal.API.Services
{
    // Rebuilds already-issued roll numbers from what is actually stored:
    // the current prefix/padding, the class's current code, the student's
    // own admission year, and the student's position (RollNumberSequence).
    // Positions never change, so nobody's order in the class moves — only
    // the text of the roll number is refreshed.
    //
    // Called after the Admin edits the Roll Number Settings or a class's
    // roll number code, so existing students never keep a stale prefix or
    // an old code. Keep the format in sync with
    // StudentService.BuildRollNumberCode:
    //   {Prefix}{2-digit admission year}{ClassCode}{padded sequence}
    public static class RollNumberRebuilder
    {
        // classId == null rebuilds every class; otherwise only that class.
        public static async Task RebuildAsync(SchoolPortalDbContext context, int? classId = null)
        {
            var settings = await context.RollNumberSettings.OrderBy(s => s.Id).FirstOrDefaultAsync();
            var prefix = settings?.Prefix ?? "R";
            var digits = Math.Max(1, settings?.SequenceDigits ?? 3);

            var query = context.Students
                .Include(s => s.Class)
                .Where(s => s.RollNumberSequence != null);
            if (classId.HasValue)
                query = query.Where(s => s.ClassId == classId.Value);

            var students = (await query.ToListAsync())
                .Where(s => s.Class != null && !string.IsNullOrWhiteSpace(s.Class.ClassCode))
                .ToList();
            if (students.Count == 0) return;

            // Students.RollNumber has a UNIQUE index that SQLite enforces
            // after every single UPDATE, so rewriting values in place can
            // briefly clash. Phase 1 frees every affected number, phase 2
            // writes the final ones; both inside one transaction so a
            // failure leaves the old numbers untouched.
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                foreach (var s in students)
                    s.RollNumber = null;
                await context.SaveChangesAsync();

                foreach (var s in students)
                {
                    var year = (s.AdmissionDate.Year % 100).ToString("D2");
                    var sequence = s.RollNumberSequence!.Value.ToString().PadLeft(digits, '0');
                    s.RollNumber = $"{prefix}{year}{s.Class.ClassCode}{sequence}";
                }
                await context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
