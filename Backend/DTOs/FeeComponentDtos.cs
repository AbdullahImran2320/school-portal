// DTOs/FeeComponentDtos.cs
using System.ComponentModel.DataAnnotations;

namespace SchoolPortal.API.DTOs
{
    public class FeeComponentDto
    {
        public int FeeComponentId { get; set; }
        public string ComponentName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Frequency { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
    }

    public class UpsertFeeComponentDto
    {
        [Required]
        public string ComponentName { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public string Frequency { get; set; } = string.Empty;

        [Required]
        public string AcademicYear { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int ClassId { get; set; }
    }
}