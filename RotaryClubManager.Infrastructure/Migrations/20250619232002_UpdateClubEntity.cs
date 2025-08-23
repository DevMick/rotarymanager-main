using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClubEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RotaryClubs");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_Code",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "FoundedDate",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Clubs");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Clubs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Adresse",
                table: "Clubs",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DateCreation",
                table: "Clubs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Frequence",
                table: "Clubs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "HeureReunion",
                table: "Clubs",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<string>(
                name: "JourReunion",
                table: "Clubs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LieuReunion",
                table: "Clubs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NumeroClub",
                table: "Clubs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NumeroTelephone",
                table: "Clubs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ParrainePar",
                table: "Clubs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_Email",
                table: "Clubs",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_NumeroClub",
                table: "Clubs",
                column: "NumeroClub",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clubs_Email",
                table: "Clubs");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_NumeroClub",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "Adresse",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "DateCreation",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "Frequence",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "HeureReunion",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "JourReunion",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "LieuReunion",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "NumeroClub",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "NumeroTelephone",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "ParrainePar",
                table: "Clubs");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Clubs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Clubs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Clubs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Clubs",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Clubs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Clubs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FoundedDate",
                table: "Clubs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Clubs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Clubs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Clubs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Clubs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RotaryClubs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Adresse = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Frequence = table.Column<string>(type: "text", nullable: false),
                    HeureReunion = table.Column<TimeSpan>(type: "time", nullable: false),
                    JourReunion = table.Column<string>(type: "text", nullable: false),
                    LieuReunion = table.Column<string>(type: "text", nullable: false),
                    NumeroClub = table.Column<string>(type: "text", nullable: false),
                    NumeroTelephone = table.Column<string>(type: "text", nullable: false),
                    ParrainePar = table.Column<string>(type: "text", nullable: false)
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
                name: "IX_Clubs_Code",
                table: "Clubs",
                column: "Code",
                unique: true);

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
    }
}
