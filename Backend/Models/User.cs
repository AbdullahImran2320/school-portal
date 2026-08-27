// Models/User.cs
namespace SchoolPortal.API.Models
{
    public enum UserRole
    {
        Pending,   // just registered, no permissions yet
        Teacher,
        Accountant,
        Admin
    }
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
    }
}