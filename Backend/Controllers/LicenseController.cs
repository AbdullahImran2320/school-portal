using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolPortal.API.DTOs.License;
using SchoolPortal.API.Services;

namespace SchoolPortal.API.Controllers;

[ApiController]
[Route("api/license")]
[Authorize]
public class LicenseController : ControllerBase
{
    private readonly ILicenseService _licenseService;

    public LicenseController(ILicenseService licenseService)
    {
        _licenseService = licenseService;
    }

    [HttpGet("status")]
    public async Task<ActionResult<LicenseStatusDto>> GetStatus()
        => Ok(await _licenseService.GetStatusAsync());

    [HttpPost("activate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LicenseActivationResponseDto>> Activate([FromBody] ActivateLicenseRequest request)
    {
        var result = await _licenseService.ActivateAsync(request.LicenseKey);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
