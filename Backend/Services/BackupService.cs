// Services/BackupService.cs
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SchoolPortal.API.Data;

namespace SchoolPortal.API.Services
{
    public class BackupInfo
    {
        public string FileName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public long SizeBytes { get; set; }
    }

    public interface IBackupService
    {
        Task RunDailyBackupIfNeededAsync();
        Task<BackupInfo> CreateBackupNowAsync();
        List<BackupInfo> ListBackups();
        string? GetBackupFilePath(string fileName);
    }

    public class BackupService : IBackupService
    {
        private readonly SchoolPortalDbContext _context;
        private readonly ILogger<BackupService> _logger;
        private const int RetentionDays = 30;

        public BackupService(SchoolPortalDbContext context, ILogger<BackupService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Backups live in a "Backups" folder next to the live database file
        // itself, wherever that actually is — SchoolPortal.db in dev,
        // %ProgramData%\BrightGrammarSchoolPortal\SchoolPortal.db once
        // installed. This avoids hardcoding either path and always backs up
        // the database that's actually in use.
        private string GetBackupFolder()
        {
            var connection = (SqliteConnection)_context.Database.GetDbConnection();
            var dbPath = connection.DataSource;
            var dbFolder = Path.GetDirectoryName(Path.GetFullPath(dbPath)) ?? AppContext.BaseDirectory;
            var backupFolder = Path.Combine(dbFolder, "Backups");
            Directory.CreateDirectory(backupFolder);
            return backupFolder;
        }

        private string GetDbPath()
        {
            var connection = (SqliteConnection)_context.Database.GetDbConnection();
            return connection.DataSource;
        }

        public async Task RunDailyBackupIfNeededAsync()
        {
            try
            {
                var folder = GetBackupFolder();
                var todayFileName = $"SchoolPortal-{DateTime.Now:yyyy-MM-dd}.db";
                var todayPath = Path.Combine(folder, todayFileName);

                if (!File.Exists(todayPath))
                {
                    await BackupToAsync(todayPath);
                    _logger.LogInformation("Daily backup created: {File}", todayFileName);
                }

                PruneOldBackups(folder);
            }
            catch (Exception ex)
            {
                // A failed backup should never stop the school from using
                // the portal today — log it and move on. The person can
                // still trigger one manually from Admin -> Backups once
                // whatever's wrong (e.g. a full disk) is noticed and fixed.
                _logger.LogError(ex, "Automatic backup failed.");
            }
        }

        public async Task<BackupInfo> CreateBackupNowAsync()
        {
            var folder = GetBackupFolder();
            var fileName = $"SchoolPortal-{DateTime.Now:yyyy-MM-dd-HHmmss}.db";
            var path = Path.Combine(folder, fileName);

            await BackupToAsync(path);
            PruneOldBackups(folder);

            var info = new FileInfo(path);
            return new BackupInfo { FileName = fileName, CreatedAt = info.CreationTime, SizeBytes = info.Length };
        }

        // Uses SQLite's own online backup API rather than copying the file
        // at the OS level. A live database can have pending writes sitting
        // in a -wal file that a plain file copy would miss entirely,
        // producing a backup that looks fine but is silently missing recent
        // data. BackupDatabase() takes a proper point-in-time snapshot
        // regardless of what's mid-write.
        private async Task BackupToAsync(string destinationPath)
        {
            var sourceConnectionString = new SqliteConnectionStringBuilder { DataSource = GetDbPath() }.ToString();
            var destConnectionString = new SqliteConnectionStringBuilder { DataSource = destinationPath }.ToString();

            using var source = new SqliteConnection(sourceConnectionString);
            using var destination = new SqliteConnection(destConnectionString);
            await source.OpenAsync();
            await destination.OpenAsync();
            source.BackupDatabase(destination);
        }

        private void PruneOldBackups(string folder)
        {
            var cutoff = DateTime.Now.AddDays(-RetentionDays);
            foreach (var file in Directory.GetFiles(folder, "SchoolPortal-*.db"))
            {
                if (File.GetCreationTime(file) < cutoff)
                {
                    try { File.Delete(file); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Could not delete old backup {File}", file); }
                }
            }
        }

        public List<BackupInfo> ListBackups()
        {
            var folder = GetBackupFolder();
            return Directory.GetFiles(folder, "SchoolPortal-*.db")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .Select(f => new BackupInfo { FileName = f.Name, CreatedAt = f.CreationTime, SizeBytes = f.Length })
                .ToList();
        }

        public string? GetBackupFilePath(string fileName)
        {
            // Reject anything that isn't a bare filename we generated ourselves —
            // this is the one place user input reaches the filesystem, so it's
            // the one place a path-traversal attempt ("../../appsettings.json")
            // needs to be blocked outright rather than merely sanitized.
            if (fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
                return null;

            var path = Path.Combine(GetBackupFolder(), fileName);
            return File.Exists(path) ? path : null;
        }
    }
}