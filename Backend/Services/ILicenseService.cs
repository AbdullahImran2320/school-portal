using SchoolPortal.API.DTOs.License;

namespace SchoolPortal.API.Services;

public interface ILicenseService
{
    Task InitializeAsync();
    Task<LicenseStatusDto> GetStatusAsync();
    Task<bool> CanUsePortalAsync();
    Task<LicenseActivationResponseDto> ActivateAsync(string licenseKey);
}
