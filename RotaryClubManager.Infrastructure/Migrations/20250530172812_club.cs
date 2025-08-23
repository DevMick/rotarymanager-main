using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class club : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Commissions_Nom",
                table: "Commissions");

            // ===== SECTION 1: TRANSFORMATION DE LA TABLE CLUBS =====

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

            // ===== SECTION 2: AJOUT DES CLUBID AUX AUTRES TABLES =====

            // Ajouter les colonnes ClubId sans valeur par défaut pour éviter les contraintes de clé étrangère
            migrationBuilder.AddColumn<Guid>(
                name: "ClubId",
                table: "Reunions",
                type: "uuid",
                nullable: true); // Nullable temporairement

            migrationBuilder.AddColumn<Guid>(
                name: "ClubId",
                table: "PaiementCotisations",
                type: "uuid",
                nullable: true); // Nullable temporairement

            migrationBuilder.AddColumn<Guid>(
                name: "ClubId",
                table: "Evenements",
                type: "uuid",
                nullable: true); // Nullable temporairement

            migrationBuilder.AddColumn<Guid>(
                name: "ClubId",
                table: "Commissions",
                type: "uuid",
                nullable: true); // Nullable temporairement

            migrationBuilder.AddColumn<Guid>(
                name: "ClubId",
                table: "Comites",
                type: "uuid",
                nullable: true); // Nullable temporairement

            // ===== SECTION DE MIGRATION DES DONNÉES =====
            // Mettre à jour les données existantes avec le premier club disponible
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    default_club_id UUID;
                BEGIN
                    -- Récupérer le premier club disponible
                    SELECT ""Id"" INTO default_club_id FROM ""Clubs"" LIMIT 1;
                    
                    IF default_club_id IS NOT NULL THEN
                        -- Mettre à jour toutes les tables
                        UPDATE ""Comites"" SET ""ClubId"" = default_club_id WHERE ""ClubId"" IS NULL;
                        UPDATE ""Evenements"" SET ""ClubId"" = default_club_id WHERE ""ClubId"" IS NULL;
                        UPDATE ""Reunions"" SET ""ClubId"" = default_club_id WHERE ""ClubId"" IS NULL;
                        UPDATE ""Commissions"" SET ""ClubId"" = default_club_id WHERE ""ClubId"" IS NULL;
                        UPDATE ""PaiementCotisations"" SET ""ClubId"" = default_club_id WHERE ""ClubId"" IS NULL;
                    ELSE
                        -- Créer un club par défaut si aucun n'existe
                        INSERT INTO ""Clubs"" (""Id"", ""Name"", ""DateCreation"", ""NumeroClub"", ""NumeroTelephone"", ""Email"", ""LieuReunion"", ""ParrainePar"", ""JourReunion"", ""HeureReunion"", ""Frequence"", ""Adresse"")
                        VALUES (gen_random_uuid(), 'Club par défaut', '2024-01-01', 'RC000', '+0000000000', 'default@club.rotary', 'Lieu par défaut', 'Rotary International', 'Mercredi', INTERVAL '18:30:00', 'Hebdomadaire', 'Adresse par défaut');
                        
                        -- Récupérer l'ID du club créé
                        SELECT ""Id"" INTO default_club_id FROM ""Clubs"" WHERE ""NumeroClub"" = 'RC000';
                        
                        -- Mettre à jour toutes les tables
                        UPDATE ""Comites"" SET ""ClubId"" = default_club_id WHERE ""ClubId"" IS NULL;
                        UPDATE ""Evenements"" SET ""ClubId"" = default_club_id WHERE ""ClubId"" IS NULL;
                        UPDATE ""Reunions"" SET ""ClubId"" = default_club_id WHERE ""ClubId"" IS NULL;
                        UPDATE ""Commissions"" SET ""ClubId"" = default_club_id WHERE ""ClubId"" IS NULL;
                        UPDATE ""PaiementCotisations"" SET ""ClubId"" = default_club_id WHERE ""ClubId"" IS NULL;
                    END IF;
                END $$;
            ");

            // Rendre les colonnes non-nullable après avoir mis à jour les données
            migrationBuilder.AlterColumn<Guid>(
                name: "ClubId",
                table: "Reunions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClubId",
                table: "PaiementCotisations",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClubId",
                table: "Evenements",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClubId",
                table: "Commissions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClubId",
                table: "Comites",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // Créer les index
            migrationBuilder.CreateIndex(
                name: "IX_Reunion_ClubId_Date_Heure_TypeReunion_Unique",
                table: "Reunions",
                columns: new[] { "ClubId", "Date", "Heure", "TypeReunionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reunions_ClubId",
                table: "Reunions",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Reunions_ClubId_Date",
                table: "Reunions",
                columns: new[] { "ClubId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Reunions_ClubId_Date_Heure",
                table: "Reunions",
                columns: new[] { "ClubId", "Date", "Heure" });

            migrationBuilder.CreateIndex(
                name: "IX_Reunions_ClubId_TypeReunionId",
                table: "Reunions",
                columns: new[] { "ClubId", "TypeReunionId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaiementCotisations_ClubId",
                table: "PaiementCotisations",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Evenement_ClubId_Libelle_Date_Unique",
                table: "Evenements",
                columns: new[] { "ClubId", "Libelle", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evenements_ClubId",
                table: "Evenements",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Evenements_ClubId_Date",
                table: "Evenements",
                columns: new[] { "ClubId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Evenements_ClubId_Date_EstInterne",
                table: "Evenements",
                columns: new[] { "ClubId", "Date", "EstInterne" });

            migrationBuilder.CreateIndex(
                name: "IX_Evenements_ClubId_EstInterne",
                table: "Evenements",
                columns: new[] { "ClubId", "EstInterne" });

            migrationBuilder.CreateIndex(
                name: "IX_Commission_ClubId_Nom_Unique",
                table: "Commissions",
                columns: new[] { "ClubId", "Nom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_ClubId",
                table: "Commissions",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_Nom",
                table: "Commissions",
                column: "Nom");

            migrationBuilder.CreateIndex(
                name: "IX_Comite_ClubId_MandatId_Nom_Unique",
                table: "Comites",
                columns: new[] { "ClubId", "MandatId", "Nom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comites_ClubId",
                table: "Comites",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Comites_ClubId_MandatId",
                table: "Comites",
                columns: new[] { "ClubId", "MandatId" });

            migrationBuilder.CreateIndex(
                name: "IX_Comites_Nom",
                table: "Comites",
                column: "Nom");

            // Ajouter les contraintes de clé étrangère
            migrationBuilder.AddForeignKey(
                name: "FK_Comites_Clubs_ClubId",
                table: "Comites",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Commissions_Clubs_ClubId",
                table: "Commissions",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Evenements_Clubs_ClubId",
                table: "Evenements",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PaiementCotisations_Clubs_ClubId",
                table: "PaiementCotisations",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reunions_Clubs_ClubId",
                table: "Reunions",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Supprimer les contraintes de clé étrangère
            migrationBuilder.DropForeignKey(
                name: "FK_Comites_Clubs_ClubId",
                table: "Comites");

            migrationBuilder.DropForeignKey(
                name: "FK_Commissions_Clubs_ClubId",
                table: "Commissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Evenements_Clubs_ClubId",
                table: "Evenements");

            migrationBuilder.DropForeignKey(
                name: "FK_PaiementCotisations_Clubs_ClubId",
                table: "PaiementCotisations");

            migrationBuilder.DropForeignKey(
                name: "FK_Reunions_Clubs_ClubId",
                table: "Reunions");

            // Supprimer les index des autres tables
            migrationBuilder.DropIndex(
                name: "IX_Reunion_ClubId_Date_Heure_TypeReunion_Unique",
                table: "Reunions");

            migrationBuilder.DropIndex(
                name: "IX_Reunions_ClubId",
                table: "Reunions");

            migrationBuilder.DropIndex(
                name: "IX_Reunions_ClubId_Date",
                table: "Reunions");

            migrationBuilder.DropIndex(
                name: "IX_Reunions_ClubId_Date_Heure",
                table: "Reunions");

            migrationBuilder.DropIndex(
                name: "IX_Reunions_ClubId_TypeReunionId",
                table: "Reunions");

            migrationBuilder.DropIndex(
                name: "IX_PaiementCotisations_ClubId",
                table: "PaiementCotisations");

            migrationBuilder.DropIndex(
                name: "IX_Evenement_ClubId_Libelle_Date_Unique",
                table: "Evenements");

            migrationBuilder.DropIndex(
                name: "IX_Evenements_ClubId",
                table: "Evenements");

            migrationBuilder.DropIndex(
                name: "IX_Evenements_ClubId_Date",
                table: "Evenements");

            migrationBuilder.DropIndex(
                name: "IX_Evenements_ClubId_Date_EstInterne",
                table: "Evenements");

            migrationBuilder.DropIndex(
                name: "IX_Evenements_ClubId_EstInterne",
                table: "Evenements");

            migrationBuilder.DropIndex(
                name: "IX_Commission_ClubId_Nom_Unique",
                table: "Commissions");

            migrationBuilder.DropIndex(
                name: "IX_Commissions_ClubId",
                table: "Commissions");

            migrationBuilder.DropIndex(
                name: "IX_Commissions_Nom",
                table: "Commissions");

            migrationBuilder.DropIndex(
                name: "IX_Comite_ClubId_MandatId_Nom_Unique",
                table: "Comites");

            migrationBuilder.DropIndex(
                name: "IX_Comites_ClubId",
                table: "Comites");

            migrationBuilder.DropIndex(
                name: "IX_Comites_ClubId_MandatId",
                table: "Comites");

            migrationBuilder.DropIndex(
                name: "IX_Comites_Nom",
                table: "Comites");

            // Supprimer les colonnes ClubId des autres tables
            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "Reunions");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "PaiementCotisations");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "Evenements");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "Commissions");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "Comites");

            // Restaurer l'index des Commissions
            migrationBuilder.CreateIndex(
                name: "IX_Commissions_Nom",
                table: "Commissions",
                column: "Nom",
                unique: true);

            // Supprimer les nouveaux index de la table Clubs
            migrationBuilder.DropIndex(
                name: "IX_Clubs_Email",
                table: "Clubs");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_NumeroClub",
                table: "Clubs");

            // Ajouter les anciennes colonnes de la table Clubs
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

            // Supprimer les nouvelles colonnes de la table Clubs
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