// Services/IAuthService.cs + AuthService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Services
{
    public interface IAuthService
    {
        Task<LoginAttemptResult> LoginAsync(LoginDto dto);
        Task<RegisterResultDto> RegisterAsync(RegisterDto dto);
        Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, ChangePasswordDto dto);
    }

    public class LoginAttemptResult
    {
        public LoginResultDto? Success { get; set; }
        public bool IsLockedOut { get; set; }
        public int LockoutMinutesRemaining { get; set; }
    }

    public class AuthService : IAuthService
    {
        private readonly SchoolPortalDbContext _context;
        private readonly IConfiguration _config;
        private readonly PasswordHasher<User> _hasher = new();
        private readonly int _maxFailedAttempts;
        private readonly int _lockoutMinutes;

        public AuthService(SchoolPortalDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _maxFailedAttempts = config.GetValue<int?>("LoginSecuritySettings:MaxFailedAttempts") ?? 5;
            _lockoutMinutes = config.GetValue<int?>("LoginSecuritySettings:LockoutMinutes") ?? 15;
        }

        public async Task<LoginAttemptResult> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
            // A nonexistent username still goes through the exact same
            // "invalid credentials" response as a wrong password below —
            // this endpoint never confirms or denies that a username exists.
            if (user == null) return new LoginAttemptResult();

            if (user.LockedOutUntil.HasValue && user.LockedOutUntil.Value > DateTime.Now)
            {
                return new LoginAttemptResult
                {
                    IsLockedOut = true,
                    LockoutMinutesRemaining = (int)Math.Ceiling((user.LockedOutUntil.Value - DateTime.Now).TotalMinutes)
                };
            }

            var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (verify == PasswordVerificationResult.Failed)
            {
                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >= _maxFailedAttempts)
                {
                    user.LockedOutUntil = DateTime.Now.AddMinutes(_lockoutMinutes);
                    // Reset the counter now, not on next successful login —
                    // otherwise a second lockout window later in the same day
                    // would trigger after just one more bad attempt instead
                    // of a fresh five, since the count never actually cleared.
                    user.FailedLoginAttempts = 0;
                    await _context.SaveChangesAsync();

                    return new LoginAttemptResult { IsLockedOut = true, LockoutMinutesRemaining = _lockoutMinutes };
                }

                await _context.SaveChangesAsync();
                return new LoginAttemptResult();
            }

            // Successful login clears any accumulated failed-attempt count —
            // a few mistyped passwords followed by the right one shouldn't
            // leave the account one bad attempt away from a lockout later.
            user.FailedLoginAttempts = 0;
            user.LockedOutUntil = null;
            await _context.SaveChangesAsync();

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(double.Parse(_config["Jwt:ExpiryMinutes"]!)),
                signingCredentials: creds
            );

            return new LoginAttemptResult
            {
                Success = new LoginResultDto
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    Username = user.Username,
                    Role = user.Role.ToString(),
                    FullName = user.FullName
                }
            };
        }
        public async Task<RegisterResultDto> RegisterAsync(RegisterDto dto)
        {
            var exists = await _context.Users.AnyAsync(u => u.Username == dto.Username);
            if (exists)
            {
                return new RegisterResultDto
                {
                    Success = false,
                    Message = "An account with this username already exists.",
                    ErrorCode = "UsernameTaken"
                };
            }

            var user = new User
            {
                Username = dto.Username,
                FullName = dto.FullName,
                Role = UserRole.Pending
            };
            user.PasswordHash = _hasher.HashPassword(user, dto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new RegisterResultDto
            {
                Success = true,
                Message = "Account created. An admin needs to approve your access before you can use the portal."
            };
        }

        public async Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (false, "User not found.");

            var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);
            if (verify == PasswordVerificationResult.Failed)
                return (false, "Current password is incorrect.");

            user.PasswordHash = _hasher.HashPassword(user, dto.NewPassword);
            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}