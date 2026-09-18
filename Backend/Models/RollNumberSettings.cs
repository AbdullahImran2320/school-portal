// Models/RollNumberSettings.cs
namespace SchoolPortal.API.Models
{
    // Singleton row (always Id = 1) holding the globally-editable parts of
    // the roll number format — same pattern as ChallanSettings. The rest of
    // the format (the class code) is set per class on the SchoolClass row,
    // and the admission year is read from each student's own AdmissionDate.
    public class RollNumberSettings
    {
        public int Id { get; set; }
        public string Prefix { get; set; } = "R";

        // How many digits the sequence number pads to, e.g. 3 -> "001".
        public int SequenceDigits { get; set; } = 3;
    }
}
