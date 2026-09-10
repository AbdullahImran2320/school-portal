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

        // Login lockout. FailedLoginAttempts resets to 0 on any successful
        // login or once a lockout is imposed (so the next window starts
        // clean rather than the count creeping up forever). LockedOutUntil
        // null or in the past means "not currently locked out".
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockedOutUntil { get; set; }
    }
}