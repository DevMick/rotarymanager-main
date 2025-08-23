using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Evenement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Evenements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Libelle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Lieu = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EstInterne = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evenements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvenementBudgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Libelle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MontantBudget = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MontantRealise = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    EvenementId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvenementBudgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvenementBudgets_Evenements_EvenementId",
                        column: x => x.EvenementId,
                        principalTable: "Evenements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvenementDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Libelle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Document = table.Column<byte[]>(type: "bytea", nullable: false),
                    DateAjout = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    EvenementId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvenementDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvenementDocuments_Evenements_EvenementId",
                        column: x => x.EvenementId,
                        principalTable: "Evenements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvenementImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Image = table.Column<byte[]>(type: "bytea", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DateAjout = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    EvenementId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvenementImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvenementImages_Evenements_EvenementId",
                        column: x => x.EvenementId,
                        principalTable: "Evenements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvenementRecettes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Libelle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Montant = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EvenementId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvenementRecettes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvenementRecettes_Evenements_EvenementId",
                        column: x => x.EvenementId,
                        principalTable: "Evenements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvenementBudget_EvenementId_Libelle_Unique",
                table: "EvenementBudgets",
                columns: new[] { "EvenementId", "Libelle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvenementBudgets_EvenementId",
                table: "EvenementBudgets",
                column: "EvenementId");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementBudgets_Libelle",
                table: "EvenementBudgets",
                column: "Libelle");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementBudgets_MontantBudget",
                table: "EvenementBudgets",
                column: "MontantBudget");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementDocument_EvenementId_Libelle_Unique",
                table: "EvenementDocuments",
                columns: new[] { "EvenementId", "Libelle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvenementDocuments_DateAjout",
                table: "EvenementDocuments",
                column: "DateAjout");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementDocuments_EvenementId",
                table: "EvenementDocuments",
                column: "EvenementId");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementDocuments_Libelle",
                table: "EvenementDocuments",
                column: "Libelle");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementImages_DateAjout",
                table: "EvenementImages",
                column: "DateAjout");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementImages_Description",
                table: "EvenementImages",
                column: "Description");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementImages_EvenementId",
                table: "EvenementImages",
                column: "EvenementId");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementRecettes_EvenementId",
                table: "EvenementRecettes",
                column: "EvenementId");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementRecettes_Libelle",
                table: "EvenementRecettes",
                column: "Libelle");

            migrationBuilder.CreateIndex(
                name: "IX_EvenementRecettes_Montant",
                table: "EvenementRecettes",
                column: "Montant");

            migrationBuilder.CreateIndex(
                name: "IX_Evenements_Date",
                table: "Evenements",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Evenements_Date_EstInterne",
                table: "Evenements",
                columns: new[] { "Date", "EstInterne" });

            migrationBuilder.CreateIndex(
                name: "IX_Evenements_EstInterne",
                table: "Evenements",
                column: "EstInterne");

            migrationBuilder.CreateIndex(
                name: "IX_Evenements_Libelle",
                table: "Evenements",
                column: "Libelle");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvenementBudgets");

            migrationBuilder.DropTable(
                name: "EvenementDocuments");

            migrationBuilder.DropTable(
                name: "EvenementImages");

            migrationBuilder.DropTable(
                name: "EvenementRecettes");

            migrationBuilder.DropTable(
                name: "Evenements");
        }
    }
}
