using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaszetApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLlmProviderConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_LlmProviders_IsPrimary",
                table: "LlmProviders",
                column: "IsPrimary",
                unique: true,
                filter: "\"IsPrimary\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_LlmProviders_ProviderName",
                table: "LlmProviders",
                column: "ProviderName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LlmProviders_IsPrimary",
                table: "LlmProviders");

            migrationBuilder.DropIndex(
                name: "IX_LlmProviders_ProviderName",
                table: "LlmProviders");
        }
    }
}
