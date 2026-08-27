// Controllers/AuditLogsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs;

namespace SchoolPortal.API.Controllers
{
    [ApiController]
    [Route("api/auditlogs")]
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : ControllerBase
    {
        private readonly SchoolPortalDbContext _context;
        public AuditLogsController(SchoolPortalDbContext context) => _context = context;

        [HttpGet]
        public async Task<ActionResult<List<AuditLogDto>>> GetAll(
            [FromQuery] string? entityName,
            [FromQuery] string? entityId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var query = _context.AuditLogs.AsQueryable();
            if (!string.IsNullOrEmpty(entityName)) query = query.Where(a => a.EntityName == entityName);
            if (!string.IsNullOrEmpty(entityId)) query = query.Where(a => a.EntityId == entityId);
            if (from.HasValue) query = query.Where(a => a.Timestamp >= from.Value);
            if (to.HasValue) query = query.Where(a => a.Timestamp <= to.Value);

            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Take(500) // cap the response — this table only grows, see note below
                .Select(a => new AuditLogDto
                {
                    AuditLogId = a.AuditLogId,
                    EntityName = a.EntityName,
                    EntityId = a.EntityId,
                    Action = a.Action,
                    ChangedBy = a.ChangedBy,
                    Timestamp = a.Timestamp,
                    Details = a.Details
                })
                .ToListAsync();

            return Ok(logs);
        }
    }
}