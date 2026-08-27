// DTOs/AuditLogDto.cs
namespace SchoolPortal.API.DTOs
{
    public class AuditLogDto
    {
        public int AuditLogId { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Details { get; set; } = string.Empty;
    }
}