using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GestionDocumentsEtBudget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Libelle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FonctionEcheances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Libelle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DateButoir = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Frequence = table.Column<int>(type: "integer", nullable: false),
                    FonctionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FonctionEcheances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FonctionEcheances_Fonctions_FonctionId",
                        column: x => x.FonctionId,
                        principalTable: "Fonctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FonctionRolesResponsabilites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Libelle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FonctionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FonctionRolesResponsabilites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FonctionRolesResponsabilites_Fonctions_FonctionId",
                        column: x => x.FonctionId,
                        principalTable: "Fonctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TypesBudget",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Libelle = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypesBudget", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TypesDocument",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Libelle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypesDocument", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategoriesBudget",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Libelle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TypeBudgetId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriesBudget", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoriesBudget_TypesBudget_TypeBudgetId",
                        column: x => x.TypeBudgetId,
                        principalTable: "TypesBudget",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Fichier = table.Column<byte[]>(type: "bytea", nullable: false),
                    CategorieId = table.Column<Guid>(type: "uuid", nullable: false),
                    TypeDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Categories_CategorieId",
                        column: x => x.CategorieId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documents_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Documents_TypesDocument_TypeDocumentId",
                        column: x => x.TypeDocumentId,
                        principalTable: "TypesDocument",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SousCategoriesBudget",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Libelle = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CategoryBudgetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SousCategoriesBudget", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SousCategoriesBudget_CategoriesBudget_CategoryBudgetId",
                        column: x => x.CategoryBudgetId,
                        principalTable: "CategoriesBudget",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SousCategoriesBudget_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RubriquesBudget",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Libelle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PrixUnitaire = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Quantite = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    SousCategoryBudgetId = table.Column<Guid>(type: "uuid", nullable: false),
                    MandatId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RubriquesBudget", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RubriquesBudget_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RubriquesBudget_Mandats_MandatId",
                        column: x => x.MandatId,
                        principalTable: "Mandats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RubriquesBudget_SousCategoriesBudget_SousCategoryBudgetId",
                        column: x => x.SousCategoryBudgetId,
                        principalTable: "SousCategoriesBudget",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RubriquesBudgetRealisees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Montant = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Commentaires = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RubriqueBudgetId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RubriquesBudgetRealisees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RubriquesBudgetRealisees_RubriquesBudget_RubriqueBudgetId",
                        column: x => x.RubriqueBudgetId,
                        principalTable: "RubriquesBudget",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categorie_Libelle_Unique",
                table: "Categories",
                column: "Libelle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoriesBudget_Libelle",
                table: "CategoriesBudget",
                column: "Libelle");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriesBudget_TypeBudgetId",
                table: "CategoriesBudget",
                column: "TypeBudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryBudget_TypeBudgetId_Libelle_Unique",
                table: "CategoriesBudget",
                columns: new[] { "TypeBudgetId", "Libelle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Document_ClubId_CategorieId_TypeDocumentId_Nom_Unique",
                table: "Documents",
                columns: new[] { "ClubId", "CategorieId", "TypeDocumentId", "Nom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CategorieId",
                table: "Documents",
                column: "CategorieId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ClubId",
                table: "Documents",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ClubId_CategorieId",
                table: "Documents",
                columns: new[] { "ClubId", "CategorieId" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ClubId_CategorieId_TypeDocumentId",
                table: "Documents",
                columns: new[] { "ClubId", "CategorieId", "TypeDocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ClubId_TypeDocumentId",
                table: "Documents",
                columns: new[] { "ClubId", "TypeDocumentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Nom",
                table: "Documents",
                column: "Nom");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TypeDocumentId",
                table: "Documents",
                column: "TypeDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_FonctionEcheances_DateButoir",
                table: "FonctionEcheances",
                column: "DateButoir");

            migrationBuilder.CreateIndex(
                name: "IX_FonctionEcheances_DateButoir_Frequence",
                table: "FonctionEcheances",
                columns: new[] { "DateButoir", "Frequence" });

            migrationBuilder.CreateIndex(
                name: "IX_FonctionEcheances_FonctionId",
                table: "FonctionEcheances",
                column: "FonctionId");

            migrationBuilder.CreateIndex(
                name: "IX_FonctionEcheances_FonctionId_DateButoir",
                table: "FonctionEcheances",
                columns: new[] { "FonctionId", "DateButoir" });

            migrationBuilder.CreateIndex(
                name: "IX_FonctionEcheances_FonctionId_Frequence",
                table: "FonctionEcheances",
                columns: new[] { "FonctionId", "Frequence" });

            migrationBuilder.CreateIndex(
                name: "IX_FonctionEcheances_FonctionId_Libelle_Unique",
                table: "FonctionEcheances",
                columns: new[] { "FonctionId", "Libelle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FonctionEcheances_Frequence",
                table: "FonctionEcheances",
                column: "Frequence");

            migrationBuilder.CreateIndex(
                name: "IX_FonctionEcheances_Libelle",
                table: "FonctionEcheances",
                column: "Libelle");

            migrationBuilder.CreateIndex(
                name: "IX_FonctionRolesResponsabilites_FonctionId",
                table: "FonctionRolesResponsabilites",
                column: "FonctionId");

            migrationBuilder.CreateIndex(
                name: "IX_FonctionRolesResponsabilites_FonctionId_Libelle_Unique",
                table: "FonctionRolesResponsabilites",
                columns: new[] { "FonctionId", "Libelle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FonctionRolesResponsabilites_Libelle",
                table: "FonctionRolesResponsabilites",
                column: "Libelle");

            migrationBuilder.CreateIndex(
                name: "IX_RubriqueBudget_ClubId_MandatId_SousCategoryBudgetId_Libelle_Unique",
                table: "RubriquesBudget",
                columns: new[] { "ClubId", "MandatId", "SousCategoryBudgetId", "Libelle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RubriquesBudget_ClubId",
                table: "RubriquesBudget",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_RubriquesBudget_ClubId_MandatId",
                table: "RubriquesBudget",
                columns: new[] { "ClubId", "MandatId" });

            migrationBuilder.CreateIndex(
                name: "IX_RubriquesBudget_ClubId_MandatId_SousCategoryBudgetId",
                table: "RubriquesBudget",
                columns: new[] { "ClubId", "MandatId", "SousCategoryBudgetId" });

            migrationBuilder.CreateIndex(
                name: "IX_RubriquesBudget_ClubId_SousCategoryBudgetId",
                table: "RubriquesBudget",
                columns: new[] { "ClubId", "SousCategoryBudgetId" });

            migrationBuilder.CreateIndex(
                name: "IX_RubriquesBudget_Libelle",
                table: "RubriquesBudget",
                column: "Libelle");

            migrationBuilder.CreateIndex(
                name: "IX_RubriquesBudget_MandatId",
                table: "RubriquesBudget",
                column: "MandatId");

            migrationBuilder.CreateIndex(
                name: "IX_RubriquesBudget_PrixUnitaire",
                table: "RubriquesBudget",
                column: "PrixUnitaire");

            migrationBuilder.CreateIndex(
                name: "IX_RubriquesBudget_SousCategoryBudgetId",
                table: "RubriquesBudget",
                column: "SousCategoryBudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_RubriquesBudgetRealisees_Date",
                table: "RubriquesBudgetRealisees",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_RubriquesBudgetRealisees_Montant",
                table: "RubriquesBudgetRealisees",
                column: "Montant");

            migrationBuilder.CreateIndex(
                name: "IX_RubriquesBudgetRealisees_RubriqueBudgetId",
                table: "RubriquesBudgetRealisees",
                column: "RubriqueBudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_RubriquesBudgetRealisees_RubriqueBudgetId_Date",
                table: "RubriquesBudgetRealisees",
                columns: new[] { "RubriqueBudgetId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_RubriquesBudgetRealisees_RubriqueBudgetId_Date_Montant",
                table: "RubriquesBudgetRealisees",
                columns: new[] { "RubriqueBudgetId", "Date", "Montant" });

            migrationBuilder.CreateIndex(
                name: "IX_SousCategoriesBudget_CategoryBudgetId",
                table: "SousCategoriesBudget",
                column: "CategoryBudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_SousCategoriesBudget_ClubId",
                table: "SousCategoriesBudget",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_SousCategoriesBudget_ClubId_CategoryBudgetId",
                table: "SousCategoriesBudget",
                columns: new[] { "ClubId", "CategoryBudgetId" });

            migrationBuilder.CreateIndex(
                name: "IX_SousCategoriesBudget_Libelle",
                table: "SousCategoriesBudget",
                column: "Libelle");

            migrationBuilder.CreateIndex(
                name: "IX_SousCategoryBudget_ClubId_CategoryBudgetId_Libelle_Unique",
                table: "SousCategoriesBudget",
                columns: new[] { "ClubId", "CategoryBudgetId", "Libelle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TypeBudget_Libelle_Unique",
                table: "TypesBudget",
                column: "Libelle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TypeDocument_Libelle_Unique",
                table: "TypesDocument",
                column: "Libelle",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "FonctionEcheances");

            migrationBuilder.DropTable(
                name: "FonctionRolesResponsabilites");

            migrationBuilder.DropTable(
                name: "RubriquesBudgetRealisees");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "TypesDocument");

            migrationBuilder.DropTable(
                name: "RubriquesBudget");

            migrationBuilder.DropTable(
                name: "SousCategoriesBudget");

            migrationBuilder.DropTable(
                name: "CategoriesBudget");

            migrationBuilder.DropTable(
                name: "TypesBudget");
        }
    }
}
