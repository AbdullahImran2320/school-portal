// DTOs/ChallanSettingsDtos.cs
using System.ComponentModel.DataAnnotations;

namespace SchoolPortal.API.DTOs
{
    public class ChallanSettingsDto
    {
        public string AccountTitle { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string PaymentTermsLine1 { get; set; } = string.Empty;
        public string PaymentTermsLine2 { get; set; } = string.Empty;
    }

    public class UpdateChallanSettingsDto
    {
        [Required, MaxLength(200)]
        public string AccountTitle { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string BankName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string AccountNumber { get; set; } = string.Empty;

        [MaxLength(300)]
        public string PaymentTermsLine1 { get; set; } = string.Empty;

        [MaxLength(300)]
        public string PaymentTermsLine2 { get; set; } = string.Empty;
    }
}
