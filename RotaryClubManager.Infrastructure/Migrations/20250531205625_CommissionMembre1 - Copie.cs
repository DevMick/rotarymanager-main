using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommissionMembre1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MembresCommission_AspNetUsers_ApplicationUserId",
                table: "MembresCommission");

            migrationBuilder.DropIndex(
                name: "IX_MembresCommission_ApplicationUserId",
                table: "MembresCommission");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "MembresCommission");

            migrationBuilder.RenameIndex(
                name: "IX_MembresCommission_CommissionId_MembreId_MandatId",
                table: "MembresCommission",
                newName: "IX_MembreCommission_Commission_Membre_Mandat_Unique");

            migrationBuilder.CreateIndex(
                name: "IX_MembresCommission_CommissionId",
                table: "MembresCommission",
                column: "CommissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MembresCommission_CommissionId",
                table: "MembresCommission");

            migrationBuilder.RenameIndex(
                name: "IX_MembreCommission_Commission_Membre_Mandat_Unique",
                table: "MembresCommission",
                newName: "IX_MembresCommission_CommissionId_MembreId_MandatId");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "MembresCommission",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MembresCommission_ApplicationUserId",
                table: "MembresCommission",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_MembresCommission_AspNetUsers_ApplicationUserId",
                table: "MembresCommission",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
