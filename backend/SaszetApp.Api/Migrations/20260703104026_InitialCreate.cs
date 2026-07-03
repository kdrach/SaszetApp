using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaszetApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LlmProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderName = table.Column<string>(type: "text", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: false),
                    EncryptedApiKey = table.Column<string>(type: "text", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LlmProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PetFoodItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EanCode = table.Column<string>(type: "text", nullable: true),
                    ProductName = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Pros = table.Column<string>(type: "jsonb", nullable: false),
                    Cons = table.Column<string>(type: "jsonb", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    ExtractedIngredients = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetFoodItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PetFoodItems_EanCode",
                table: "PetFoodItems",
                column: "EanCode");

            migrationBuilder.CreateIndex(
                name: "IX_PetFoodItems_Language",
                table: "PetFoodItems",
                column: "Language");

            migrationBuilder.CreateIndex(
                name: "IX_PetFoodItems_ProductName",
                table: "PetFoodItems",
                column: "ProductName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LlmProviders");

            migrationBuilder.DropTable(
                name: "PetFoodItems");
        }
    }
}
