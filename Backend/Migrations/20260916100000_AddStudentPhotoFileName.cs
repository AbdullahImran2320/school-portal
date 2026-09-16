using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPortal.API.Migrations
{
    // Student.PhotoFileName was added to the model but no migration was ever
    // generated for it, so Migrate() never created the column and every
    // query against Students (which always selects every scalar property)
    // failed with "SQLite Error 1: no such column: s.PhotoFileName".
    public partial class AddStudentPhotoFileName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoFileName",
                table: "Students",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoFileName",
                table: "Students");
        }
    }
}
