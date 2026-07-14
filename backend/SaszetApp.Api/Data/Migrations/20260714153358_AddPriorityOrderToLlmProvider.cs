using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaszetApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPriorityOrderToLlmProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LlmProviders_ProviderName",
                table: "LlmProviders");

            migrationBuilder.AddColumn<int>(
                name: "PriorityOrder",
                table: "LlmProviders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_LlmProviders_ProviderName",
                table: "LlmProviders",
                column: "ProviderName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LlmProviders_ProviderName",
                table: "LlmProviders");

            migrationBuilder.DropColumn(
                name: "PriorityOrder",
                table: "LlmProviders");

            migrationBuilder.CreateIndex(
                name: "IX_LlmProviders_ProviderName",
                table: "LlmProviders",
                column: "ProviderName",
                unique: true);
        }
    }
}
