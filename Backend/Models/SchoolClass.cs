// Models/SchoolClass.cs
namespace SchoolPortal.API.Models
{
    public class SchoolClass
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty; // e.g. "5th Grade"
        public string Section { get; set; } = string.Empty;   // e.g. "A"
        public string AcademicYear { get; set; } = string.Empty; // single year, e.g. "2026"
        public int PromotionOrder { get; set; }

        // Navigation
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}