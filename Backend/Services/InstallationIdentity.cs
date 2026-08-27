using System.Security.Cryptography;
using System.Text;

namespace SchoolPortal.API.Services;

public static class InstallationIdentity
{
    public static string Create()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "BayHeightsSchoolPortal");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "installation.id");

        if (File.Exists(path))
        {
            var existing = File.ReadAllText(path).Trim();
            if (!string.IsNullOrWhiteSpace(existing)) return existing;
        }

        var raw = $"{Environment.MachineName}|{Environment.OSVersion}|{Guid.NewGuid():N}";
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        File.WriteAllText(path, id);
        return id;
    }
}
