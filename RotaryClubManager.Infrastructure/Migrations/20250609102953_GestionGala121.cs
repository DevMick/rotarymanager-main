using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GestionGala121 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GalaTombola_GalaId_MembreId_Unique",
                table: "GalaTombolas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GalaTombola_MembreOrExterne",
                table: "GalaTombolas");

            migrationBuilder.DropIndex(
                name: "IX_GalaTicket_GalaId_MembreId_Unique",
                table: "GalaTickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GalaTicket_MembreOrExterne",
                table: "GalaTickets");

            migrationBuilder.CreateIndex(
                name: "IX_GalaTombola_GalaId_MembreId_Unique",
                table: "GalaTombolas",
                columns: new[] { "GalaId", "MembreId" },
                unique: true,
                filter: "\"MembreId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GalaTombola_MembreOrExterne",
                table: "GalaTombolas",
                sql: "(\"MembreId\" IS NOT NULL) OR (\"Externe\" IS NOT NULL AND \"Externe\" != '')");

            migrationBuilder.CreateIndex(
                name: "IX_GalaTicket_GalaId_MembreId_Unique",
                table: "GalaTickets",
                columns: new[] { "GalaId", "MembreId" },
                unique: true,
                filter: "\"MembreId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GalaTicket_MembreOrExterne",
                table: "GalaTickets",
                sql: "(\"MembreId\" IS NOT NULL) OR (\"Externe\" IS NOT NULL AND \"Externe\" != '')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GalaTombola_GalaId_MembreId_Unique",
                table: "GalaTombolas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GalaTombola_MembreOrExterne",
                table: "GalaTombolas");

            migrationBuilder.DropIndex(
                name: "IX_GalaTicket_GalaId_MembreId_Unique",
                table: "GalaTickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GalaTicket_MembreOrExterne",
                table: "GalaTickets");

            migrationBuilder.CreateIndex(
                name: "IX_GalaTombola_GalaId_MembreId_Unique",
                table: "GalaTombolas",
                columns: new[] { "GalaId", "MembreId" },
                unique: true,
                filter: "MembreId IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GalaTombola_MembreOrExterne",
                table: "GalaTombolas",
                sql: "(MembreId IS NOT NULL) OR (Externe IS NOT NULL AND Externe != '')");

            migrationBuilder.CreateIndex(
                name: "IX_GalaTicket_GalaId_MembreId_Unique",
                table: "GalaTickets",
                columns: new[] { "GalaId", "MembreId" },
                unique: true,
                filter: "MembreId IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GalaTicket_MembreOrExterne",
                table: "GalaTickets",
                sql: "(MembreId IS NOT NULL) OR (Externe IS NOT NULL AND Externe != '')");
        }
    }
}
