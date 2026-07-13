using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaszetApp.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScanRateLimiting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "UserScanLimits",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    MaxScans = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserScanLimits", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "UserScanUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserScanUsages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserScanUsages_ScannedAt",
                table: "UserScanUsages",
                column: "ScannedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserScanUsages_UserId",
                table: "UserScanUsages",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "UserScanLimits");

            migrationBuilder.DropTable(
                name: "UserScanUsages");
        }
    }
}
