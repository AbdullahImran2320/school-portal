using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly SchoolPortalDbContext _context;
        public UsersController(SchoolPortalDbContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<List<UserSummaryDto>>> GetAll()
        {
            var users = await _context.Users
                .Select(u => new UserSummaryDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    FullName = u.FullName,
                    Role = u.Role.ToString()
                })
                .ToListAsync();
            return Ok(users);
        }

        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateRole(int id, UpdateRoleDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (!Enum.TryParse<UserRole>(dto.Role, out var newRole))
                return BadRequest(new { message = "Invalid role. Must be Pending, Teacher, Accountant, or Admin." });

            user.Role = newRole;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}