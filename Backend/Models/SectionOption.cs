// Models/SectionOption.cs
namespace SchoolPortal.API.Models
{
    // The fixed list of section labels an Admin defines once (e.g. "A", "B",
    // "Red", "Blue", "Boys", "Girls"). Kept separate from SchoolClass so the
    // same label can be reused across classes without retyping it, and so
    // the "pick from a list" UI has something to populate its dropdown from.
    public class SectionOption
    {
        public int SectionOptionId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
