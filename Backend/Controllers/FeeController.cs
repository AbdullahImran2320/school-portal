// Controllers/FeeComponentsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;
using SchoolPortal.API.Repositories;

namespace SchoolPortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Accountant")]
    public class FeeComponentsController : ControllerBase
    {
        private readonly IFeeComponentRepository _repository;
        public FeeComponentsController(IFeeComponentRepository repository) => _repository = repository;

        [HttpGet]
        public async Task<ActionResult<List<FeeComponentDto>>> GetAll()
        {
            var items = await _repository.GetAllAsync();
            return Ok(items.Select(MapToDto));
        }

        [HttpGet("class/{classId}")]
        public async Task<ActionResult<List<FeeComponentDto>>> GetByClass(int classId)
        {
            var items = await _repository.GetByClassIdAsync(classId);
            return Ok(items.Select(MapToDto));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("rollover")]
        public async Task<IActionResult> Rollover([FromQuery] string fromYear, [FromQuery] string toYear)
        {
            var all = await _repository.GetAllAsync();
            var toCopy = all.Where(f => f.AcademicYear == fromYear).ToList();
            if (!toCopy.Any()) return NotFound(new { message = $"No fee components found for {fromYear}" });

            foreach (var f in toCopy)
            {
                await _repository.AddAsync(new FeeComponent
                {
                    ClassId = f.ClassId,
                    ComponentName = f.ComponentName,
                    Amount = f.Amount, // same rate — admin edits individual rows afterward if fees are increasing
                    Frequency = f.Frequency,
                    AcademicYear = toYear
                });
            }
            return Ok(new { message = $"Rolled over {toCopy.Count} fee components from {fromYear} to {toYear}" });
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<FeeComponentDto>> Create(UpsertFeeComponentDto dto)
        {
            if (!Enum.TryParse<FeeFrequency>(dto.Frequency, out _))
                return BadRequest(new { message = "Invalid Frequency. Must be OneTime, Yearly, or Monthly." });
            var entity = new FeeComponent
            {
                ComponentName = dto.ComponentName,
                Amount = dto.Amount,
                Frequency = Enum.Parse<FeeFrequency>(dto.Frequency),
                AcademicYear = dto.AcademicYear,
                ClassId = dto.ClassId
            };
            var created = await _repository.AddAsync(entity);
            var full = await _repository.GetByIdAsync(created.FeeComponentId);
            return Ok(MapToDto(full!));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpsertFeeComponentDto dto)
        {
            if (!Enum.TryParse<FeeFrequency>(dto.Frequency, out _))
                return BadRequest(new { message = "Invalid Frequency. Must be OneTime, Yearly, or Monthly." });
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.ComponentName = dto.ComponentName;
            existing.Amount = dto.Amount;
            existing.Frequency = Enum.Parse<FeeFrequency>(dto.Frequency);
            existing.AcademicYear = dto.AcademicYear;
            existing.ClassId = dto.ClassId;

            await _repository.UpdateAsync(existing);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        private static FeeComponentDto MapToDto(FeeComponent f) => new()
        {
            FeeComponentId = f.FeeComponentId,
            ComponentName = f.ComponentName,
            Amount = f.Amount,
            Frequency = f.Frequency.ToString(),
            AcademicYear = f.AcademicYear,
            ClassId = f.ClassId,
            ClassName = f.Class?.ClassName ?? ""
        };


    }
}