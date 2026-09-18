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
        public string ClassCode { get; set; } = string.Empty;
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

        // Short code used to build this class's students' roll numbers,
        // e.g. "PGA" for Playgroup-A. Required up front — a class can't
        // silently sit with no code and later produce unformattable roll
        // numbers.
        [Required, MaxLength(10)]
        public string ClassCode { get; set; } = string.Empty;
    }

    public class AddSectionDto
    {
        [Required, MaxLength(50)]
        public string ClassName { get; set; } = string.Empty;

        [Required]
        public string AcademicYear { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Section { get; set; } = string.Empty;

        [Required, MaxLength(10)]
        public string ClassCode { get; set; } = string.Empty;
    }

    public class UpdateClassCodeDto
    {
        [Required, MaxLength(10)]
        public string ClassCode { get; set; } = string.Empty;
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

    public class MoveStudentsDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "A valid target section is required")]
        public int ToClassId { get; set; }
    }

    public class MoveStudentsResultDto
    {
        public int MovedCount { get; set; }
        public string FromLabel { get; set; } = string.Empty;
        public string ToLabel { get; set; } = string.Empty;
    }
}
