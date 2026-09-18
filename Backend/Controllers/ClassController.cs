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
                    ClassCode = c.ClassCode,
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
                    ClassCode = c.ClassCode,
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
            var code = dto.ClassCode.Trim();

            var exists = await _context.Classes
                .AnyAsync(c => c.ClassName == name && c.AcademicYear == dto.AcademicYear);
            if (exists)
                return Conflict($"'{name}' already exists for {dto.AcademicYear}.");

            var codeTaken = await _context.Classes.AnyAsync(c => c.ClassCode == code);
            if (codeTaken)
                return Conflict($"Roll number code '{code}' is already used by another class. Each class needs its own code.");

            var nextOrder = await _context.Classes.AnyAsync()
                ? await _context.Classes.MaxAsync(c => c.PromotionOrder) + 1
                : 1;

            var newClass = new SchoolClass
            {
                ClassName = name,
                Section = "",
                AcademicYear = dto.AcademicYear,
                PromotionOrder = nextOrder,
                ClassCode = code
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
                ClassCode = newClass.ClassCode,
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
            var code = dto.ClassCode.Trim();

            var validLabel = await _context.SectionOptions.AnyAsync(s => s.Name == section);
            if (!validLabel)
                return BadRequest($"'{section}' isn't in the section list. Add it there first.");

            var codeTaken = await _context.Classes.AnyAsync(c => c.ClassCode == code);
            if (codeTaken)
                return Conflict($"Roll number code '{code}' is already used by another class/section. Each needs its own code.");

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
                PromotionOrder = promotionOrder,
                ClassCode = code
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
                ClassCode = newSection.ClassCode,
                StudentCount = 0
            });
        }

        // The class code can also be set or corrected after the fact —
        // e.g. for classes that existed before this feature and still have
        // a blank code. Kept as its own endpoint (like SetRollNumber on
        // students) rather than folded into a general class edit, since no
        // general "edit class" endpoint exists yet.
        [HttpPut("{id}/code")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ClassDto>> UpdateClassCode(int id, UpdateClassCodeDto dto)
        {
            var cls = await _context.Classes.FindAsync(id);
            if (cls == null) return NotFound();

            var code = dto.ClassCode.Trim();
            var codeTaken = await _context.Classes.AnyAsync(c => c.ClassCode == code && c.ClassId != id);
            if (codeTaken)
                return Conflict($"Roll number code '{code}' is already used by another class/section. Each needs its own code.");

            cls.ClassCode = code;
            await _context.SaveChangesAsync();

            return Ok(new ClassDto
            {
                ClassId = cls.ClassId,
                ClassName = cls.ClassName,
                Section = cls.Section,
                AcademicYear = cls.AcademicYear,
                PromotionOrder = cls.PromotionOrder,
                ClassCode = cls.ClassCode,
                StudentCount = await _context.Students.CountAsync(s => s.ClassId == cls.ClassId && s.AdmissionStatus == AdmissionStatus.Admitted)
            });
        }

        // Moves every student out of one section and into another section of
        // the SAME grade/year. This is the missing half of "Can't delete —
        // N student(s) are still assigned. Reassign them first." below: it's
        // what actually does that reassigning, in one click, for the common
        // case of an accidentally-created or now-empty-ish section.
        //
        // Deliberately restricted to sibling sections (same ClassName +
        // AcademicYear) rather than any class: that's the one case where
        // nothing else needs to change. Fee structure is configured per
        // grade, not per section, and this never touches already-generated
        // FeeLedger/StudentCharge rows (they're keyed by StudentId + Year,
        // not ClassId, so a same-grade section move doesn't invalidate
        // anything already billed). Moving a student to a genuinely
        // different grade is a separate action that already exists —
        // editing that one student's Class on the Students page — since a
        // cross-grade move can have real fee/promotion implications this
        // bulk action intentionally stays out of.
        [HttpPost("{fromClassId}/move-students")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<MoveStudentsResultDto>> MoveStudents(int fromClassId, MoveStudentsDto dto)
        {
            if (fromClassId == dto.ToClassId)
                return BadRequest("Source and target section are the same.");

            var fromClass = await _context.Classes.FindAsync(fromClassId);
            if (fromClass == null) return NotFound($"Section {fromClassId} not found.");

            var toClass = await _context.Classes.FindAsync(dto.ToClassId);
            if (toClass == null) return NotFound($"Target section {dto.ToClassId} not found.");

            if (fromClass.ClassName != toClass.ClassName || fromClass.AcademicYear != toClass.AcademicYear)
                return BadRequest("Students can only be bulk-moved between sections of the same class and year. To move a student to a different grade, edit that student individually instead.");

            var students = await _context.Students.Where(s => s.ClassId == fromClassId).ToListAsync();

            foreach (var student in students)
            {
                student.ClassId = toClass.ClassId;

                // Same rule as StudentService.UpdateStudentAsync: a roll
                // number is tied to which class register a student appears
                // in day to day, so it's cleared on a class change rather
                // than silently carried over into the new section — the
                // Admin can assign a fresh one afterward if they want.
                student.RollNumber = null;
                student.RollNumberSequence = null;
            }

            await _context.SaveChangesAsync();

            string Label(SchoolClass c) => string.IsNullOrEmpty(c.Section) ? c.ClassName : $"{c.ClassName} - {c.Section}";

            return Ok(new MoveStudentsResultDto
            {
                MovedCount = students.Count,
                FromLabel = Label(fromClass),
                ToLabel = Label(toClass)
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
