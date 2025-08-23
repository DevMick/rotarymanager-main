using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ComiteMembre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComiteMembres_Comites_ComiteId",
                table: "ComiteMembres");

            migrationBuilder.RenameColumn(
                name: "ComiteId",
                table: "ComiteMembres",
                newName: "MandatId");

            migrationBuilder.RenameIndex(
                name: "IX_ComiteMembres_ComiteId_MembreId_FonctionId",
                table: "ComiteMembres",
                newName: "IX_ComiteMembres_MandatId_MembreId_FonctionId");

            migrationBuilder.AlterColumn<string>(
                name: "Comite",
                table: "Mandats",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_ComiteMembres_Mandats_MandatId",
                table: "ComiteMembres",
                column: "MandatId",
                principalTable: "Mandats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComiteMembres_Mandats_MandatId",
                table: "ComiteMembres");

            migrationBuilder.RenameColumn(
                name: "MandatId",
                table: "ComiteMembres",
                newName: "ComiteId");

            migrationBuilder.RenameIndex(
                name: "IX_ComiteMembres_MandatId_MembreId_FonctionId",
                table: "ComiteMembres",
                newName: "IX_ComiteMembres_ComiteId_MembreId_FonctionId");

            migrationBuilder.AlterColumn<string>(
                name: "Comite",
                table: "Mandats",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddForeignKey(
                name: "FK_ComiteMembres_Comites_ComiteId",
                table: "ComiteMembres",
                column: "ComiteId",
                principalTable: "Comites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
