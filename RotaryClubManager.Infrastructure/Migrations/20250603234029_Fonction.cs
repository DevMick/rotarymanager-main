using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fonction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "Fonctions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fonctions_ParentId",
                table: "Fonctions",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Fonctions_Fonctions_ParentId",
                table: "Fonctions",
                column: "ParentId",
                principalTable: "Fonctions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fonctions_Fonctions_ParentId",
                table: "Fonctions");

            migrationBuilder.DropIndex(
                name: "IX_Fonctions_ParentId",
                table: "Fonctions");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Fonctions");
        }
    }
}
