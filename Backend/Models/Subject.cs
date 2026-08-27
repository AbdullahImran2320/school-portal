// Models/Subject.cs
namespace SchoolPortal.API.Models
{
    public class Subject
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;

        public int ClassId { get; set; }
        public SchoolClass Class { get; set; } = null!;
    }
}