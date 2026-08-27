// DTOs/ParentDtos.cs
using System.ComponentModel.DataAnnotations;

namespace SchoolPortal.API.DTOs
{
    public class ParentDto
    {
        public int ParentId { get; set; }
        public string FatherName { get; set; } = string.Empty;
        public string FatherMobile { get; set; } = string.Empty;
        public string? FatherOccupation { get; set; }
        public string? MotherName { get; set; }
        public string? MotherMobile { get; set; }
        public string PrimaryGuardian { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int ChildrenCount { get; set; }
    }

    public class UpsertParentDto
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string FatherName { get; set; } = string.Empty;

        [Required]
        public string FatherMobile { get; set; } = string.Empty;

        public string? FatherOccupation { get; set; }
        public string? MotherName { get; set; }
        public string? MotherMobile { get; set; }

        [Required]
        public string PrimaryGuardian { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;
    }

    public class CreateParentDto
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string FatherName { get; set; } = string.Empty;

        [Required, StringLength(20, MinimumLength = 7)]
        public string FatherMobile { get; set; } = string.Empty;

        public string? FatherOccupation { get; set; }

        public string? MotherName { get; set; }
        public string? MotherMobile { get; set; }

        // Validated by Enum.TryParse in the controller, same pattern used
        // for AdmissionStatus on CreateStudentDto — must be Mother, Father,
        // MotherAndFather, or Other.
        public string PrimaryGuardian { get; set; } = "Father";

        [Required, StringLength(200, MinimumLength = 5)]
        public string Address { get; set; } = string.Empty;
    }

    public class UpdateParentDto
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string FatherName { get; set; } = string.Empty;

        [Required, StringLength(20, MinimumLength = 7)]
        public string FatherMobile { get; set; } = string.Empty;

        public string? FatherOccupation { get; set; }
        public string? MotherName { get; set; }
        public string? MotherMobile { get; set; }
        public string PrimaryGuardian { get; set; } = "Father";

        [Required, StringLength(200, MinimumLength = 5)]
        public string Address { get; set; } = string.Empty;
    }
}
