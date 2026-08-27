namespace SchoolPortal.API.Models;

public class LicenseInfo
{
    public int Id { get; set; }
    public DateTime TrialStartDate { get; set; }
    public DateTime TrialEndDate { get; set; }
    public bool IsActivated { get; set; }
    public string? LicenseKey { get; set; }
    public DateTime? LicenseStartDate { get; set; }
    public DateTime? LicenseEndDate { get; set; }
    public DateTime? LastSeenDate { get; set; }
    public string InstallationId { get; set; } = string.Empty;
    public string? SignedLicense { get; set; }
    public DateTime? LastOnlineValidationUtc { get; set; }
    public DateTime? OfflineGraceUntilUtc { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
