namespace SchoolPortal.API.DTOs.License;

public class LicenseRemoteResponseDto
{
    public bool Valid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? SignedLicense { get; set; }
    public DateTime? EndDateUtc { get; set; }
}
