namespace SchoolPortal.API.DTOs.License;

public class LicenseStatusDto
{
    public bool IsActivated { get; set; }
    public bool IsTrial { get; set; }
    public bool IsExpired { get; set; }
    public bool ShowWarning { get; set; }
    public int DaysRemaining { get; set; }
    public DateTime TrialStartDate { get; set; }
    public DateTime TrialEndDate { get; set; }
    public DateTime? LicenseEndDate { get; set; }
    public string Message { get; set; } = string.Empty;
}
