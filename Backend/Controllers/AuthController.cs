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
    } }