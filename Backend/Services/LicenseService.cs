using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;
using SchoolPortal.API.DTOs.License;

namespace SchoolPortal.API.Services;

public class LicenseService : ILicenseService
{
    private const int WarningDays = 30;
    private const int OfflineGraceDays = 14;
    private readonly SchoolPortalDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LicenseService> _logger;

    public LicenseService(
        SchoolPortalDbContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<LicenseService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        var license = await _context.LicenseInfos.OrderBy(l => l.Id).FirstOrDefaultAsync();
        var today = DateTime.UtcNow.Date;

        if (license is null)
        {
            license = new Models.LicenseInfo
            {
                TrialStartDate = today,
                TrialEndDate = today.AddMonths(3),
                IsActivated = false,
                InstallationId = InstallationIdentity.Create(),
                LastSeenDate = today,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.LicenseInfos.Add(license);
            await _context.SaveChangesAsync();
            return;
        }

        if (string.IsNullOrWhiteSpace(license.InstallationId))
        {
            license.InstallationId = InstallationIdentity.Create();
            license.UpdatedAt = DateTime.UtcNow;
        }

        if (license.LastSeenDate.HasValue && today > license.LastSeenDate.Value.Date)
        {
            license.LastSeenDate = today;
            license.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<LicenseStatusDto> GetStatusAsync()
    {
        await InitializeAsync();
        var license = await _context.LicenseInfos.OrderBy(l => l.Id).FirstAsync();
        var now = DateTime.UtcNow;

        if (license.LastSeenDate.HasValue && now.Date < license.LastSeenDate.Value.Date)
        {
            return BuildStatus(license, 0, true,
                "The computer date appears to have moved backwards. Please correct the date and time.");
        }

        if (license.IsActivated)
        {
            var signatureValid = VerifySignedLicense(license.SignedLicense, license.InstallationId, out var signedEndDate);
            var endDate = signedEndDate ?? license.LicenseEndDate;

            if (!signatureValid || !endDate.HasValue)
            {
                return BuildStatus(license, 0, true,
                    "The installed license could not be verified. Please reconnect to the Internet and renew the license.");
            }

            var remaining = Math.Max(0, (endDate.Value.Date - now.Date).Days);

            if (remaining <= 0)
            {
                return BuildStatus(license, 0, true, "Your license has expired. Please renew it.");
            }

            var offlineAllowed = license.OfflineGraceUntilUtc.HasValue && now <= license.OfflineGraceUntilUtc.Value;
            var serverUrl = _configuration["License:ServerUrl"];
            var shouldRefresh = !license.LastOnlineValidationUtc.HasValue ||
                                now - license.LastOnlineValidationUtc.Value >= TimeSpan.FromDays(1);

            if (!string.IsNullOrWhiteSpace(serverUrl) && shouldRefresh)
            {
                var refreshed = await RefreshFromServerAsync(license);
                if (!refreshed && !offlineAllowed)
                {
                    return BuildStatus(license, 0, true,
                        "The license server could not be reached and the offline grace period has ended.");
                }
            }

            return BuildStatus(license, remaining, false,
                remaining <= WarningDays
                    ? $"Your license expires in {remaining} day(s). Please renew it."
                    : "License is active.");
        }

        var trialRemaining = Math.Max(0, (license.TrialEndDate.Date - now.Date).Days);
        return BuildStatus(license, trialRemaining, trialRemaining <= 0,
            trialRemaining <= 0
                ? "Your free trial has expired. Please activate a license."
                : trialRemaining <= WarningDays
                    ? $"Your free trial expires in {trialRemaining} day(s)."
                    : "Free trial is active.");
    }

    public async Task<bool> CanUsePortalAsync()
    {
        var status = await GetStatusAsync();
        return !status.IsExpired;
    }

    public async Task<LicenseActivationResponseDto> ActivateAsync(string licenseKey)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
            return new LicenseActivationResponseDto { Success = false, Message = "License key is required." };

        await InitializeAsync();
        var license = await _context.LicenseInfos.OrderBy(l => l.Id).FirstAsync();
        var serverUrl = _configuration["License:ServerUrl"]?.TrimEnd('/');

        if (string.IsNullOrWhiteSpace(serverUrl))
            return new LicenseActivationResponseDto { Success = false, Message = "License server is not configured." };

        try
        {
            var client = _httpClientFactory.CreateClient("LicenseServer");
            var response = await client.PostAsJsonAsync($"{serverUrl}/api/client/licenses/activate", new
            {
                licenseKey = licenseKey.Trim(),
                installationId = license.InstallationId
            });

            if (!response.IsSuccessStatusCode)
                return new LicenseActivationResponseDto { Success = false, Message = "License server rejected the activation request." };

            var remote = await response.Content.ReadFromJsonAsync<LicenseRemoteResponseDto>();
            if (remote is null || !remote.Valid || string.IsNullOrWhiteSpace(remote.SignedLicense))
                return new LicenseActivationResponseDto { Success = false, Message = remote?.Message ?? "Invalid license." };

            if (!VerifySignedLicense(remote.SignedLicense, license.InstallationId, out var endDate))
                return new LicenseActivationResponseDto { Success = false, Message = "The license signature could not be verified." };

            license.IsActivated = true;
            license.LicenseKey = licenseKey.Trim();
            license.LicenseStartDate = DateTime.UtcNow.Date;
            license.LicenseEndDate = endDate ?? remote.EndDateUtc;
            license.SignedLicense = remote.SignedLicense;
            license.LastOnlineValidationUtc = DateTime.UtcNow;
            license.OfflineGraceUntilUtc = DateTime.UtcNow.AddDays(OfflineGraceDays);
            license.LastSeenDate = DateTime.UtcNow.Date;
            license.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new LicenseActivationResponseDto
            {
                Success = true,
                Message = "License activated successfully.",
                LicenseEndDate = license.LicenseEndDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "License activation server unavailable.");
            return new LicenseActivationResponseDto
            {
                Success = false,
                Message = "The license server could not be reached. Please connect the computer to the Internet and try again."
            };
        }
    }

    private async Task<bool> RefreshFromServerAsync(Models.LicenseInfo license)
    {
        var serverUrl = _configuration["License:ServerUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(serverUrl) || string.IsNullOrWhiteSpace(license.SignedLicense))
            return false;

        try
        {
            var client = _httpClientFactory.CreateClient("LicenseServer");
            var response = await client.PostAsJsonAsync($"{serverUrl}/api/client/licenses/validate", new
            {
                signedLicense = license.SignedLicense,
                installationId = license.InstallationId
            });

            if (!response.IsSuccessStatusCode)
                return false;

            var remote = await response.Content.ReadFromJsonAsync<LicenseRemoteResponseDto>();
            if (remote is null || !remote.Valid || string.IsNullOrWhiteSpace(remote.SignedLicense))
                return false;

            if (!VerifySignedLicense(remote.SignedLicense, license.InstallationId, out var endDate))
                return false;

            license.SignedLicense = remote.SignedLicense;
            license.LicenseEndDate = endDate ?? remote.EndDateUtc;
            license.LastOnlineValidationUtc = DateTime.UtcNow;
            license.OfflineGraceUntilUtc = DateTime.UtcNow.AddDays(OfflineGraceDays);
            license.LastSeenDate = DateTime.UtcNow.Date;
            license.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "License validation server unavailable.");
            return false;
        }
    }

    private bool VerifySignedLicense(string? token, string installationId, out DateTime? endDate)
    {
        endDate = null;
        if (string.IsNullOrWhiteSpace(token)) return false;

        var parts = token.Split('.', 2);
        if (parts.Length != 2) return false;

        try
        {
            var data = Base64UrlDecode(parts[0]);
            var signature = Base64UrlDecode(parts[1]);
            var publicKey = _configuration["License:PublicKeyPem"];
            if (string.IsNullOrWhiteSpace(publicKey)) return false;

            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKey);

            if (!rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                return false;

            using var document = JsonDocument.Parse(data);
            var root = document.RootElement;
            var tokenInstallation = root.GetProperty("installationId").GetString();
            if (!string.Equals(tokenInstallation, installationId, StringComparison.Ordinal))
                return false;

            var tokenEnd = root.GetProperty("endDateUtc").GetDateTime();
            endDate = tokenEnd;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }

    private static LicenseStatusDto BuildStatus(Models.LicenseInfo license, int daysRemaining, bool expired, string message)
    {
        return new LicenseStatusDto
        {
            IsActivated = license.IsActivated,
            IsTrial = !license.IsActivated,
            IsExpired = expired,
            ShowWarning = !expired && daysRemaining <= WarningDays,
            DaysRemaining = daysRemaining,
            TrialStartDate = license.TrialStartDate,
            TrialEndDate = license.TrialEndDate,
            LicenseEndDate = license.LicenseEndDate,
            Message = message
        };
    }
}
