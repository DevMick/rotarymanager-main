using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GestionGala12Fixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GalaTombola_GalaId_MembreId_Unique",
                table: "GalaTombolas");

            migrationBuilder.DropIndex(
                name: "IX_GalaTicket_GalaId_MembreId_Unique",
                table: "GalaTickets");

            migrationBuilder.AlterColumn<string>(
                name: "MembreId",
                table: "GalaTombolas",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Externe",
                table: "GalaTombolas",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MembreId",
                table: "GalaTickets",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Externe",
                table: "GalaTickets",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            // Index avec guillemets pour PostgreSQL
            migrationBuilder.CreateIndex(
                name: "IX_GalaTombola_GalaId_MembreId_Unique",
                table: "GalaTombolas",
                columns: new[] { "GalaId", "MembreId" },
                unique: true,
                filter: "\"MembreId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GalaTombolas_Externe",
                table: "GalaTombolas",
                column: "Externe");

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

            migrationBuilder.CreateIndex(
                name: "IX_GalaTickets_Externe",
                table: "GalaTickets",
                column: "Externe");

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

            migrationBuilder.DropIndex(
                name: "IX_GalaTombolas_Externe",
                table: "GalaTombolas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GalaTombola_MembreOrExterne",
                table: "GalaTombolas");

            migrationBuilder.DropIndex(
                name: "IX_GalaTicket_GalaId_MembreId_Unique",
                table: "GalaTickets");

            migrationBuilder.DropIndex(
                name: "IX_GalaTickets_Externe",
                table: "GalaTickets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GalaTicket_MembreOrExterne",
                table: "GalaTickets");

            migrationBuilder.DropColumn(
                name: "Externe",
                table: "GalaTombolas");

            migrationBuilder.DropColumn(
                name: "Externe",
                table: "GalaTickets");

            migrationBuilder.AlterColumn<string>(
                name: "MembreId",
                table: "GalaTombolas",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MembreId",
                table: "GalaTickets",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GalaTombola_GalaId_MembreId_Unique",
                table: "GalaTombolas",
                columns: new[] { "GalaId", "MembreId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GalaTicket_GalaId_MembreId_Unique",
                table: "GalaTickets",
                columns: new[] { "GalaId", "MembreId" },
                unique: true);
        }
    }
}