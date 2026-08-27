// Models/Parent.cs
namespace SchoolPortal.API.Models
{
    public enum PrimaryGuardian
    {
        Mother,
        Father,
        MotherAndFather,
        Other
    }

    public class Parent
    {
        public int ParentId { get; set; }

        public string FatherName { get; set; } = string.Empty;
        public string FatherMobile { get; set; } = string.Empty;
        public string? FatherOccupation { get; set; }

        public string? MotherName { get; set; }
        public string? MotherMobile { get; set; }

        public PrimaryGuardian PrimaryGuardian { get; set; } = PrimaryGuardian.Father;
        public string Address { get; set; } = string.Empty;

        // Navigation
        public ICollection<Student> Children { get; set; } = new List<Student>();
    }
}