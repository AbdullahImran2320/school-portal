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

    public class UnresolvedPromotionDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CurrentClassName { get; set; } = string.Empty;
        public string CurrentSection { get; set; } = string.Empty;
        public List<string> AvailableSectionsInNextGrade { get; set; } = new();
    }

    public class PromotionResultDto
    {
        public int PromotedCount { get; set; }
        public int GraduatedCount { get; set; }
        public int HeldBackCount { get; set; }
        public int AlreadyProcessedCount { get; set; }

        // Students whose section doesn't exist in the next grade and where
        // the next grade has more than one section, so we can't safely
        // guess which one they belong in. They are NOT promoted — they stay
        // in their current class until an Admin assigns them manually via
        // the regular student-edit screen, then can be re-run through
        // promotion (they'll be skipped as "already processed" once they
        // have a fee ledger for the target year — see the repeater/held-back
        // path if that's not the case).
        public List<UnresolvedPromotionDto> UnresolvedSections { get; set; } = new();

        public List<string> Errors { get; set; } = new();
    }
}
