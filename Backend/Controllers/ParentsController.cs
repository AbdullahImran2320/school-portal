// Controllers/ParentsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Controllers
{
    [ApiController]
    [Route("api/parents")]
    [Authorize(Roles = "Admin,Accountant,Teacher")]
    public class ParentsController : ControllerBase
    {
        private readonly SchoolPortalDbContext _context;
        public ParentsController(SchoolPortalDbContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<List<ParentDto>>> GetAll()
        {
            var parents = await _context.Parents.ToListAsync();
            return Ok(parents.Select(MapToDto).ToList());
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<ParentDto>>> Search([FromQuery] string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile)) return Ok(new List<ParentDto>());

            var parents = await _context.Parents
                .Where(p => p.FatherMobile.Contains(mobile) ||
                            (p.MotherMobile != null && p.MotherMobile.Contains(mobile)))
                .ToListAsync();

            return Ok(parents.Select(MapToDto).ToList());
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ParentDto>> GetById(int id)
        {
            var parent = await _context.Parents.FindAsync(id);
            if (parent == null) return NotFound();
            return Ok(MapToDto(parent));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<ParentDto>> Create(CreateParentDto dto)
        {
            if (!Enum.TryParse<PrimaryGuardian>(dto.PrimaryGuardian, out var guardian))
                return BadRequest(new { message = "Invalid PrimaryGuardian. Must be Mother, Father, MotherAndFather, or Other." });

            var parent = new Parent
            {
                FatherName = dto.FatherName,
                FatherMobile = dto.FatherMobile,
                FatherOccupation = dto.FatherOccupation,
                MotherName = dto.MotherName,
                MotherMobile = dto.MotherMobile,
                PrimaryGuardian = guardian,
                Address = dto.Address
            };

            _context.Parents.Add(parent);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = parent.ParentId }, MapToDto(parent));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateParentDto dto)
        {
            if (!Enum.TryParse<PrimaryGuardian>(dto.PrimaryGuardian, out var guardian))
                return BadRequest(new { message = "Invalid PrimaryGuardian. Must be Mother, Father, MotherAndFather, or Other." });

            var parent = await _context.Parents.FindAsync(id);
            if (parent == null) return NotFound();

            parent.FatherName = dto.FatherName;
            parent.FatherMobile = dto.FatherMobile;
            parent.FatherOccupation = dto.FatherOccupation;
            parent.MotherName = dto.MotherName;
            parent.MotherMobile = dto.MotherMobile;
            parent.PrimaryGuardian = guardian;
            parent.Address = dto.Address;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static ParentDto MapToDto(Parent p) => new()
        {
            ParentId = p.ParentId,
            FatherName = p.FatherName,
            FatherMobile = p.FatherMobile,
            FatherOccupation = p.FatherOccupation,
            MotherName = p.MotherName,
            MotherMobile = p.MotherMobile,
            PrimaryGuardian = p.PrimaryGuardian.ToString(),
            Address = p.Address
        };
    }
}