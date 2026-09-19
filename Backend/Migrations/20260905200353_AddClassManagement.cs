using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPortal.API.Migrations
{
    /// <inheritdoc />
    public partial class AddClassManagement : Migration
    {
        // Every statement here is written as "IF NOT EXISTS" on purpose.
        //
        // Older builds of the app created the LicenseInfos table themselves
        // with raw SQL at startup (before this migration existed). A school
        // database installed from one of those builds therefore already has
        // LicenseInfos, but no record of this migration in
        // __EFMigrationsHistory. A plain CreateTable then fails on upgrade
        // with: SQLite Error 1: 'table "LicenseInfos" already exists'.
        //
        // Using IF NOT EXISTS makes this migration safe on all three cases:
        // a brand-new database, an already-migrated one (Up never re-runs),
        // and an older database that already has some of these objects.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""LicenseInfos"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_LicenseInfos"" PRIMARY KEY AUTOINCREMENT,
    ""TrialStartDate"" TEXT NOT NULL,
    ""TrialEndDate"" TEXT NOT NULL,
    ""IsActivated"" INTEGER NOT NULL,
    ""LicenseKey"" TEXT NULL,
    ""LicenseStartDate"" TEXT NULL,
    ""LicenseEndDate"" TEXT NULL,
    ""LastSeenDate"" TEXT NULL,
    ""InstallationId"" TEXT NOT NULL,
    ""SignedLicense"" TEXT NULL,
    ""LastOnlineValidationUtc"" TEXT NULL,
    ""OfflineGraceUntilUtc"" TEXT NULL,
    ""CreatedAt"" TEXT NOT NULL,
    ""UpdatedAt"" TEXT NOT NULL
);");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""SectionOptions"" (
    ""SectionOptionId"" INTEGER NOT NULL CONSTRAINT ""PK_SectionOptions"" PRIMARY KEY AUTOINCREMENT,
    ""Name"" TEXT NOT NULL
);");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Classes_ClassName_Section_AcademicYear\" " +
                "ON \"Classes\" (\"ClassName\", \"Section\", \"AcademicYear\");");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_LicenseInfos_InstallationId\" " +
                "ON \"LicenseInfos\" (\"InstallationId\");");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_SectionOptions_Name\" " +
                "ON \"SectionOptions\" (\"Name\");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LicenseInfos");

            migrationBuilder.DropTable(
                name: "SectionOptions");

            migrationBuilder.DropIndex(
                name: "IX_Classes_ClassName_Section_AcademicYear",
                table: "Classes");
        }
    }
}
