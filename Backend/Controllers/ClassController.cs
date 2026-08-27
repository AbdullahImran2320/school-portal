// Controllers/ClassesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs;

namespace SchoolPortal.API.Controllers
{
    [ApiController]
    [Route("api/classes")]
    [Authorize(Roles = "Admin,Accountant,Teacher")]
    public class ClassesController : ControllerBase
    {
        private readonly SchoolPortalDbContext _context;
        public ClassesController(SchoolPortalDbContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<List<ClassDto>>> GetAll()
        {
            var classes = await _context.Classes
                .OrderBy(c => c.PromotionOrder)
                .Select(c => new ClassDto
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName,
                    AcademicYear = c.AcademicYear,
                    PromotionOrder = c.PromotionOrder
                })
                .ToListAsync();
            return Ok(classes);
        }
    }
}