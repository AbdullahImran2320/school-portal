using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolPortal.API.DTOs;
using SchoolPortal.API.Services;

namespace SchoolPortal.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService) => _authService = authService;

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResultDto>> Login(LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (result == null) return Unauthorized(new { message = "Invalid username or password" });
            return Ok(result);
        }
   

    [AllowAnonymous]
  [HttpPost("register")]
  public async Task<ActionResult<RegisterResultDto>> Register(RegisterDto dto)
  {
      var result = await _authService.RegisterAsync(dto);

      if (!result.Success && result.ErrorCode == "UsernameTaken")
          return Conflict(result); // 409 — frontend checks this status code and offers the Login form

      if (!result.Success) return BadRequest(result);

      return Ok(result);
        }

        // Any authenticated role can change their own password — this is
        // deliberately not Admin-only, since an Accountant or Teacher stuck
        // with a seeded/default password needs a way to change it themselves
        // without asking an Admin to do it for them.
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var (success, error) = await _authService.ChangePasswordAsync(userId, dto);
            if (!success) return BadRequest(new { message = error });

            return Ok(new { message = "Password changed successfully." });
        }
    } }