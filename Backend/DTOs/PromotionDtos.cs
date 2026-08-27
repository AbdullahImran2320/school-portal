// DTOs/PromotionDtos.cs
using System.ComponentModel.DataAnnotations;

namespace SchoolPortal.API.DTOs
{
    public class PromoteClassesDto
    {
        [Required]
        public string FromAcademicYear { get; set; } = string.Empty;

        [Required]
        public string ToAcademicYear { get; set; } = string.Empty;

        public List<int> HoldBackStudentIds { get; set; } = new(); // repeaters — stay in the same class
    }

    public class PromotionResultDto
    {
        public int PromotedCount { get; set; }
        public int GraduatedCount { get; set; }
        public int HeldBackCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}