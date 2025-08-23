using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GestionGala122 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_GalaTableAffectations_GalaTableId",
                table: "GalaTableAffectations",
                newName: "IX_GalaTableAffectation_GalaTableId");

            migrationBuilder.RenameIndex(
                name: "IX_GalaTableAffectations_GalaInvitesId",
                table: "GalaTableAffectations",
                newName: "IX_GalaTableAffectation_GalaInvitesId");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateAjout",
                table: "GalaTableAffectations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()"); // Corrigé pour PostgreSQL

            migrationBuilder.CreateIndex(
                name: "IX_GalaTableAffectation_DateAjout",
                table: "GalaTableAffectations",
                column: "DateAjout");

            migrationBuilder.CreateIndex(
                name: "IX_GalaTableAffectation_DateAjout_GalaTableId",
                table: "GalaTableAffectations",
                columns: new[] { "DateAjout", "GalaTableId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GalaTableAffectation_DateAjout",
                table: "GalaTableAffectations");

            migrationBuilder.DropIndex(
                name: "IX_GalaTableAffectation_DateAjout_GalaTableId",
                table: "GalaTableAffectations");

            migrationBuilder.DropColumn(
                name: "DateAjout",
                table: "GalaTableAffectations");

            migrationBuilder.RenameIndex(
                name: "IX_GalaTableAffectation_GalaTableId",
                table: "GalaTableAffectations",
                newName: "IX_GalaTableAffectations_GalaTableId");

            migrationBuilder.RenameIndex(
                name: "IX_GalaTableAffectation_GalaInvitesId",
                table: "GalaTableAffectations",
                newName: "IX_GalaTableAffectations_GalaInvitesId");
        }
    }
}