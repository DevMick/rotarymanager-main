using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRotaryClubTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RotaryClubs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroClub = table.Column<string>(type: "text", nullable: false),
                    NumeroTelephone = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    LieuReunion = table.Column<string>(type: "text", nullable: false),
                    ParrainePar = table.Column<string>(type: "text", nullable: false),
                    JourReunion = table.Column<string>(type: "text", nullable: false),
                    HeureReunion = table.Column<TimeSpan>(type: "time", nullable: false),
                    Frequence = table.Column<string>(type: "text", nullable: false),
                    Adresse = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RotaryClubs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RotaryClubs_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RotaryClubs_ClubId",
                table: "RotaryClubs",
                column: "ClubId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RotaryClubs_Email",
                table: "RotaryClubs",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_RotaryClubs_JourReunion",
                table: "RotaryClubs",
                column: "JourReunion");

            migrationBuilder.CreateIndex(
                name: "IX_RotaryClubs_NumeroClub",
                table: "RotaryClubs",
                column: "NumeroClub",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RotaryClubs");
        }
    }
}
