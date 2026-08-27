namespace SchoolPortal.API.DTOs.License;

public class LicenseActivationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? LicenseEndDate { get; set; }
}
