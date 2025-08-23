using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class initial10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Création de la table Cotisations
            migrationBuilder.CreateTable(
                name: "Cotisations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Montant = table.Column<int>(type: "integer", nullable: false),
                    MembreId = table.Column<string>(type: "text", nullable: false),
                    MandatId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cotisations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cotisations_AspNetUsers_MembreId",
                        column: x => x.MembreId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cotisations_Mandats_MandatId",
                        column: x => x.MandatId,
                        principalTable: "Mandats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Création de la table PaiementCotisations
            migrationBuilder.CreateTable(
                name: "PaiementCotisations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Montant = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp", nullable: false),
                    Commentaires = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MembreId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaiementCotisations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaiementCotisations_AspNetUsers_MembreId",
                        column: x => x.MembreId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Index pour Cotisations
            migrationBuilder.CreateIndex(
                name: "IX_Cotisations_MandatId",
                table: "Cotisations",
                column: "MandatId");

            migrationBuilder.CreateIndex(
                name: "IX_Cotisations_MembreId",
                table: "Cotisations",
                column: "MembreId");

            migrationBuilder.CreateIndex(
                name: "IX_Cotisations_Montant",
                table: "Cotisations",
                column: "Montant");

            migrationBuilder.CreateIndex(
                name: "IX_Cotisations_MembreId_MandatId",
                table: "Cotisations",
                columns: new[] { "MembreId", "MandatId" },
                unique: true);

            // Index pour PaiementCotisations
            migrationBuilder.CreateIndex(
                name: "IX_PaiementCotisations_Date",
                table: "PaiementCotisations",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementCotisations_MembreId",
                table: "PaiementCotisations",
                column: "MembreId");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementCotisations_Montant",
                table: "PaiementCotisations",
                column: "Montant");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementCotisations_MembreId_Date",
                table: "PaiementCotisations",
                columns: new[] { "MembreId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Suppression des tables (dans l'ordre inverse de création)
            migrationBuilder.DropTable(name: "PaiementCotisations");
            migrationBuilder.DropTable(name: "Cotisations");
        }
    }
}