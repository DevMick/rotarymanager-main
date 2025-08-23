using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdreJourRapportTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrdreJourRapports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdreDuJourId = table.Column<Guid>(type: "uuid", nullable: false),
                    Texte = table.Column<string>(type: "text", nullable: false),
                    Divers = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdreJourRapports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdreJourRapports_OrdresDuJour_OrdreDuJourId",
                        column: x => x.OrdreDuJourId,
                        principalTable: "OrdresDuJour",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrdreJourRapports_OrdreDuJourId",
                table: "OrdreJourRapports",
                column: "OrdreDuJourId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdreJourRapports_OrdreDuJourId_Texte",
                table: "OrdreJourRapports",
                columns: new[] { "OrdreDuJourId", "Texte" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrdreJourRapports");
        }
    }
}
