using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolPortal.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRollNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally a no-op. This id is already recorded as applied
            // in __EFMigrationsHistory on every existing install (it was
            // scaffolded before RollNumber existed on the model), so its
            // Up() must stay empty to match what actually happened there.
            // The real column/index are added by the later
            // AddRollNumberColumnFix migration instead, so both existing
            // installs (which skip this one) and brand-new installs (which
            // run this as a genuine no-op, then run the fix migration for
            // real) end up with exactly one AddColumn — never two.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
