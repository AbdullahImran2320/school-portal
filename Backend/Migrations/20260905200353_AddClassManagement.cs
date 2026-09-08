using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPortal.API.Migrations
{
    /// <inheritdoc />
    public partial class AddClassManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LicenseInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrialStartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TrialEndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActivated = table.Column<bool>(type: "INTEGER", nullable: false),
                    LicenseKey = table.Column<string>(type: "TEXT", nullable: true),
                    LicenseStartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LicenseEndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastSeenDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InstallationId = table.Column<string>(type: "TEXT", nullable: false),
                    SignedLicense = table.Column<string>(type: "TEXT", nullable: true),
                    LastOnlineValidationUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OfflineGraceUntilUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicenseInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SectionOptions",
                columns: table => new
                {
                    SectionOptionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectionOptions", x => x.SectionOptionId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Classes_ClassName_Section_AcademicYear",
                table: "Classes",
                columns: new[] { "ClassName", "Section", "AcademicYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LicenseInfos_InstallationId",
                table: "LicenseInfos",
                column: "InstallationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SectionOptions_Name",
                table: "SectionOptions",
                column: "Name",
                unique: true);
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
