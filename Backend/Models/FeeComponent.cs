// Models/FeeComponent.cs
namespace SchoolPortal.API.Models
{
    public enum FeeFrequency
    {
        OneTime,   // e.g. Admission Fee
        Yearly,    // e.g. Exam Fee, Stationery Fee
        Monthly    // the recurring monthly fee
    }

    public class FeeComponent
    {
        public int FeeComponentId { get; set; }
        public string ComponentName { get; set; } = string.Empty; // "Admission Fee", "Exam Fee", etc.
        public decimal Amount { get; set; }
        public FeeFrequency Frequency { get; set; }
        public string AcademicYear { get; set; } = string.Empty;

        // Foreign key
        public int ClassId { get; set; }
        public SchoolClass Class { get; set; } = null!;
    }
}