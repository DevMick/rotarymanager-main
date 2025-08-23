using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class initial6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Clubs_PrimaryClubId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "UserClubs");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "PrimaryClubId",
                table: "AspNetUsers",
                newName: "ClubId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_PrimaryClubId",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_ClubId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Clubs_ClubId",
                table: "AspNetUsers",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Clubs_ClubId",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "ClubId",
                table: "AspNetUsers",
                newName: "PrimaryClubId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_ClubId",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_PrimaryClubId");

            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "UserClubs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "AspNetUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Clubs_PrimaryClubId",
                table: "AspNetUsers",
                column: "PrimaryClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
