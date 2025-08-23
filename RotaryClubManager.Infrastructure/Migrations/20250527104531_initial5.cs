using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class initial5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reunions_Date",
                table: "Reunions");

            migrationBuilder.DropIndex(
                name: "IX_Reunions_TypeReunionId_Date",
                table: "Reunions");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "Heure",
                table: "Reunions",
                type: "time",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "interval");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "Reunions",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateIndex(
                name: "IX_Reunions_Date_Heure",
                table: "Reunions",
                columns: new[] { "Date", "Heure" });

            migrationBuilder.CreateIndex(
                name: "IX_Reunions_TypeReunionId",
                table: "Reunions",
                column: "TypeReunionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reunions_Date_Heure",
                table: "Reunions");

            migrationBuilder.DropIndex(
                name: "IX_Reunions_TypeReunionId",
                table: "Reunions");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "Heure",
                table: "Reunions",
                type: "interval",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "Reunions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.CreateIndex(
                name: "IX_Reunions_Date",
                table: "Reunions",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Reunions_TypeReunionId_Date",
                table: "Reunions",
                columns: new[] { "TypeReunionId", "Date" });
        }
    }
}
