// DTOs/RollNumberSettingsDtos.cs
using System.ComponentModel.DataAnnotations;

namespace SchoolPortal.API.DTOs
{
    public class RollNumberSettingsDto
    {
        public string Prefix { get; set; } = string.Empty;
        public int SequenceDigits { get; set; }
    }

    public class UpdateRollNumberSettingsDto
    {
        [Required, MaxLength(10)]
        public string Prefix { get; set; } = string.Empty;

        [Range(1, 6, ErrorMessage = "Sequence digits must be between 1 and 6")]
        public int SequenceDigits { get; set; }
    }
}
