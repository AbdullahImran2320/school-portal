// Models/Student.cs
namespace SchoolPortal.API.Models
{
    public enum AdmissionStatus
    {
        Applied,
        Admitted,
        Withdrawn,
        Rejected,
        Graduated
    }

    public class Student
    {
        public int StudentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? RollNumber { get; set; } // unique school-wide; null means not yet assigned
        public string? PhotoFileName { get; set; } // stored on disk, see StudentPhotoService; null means no photo set
        public string BFormNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        // Filename only, never a full path — the actual file lives in a
        // StudentPhotos folder next to the database (see StudentsController),
        // resolved the same way Backups resolves its folder: relative to
        // wherever the live SQLite file actually is, not a hardcoded path
        // and never inside wwwroot (which build.bat deletes and recreates
        // on every production build — storing photos there would silently
        // wipe them out the next time the app is rebuilt/reinstalled).
        public DateTime AdmissionDate { get; set; }
        public AdmissionStatus AdmissionStatus { get; set; } = AdmissionStatus.Applied;
        public decimal MonthlyDiscountAmount { get; set; } = 0;
        public string? DiscountReason { get; set; } // "Sibling Discount", "Staff Scholarship", etc.

        // Foreign keys
        public int ClassId { get; set; }
        public SchoolClass Class { get; set; } = null!;

        public int ParentId { get; set; }
        public Parent Parent { get; set; } = null!;
      
   
    }
}