using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Repositories
{


    public class StudentRepository : IStudentRepository
    {
        private readonly SchoolPortalDbContext _context;

        public StudentRepository(SchoolPortalDbContext context)
        {
            _context = context;
        }

        public async Task<List<Student>> GetAllAsync()
        {
            // Ordered the way the classes are arranged (Playgroup, Nursery,
            // Prep, Class 1, ...), then by section (A, B, ...), then by the
            // student's position in that section. Without an explicit order
            // the database returns rows in the order they were inserted, so
            // bulk-imported students all landed at the bottom of the list.
            // Students without a roll number yet come last within their
            // section, alphabetically.
            return await _context.Students
                .Include(s => s.Class)
                .Include(s => s.Parent)
                .OrderBy(s => s.Class.PromotionOrder)
                .ThenBy(s => s.Class.Section)
                .ThenBy(s => s.Class.AcademicYear)
                .ThenBy(s => s.RollNumberSequence == null)
                .ThenBy(s => s.RollNumberSequence)
                .ThenBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Student?> GetByIdAsync(int id)
        {
            return await _context.Students
                .Include(s => s.Class)
                .Include(s => s.Parent)
                .FirstOrDefaultAsync(s => s.StudentId == id);
        }

        public async Task<Student> AddAsync(Student student)
        {
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            return student;
        }

        public async Task<bool> UpdateAsync(Student student)
        {
            _context.Students.Update(student);
            var rows = await _context.SaveChangesAsync();
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return false;

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}