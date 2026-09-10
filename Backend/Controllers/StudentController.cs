using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;
using SchoolPortal.API.Services;

namespace SchoolPortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Accountant,Teacher")]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly IStudentImportService _importService;

        public StudentsController(IStudentService studentService, IStudentImportService importService)
        {
            _studentService = studentService;
            _importService = importService;
        }

        [HttpGet]
        public async Task<ActionResult<List<StudentDto>>> GetAll()
        {
            return Ok(await _studentService.GetAllStudentsAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StudentDto>> GetById(int id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null) return NotFound();
            return Ok(student);
        }


        [Authorize(Roles = "Admin")]
        [HttpPost]

        public async Task<ActionResult<StudentDto>> Create(CreateStudentDto dto)
        {
            if (!Enum.TryParse<AdmissionStatus>(dto.AdmissionStatus, out _))
                return BadRequest(new { message = "Invalid AdmissionStatus. Must be Applied, Admitted, Withdrawn, Rejected, or Graduated." });
            var created = await _studentService.CreateStudentAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.StudentId }, created);
        }

        // Blank workbook with headers, formatted example row, and a * on
        // required columns — so the format is shown, not just documented,
        // and a typo'd column order (the #1 way bulk imports silently
        // corrupt data) is much less likely.
        [Authorize(Roles = "Admin")]
        [HttpGet("import/template")]
        public IActionResult DownloadImportTemplate()
        {
            var bytes = _importService.BuildTemplateWorkbook();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Student-Import-Template.xlsx");
        }

        // Processes every row independently — one bad row (a typo'd class
        // name, a malformed B-Form number) is reported against that row
        // and skipped, never aborts the other 999 good ones. Re-uploading
        // the same file after fixing the failed rows is safe: rows already
        // imported are detected by B-Form number and skipped, not
        // duplicated.
        [Authorize(Roles = "Admin")]
        [HttpPost("import")]
        [RequestSizeLimit(20_000_000)] // 20MB — generous for a spreadsheet of even several thousand rows
        public async Task<ActionResult<StudentImportResultDto>> ImportStudents(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var extension = Path.GetExtension(file.FileName);
            if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Please upload an .xlsx file (use the template download for the right format)." });

            using var stream = file.OpenReadStream();
            var result = await _importService.ImportAsync(stream);
            return Ok(result);
        }


        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateStudentDto dto)
        {
            if (!Enum.TryParse<AdmissionStatus>(dto.AdmissionStatus, out _))
                return BadRequest(new { message = "Invalid AdmissionStatus. Must be Applied, Admitted, Withdrawn, Rejected, or Graduated." });
            var updated = await _studentService.UpdateStudentAsync(id, dto);
            if (!updated) return NotFound();
            return NoContent();
        }


        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _studentService.DeleteStudentAsync(id);
                if (!deleted) return NotFound();
                return NoContent();
            }
            catch (DbUpdateException)
            {
                // FeeLedger/StudentCharge cascade-delete with the student, but
                // Payment rows restrict deletion of the ledger/charge they
                // reference — so a student with payment history can't be
                // deleted outright. Surface that as a clear 409, not a 500.
                return Conflict(new
                {
                    message = "This student has payment history and can't be deleted. " +
                               "Set their admission status to Withdrawn instead, or remove their payment records first."
                });
            }
        }

        [HttpGet("by-class/{classId}")]
        public async Task<ActionResult<List<StudentDto>>> GetByClass(int classId)
        {
            var all = await _studentService.GetAllStudentsAsync();
            return Ok(all.Where(s => s.ClassId == classId).ToList());
        }


        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/discount")]
        public async Task<IActionResult> SetDiscount(int id, SetDiscountDto dto)
        {
            var updated = await _studentService.SetDiscountAsync(id, dto.MonthlyDiscountAmount, dto.Reason, dto.ApplyToRemainingMonthsThisYear);
            if (!updated) return NotFound();
            return NoContent();
        }
    }
}