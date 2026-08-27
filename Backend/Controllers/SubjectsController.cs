// Controllers/SubjectsController.cs
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
    public class SubjectsController : ControllerBase
    {
        private readonly SchoolPortalDbContext _context;
        public SubjectsController(SchoolPortalDbContext context) => _context = context;

        [HttpGet("class/{classId}")]
        public async Task<ActionResult<List<SubjectDto>>> GetByClass(int classId)
        {
            var subjects = await _context.Subjects
                .Include(s => s.Class)
                .Where(s => s.ClassId == classId)
                .Select(s => new SubjectDto
                {
                    SubjectId = s.SubjectId,
                    SubjectName = s.SubjectName,
                    ClassId = s.ClassId,
                    ClassName = s.Class.ClassName
                })
                .ToListAsync();
            return Ok(subjects);
        }

        [Authorize(Roles = "Admin,Teacher")]
        [HttpPost]
        public async Task<ActionResult<SubjectDto>> Create(CreateSubjectDto dto)
        {
            var subject = new Subject { SubjectName = dto.SubjectName, ClassId = dto.ClassId };
            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();
            return Ok(new SubjectDto { SubjectId = subject.SubjectId, SubjectName = subject.SubjectName, ClassId = subject.ClassId });
        }

        [Authorize(Roles = "Admin,Teacher")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null) return NotFound();
            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}