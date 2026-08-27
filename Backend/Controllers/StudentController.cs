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

        public StudentsController(IStudentService studentService)
        {
            _studentService = studentService;
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