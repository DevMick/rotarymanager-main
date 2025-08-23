using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class initialcreate23 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Comites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MandatId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comites_Mandats_MandatId",
                        column: x => x.MandatId,
                        principalTable: "Mandats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fonctions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NomFonction = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fonctions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComiteMembres",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ComiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembreId = table.Column<string>(type: "text", nullable: false),
                    FonctionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComiteMembres", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComiteMembres_AspNetUsers_MembreId",
                        column: x => x.MembreId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComiteMembres_Comites_ComiteId",
                        column: x => x.ComiteId,
                        principalTable: "Comites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComiteMembres_Fonctions_FonctionId",
                        column: x => x.FonctionId,
                        principalTable: "Fonctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComiteMembres_ComiteId_MembreId_FonctionId",
                table: "ComiteMembres",
                columns: new[] { "ComiteId", "MembreId", "FonctionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComiteMembres_FonctionId",
                table: "ComiteMembres",
                column: "FonctionId");

            migrationBuilder.CreateIndex(
                name: "IX_ComiteMembres_MembreId",
                table: "ComiteMembres",
                column: "MembreId");

            migrationBuilder.CreateIndex(
                name: "IX_Comites_MandatId",
                table: "Comites",
                column: "MandatId");

            migrationBuilder.CreateIndex(
                name: "IX_Fonctions_NomFonction",
                table: "Fonctions",
                column: "NomFonction");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComiteMembres");

            migrationBuilder.DropTable(
                name: "Comites");

            migrationBuilder.DropTable(
                name: "Fonctions");
        }
    }
}
