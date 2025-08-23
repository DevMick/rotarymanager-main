using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GestionGala : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Galas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Libelle = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Lieu = table.Column<string>(type: "text", nullable: false),
                    NombreTables = table.Column<int>(type: "integer", nullable: false),
                    NombreSouchesTickets = table.Column<int>(type: "integer", nullable: false),
                    QuantiteParSoucheTickets = table.Column<int>(type: "integer", nullable: false),
                    NombreSouchesTombola = table.Column<int>(type: "integer", nullable: false),
                    QuantiteParSoucheTombola = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Galas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GalaInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom_Prenom = table.Column<string>(type: "text", nullable: false),
                    GalaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GalaInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GalaInvites_Galas_GalaId",
                        column: x => x.GalaId,
                        principalTable: "Galas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GalaTables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TableLibelle = table.Column<string>(type: "text", nullable: false),
                    GalaId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GalaTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GalaTables_Galas_GalaId",
                        column: x => x.GalaId,
                        principalTable: "Galas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GalaTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantite = table.Column<int>(type: "integer", nullable: false),
                    GalaId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembreId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GalaTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GalaTickets_AspNetUsers_MembreId",
                        column: x => x.MembreId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GalaTickets_Galas_GalaId",
                        column: x => x.GalaId,
                        principalTable: "Galas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GalaTombolas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantite = table.Column<int>(type: "integer", nullable: false),
                    GalaId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembreId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GalaTombolas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GalaTombolas_AspNetUsers_MembreId",
                        column: x => x.MembreId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GalaTombolas_Galas_GalaId",
                        column: x => x.GalaId,
                        principalTable: "Galas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GalaTableAffectations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GalaTableId = table.Column<Guid>(type: "uuid", nullable: false),
                    GalaInvitesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GalaTableAffectations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GalaTableAffectations_GalaInvites_GalaInvitesId",
                        column: x => x.GalaInvitesId,
                        principalTable: "GalaInvites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GalaTableAffectations_GalaTables_GalaTableId",
                        column: x => x.GalaTableId,
                        principalTable: "GalaTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GalaInvites_GalaId",
                table: "GalaInvites",
                column: "GalaId");

            migrationBuilder.CreateIndex(
                name: "IX_GalaTableAffectation_GalaTableId_GalaInvitesId_Unique",
                table: "GalaTableAffectations",
                columns: new[] { "GalaTableId", "GalaInvitesId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GalaTableAffectations_GalaInvitesId",
                table: "GalaTableAffectations",
                column: "GalaInvitesId");

            migrationBuilder.CreateIndex(
                name: "IX_GalaTableAffectations_GalaTableId",
                table: "GalaTableAffectations",
                column: "GalaTableId");

            migrationBuilder.CreateIndex(
                name: "IX_GalaTables_GalaId",
                table: "GalaTables",
                column: "GalaId");

            migrationBuilder.CreateIndex(
                name: "IX_GalaTicket_GalaId_MembreId_Unique",
                table: "GalaTickets",
                columns: new[] { "GalaId", "MembreId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GalaTickets_GalaId",
                table: "GalaTickets",
                column: "GalaId");

            migrationBuilder.CreateIndex(
                name: "IX_GalaTickets_MembreId",
                table: "GalaTickets",
                column: "MembreId");

            migrationBuilder.CreateIndex(
                name: "IX_GalaTickets_Quantite",
                table: "GalaTickets",
                column: "Quantite");

            migrationBuilder.CreateIndex(
                name: "IX_GalaTombola_GalaId_MembreId_Unique",
                table: "GalaTombolas",
                columns: new[] { "GalaId", "MembreId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GalaTombolas_GalaId",
                table: "GalaTombolas",
                column: "GalaId");

            migrationBuilder.CreateIndex(
                name: "IX_GalaTombolas_MembreId",
                table: "GalaTombolas",
                column: "MembreId");

            migrationBuilder.CreateIndex(
                name: "IX_GalaTombolas_Quantite",
                table: "GalaTombolas",
                column: "Quantite");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GalaTableAffectations");

            migrationBuilder.DropTable(
                name: "GalaTickets");

            migrationBuilder.DropTable(
                name: "GalaTombolas");

            migrationBuilder.DropTable(
                name: "GalaInvites");

            migrationBuilder.DropTable(
                name: "GalaTables");

            migrationBuilder.DropTable(
                name: "Galas");
        }
    }
}
