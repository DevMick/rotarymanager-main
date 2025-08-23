using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixMembreCommissionFinal1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Vérification conditionnelle de l'existence de la contrainte avant suppression
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE constraint_name = 'FK_MembresCommission_CommissionClub_CommissionClubId'
                        AND table_name = 'MembresCommission'
                    ) THEN
                        ALTER TABLE ""MembresCommission"" DROP CONSTRAINT ""FK_MembresCommission_CommissionClub_CommissionClubId"";
                    END IF;
                END $$;
            ");

            // Vérification conditionnelle de l'existence de l'index avant suppression
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_indexes 
                        WHERE indexname = 'IX_MembresCommission_CommissionClubId'
                        AND tablename = 'MembresCommission'
                    ) THEN
                        DROP INDEX ""IX_MembresCommission_CommissionClubId"";
                    END IF;
                END $$;
            ");

            // Vérification conditionnelle de l'existence de la colonne avant suppression
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'MembresCommission' 
                        AND column_name = 'CommissionClubId'
                    ) THEN
                        ALTER TABLE ""MembresCommission"" DROP COLUMN ""CommissionClubId"";
                    END IF;
                END $$;
            ");

            // Vérification conditionnelle de l'existence de la table avant suppression
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.tables 
                        WHERE table_name = 'CommissionClub'
                    ) THEN
                        DROP TABLE ""CommissionClub"";
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CommissionClubId",
                table: "MembresCommission",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommissionClub",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EstActive = table.Column<bool>(type: "boolean", nullable: false),
                    NotesSpecifiques = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionClub", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionClub_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommissionClub_Commissions_CommissionId",
                        column: x => x.CommissionId,
                        principalTable: "Commissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MembresCommission_CommissionClubId",
                table: "MembresCommission",
                column: "CommissionClubId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionClub_ClubId",
                table: "CommissionClub",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionClub_CommissionId",
                table: "CommissionClub",
                column: "CommissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_MembresCommission_CommissionClub_CommissionClubId",
                table: "MembresCommission",
                column: "CommissionClubId",
                principalTable: "CommissionClub",
                principalColumn: "Id");
        }
    }
}