// DTOs/StudentImportDtos.cs
namespace SchoolPortal.API.DTOs
{
    public class StudentImportRowResult
    {
        public int RowNumber { get; set; }
        public string StudentName { get; set; } = string.Empty;

        // "Imported", "Skipped" (already exists), or "Failed" (bad data)
        public string Outcome { get; set; } = string.Empty;
        public string? Detail { get; set; }
    }

    public class StudentImportResultDto
    {
        public int TotalRows { get; set; }
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public int FailedCount { get; set; }
        public List<StudentImportRowResult> Results { get; set; } = new();
    }
}
