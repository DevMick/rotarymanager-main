using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RotaryClubManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ManualCleanupCommissionStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Script SQL sécurisé pour nettoyer la structure
            migrationBuilder.Sql(@"
                -- 1. Supprimer les contraintes seulement si elles existent
                DO $$ 
                BEGIN
                    -- Supprimer FK vers CommissionsClub si elle existe
                    IF EXISTS (SELECT 1 FROM information_schema.table_constraints 
                               WHERE constraint_name = 'FK_MembresCommission_CommissionsClub_CommissionClubId' 
                               AND table_name = 'MembresCommission') THEN
                        ALTER TABLE ""MembresCommission"" DROP CONSTRAINT ""FK_MembresCommission_CommissionsClub_CommissionClubId"";
                    END IF;

                    -- Supprimer FK de CommissionsClub vers Clubs si elle existe
                    IF EXISTS (SELECT 1 FROM information_schema.table_constraints 
                               WHERE constraint_name = 'FK_CommissionsClub_Clubs_ClubId' 
                               AND table_name = 'CommissionsClub') THEN
                        ALTER TABLE ""CommissionsClub"" DROP CONSTRAINT ""FK_CommissionsClub_Clubs_ClubId"";
                    END IF;

                    -- Supprimer FK de CommissionsClub vers Commissions si elle existe
                    IF EXISTS (SELECT 1 FROM information_schema.table_constraints 
                               WHERE constraint_name = 'FK_CommissionsClub_Commissions_CommissionId' 
                               AND table_name = 'CommissionsClub') THEN
                        ALTER TABLE ""CommissionsClub"" DROP CONSTRAINT ""FK_CommissionsClub_Commissions_CommissionId"";
                    END IF;
                END $$;

                -- 2. Supprimer les index seulement s'ils existent
                DROP INDEX IF EXISTS ""IX_MembresCommission_CommissionClubId"";
                DROP INDEX IF EXISTS ""IX_CommissionsClub_ClubId"";
                DROP INDEX IF EXISTS ""IX_CommissionsClub_CommissionId"";

                -- 3. Supprimer les colonnes seulement si elles existent
                DO $$ 
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name = 'MembresCommission' AND column_name = 'CommissionClubId') THEN
                        ALTER TABLE ""MembresCommission"" DROP COLUMN ""CommissionClubId"";
                    END IF;
                END $$;

                -- 4. Ajouter CommissionId seulement si elle n'existe pas
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'MembresCommission' AND column_name = 'CommissionId') THEN
                        ALTER TABLE ""MembresCommission"" ADD COLUMN ""CommissionId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
                    END IF;
                END $$;

                -- 5. Supprimer la table CommissionsClub si elle existe
                DROP TABLE IF EXISTS ""CommissionsClub"";

                -- 6. Créer les nouveaux index et contraintes
                CREATE INDEX IF NOT EXISTS ""IX_MembresCommission_CommissionId"" ON ""MembresCommission"" (""CommissionId"");

                -- Index unique corrigé
                DROP INDEX IF EXISTS ""IX_MembresCommission_CommissionId_MembreId_MandatId"";
                CREATE UNIQUE INDEX ""IX_MembresCommission_CommissionId_MembreId_MandatId"" 
                ON ""MembresCommission"" (""CommissionId"", ""MembreId"", ""MandatId"");

                -- 7. Ajouter la nouvelle foreign key
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints 
                                   WHERE constraint_name = 'FK_MembresCommission_Commissions_CommissionId') THEN
                        ALTER TABLE ""MembresCommission"" 
                        ADD CONSTRAINT ""FK_MembresCommission_Commissions_CommissionId"" 
                        FOREIGN KEY (""CommissionId"") REFERENCES ""Commissions"" (""Id"") ON DELETE CASCADE;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback - Recréer l'ancienne structure
            migrationBuilder.Sql(@"
                -- Supprimer la nouvelle structure
                ALTER TABLE ""MembresCommission"" DROP CONSTRAINT IF EXISTS ""FK_MembresCommission_Commissions_CommissionId"";
                DROP INDEX IF EXISTS ""IX_MembresCommission_CommissionId"";
                DROP INDEX IF EXISTS ""IX_MembresCommission_CommissionId_MembreId_MandatId"";
                ALTER TABLE ""MembresCommission"" DROP COLUMN IF EXISTS ""CommissionId"";

                -- Recréer CommissionsClub
                CREATE TABLE IF NOT EXISTS ""CommissionsClub"" (
                    ""Id"" uuid NOT NULL,
                    ""CommissionId"" uuid NOT NULL,
                    ""ClubId"" uuid NOT NULL,
                    ""EstActive"" boolean NOT NULL,
                    ""DateCreation"" timestamp with time zone NOT NULL,
                    ""NotesSpecifiques"" character varying(500),
                    CONSTRAINT ""PK_CommissionsClub"" PRIMARY KEY (""Id"")
                );

                -- Recréer CommissionClubId
                ALTER TABLE ""MembresCommission"" ADD COLUMN ""CommissionClubId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
            ");
        }
    }
}