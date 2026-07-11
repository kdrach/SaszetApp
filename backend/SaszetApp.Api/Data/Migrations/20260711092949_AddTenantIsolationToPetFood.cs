using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaszetApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIsolationToPetFood : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "PetFoodItems",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PetFoodItems");
        }
    }
}
