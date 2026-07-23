using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaszetApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Redundant operations removed because they were already applied in 20260721113248_AddUsersAndCats.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Empty down migration.
        }
    }
}
