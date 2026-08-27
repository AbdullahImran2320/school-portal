using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPortal.API.Migrations
{
    public partial class AddManualFineAmount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ManualFineAmount",
                table: "FeeLedgers",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ManualFineAmount",
                table: "FeeLedgers");
        }
    }
}
