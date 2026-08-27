// Controllers/ExamsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Accountant,Teacher")]
    public class ExamsController : ControllerBase
    {
        private readonly SchoolPortalDbContext _context;
        public ExamsController(SchoolPortalDbContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<List<ExamDto>>> GetAll()
        {
            var exams = await _context.Exams
                .Select(e => new ExamDto { ExamId = e.ExamId, ExamName = e.ExamName, Term = e.Term, AcademicYear = e.AcademicYear })
                .ToListAsync();
            return Ok(exams);
        }

        [Authorize(Roles = "Admin,Teacher")]
        [HttpPost]
        public async Task<ActionResult<ExamDto>> Create(CreateExamDto dto)
        {
            var exam = new Exam { ExamName = dto.ExamName, Term = dto.Term, AcademicYear = dto.AcademicYear };
            _context.Exams.Add(exam);
            await _context.SaveChangesAsync();
            return Ok(new ExamDto { ExamId = exam.ExamId, ExamName = exam.ExamName, Term = exam.Term, AcademicYear = exam.AcademicYear });
        }

        [Authorize(Roles = "Admin,Teacher")]
        [HttpPut("{id}")]
        public async Task<ActionResult<ExamDto>> Update(int id, CreateExamDto dto)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            exam.ExamName = dto.ExamName;
            exam.Term = dto.Term;
            exam.AcademicYear = dto.AcademicYear;
            await _context.SaveChangesAsync();

            return Ok(new ExamDto { ExamId = exam.ExamId, ExamName = exam.ExamName, Term = exam.Term, AcademicYear = exam.AcademicYear });
        }

        [Authorize(Roles = "Admin,Teacher")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            // Results.ExamId is a required FK with cascade delete configured at the
            // database level, so a raw delete here would silently wipe out any
            // recorded student results for this exam. Block it instead, matching
            // the Restrict-by-default philosophy used elsewhere in this DbContext
            // (Student<->Class, Payment<->Ledger, etc.).
            var hasResults = await _context.Results.AnyAsync(r => r.ExamId == id);
            if (hasResults)
            {
                return Conflict(new { message = "This exam has recorded results and cannot be deleted. Remove its results first if you really need to delete it." });
            }

            _context.Exams.Remove(exam);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
