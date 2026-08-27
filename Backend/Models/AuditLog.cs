// Models/AuditLog.cs
namespace SchoolPortal.API.Models
{
    public class AuditLog
    {
        public int AuditLogId { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // Added, Modified, Deleted
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Details { get; set; } = string.Empty;
    }
}