using System.ComponentModel.DataAnnotations;

namespace SchoolPortal.API.DTOs
{
    public class StudentDto
    {
        public int StudentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? RollNumber { get; set; }
        // The raw position within the student's class/section that the
        // formatted RollNumber above was built from — this, not the
        // formatted string, is what an editable "roll number" input should
        // actually read and write (see StudentsController's roll-number
        // endpoint).
        public int? RollNumberSequence { get; set; }
        public string BFormNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public DateTime AdmissionDate { get; set; }
        public string AdmissionStatus { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public bool HasPhoto { get; set; }
        public int ParentId { get; set; }
        public string FatherName { get; set; } = string.Empty;
        public string FatherMobile { get; set; } = string.Empty;
        public string? MotherName { get; set; }
        public string? MotherMobile { get; set; }
    }



public class CreateStudentDto
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        // Left null to auto-assign the next available position in this
        // student's class/section; set explicitly to skip auto-assignment
        // (e.g. re-entering a student whose paper roll number is already
        // fixed). This is a position within the class, not the formatted
        // code itself — the formatted RollNumber is always generated from
        // it, the class's ClassCode, and the roll number settings.
        [Range(1, int.MaxValue, ErrorMessage = "Roll number position must be positive")]
        public int? RollNumberSequence { get; set; }

        [Required, RegularExpression(@"^\d{5}-\d{7}-\d{1}$", ErrorMessage = "B-Form number must be in format 12345-1234567-1")]
        public string BFormNumber { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; } = string.Empty;

        [Required]
        public DateTime AdmissionDate { get; set; }

        public string AdmissionStatus { get; set; } = "Applied";

        [Range(1, int.MaxValue, ErrorMessage = "A valid ClassId is required")]
        public int ClassId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A valid ParentId is required")]
        public int ParentId { get; set; }
    }

    public class UpdateStudentDto
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required, RegularExpression(@"^\d{5}-\d{7}-\d{1}$")]
        public string BFormNumber { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; } = string.Empty;

        [Required]
        public string AdmissionStatus { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int ClassId { get; set; }
    }

    // Deliberately separate from UpdateStudentDto — same reasoning as why
    // ParentId isn't editable through the general update: assigning a roll
    // number is its own distinct action with its own validation, not a
    // side effect of an unrelated field edit. The value here is a position
    // within the student's own class/section (see SetRollNumberAsync) —
    // moving them to a new position shifts their classmates by one, the
    // same as before, just scoped to the class instead of the whole school.
    public class SetRollNumberDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Roll number position must be positive")]
        public int RollNumberSequence { get; set; }
    }

    public class AssignRollNumbersResultDto
    {
        public int AssignedCount { get; set; }
        // Students whose class doesn't have a ClassCode set yet — a
        // formatted roll number can't be built for them until the Admin
        // sets one in Manage Classes, so they're reported here instead of
        // silently skipped or failing the whole batch.
        public int SkippedNoClassCodeCount { get; set; }
    }

    public class SetDiscountDto
    {
        [Range(0, double.MaxValue, ErrorMessage = "Discount amount can't be negative")]
        public decimal MonthlyDiscountAmount { get; set; }
        public string? Reason { get; set; }
        public bool ApplyToRemainingMonthsThisYear { get; set; } = true;
    }
}
