// DTOs/ClassDtos.cs
namespace SchoolPortal.API.DTOs
{
    public class ClassDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public int PromotionOrder { get; set; }
    }
}