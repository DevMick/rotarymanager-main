using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClubEntityCorrected : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Supprimer l'index unique sur Code avant de supprimer la colonne
            migrationBuilder.DropIndex(
                name: "IX_Clubs_Code",
                table: "Clubs");

            // 2. Ajouter les nouvelles colonnes obligatoires avec des valeurs par défaut temporaires
            migrationBuilder.AddColumn<string>(
                name: "DateCreation",
                table: "Clubs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "2024-01-01");

            migrationBuilder.AddColumn<string>(
                name: "NumeroClub",
                table: "Clubs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "TEMP");

            migrationBuilder.AddColumn<string>(
                name: "NumeroTelephone",
                table: "Clubs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "+0000000000");

            migrationBuilder.AddColumn<string>(
                name: "LieuReunion",
                table: "Clubs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Lieu à définir");

            migrationBuilder.AddColumn<string>(
                name: "ParrainePar",
                table: "Clubs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Rotary International");

            migrationBuilder.AddColumn<string>(
                name: "JourReunion",
                table: "Clubs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Mercredi");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "HeureReunion",
                table: "Clubs",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(18, 30, 0));

            migrationBuilder.AddColumn<string>(
                name: "Frequence",
                table: "Clubs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Hebdomadaire");

            migrationBuilder.AddColumn<string>(
                name: "Adresse",
                table: "Clubs",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "Adresse à définir");

            // 3. Mettre à jour la colonne Email pour la rendre obligatoire
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

            // 4. Mettre à jour les données existantes AVANT de créer les index uniques

            // Générer des numéros de club uniques
            migrationBuilder.Sql(@"
                UPDATE ""Clubs"" 
                SET ""NumeroClub"" = 'RC' || LPAD(ROW_NUMBER() OVER (ORDER BY ""Id"")::text, 3, '0')
                WHERE ""NumeroClub"" = 'TEMP';
            ");

            // Mettre à jour les emails vides avec des valeurs uniques
            migrationBuilder.Sql(@"
                UPDATE ""Clubs"" 
                SET ""Email"" = CASE 
                    WHEN ""Email"" = '' OR ""Email"" IS NULL 
                    THEN LOWER(REPLACE(""Name"", ' ', '')) || ROW_NUMBER() OVER (ORDER BY ""Id"") || '@club.rotary'
                    ELSE ""Email""
                END
                WHERE ""Email"" = '' OR ""Email"" IS NULL;
            ");

            // Combiner Address, City, Country en Adresse pour les clubs existants
            migrationBuilder.Sql(@"
                UPDATE ""Clubs"" 
                SET ""Adresse"" = COALESCE(""Address"", '') || 
                                CASE WHEN ""City"" IS NOT NULL AND ""City"" != '' 
                                     THEN ', ' || ""City"" 
                                     ELSE '' END ||
                                CASE WHEN ""Country"" IS NOT NULL AND ""Country"" != '' 
                                     THEN ', ' || ""Country"" 
                                     ELSE '' END
                WHERE ""Adresse"" = 'Adresse à définir' AND 
                      (""Address"" IS NOT NULL OR ""City"" IS NOT NULL OR ""Country"" IS NOT NULL);
            ");

            // Copier PhoneNumber vers NumeroTelephone
            migrationBuilder.Sql(@"
                UPDATE ""Clubs"" 
                SET ""NumeroTelephone"" = ""PhoneNumber""
                WHERE ""PhoneNumber"" IS NOT NULL AND ""PhoneNumber"" != '' AND ""NumeroTelephone"" = '+0000000000';
            ");

            // Nettoyer les adresses vides
            migrationBuilder.Sql(@"
                UPDATE ""Clubs"" 
                SET ""Adresse"" = 'Adresse à définir'
                WHERE ""Adresse"" = '' OR ""Adresse"" = ', ' OR ""Adresse"" = ', , ';
            ");

            // 5. MAINTENANT créer les index uniques (après avoir mis à jour les données)
            migrationBuilder.CreateIndex(
                name: "IX_Clubs_NumeroClub",
                table: "Clubs",
                column: "NumeroClub",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_Email",
                table: "Clubs",
                column: "Email",
                unique: true);

            // 6. Supprimer les anciennes colonnes (sauf Email qui a été modifiée)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Supprimer les nouveaux index
            migrationBuilder.DropIndex(
                name: "IX_Clubs_Email",
                table: "Clubs");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_NumeroClub",
                table: "Clubs");

            // Ajouter les anciennes colonnes
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

            // Remettre Email comme nullable
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Clubs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            // Migrer les données vers les anciennes colonnes
            migrationBuilder.Sql(@"
                UPDATE ""Clubs"" 
                SET ""Code"" = ""NumeroClub"",
                    ""PhoneNumber"" = ""NumeroTelephone"",
                    ""Address"" = ""Adresse"",
                    ""Description"" = 'Description du club ' || ""Name"",
                    ""IsActive"" = true;
            ");

            // Supprimer les nouvelles colonnes
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

            // Remettre l'ancien index
            migrationBuilder.CreateIndex(
                name: "IX_Clubs_Code",
                table: "Clubs",
                column: "Code",
                unique: true);
        }
    }
}