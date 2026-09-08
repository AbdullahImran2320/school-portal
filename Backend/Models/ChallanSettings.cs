// Models/ChallanSettings.cs
namespace SchoolPortal.API.Models
{
    // Singleton row (always Id = 1) holding the bank/payment details printed
    // on every fee challan. Kept separate from appsettings.json specifically
    // so the principal can edit it from the app without a developer present.
    public class ChallanSettings
    {
        public int Id { get; set; }
        public string AccountTitle { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string PaymentTermsLine1 { get; set; } = string.Empty;
        public string PaymentTermsLine2 { get; set; } = string.Empty;
    }
}
