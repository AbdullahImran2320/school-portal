// DTOs/ClassDtos.cs
using System.ComponentModel.DataAnnotations;

namespace SchoolPortal.API.DTOs
{
    public class ClassDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty; // "" means no sections yet
        public string AcademicYear { get; set; } = string.Empty;
        public int PromotionOrder { get; set; }
        public int StudentCount { get; set; }
    }

    // Groups every SchoolClass row that shares a ClassName — one entry per
    // grade level, with its sections nested inside. This is what the
    // Manage Classes screen actually renders.
    public class ClassGroupDto
    {
        public string ClassName { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public int PromotionOrder { get; set; }
        public List<ClassDto> Sections { get; set; } = new();
    }

    public class CreateClassDto
    {
        [Required, MaxLength(50)]
        public string ClassName { get; set; } = string.Empty;

        [Required]
        public string AcademicYear { get; set; } = string.Empty;
    }

    public class AddSectionDto
    {
        [Required, MaxLength(50)]
        public string ClassName { get; set; } = string.Empty;

        [Required]
        public string AcademicYear { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Section { get; set; } = string.Empty;
    }

    public class SectionOptionDto
    {
        public int SectionOptionId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CreateSectionOptionDto
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;
    }
}
