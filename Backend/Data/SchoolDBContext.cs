// Data/SchoolPortalDbContext.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SchoolPortal.API.Models;

namespace SchoolPortal.API.Data
{
    public class SchoolPortalDbContext : DbContext
    {
        public SchoolPortalDbContext(DbContextOptions<SchoolPortalDbContext> options)
            : base(options) { }

        public DbSet<Student> Students { get; set; }
        public DbSet<Parent> Parents { get; set; }
        public DbSet<SchoolClass> Classes { get; set; }
        public DbSet<FeeComponent> FeeComponents { get; set; }
        public DbSet<StudentCharge> StudentCharges { get; set; }
        public DbSet<FeeLedger> FeeLedgers { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<Result> Results { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<LicenseInfo> LicenseInfos { get; set; }

        private readonly IHttpContextAccessor? _httpContextAccessor;

        public SchoolPortalDbContext(DbContextOptions<SchoolPortalDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private class PendingAudit
        {
            public EntityEntry Entry { get; set; } = null!;
            public string EntityName { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public List<string> Changes { get; set; } = new();
        }

        private List<PendingAudit> CapturePendingAudits()
        {
            ChangeTracker.DetectChanges();
            var pending = new List<PendingAudit>();

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog) continue; // never audit the audit log itself
                if (entry.State is EntityState.Detached or EntityState.Unchanged) continue;

                var audit = new PendingAudit
                {
                    Entry = entry,
                    EntityName = entry.Entity.GetType().Name,
                    Action = entry.State.ToString()
                };

                foreach (var prop in entry.Properties)
                {
                    if (prop.Metadata.IsPrimaryKey()) continue;

                    if (entry.State == EntityState.Modified && prop.IsModified && !Equals(prop.OriginalValue, prop.CurrentValue))
                        audit.Changes.Add($"{prop.Metadata.Name}: {prop.OriginalValue} -> {prop.CurrentValue}");
                    else if (entry.State == EntityState.Added)
                        audit.Changes.Add($"{prop.Metadata.Name}: {prop.CurrentValue}");
                }

                pending.Add(audit);
            }
            return pending;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var pending = CapturePendingAudits();
            var result = await base.SaveChangesAsync(cancellationToken); // real IDs get assigned here for new rows

            if (pending.Count > 0)
            {
                var username = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "System";
                foreach (var audit in pending)
                {
                    var entityId = audit.Entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "";
                    AuditLogs.Add(new AuditLog
                    {
                        EntityName = audit.EntityName,
                        EntityId = entityId,
                        Action = audit.Action,
                        ChangedBy = username,
                        Details = audit.Changes.Any() ? string.Join("; ", audit.Changes) : "(no field-level changes)"
                    });
                }
                await base.SaveChangesAsync(cancellationToken); // second, smaller save — just the new audit rows
            }

            return result;
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Prevent accidental cascade delete: deleting a class or parent
            // should not silently wipe out students. Restrict instead.
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Class)
                .WithMany(c => c.Students)
                .HasForeignKey(s => s.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Parent)
                .WithMany(p => p.Children)
                .HasForeignKey(s => s.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Enum stored as string in DB, not int — makes the SQLite file
            // human-readable if you ever open it directly, and safer if you
            // reorder the enum later.
            modelBuilder.Entity<Student>()
                .Property(s => s.AdmissionStatus)
                .HasConversion<string>();

            modelBuilder.Entity<Parent>()
                  .Property(p => p.PrimaryGuardian)
                  .HasConversion<string>();

            modelBuilder.Entity<FeeComponent>()
                .Property(f => f.Frequency)
                .HasConversion<string>();

            modelBuilder.Entity<StudentCharge>()
                .Property(s => s.Status)
                .HasConversion<string>();

            modelBuilder.Entity<FeeLedger>()
                .Property(f => f.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Ledger)
                .WithMany()
                .HasForeignKey(p => p.LedgerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Charge)
                .WithMany()
                .HasForeignKey(p => p.ChargeId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<User>().Property(u => u.Role).HasConversion<string>();
            modelBuilder.Entity<SchoolClass>().HasKey(c => c.ClassId);
            modelBuilder.Entity<FeeLedger>().HasKey(l => l.LedgerId);
            modelBuilder.Entity<StudentCharge>().HasKey(c => c.ChargeId);
            modelBuilder.Entity<Attendance>().Property(a => a.Status).HasConversion<string>();

            modelBuilder.Entity<LicenseInfo>()
                .HasIndex(l => l.InstallationId)
                .IsUnique();

            // One attendance record per student per day — the service below upserts
            // instead of inserting blindly, but this index is the hard guarantee
            // that a bug (or a retried request) can never create a duplicate.
            modelBuilder.Entity<Attendance>()
                .HasIndex(a => new { a.StudentId, a.Date })
                .IsUnique();
        }
    }
}