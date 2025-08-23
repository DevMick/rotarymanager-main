using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class initial3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TypesReunion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Libelle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypesReunion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reunions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TypeReunionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reunions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reunions_TypesReunion_TypeReunionId",
                        column: x => x.TypeReunionId,
                        principalTable: "TypesReunion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvitesReunion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Prenom = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Telephone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Organisation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReunionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitesReunion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvitesReunion_Reunions_ReunionId",
                        column: x => x.ReunionId,
                        principalTable: "Reunions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ListesPresence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MembreId = table.Column<string>(type: "text", nullable: false),
                    ReunionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListesPresence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListesPresence_AspNetUsers_MembreId",
                        column: x => x.MembreId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ListesPresence_Reunions_ReunionId",
                        column: x => x.ReunionId,
                        principalTable: "Reunions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrdresDuJour",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ReunionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdresDuJour", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdresDuJour_Reunions_ReunionId",
                        column: x => x.ReunionId,
                        principalTable: "Reunions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReunionDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Libelle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReunionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Document = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReunionDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReunionDocuments_Reunions_ReunionId",
                        column: x => x.ReunionId,
                        principalTable: "Reunions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvitesReunion_Email",
                table: "InvitesReunion",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_InvitesReunion_Nom_Prenom",
                table: "InvitesReunion",
                columns: new[] { "Nom", "Prenom" });

            migrationBuilder.CreateIndex(
                name: "IX_InvitesReunion_ReunionId",
                table: "InvitesReunion",
                column: "ReunionId");

            migrationBuilder.CreateIndex(
                name: "IX_ListesPresence_MembreId",
                table: "ListesPresence",
                column: "MembreId");

            migrationBuilder.CreateIndex(
                name: "IX_ListesPresence_MembreId_ReunionId",
                table: "ListesPresence",
                columns: new[] { "MembreId", "ReunionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListesPresence_ReunionId",
                table: "ListesPresence",
                column: "ReunionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdresDuJour_ReunionId",
                table: "OrdresDuJour",
                column: "ReunionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReunionDocuments_Libelle",
                table: "ReunionDocuments",
                column: "Libelle");

            migrationBuilder.CreateIndex(
                name: "IX_ReunionDocuments_ReunionId",
                table: "ReunionDocuments",
                column: "ReunionId");

            migrationBuilder.CreateIndex(
                name: "IX_Reunions_Date",
                table: "Reunions",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Reunions_TypeReunionId_Date",
                table: "Reunions",
                columns: new[] { "TypeReunionId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_TypesReunion_Libelle",
                table: "TypesReunion",
                column: "Libelle",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvitesReunion");

            migrationBuilder.DropTable(
                name: "ListesPresence");

            migrationBuilder.DropTable(
                name: "OrdresDuJour");

            migrationBuilder.DropTable(
                name: "ReunionDocuments");

            migrationBuilder.DropTable(
                name: "Reunions");

            migrationBuilder.DropTable(
                name: "TypesReunion");
        }
    }
}
