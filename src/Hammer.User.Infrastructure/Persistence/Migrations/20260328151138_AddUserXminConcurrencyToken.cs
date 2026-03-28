using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hammer.User.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserXminConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // xmin is a built-in PostgreSQL system column — no schema change needed.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // xmin is a built-in PostgreSQL system column — nothing to revert.
        }
    }
}
