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

        // Short, admin-set code used to build each student's formatted roll
        // number for this class/section, e.g. "PGA" for Playgroup-A, "C10A"
        // for Class 10-A. Blank until the admin sets one — students in a
        // class with no code yet can't be assigned a formatted roll number.
        public string ClassCode { get; set; } = string.Empty;

        // Navigation
        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}