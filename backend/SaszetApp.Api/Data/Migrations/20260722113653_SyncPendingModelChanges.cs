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
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserScanUsages",
                type: "character varying(36)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserScanLimits",
                type: "character varying(36)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");


            migrationBuilder.AddForeignKey(
                name: "FK_UserScanLimits_Users_UserId",
                table: "UserScanLimits",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserScanUsages_Users_UserId",
                table: "UserScanUsages",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserScanLimits_Users_UserId",
                table: "UserScanLimits");

            migrationBuilder.DropForeignKey(
                name: "FK_UserScanUsages_Users_UserId",
                table: "UserScanUsages");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserScanUsages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(36)");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserScanLimits",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(36)");

        }
    }
}
