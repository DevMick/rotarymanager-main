using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMontantRealiseToRubriqueBudget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_RubriquesBudget_SousCategoryBudgetId",
                table: "RubriquesBudget",
                newName: "IX_RubriqueBudget_SousCategoryBudgetId");

            migrationBuilder.RenameIndex(
                name: "IX_RubriquesBudget_PrixUnitaire",
                table: "RubriquesBudget",
                newName: "IX_RubriqueBudget_PrixUnitaire");

            migrationBuilder.RenameIndex(
                name: "IX_RubriquesBudget_MandatId",
                table: "RubriquesBudget",
                newName: "IX_RubriqueBudget_MandatId");

            migrationBuilder.RenameIndex(
                name: "IX_RubriquesBudget_Libelle",
                table: "RubriquesBudget",
                newName: "IX_RubriqueBudget_Libelle");

            migrationBuilder.RenameIndex(
                name: "IX_RubriquesBudget_ClubId_SousCategoryBudgetId",
                table: "RubriquesBudget",
                newName: "IX_RubriqueBudget_ClubId_SousCategoryBudgetId");

            migrationBuilder.RenameIndex(
                name: "IX_RubriquesBudget_ClubId_MandatId_SousCategoryBudgetId",
                table: "RubriquesBudget",
                newName: "IX_RubriqueBudget_ClubId_MandatId_SousCategoryBudgetId");

            migrationBuilder.RenameIndex(
                name: "IX_RubriquesBudget_ClubId_MandatId",
                table: "RubriquesBudget",
                newName: "IX_RubriqueBudget_ClubId_MandatId");

            migrationBuilder.RenameIndex(
                name: "IX_RubriquesBudget_ClubId",
                table: "RubriquesBudget",
                newName: "IX_RubriqueBudget_ClubId");

            migrationBuilder.AddColumn<decimal>(
                name: "MontantRealise",
                table: "RubriquesBudget",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_RubriqueBudget_ClubId_MandatId_Budget",
                table: "RubriquesBudget",
                columns: new[] { "ClubId", "MandatId", "PrixUnitaire", "Quantite" });

            migrationBuilder.CreateIndex(
                name: "IX_RubriqueBudget_ClubId_MandatId_MontantRealise",
                table: "RubriquesBudget",
                columns: new[] { "ClubId", "MandatId", "MontantRealise" });

            migrationBuilder.CreateIndex(
                name: "IX_RubriqueBudget_MontantRealise",
                table: "RubriquesBudget",
                column: "MontantRealise");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RubriqueBudget_ClubId_MandatId_Budget",
                table: "RubriquesBudget");

            migrationBuilder.DropIndex(
                name: "IX_RubriqueBudget_ClubId_MandatId_MontantRealise",
                table: "RubriquesBudget");

            migrationBuilder.DropIndex(
                name: "IX_RubriqueBudget_MontantRealise",
                table: "RubriquesBudget");

            migrationBuilder.DropColumn(
                name: "MontantRealise",
                table: "RubriquesBudget");

            migrationBuilder.RenameIndex(
                name: "IX_RubriqueBudget_SousCategoryBudgetId",
                table: "RubriquesBudget",
                newName: "IX_RubriquesBudget_SousCategoryBudgetId");

            migrationBuilder.RenameIndex(
                name: "IX_RubriqueBudget_PrixUnitaire",
                table: "RubriquesBudget",
                newName: "IX_RubriquesBudget_PrixUnitaire");

            migrationBuilder.RenameIndex(
                name: "IX_RubriqueBudget_MandatId",
                table: "RubriquesBudget",
                newName: "IX_RubriquesBudget_MandatId");

            migrationBuilder.RenameIndex(
                name: "IX_RubriqueBudget_Libelle",
                table: "RubriquesBudget",
                newName: "IX_RubriquesBudget_Libelle");

            migrationBuilder.RenameIndex(
                name: "IX_RubriqueBudget_ClubId_SousCategoryBudgetId",
                table: "RubriquesBudget",
                newName: "IX_RubriquesBudget_ClubId_SousCategoryBudgetId");

            migrationBuilder.RenameIndex(
                name: "IX_RubriqueBudget_ClubId_MandatId_SousCategoryBudgetId",
                table: "RubriquesBudget",
                newName: "IX_RubriquesBudget_ClubId_MandatId_SousCategoryBudgetId");

            migrationBuilder.RenameIndex(
                name: "IX_RubriqueBudget_ClubId_MandatId",
                table: "RubriquesBudget",
                newName: "IX_RubriquesBudget_ClubId_MandatId");

            migrationBuilder.RenameIndex(
                name: "IX_RubriqueBudget_ClubId",
                table: "RubriquesBudget",
                newName: "IX_RubriquesBudget_ClubId");
        }
    }
}
