// Controllers/ClassesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Controllers
{
    [ApiController]
    [Route("api/classes")]
    public class ClassesController : ControllerBase
    {
        private readonly SchoolPortalDbContext _context;
        public ClassesController(SchoolPortalDbContext context) => _context = context;

        // Read access stays open to Admin/Accountant/Teacher — they all need
        // the class list for dropdowns elsewhere in the app. Only the
        // write endpoints below are locked to Admin.
        [HttpGet]
        [Authorize(Roles = "Admin,Accountant,Teacher")]
        public async Task<ActionResult<List<ClassDto>>> GetAll()
        {
            var classes = await _context.Classes
                .OrderBy(c => c.PromotionOrder)
                .ThenBy(c => c.Section)
                .Select(c => new ClassDto
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName,
                    Section = c.Section,
                    AcademicYear = c.AcademicYear,
                    PromotionOrder = c.PromotionOrder,
                    StudentCount = c.Students.Count(s => s.AdmissionStatus == AdmissionStatus.Admitted)
                })
                .ToListAsync();
            return Ok(classes);
        }

        // Same rows as GetAll, grouped by class name — what the Manage
        // Classes screen actually binds to, so it doesn't have to
        // re-implement the grouping in the frontend.
        [HttpGet("grouped")]
        [Authorize(Roles = "Admin,Accountant,Teacher")]
        public async Task<ActionResult<List<ClassGroupDto>>> GetGrouped()
        {
            var flat = await GetAllInternal();

            var grouped = flat
                .GroupBy(c => new { c.ClassName, c.AcademicYear, c.PromotionOrder })
                .OrderBy(g => g.Key.PromotionOrder)
                .Select(g => new ClassGroupDto
                {
                    ClassName = g.Key.ClassName,
                    AcademicYear = g.Key.AcademicYear,
                    PromotionOrder = g.Key.PromotionOrder,
                    Sections = g.OrderBy(c => c.Section).ToList()
                })
                .ToList();

            return Ok(grouped);
        }

        private async Task<List<ClassDto>> GetAllInternal()
        {
            return await _context.Classes
                .OrderBy(c => c.PromotionOrder)
                .ThenBy(c => c.Section)
                .Select(c => new ClassDto
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName,
                    Section = c.Section,
                    AcademicYear = c.AcademicYear,
                    PromotionOrder = c.PromotionOrder,
                    StudentCount = c.Students.Count(s => s.AdmissionStatus == AdmissionStatus.Admitted)
                })
                .ToListAsync();
        }

        // Creates a brand-new grade level with no sections yet (Section = "").
        // Always appended at the end of the promotion sequence — inserting a
        // class in the middle isn't supported, since that would require
        // renumbering every PromotionOrder above it and quietly reshuffling
        // where every existing student promotes to next year.
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ClassDto>> CreateClass(CreateClassDto dto)
        {
            var name = dto.ClassName.Trim();

            var exists = await _context.Classes
                .AnyAsync(c => c.ClassName == name && c.AcademicYear == dto.AcademicYear);
            if (exists)
                return Conflict($"'{name}' already exists for {dto.AcademicYear}.");

            var nextOrder = await _context.Classes.AnyAsync()
                ? await _context.Classes.MaxAsync(c => c.PromotionOrder) + 1
                : 1;

            var newClass = new SchoolClass
            {
                ClassName = name,
                Section = "",
                AcademicYear = dto.AcademicYear,
                PromotionOrder = nextOrder
            };

            _context.Classes.Add(newClass);
            await _context.SaveChangesAsync();

            return Ok(new ClassDto
            {
                ClassId = newClass.ClassId,
                ClassName = newClass.ClassName,
                Section = newClass.Section,
                AcademicYear = newClass.AcademicYear,
                PromotionOrder = newClass.PromotionOrder,
                StudentCount = 0
            });
        }

        // Adds a section under an existing class name. Shares that class's
        // PromotionOrder and AcademicYear automatically — the admin only
        // picks the class and the section label.
        //
        // If the class currently only has its placeholder "" (unsectioned)
        // row and that row has no students yet, it's removed here so the
        // class cleanly switches over to being section-based instead of
        // leaving a phantom empty section behind.
        [HttpPost("sections")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ClassDto>> AddSection(AddSectionDto dto)
        {
            var section = dto.Section.Trim();

            var validLabel = await _context.SectionOptions.AnyAsync(s => s.Name == section);
            if (!validLabel)
                return BadRequest($"'{section}' isn't in the section list. Add it there first.");

            var siblingRows = await _context.Classes
                .Where(c => c.ClassName == dto.ClassName && c.AcademicYear == dto.AcademicYear)
                .ToListAsync();

            if (siblingRows.Count == 0)
                return NotFound($"No class named '{dto.ClassName}' for {dto.AcademicYear}.");

            if (siblingRows.Any(c => c.Section == section))
                return Conflict($"'{dto.ClassName}' already has a '{section}' section.");

            var promotionOrder = siblingRows[0].PromotionOrder;

            // Copy the fee structure from an existing row so a new section
            // isn't born with Rs 0 fees, requiring a manual re-entry every
            // time — prefer the unsectioned placeholder (the common case:
            // "Class 1" already has fees set up, now it's getting sections),
            // otherwise fall back to whichever section already exists.
            var sourceRowId = siblingRows.FirstOrDefault(c => c.Section == "")?.ClassId
                ?? siblingRows.First().ClassId;

            // Read these out BEFORE the placeholder might get deleted below —
            // FeeComponent.ClassId is a required foreign key, so EF Core
            // cascade-deletes a class's fee components the moment that class
            // row is deleted. Reading first and building plain copies (not
            // reusing the tracked entities) means the copies survive that
            // cascade regardless of which row disappears.
            var componentsToCopy = await _context.FeeComponents
                .Where(f => f.ClassId == sourceRowId && f.AcademicYear == dto.AcademicYear)
                .Select(f => new FeeComponent
                {
                    ComponentName = f.ComponentName,
                    Amount = f.Amount,
                    Frequency = f.Frequency,
                    AcademicYear = f.AcademicYear
                })
                .ToListAsync();

            var placeholder = siblingRows.FirstOrDefault(c => c.Section == "");
            if (placeholder != null)
            {
                var placeholderHasStudents = await _context.Students.AnyAsync(s => s.ClassId == placeholder.ClassId);
                if (!placeholderHasStudents)
                    _context.Classes.Remove(placeholder);
            }

            var newSection = new SchoolClass
            {
                ClassName = dto.ClassName,
                Section = section,
                AcademicYear = dto.AcademicYear,
                PromotionOrder = promotionOrder
            };

            _context.Classes.Add(newSection);
            await _context.SaveChangesAsync();

            if (componentsToCopy.Count > 0)
            {
                foreach (var component in componentsToCopy)
                    component.ClassId = newSection.ClassId;

                _context.FeeComponents.AddRange(componentsToCopy);
                await _context.SaveChangesAsync();
            }

            return Ok(new ClassDto
            {
                ClassId = newSection.ClassId,
                ClassName = newSection.ClassName,
                Section = newSection.Section,
                AcademicYear = newSection.AcademicYear,
                PromotionOrder = newSection.PromotionOrder,
                StudentCount = 0
            });
        }

        // Deletes a single row — either an unsectioned class or one section
        // of a sectioned class. Blocked while students are still assigned to it.
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSection(int id)
        {
            var cls = await _context.Classes.FindAsync(id);
            if (cls == null) return NotFound();

            var studentCount = await _context.Students.CountAsync(s => s.ClassId == id);
            if (studentCount > 0)
                return Conflict($"Can't delete — {studentCount} student(s) are still assigned. Reassign them first.");

            _context.Classes.Remove(cls);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Deletes an entire grade level — every section row that shares this
        // class name/year. Blocked if ANY of those sections still has students.
        [HttpDelete("group")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteClassGroup([FromQuery] string className, [FromQuery] string academicYear)
        {
            var rows = await _context.Classes
                .Where(c => c.ClassName == className && c.AcademicYear == academicYear)
                .ToListAsync();

            if (rows.Count == 0) return NotFound();

            var rowIds = rows.Select(r => r.ClassId).ToList();
            var studentCount = await _context.Students.CountAsync(s => rowIds.Contains(s.ClassId));
            if (studentCount > 0)
                return Conflict($"Can't delete '{className}' — {studentCount} student(s) are still assigned across its sections. Reassign them first.");

            _context.Classes.RemoveRange(rows);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // --- Section label list (the fixed, admin-managed dropdown source) ---

        [HttpGet("section-options")]
        [Authorize(Roles = "Admin,Accountant,Teacher")]
        public async Task<ActionResult<List<SectionOptionDto>>> GetSectionOptions()
        {
            var options = await _context.SectionOptions
                .OrderBy(s => s.Name)
                .Select(s => new SectionOptionDto { SectionOptionId = s.SectionOptionId, Name = s.Name })
                .ToListAsync();
            return Ok(options);
        }

        [HttpPost("section-options")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SectionOptionDto>> AddSectionOption(CreateSectionOptionDto dto)
        {
            var name = dto.Name.Trim();
            var exists = await _context.SectionOptions.AnyAsync(s => s.Name == name);
            if (exists) return Conflict($"'{name}' is already in the list.");

            var option = new SectionOption { Name = name };
            _context.SectionOptions.Add(option);
            await _context.SaveChangesAsync();

            return Ok(new SectionOptionDto { SectionOptionId = option.SectionOptionId, Name = option.Name });
        }

        [HttpDelete("section-options/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSectionOption(int id)
        {
            var option = await _context.SectionOptions.FindAsync(id);
            if (option == null) return NotFound();

            // Labels already used by an existing class row stay deletable —
            // deleting the label doesn't retroactively rename existing
            // sections, it just stops it being offered for new ones.
            _context.SectionOptions.Remove(option);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
