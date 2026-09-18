using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolPortal.API.Data;

#nullable disable

namespace SchoolPortal.API.Migrations
{
    // Roll numbers switch from a single school-wide integer to a formatted
    // code (prefix + 2-digit admission year + a per-class code + a
    // sequence that resets per class/section), matching the new
    // RollNumberSettings screen and SchoolClass.ClassCode field.
    //
    // The [Migration] attribute below is what lets EF Core discover and
    // order this migration at runtime (via Database.Migrate() in
    // Program.cs) — a separate Designer.cs isn't required for that, only
    // for design-time tooling like `dotnet ef migrations script`, so this
    // migration doesn't have one.
    //
    // IMPORTANT: because there is no Designer.cs (no target model), EF's
    // SQLite provider cannot emit DropColumnOperation — it would need to
    // rebuild the table from that model and throws
    // "SQLite does not support this migration operation ('DropColumnOperation')".
    // So every column drop below is done with raw SQL instead, using
    // SQLite's native ALTER TABLE ... DROP COLUMN (SQLite 3.35+, which the
    // SQLite bundled with .NET 8 has). SQLite refuses to drop an indexed
    // column, so the index is always dropped first.
    [DbContext(typeof(SchoolPortalDbContext))]
    [Migration("20260918120000_AddRollNumberFormatting")]
    public partial class AddRollNumberFormatting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClassCode",
                table: "Classes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "RollNumberSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Prefix = table.Column<string>(type: "TEXT", nullable: false),
                    SequenceDigits = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RollNumberSettings", x => x.Id);
                });

            // Seeded here (rather than left for GetOrCreateRollNumberSettingsAsync
            // to create on first read) so the Roll Number Settings screen has
            // something to show immediately after this migration runs.
            migrationBuilder.Sql(
                "INSERT INTO \"RollNumberSettings\" (\"Prefix\", \"SequenceDigits\") VALUES ('R', 3);");

            // Roll numbers switch from a plain unique integer to a formatted
            // string, plus a new RollNumberSequence column that holds the
            // actual per-class position the "shift by one" reorder logic
            // operates on. The old integers can't be carried forward as the
            // new per-class sequence: they were assigned school-wide, so
            // reusing them per class would misorder every class's roster
            // (and every class's ClassCode is blank until the Admin sets
            // one anyway — there's no format to build yet). This is a
            // clean cutover: every student's roll number is cleared here,
            // to be reassigned afterward via "Assign Missing Roll Numbers"
            // or by hand, once class codes and the prefix/padding are set.
            migrationBuilder.DropIndex(
                name: "IX_Students_RollNumber",
                table: "Students");

            // Raw SQL instead of migrationBuilder.DropColumn (see note at top).
            migrationBuilder.Sql(
                "ALTER TABLE \"Students\" DROP COLUMN \"RollNumber\";");

            migrationBuilder.AddColumn<string>(
                name: "RollNumber",
                table: "Students",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RollNumberSequence",
                table: "Students",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_RollNumber",
                table: "Students",
                column: "RollNumber",
                unique: true,
                filter: "\"RollNumber\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_RollNumber",
                table: "Students");

            migrationBuilder.Sql(
                "ALTER TABLE \"Students\" DROP COLUMN \"RollNumberSequence\";");

            migrationBuilder.Sql(
                "ALTER TABLE \"Students\" DROP COLUMN \"RollNumber\";");

            migrationBuilder.AddColumn<int>(
                name: "RollNumber",
                table: "Students",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_RollNumber",
                table: "Students",
                column: "RollNumber",
                unique: true,
                filter: "\"RollNumber\" IS NOT NULL");

            migrationBuilder.DropTable(
                name: "RollNumberSettings");

            migrationBuilder.Sql(
                "ALTER TABLE \"Classes\" DROP COLUMN \"ClassCode\";");
        }
    }
}