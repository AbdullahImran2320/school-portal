using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPortal.API.Migrations
{
    // The original "AddRollNumber" migration (20260914033128) was recorded in
    // __EFMigrationsHistory back when its Up() was still an empty stub, so
    // Database.Migrate() treats it as already applied and will never re-run
    // it now that it has real content. This migration carries that same
    // change forward under a new, not-yet-applied id so it actually reaches
    // the database on next startup.
    public partial class AddRollNumberColumnFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_RollNumber",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "RollNumber",
                table: "Students");
        }
    }
}
