using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Domain.Entities.Formation;
using RotaryClubManager.Domain.Identity;
using static RotaryClubManager.Domain.Entities.InviteReunion;

namespace RotaryClubManager.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        // ===== AJOUT DES DBSETS POUR LES GALAS =====
        public DbSet<OrdreJourRapport> OrdreJourRapports { get; set; }
        public DbSet<Gala> Galas { get; set; }
        public DbSet<GalaInvites> GalaInvites { get; set; }
        public DbSet<GalaTable> GalaTables { get; set; }
        public DbSet<GalaTableAffectation> GalaTableAffectations { get; set; }
        public DbSet<GalaTicket> GalaTickets { get; set; }
        public DbSet<GalaTombola> GalaTombolas { get; set; }

        // ===== AJOUT DES DBSETS DANS APPLICATIONDBCONTEXT =====
        public DbSet<Categorie> Categories { get; set; }
        public DbSet<TypeDocument> TypesDocument { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<TypeBudget> TypesBudget { get; set; }
        public DbSet<CategoryBudget> CategoriesBudget { get; set; }
        public DbSet<SousCategoryBudget> SousCategoriesBudget { get; set; }
        public DbSet<RubriqueBudget> RubriquesBudget { get; set; }
        public DbSet<RubriqueBudgetRealise> RubriquesBudgetRealisees { get; set; }
        public DbSet<FonctionEcheances> FonctionEcheances { get; set; }
        public DbSet<FonctionRolesResponsabilites> FonctionRolesResponsabilites { get; set; }
        public DbSet<Evenement> Evenements { get; set; }
        public DbSet<EvenementDocument> EvenementDocuments { get; set; }
        public DbSet<EvenementImage> EvenementImages { get; set; }
        public DbSet<EvenementBudget> EvenementBudgets { get; set; }
        public DbSet<EvenementRecette> EvenementRecettes { get; set; }
        public DbSet<Cotisation> Cotisations { get; set; }
        public DbSet<PaiementCotisation> PaiementCotisations { get; set; }
        public DbSet<Fonction> Fonctions { get; set; }
        public DbSet<ComiteMembre> ComiteMembres { get; set; }
        public DbSet<Comite> Comites { get; set; }
        public DbSet<UserClub> UserClubs { get; set; }
        public DbSet<Club> Clubs { get; set; }
        public DbSet<Commission> Commissions { get; set; }
        // SUPPRIMÉ : public DbSet<CommissionClub> CommissionsClub { get; set; }
        public DbSet<MembreCommission> MembresCommission { get; set; }
        public DbSet<Mandat> Mandats { get; set; }

        // ===== NOUVELLES ENTITÉS POUR LES RÉUNIONS =====
        public DbSet<TypeReunion> TypesReunion { get; set; }
        public DbSet<Reunion> Reunions { get; set; }
        public DbSet<OrdreDuJour> OrdresDuJour { get; set; }
        public DbSet<ListePresence> ListesPresence { get; set; }
        public DbSet<InviteReunion> InvitesReunion { get; set; }
        public DbSet<ReunionDocument> ReunionDocuments { get; set; }

        // ===== ENTITÉS POUR LE MODULE DE FORMATION =====
        public DbSet<DocumentFormation> DocumentsFormation { get; set; }
        public DbSet<ChunkDocument> ChunksDocument { get; set; }
        public DbSet<SessionFormation> SessionsFormation { get; set; }
        public DbSet<QuestionFormation> QuestionsFormation { get; set; }
        public DbSet<ReponseUtilisateur> ReponsesUtilisateur { get; set; }
        public DbSet<BadgeFormation> BadgesFormation { get; set; }
        // Ajoutez cette méthode TEMPORAIREMENT dans ApplicationDbContext

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            // Forcer EF à ignorer le cache du modèle
            optionsBuilder.EnableServiceProviderCaching(false);
            optionsBuilder.EnableSensitiveDataLogging(true);

            // Forcer la reconstruction du modèle à chaque démarrage
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // ===== CONFIGURATIONS ENTITY FRAMEWORK MISES À JOUR =====
            // Configuration de RotaryClub

            // Configuration d'OrdreJourRapport
            modelBuilder.Entity<OrdreJourRapport>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuration des propriétés
                entity.Property(e => e.OrdreDuJourId)
                      .IsRequired();

                entity.Property(e => e.Texte)
                      .IsRequired();

                entity.Property(e => e.Divers)
                      .IsRequired(false); // Propriété optionnelle

                // Relation avec OrdreDuJour
                entity.HasOne(e => e.OrdreDuJour)
                      .WithMany() // Ajoutez une navigation property dans OrdreDuJour si nécessaire
                      .HasForeignKey(e => e.OrdreDuJourId)
                      .OnDelete(DeleteBehavior.Cascade); // Supprime les rapports si l'ordre du jour est supprimé

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.OrdreDuJourId);

                // Index composite pour les recherches par ordre du jour
                entity.HasIndex(e => new { e.OrdreDuJourId, e.Texte });

                // N'oubliez pas d'ajouter le DbSet dans votre ApplicationDbContext :

            });

            // Configuration de GalaTableAffectation avec support multi-SGBD
            modelBuilder.Entity<GalaTableAffectation>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuration des propriétés
                entity.Property(e => e.GalaTableId)
                      .IsRequired();

                entity.Property(e => e.GalaInvitesId)
                      .IsRequired();

                // Configuration conditionnelle selon le provider de base de données
                var databaseProvider = Database.ProviderName;

                if (databaseProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
                {
                    // PostgreSQL
                    entity.Property(e => e.DateAjout)
                          .IsRequired()
                          .HasDefaultValueSql("NOW()");
                }
                else if (databaseProvider == "Microsoft.EntityFrameworkCore.SqlServer")
                {
                    // SQL Server
                    entity.Property(e => e.DateAjout)
                          .IsRequired()
                          .HasDefaultValueSql("GETUTCDATE()");
                }
                else if (databaseProvider == "Microsoft.EntityFrameworkCore.Sqlite")
                {
                    // SQLite
                    entity.Property(e => e.DateAjout)
                          .IsRequired()
                          .HasDefaultValueSql("datetime('now')");
                }
                else
                {
                    // Fallback sans valeur par défaut SQL
                    entity.Property(e => e.DateAjout)
                          .IsRequired();
                }

                // Relation avec GalaTable
                entity.HasOne(e => e.GalaTable)
                      .WithMany(e => e.TableAffectations)
                      .HasForeignKey(e => e.GalaTableId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Relation avec GalaInvites
                entity.HasOne(e => e.GalaInvites)
                      .WithMany(e => e.TableAffectations)
                      .HasForeignKey(e => e.GalaInvitesId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.GalaTableId)
                      .HasDatabaseName("IX_GalaTableAffectation_GalaTableId");

                entity.HasIndex(e => e.GalaInvitesId)
                      .HasDatabaseName("IX_GalaTableAffectation_GalaInvitesId");

                entity.HasIndex(e => e.DateAjout)
                      .HasDatabaseName("IX_GalaTableAffectation_DateAjout");

                // Index composite pour les recherches
                entity.HasIndex(e => new { e.GalaTableId, e.GalaInvitesId })
                      .HasDatabaseName("IX_GalaTableAffectation_GalaTableId_GalaInvitesId");

                // Index unique pour éviter qu'un invité soit affecté plusieurs fois à la même table
                entity.HasIndex(e => new { e.GalaTableId, e.GalaInvitesId })
                      .IsUnique()
                      .HasDatabaseName("IX_GalaTableAffectation_GalaTableId_GalaInvitesId_Unique");

                // Index composite pour l'historique
                entity.HasIndex(e => new { e.DateAjout, e.GalaTableId })
                      .HasDatabaseName("IX_GalaTableAffectation_DateAjout_GalaTableId");
            });

            // Configuration de GalaTicket corrigée pour PostgreSQL
            modelBuilder.Entity<GalaTicket>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuration des propriétés
                entity.Property(e => e.Quantite)
                      .IsRequired();

                entity.Property(e => e.GalaId)
                      .IsRequired();

                entity.Property(e => e.MembreId)
                      .IsRequired(false); // Maintenant optionnel

                entity.Property(e => e.Externe)
                      .HasMaxLength(250)
                      .IsRequired(false); // Optionnel

                // Relation avec Gala
                entity.HasOne(e => e.Gala)
                      .WithMany(e => e.Tickets)
                      .HasForeignKey(e => e.GalaId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Relation avec ApplicationUser (Membre) - maintenant optionnelle
                entity.HasOne(e => e.Membre)
                      .WithMany()
                      .HasForeignKey(e => e.MembreId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired(false);

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.GalaId);
                entity.HasIndex(e => e.MembreId);
                entity.HasIndex(e => e.Quantite);
                entity.HasIndex(e => e.Externe);

                // Index composite pour les recherches par gala et membre
                entity.HasIndex(e => new { e.GalaId, e.MembreId });

                // Contrainte : soit MembreId soit Externe doit être renseigné
                // Utilisation de guillemets pour PostgreSQL
                entity.HasCheckConstraint("CK_GalaTicket_MembreOrExterne",
                    "(\"MembreId\" IS NOT NULL) OR (\"Externe\" IS NOT NULL AND \"Externe\" != '')");

                // Index unique modifié pour tenir compte des participants externes
                // Seuls les membres ne peuvent avoir qu'une entrée par gala
                // Utilisation de guillemets pour PostgreSQL
                entity.HasIndex(e => new { e.GalaId, e.MembreId })
                      .IsUnique()
                      .HasDatabaseName("IX_GalaTicket_GalaId_MembreId_Unique")
                      .HasFilter("\"MembreId\" IS NOT NULL");
            });

            // Configuration de GalaTombola corrigée pour PostgreSQL
            modelBuilder.Entity<GalaTombola>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuration des propriétés
                entity.Property(e => e.Quantite)
                      .IsRequired();

                entity.Property(e => e.GalaId)
                      .IsRequired();

                entity.Property(e => e.MembreId)
                      .IsRequired(false); // Maintenant optionnel

                entity.Property(e => e.Externe)
                      .HasMaxLength(250)
                      .IsRequired(false); // Optionnel

                // Relation avec Gala
                entity.HasOne(e => e.Gala)
                      .WithMany(e => e.Tombolas)
                      .HasForeignKey(e => e.GalaId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Relation avec ApplicationUser (Membre) - maintenant optionnelle
                entity.HasOne(e => e.Membre)
                      .WithMany()
                      .HasForeignKey(e => e.MembreId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired(false);

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.GalaId);
                entity.HasIndex(e => e.MembreId);
                entity.HasIndex(e => e.Quantite);
                entity.HasIndex(e => e.Externe);

                // Index composite pour les recherches par gala et membre
                entity.HasIndex(e => new { e.GalaId, e.MembreId });

                // Contrainte : soit MembreId soit Externe doit être renseigné
                // Utilisation de guillemets pour PostgreSQL
                entity.HasCheckConstraint("CK_GalaTombola_MembreOrExterne",
                    "(\"MembreId\" IS NOT NULL) OR (\"Externe\" IS NOT NULL AND \"Externe\" != '')");

                // Index unique modifié pour tenir compte des participants externes
                // Seuls les membres ne peuvent avoir qu'une entrée par gala
                // Utilisation de guillemets pour PostgreSQL
                entity.HasIndex(e => new { e.GalaId, e.MembreId })
                      .IsUnique()
                      .HasDatabaseName("IX_GalaTombola_GalaId_MembreId_Unique")
                      .HasFilter("\"MembreId\" IS NOT NULL");
            });

            // Configuration de Categorie
            modelBuilder.Entity<Categorie>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Libelle)
                      .IsRequired()
                      .HasMaxLength(100);

                // Relations
                entity.HasMany(e => e.Documents)
                      .WithOne(e => e.Categorie)
                      .HasForeignKey(e => e.CategorieId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Index
                entity.HasIndex(e => e.Libelle)
                      .IsUnique()
                      .HasDatabaseName("IX_Categorie_Libelle_Unique");
            });

            // Configuration de TypeDocument
            modelBuilder.Entity<TypeDocument>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Libelle)
                      .IsRequired()
                      .HasMaxLength(100);

                // Relations
                entity.HasMany(e => e.Documents)
                      .WithOne(e => e.TypeDocument)
                      .HasForeignKey(e => e.TypeDocumentId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Index
                entity.HasIndex(e => e.Libelle)
                      .IsUnique()
                      .HasDatabaseName("IX_TypeDocument_Libelle_Unique");
            });

            // Configuration de Document
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Nom)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.Description)
                      .HasMaxLength(1000);

                entity.Property(e => e.Fichier)
                      .IsRequired()
                      .HasColumnType("bytea"); // Type PostgreSQL pour les données binaires

                entity.Property(e => e.CategorieId)
                      .IsRequired();

                entity.Property(e => e.TypeDocumentId)
                      .IsRequired();

                entity.Property(e => e.ClubId)
                      .IsRequired();

                // Relations
                entity.HasOne(e => e.Categorie)
                      .WithMany(e => e.Documents)
                      .HasForeignKey(e => e.CategorieId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.TypeDocument)
                      .WithMany(e => e.Documents)
                      .HasForeignKey(e => e.TypeDocumentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Club)
                      .WithMany() // Pas de navigation inverse définie dans Club
                      .HasForeignKey(e => e.ClubId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.ClubId);
                entity.HasIndex(e => e.CategorieId);
                entity.HasIndex(e => e.TypeDocumentId);
                entity.HasIndex(e => e.Nom);

                // Index composites
                entity.HasIndex(e => new { e.ClubId, e.CategorieId });
                entity.HasIndex(e => new { e.ClubId, e.TypeDocumentId });
                entity.HasIndex(e => new { e.ClubId, e.CategorieId, e.TypeDocumentId });

                // Index unique pour éviter les doublons de nom dans une même catégorie/type/club
                entity.HasIndex(e => new { e.ClubId, e.CategorieId, e.TypeDocumentId, e.Nom })
                      .IsUnique()
                      .HasDatabaseName("IX_Document_ClubId_CategorieId_TypeDocumentId_Nom_Unique");
            });

            // Configuration de TypeBudget
            modelBuilder.Entity<TypeBudget>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Libelle)
                      .IsRequired()
                      .HasMaxLength(50);

                // Relations
                entity.HasMany(e => e.CategoriesBudget)
                      .WithOne(e => e.TypeBudget)
                      .HasForeignKey(e => e.TypeBudgetId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Index
                entity.HasIndex(e => e.Libelle)
                      .IsUnique()
                      .HasDatabaseName("IX_TypeBudget_Libelle_Unique");
            });

            // Configuration de CategoryBudget
            modelBuilder.Entity<CategoryBudget>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Libelle)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.TypeBudgetId)
                      .IsRequired();

                // Relations
                entity.HasOne(e => e.TypeBudget)
                      .WithMany(e => e.CategoriesBudget)
                      .HasForeignKey(e => e.TypeBudgetId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.SousCategories)
                      .WithOne(e => e.CategoryBudget)
                      .HasForeignKey(e => e.CategoryBudgetId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index
                entity.HasIndex(e => e.TypeBudgetId);
                entity.HasIndex(e => e.Libelle);

                // Index unique pour éviter les doublons dans un même type de budget
                entity.HasIndex(e => new { e.TypeBudgetId, e.Libelle })
                      .IsUnique()
                      .HasDatabaseName("IX_CategoryBudget_TypeBudgetId_Libelle_Unique");
            });

            // Configuration de SousCategoryBudget
            modelBuilder.Entity<SousCategoryBudget>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Libelle)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.Property(e => e.CategoryBudgetId)
                      .IsRequired();

                entity.Property(e => e.ClubId)
                      .IsRequired();

                // Relations
                entity.HasOne(e => e.CategoryBudget)
                      .WithMany(e => e.SousCategories)
                      .HasForeignKey(e => e.CategoryBudgetId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Club)
                      .WithMany() // Pas de navigation inverse définie dans Club
                      .HasForeignKey(e => e.ClubId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Rubriques)
                      .WithOne(e => e.SousCategoryBudget)
                      .HasForeignKey(e => e.SousCategoryBudgetId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index
                entity.HasIndex(e => e.CategoryBudgetId);
                entity.HasIndex(e => e.ClubId);
                entity.HasIndex(e => e.Libelle);

                // Index composite
                entity.HasIndex(e => new { e.ClubId, e.CategoryBudgetId });

                // Index unique pour éviter les doublons dans un même club/catégorie
                entity.HasIndex(e => new { e.ClubId, e.CategoryBudgetId, e.Libelle })
                      .IsUnique()
                      .HasDatabaseName("IX_SousCategoryBudget_ClubId_CategoryBudgetId_Libelle_Unique");
            });

            // Configuration de RubriqueBudget dans OnModelCreating
            modelBuilder.Entity<RubriqueBudget>(entity =>
            {
                // Clé primaire
                entity.HasKey(e => e.Id);

                // Configuration des propriétés
                entity.Property(e => e.Libelle)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.PrixUnitaire)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Quantite)
                      .IsRequired()
                      .HasDefaultValue(1);

                entity.Property(e => e.MontantRealise)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)")
                      .HasDefaultValue(0m);

                entity.Property(e => e.SousCategoryBudgetId)
                      .IsRequired();

                entity.Property(e => e.MandatId)
                      .IsRequired();

                entity.Property(e => e.ClubId)
                      .IsRequired();

                // Relations
                entity.HasOne(e => e.SousCategoryBudget)
                      .WithMany(e => e.Rubriques)
                      .HasForeignKey(e => e.SousCategoryBudgetId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Mandat)
                      .WithMany() // Pas de navigation inverse définie dans Mandat
                      .HasForeignKey(e => e.MandatId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Club)
                      .WithMany() // Pas de navigation inverse définie dans Club
                      .HasForeignKey(e => e.ClubId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Realisations)
                      .WithOne(e => e.RubriqueBudget)
                      .HasForeignKey(e => e.RubriqueBudgetId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.SousCategoryBudgetId)
                      .HasDatabaseName("IX_RubriqueBudget_SousCategoryBudgetId");

                entity.HasIndex(e => e.MandatId)
                      .HasDatabaseName("IX_RubriqueBudget_MandatId");

                entity.HasIndex(e => e.ClubId)
                      .HasDatabaseName("IX_RubriqueBudget_ClubId");

                entity.HasIndex(e => e.Libelle)
                      .HasDatabaseName("IX_RubriqueBudget_Libelle");

                entity.HasIndex(e => e.PrixUnitaire)
                      .HasDatabaseName("IX_RubriqueBudget_PrixUnitaire");

                entity.HasIndex(e => e.MontantRealise)
                      .HasDatabaseName("IX_RubriqueBudget_MontantRealise");

                // Index composites pour optimiser les requêtes fréquentes
                entity.HasIndex(e => new { e.ClubId, e.MandatId })
                      .HasDatabaseName("IX_RubriqueBudget_ClubId_MandatId");

                entity.HasIndex(e => new { e.ClubId, e.SousCategoryBudgetId })
                      .HasDatabaseName("IX_RubriqueBudget_ClubId_SousCategoryBudgetId");

                entity.HasIndex(e => new { e.ClubId, e.MandatId, e.SousCategoryBudgetId })
                      .HasDatabaseName("IX_RubriqueBudget_ClubId_MandatId_SousCategoryBudgetId");

                // Index unique pour éviter les doublons dans un même club/mandat/sous-catégorie
                entity.HasIndex(e => new { e.ClubId, e.MandatId, e.SousCategoryBudgetId, e.Libelle })
                      .IsUnique()
                      .HasDatabaseName("IX_RubriqueBudget_ClubId_MandatId_SousCategoryBudgetId_Libelle_Unique");

                // Index pour les statistiques et rapports
                entity.HasIndex(e => new { e.ClubId, e.MandatId, e.MontantRealise })
                      .HasDatabaseName("IX_RubriqueBudget_ClubId_MandatId_MontantRealise");

                entity.HasIndex(e => new { e.ClubId, e.MandatId, e.PrixUnitaire, e.Quantite })
                      .HasDatabaseName("IX_RubriqueBudget_ClubId_MandatId_Budget");

                // Configuration du nom de table (optionnel si vous utilisez des conventions différentes)
                entity.ToTable("RubriquesBudget");
            });

            // Configuration de RubriqueBudgetRealise
            modelBuilder.Entity<RubriqueBudgetRealise>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Date)
                      .IsRequired()
                      .HasColumnType("timestamp with time zone");

                entity.Property(e => e.Montant)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Commentaires)
                      .HasMaxLength(500);

                entity.Property(e => e.RubriqueBudgetId)
                      .IsRequired();

                // Relations
                entity.HasOne(e => e.RubriqueBudget)
                      .WithMany(e => e.Realisations)
                      .HasForeignKey(e => e.RubriqueBudgetId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.RubriqueBudgetId);
                entity.HasIndex(e => e.Date);
                entity.HasIndex(e => e.Montant);

                // Index composite pour les requêtes temporelles par rubrique
                entity.HasIndex(e => new { e.RubriqueBudgetId, e.Date });
                entity.HasIndex(e => new { e.RubriqueBudgetId, e.Date, e.Montant });
            });

            // Configuration de FonctionEcheances
            modelBuilder.Entity<FonctionEcheances>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuration des propriétés
                entity.Property(e => e.Libelle)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.DateButoir)
                      .IsRequired()
                      .HasColumnType("timestamp with time zone");

                entity.Property(e => e.Frequence)
                      .IsRequired()
                      .HasConversion<int>(); // Conversion enum vers int

                entity.Property(e => e.FonctionId)
                      .IsRequired();

                // Relation avec Fonction
                entity.HasOne(e => e.Fonction)
                      .WithMany() // Pas de navigation inverse définie dans Fonction pour les échéances
                      .HasForeignKey(e => e.FonctionId)
                      .OnDelete(DeleteBehavior.Cascade); // Supprime les échéances si la fonction est supprimée

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.FonctionId);
                entity.HasIndex(e => e.DateButoir);
                entity.HasIndex(e => e.Frequence);
                entity.HasIndex(e => e.Libelle);

                // Index composites pour optimiser les requêtes courantes
                entity.HasIndex(e => new { e.FonctionId, e.DateButoir });
                entity.HasIndex(e => new { e.FonctionId, e.Frequence });
                entity.HasIndex(e => new { e.DateButoir, e.Frequence });

                // Index composite pour éviter les doublons de libellé dans une même fonction
                entity.HasIndex(e => new { e.FonctionId, e.Libelle })
                      .IsUnique()
                      .HasDatabaseName("IX_FonctionEcheances_FonctionId_Libelle_Unique");
            });

            // Configuration de FonctionRolesResponsabilites
            modelBuilder.Entity<FonctionRolesResponsabilites>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuration des propriétés
                entity.Property(e => e.Libelle)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.Description)
                      .HasMaxLength(1000);

                entity.Property(e => e.FonctionId)
                      .IsRequired();

                // Relation avec Fonction
                entity.HasOne(e => e.Fonction)
                      .WithMany() // Pas de navigation inverse définie dans Fonction pour les rôles/responsabilités
                      .HasForeignKey(e => e.FonctionId)
                      .OnDelete(DeleteBehavior.Cascade); // Supprime les rôles/responsabilités si la fonction est supprimée

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.FonctionId);
                entity.HasIndex(e => e.Libelle);

                // Index composite pour éviter les doublons de libellé dans une même fonction
                entity.HasIndex(e => new { e.FonctionId, e.Libelle })
                      .IsUnique()
                      .HasDatabaseName("IX_FonctionRolesResponsabilites_FonctionId_Libelle_Unique");

                // Index composite pour les requêtes par fonction et libellé
                entity.HasIndex(e => new { e.FonctionId, e.Libelle });
            });

            // Configuration d'Evenement
            modelBuilder.Entity<Evenement>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuration des propriétés
                entity.Property(e => e.Libelle)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.Date)
                      .IsRequired()
                      .HasColumnType("timestamp with time zone");

                entity.Property(e => e.Lieu)
                      .HasMaxLength(300);

                entity.Property(e => e.Description)
                      .HasMaxLength(1000);

                entity.Property(e => e.EstInterne)
                      .IsRequired()
                      .HasDefaultValue(true);

                entity.Property(e => e.ClubId)
                      .IsRequired();

                // Relation avec Club
                entity.HasOne(e => e.Club)
                      .WithMany() // Pas de navigation inverse définie dans Club pour les Evenements
                      .HasForeignKey(e => e.ClubId)
                      .OnDelete(DeleteBehavior.Cascade); // Supprime les événements si le club est supprimé

                // Relations one-to-many existantes
                entity.HasMany(e => e.Documents)
                      .WithOne(e => e.Evenement)
                      .HasForeignKey(e => e.EvenementId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Images)
                      .WithOne(e => e.Evenement)
                      .HasForeignKey(e => e.EvenementId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Budgets)
                      .WithOne(e => e.Evenement)
                      .HasForeignKey(e => e.EvenementId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Recettes)
                      .WithOne(e => e.Evenement)
                      .HasForeignKey(e => e.EvenementId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.ClubId);
                entity.HasIndex(e => e.Date);
                entity.HasIndex(e => e.EstInterne);
                entity.HasIndex(e => e.Libelle);

                // Index composites pour optimiser les requêtes courantes
                entity.HasIndex(e => new { e.ClubId, e.Date });
                entity.HasIndex(e => new { e.ClubId, e.EstInterne });
                entity.HasIndex(e => new { e.Date, e.EstInterne });
                entity.HasIndex(e => new { e.ClubId, e.Date, e.EstInterne }); // Index composite principal

                // Index composite pour éviter les doublons de libellé dans un même club à la même date
                entity.HasIndex(e => new { e.ClubId, e.Libelle, e.Date })
                      .IsUnique()
                      .HasDatabaseName("IX_Evenement_ClubId_Libelle_Date_Unique");
            });

            // Configuration d'EvenementDocument
            modelBuilder.Entity<EvenementDocument>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuration des propriétés
                entity.Property(e => e.Libelle)
                      .HasMaxLength(200);

                entity.Property(e => e.Document)
                      .IsRequired()
                      .HasColumnType("bytea"); // Type PostgreSQL pour les données binaires

                entity.Property(e => e.DateAjout)
                      .IsRequired()
                      .HasColumnType("timestamp with time zone")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.EvenementId)
                      .IsRequired();

                // Relation avec Evenement
                entity.HasOne(e => e.Evenement)
                      .WithMany(e => e.Documents)
                      .HasForeignKey(e => e.EvenementId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.EvenementId);
                entity.HasIndex(e => e.DateAjout);
                entity.HasIndex(e => e.Libelle);

                // Index composite pour éviter les doublons de libellé dans un même événement
                entity.HasIndex(e => new { e.EvenementId, e.Libelle })
                      .IsUnique()
                      .HasDatabaseName("IX_EvenementDocument_EvenementId_Libelle_Unique");
            });

            // Configuration d'EvenementImage
            modelBuilder.Entity<EvenementImage>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuration des propriétés
                entity.Property(e => e.Image)
                      .IsRequired()
                      .HasColumnType("bytea"); // Type PostgreSQL pour les données binaires

                entity.Property(e => e.Description)
                      .HasMaxLength(500);

                entity.Property(e => e.DateAjout)
                      .IsRequired()
                      .HasColumnType("timestamp with time zone")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.EvenementId)
                      .IsRequired();

                // Relation avec Evenement
                entity.HasOne(e => e.Evenement)
                      .WithMany(e => e.Images)
                      .HasForeignKey(e => e.EvenementId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.EvenementId);
                entity.HasIndex(e => e.DateAjout);
                entity.HasIndex(e => e.Description);
            });

            // Configuration d'EvenementBudget
            modelBuilder.Entity<EvenementBudget>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuration des propriétés
                entity.Property(e => e.Libelle)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.MontantBudget)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)"); // Précision pour les montants

                entity.Property(e => e.MontantRealise)
                      .HasColumnType("decimal(18,2)")
                      .HasDefaultValue(0);

                entity.Property(e => e.EvenementId)
                      .IsRequired();

                // Relation avec Evenement
                entity.HasOne(e => e.Evenement)
                      .WithMany(e => e.Budgets)
                      .HasForeignKey(e => e.EvenementId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.EvenementId);
                entity.HasIndex(e => e.Libelle);
                entity.HasIndex(e => e.MontantBudget);

                // Index unique pour éviter les doublons de libellé dans un même événement
                entity.HasIndex(e => new { e.EvenementId, e.Libelle })
                      .IsUnique()
                      .HasDatabaseName("IX_EvenementBudget_EvenementId_Libelle_Unique");
            });

            // Configuration d'EvenementRecette
            modelBuilder.Entity<EvenementRecette>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuration des propriétés
                entity.Property(e => e.Libelle)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.Montant)
                      .IsRequired()
                      .HasColumnType("decimal(18,2)"); // Précision pour les montants

                entity.Property(e => e.EvenementId)
                      .IsRequired();

                // Relation avec Evenement
                entity.HasOne(e => e.Evenement)
                      .WithMany(e => e.Recettes)
                      .HasForeignKey(e => e.EvenementId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.EvenementId);
                entity.HasIndex(e => e.Libelle);
                entity.HasIndex(e => e.Montant);
            });

            // Configuration de ReunionDocument
            modelBuilder.Entity<ReunionDocument>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuration des propriétés
                entity.Property(e => e.Libelle)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.ReunionId)
                      .IsRequired();

                entity.Property(e => e.Document)
                      .IsRequired()
                      .HasColumnType("bytea"); // Type PostgreSQL pour les données binaires

                // Relation avec Reunion
                entity.HasOne(e => e.Reunion)
                      .WithMany(e => e.Documents)
                      .HasForeignKey(e => e.ReunionId)
                      .OnDelete(DeleteBehavior.Cascade); // Supprime les documents si la réunion est supprimée

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.ReunionId);
                entity.HasIndex(e => e.Libelle);

                // Index composite pour les recherches par réunion et libellé
                entity.HasIndex(e => new { e.ReunionId, e.Libelle });

                // Contrainte d'unicité pour éviter les doublons de libellé dans une même réunion
                entity.HasIndex(e => new { e.ReunionId, e.Libelle })
                      .IsUnique()
                      .HasDatabaseName("IX_ReunionDocument_ReunionId_Libelle_Unique");
            });

            // Configuration de Cotisation
            modelBuilder.Entity<Cotisation>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuration des propriétés
                entity.Property(e => e.Montant).IsRequired();
                entity.Property(e => e.MembreId).IsRequired();
                entity.Property(e => e.MandatId).IsRequired();

                // Relation avec ApplicationUser (Membre)
                entity.HasOne(e => e.Membre)
                      .WithMany() // Pas de navigation inverse définie
                      .HasForeignKey(e => e.MembreId)
                      .OnDelete(DeleteBehavior.Restrict); // Empêche la suppression du membre s'il a des cotisations

                // Relation avec Mandat
                entity.HasOne(e => e.Mandat)
                      .WithMany() // Vous pouvez ajouter une navigation inverse dans Mandat si nécessaire
                      .HasForeignKey(e => e.MandatId)
                      .OnDelete(DeleteBehavior.Cascade); // Supprime les cotisations si le mandat est supprimé

                // Index unique pour éviter qu'un membre ait plusieurs cotisations pour le même mandat
                entity.HasIndex(e => new { e.MembreId, e.MandatId }).IsUnique();

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.MembreId);
                entity.HasIndex(e => e.MandatId);
                entity.HasIndex(e => e.Montant); // Pour les requêtes de statistiques
            });

            // Configuration de PaiementCotisation
            modelBuilder.Entity<PaiementCotisation>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuration des propriétés
                entity.Property(e => e.Montant).IsRequired();
                entity.Property(e => e.Date).IsRequired().HasColumnType("timestamp"); // Type PostgreSQL pour les dates
                entity.Property(e => e.MembreId).IsRequired();
                entity.Property(e => e.Commentaires).HasMaxLength(500); // Limite la taille des commentaires

                // Relation avec ApplicationUser (Membre)
                entity.HasOne(e => e.Membre)
                      .WithMany() // Pas de navigation inverse définie
                      .HasForeignKey(e => e.MembreId)
                      .OnDelete(DeleteBehavior.Restrict); // Empêche la suppression du membre s'il a des paiements

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.MembreId);
                entity.HasIndex(e => e.Date); // Pour les requêtes par période
                entity.HasIndex(e => e.Montant); // Pour les requêtes de statistiques
                entity.HasIndex(e => new { e.MembreId, e.Date }); // Index composite pour les requêtes courantes
            });

            // Configuration de Fonction
            modelBuilder.Entity<Fonction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NomFonction).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.NomFonction).IsUnique(); // Assurer l'unicité du nom
            });

            // Configuration de ComiteMembre mise à jour
            modelBuilder.Entity<ComiteMembre>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Relation avec le mandat (remplace la relation directe avec Comite)
                entity.HasOne(e => e.Mandat)
                    .WithMany()
                    .HasForeignKey(e => e.MandatId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relation avec le membre
                entity.HasOne(e => e.Membre)
                    .WithMany()
                    .HasForeignKey(e => e.MembreId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relation avec la fonction
                entity.HasOne(e => e.Fonction)
                    .WithMany()
                    .HasForeignKey(e => e.FonctionId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Index unique mis à jour (remplace ComiteId par MandatId)
                entity.HasIndex(e => new { e.MandatId, e.MembreId, e.FonctionId })
                    .IsUnique();
            });

            // Configuration de Comite
            modelBuilder.Entity<Comite>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuration des propriétés
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(100);
                entity.Property(e => e.MandatId).IsRequired();
                entity.Property(e => e.ClubId).IsRequired();

                // Relation avec Mandat
                entity.HasOne(e => e.Mandat)
                      .WithMany()
                      .HasForeignKey(e => e.MandatId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Relation avec Club
                entity.HasOne(e => e.Club)
                      .WithMany() // Pas de navigation inverse définie dans Club pour les Comités
                      .HasForeignKey(e => e.ClubId)
                      .OnDelete(DeleteBehavior.Cascade); // Supprime les comités si le club est supprimé

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.ClubId);
                entity.HasIndex(e => e.MandatId);
                entity.HasIndex(e => e.Nom);

                // Index composite pour éviter les doublons de nom dans un même club/mandat
                entity.HasIndex(e => new { e.ClubId, e.MandatId, e.Nom })
                      .IsUnique()
                      .HasDatabaseName("IX_Comite_ClubId_MandatId_Nom_Unique");

                // Index composite pour les requêtes par club et mandat
                entity.HasIndex(e => new { e.ClubId, e.MandatId });
            });

            // Configuration de Commission (NETTOYÉE - SANS CommissionClub)
            modelBuilder.Entity<Commission>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuration des propriétés
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.ClubId).IsRequired();

                // Relation avec Club
                entity.HasOne(e => e.Club)
                      .WithMany() // Pas de navigation inverse définie dans Club pour les Commissions
                      .HasForeignKey(e => e.ClubId)
                      .OnDelete(DeleteBehavior.Cascade); // Supprime les commissions si le club est supprimé

                // SUPPRIMÉ : Relation avec CommissionClub
                /*
                entity.HasMany(e => e.CommissionsClub)
                      .WithOne(e => e.Commission)
                      .HasForeignKey(e => e.CommissionId)
                      .OnDelete(DeleteBehavior.Cascade);
                */

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.ClubId);
                entity.HasIndex(e => e.Nom);

                // Index composite pour éviter les doublons de nom dans un même club
                entity.HasIndex(e => new { e.ClubId, e.Nom })
                      .IsUnique()
                      .HasDatabaseName("IX_Commission_ClubId_Nom_Unique");
            });

            // Configuration de MembreCommission (NETTOYÉE)
            modelBuilder.Entity<MembreCommission>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuration des propriétés
                entity.Property(e => e.EstResponsable).IsRequired();
                entity.Property(e => e.DateNomination).IsRequired();
                entity.Property(e => e.EstActif).IsRequired();
                entity.Property(e => e.Commentaires).HasMaxLength(500);
                entity.Property(e => e.MembreId).IsRequired();
                entity.Property(e => e.CommissionId).IsRequired();
                entity.Property(e => e.MandatId).IsRequired();

                // Relation avec Commission (directe)
                entity.HasOne(e => e.Commission)
                      .WithMany() // Pas de navigation inverse pour simplifier
                      .HasForeignKey(e => e.CommissionId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Relation avec ApplicationUser (Membre) - UNE SEULE RELATION
                entity.HasOne(e => e.Membre)
                      .WithMany() // Supprimez la navigation inverse si elle existe
                      .HasForeignKey(e => e.MembreId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relation avec Mandat
                entity.HasOne(e => e.Mandat)
                      .WithMany(e => e.MembresCommission)
                      .HasForeignKey(e => e.MandatId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Index unique pour éviter les doublons
                entity.HasIndex(e => new { e.CommissionId, e.MembreId, e.MandatId })
                      .IsUnique()
                      .HasDatabaseName("IX_MembreCommission_Commission_Membre_Mandat_Unique");

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.CommissionId);
                entity.HasIndex(e => e.MembreId);
                entity.HasIndex(e => e.MandatId);
            });

            // Configuration de Club mise à jour
            modelBuilder.Entity<Club>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DateCreation).HasMaxLength(50);
                entity.Property(e => e.NumeroClub); // Nullable int
                entity.Property(e => e.NumeroTelephone).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.LieuReunion).HasMaxLength(200);
                entity.Property(e => e.ParrainePar).HasMaxLength(100);
                entity.Property(e => e.JourReunion).HasMaxLength(20);
                entity.Property(e => e.HeureReunion);
                entity.Property(e => e.Frequence).HasMaxLength(50);
                entity.Property(e => e.Adresse).HasMaxLength(300);

                // Index unique sur le numéro de club (seulement si renseigné)
                entity.HasIndex(e => e.NumeroClub).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();

                entity.HasMany(e => e.Mandats)
                      .WithOne(e => e.Club)
                      .HasForeignKey(e => e.ClubId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuration de Mandat
            modelBuilder.Entity<Mandat>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Annee).IsRequired();
                entity.Property(e => e.DateDebut).IsRequired();
                entity.Property(e => e.DateFin).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.MontantCotisation).IsRequired();
                entity.Property(e => e.EstActuel).IsRequired();
                entity.HasOne(e => e.Club)
                      .WithMany(e => e.Mandats)
                      .HasForeignKey(e => e.ClubId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(e => e.MembresCommission)
                      .WithOne(e => e.Mandat)
                      .HasForeignKey(e => e.MandatId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.ClubId, e.EstActuel })
                      .HasFilter("EstActuel = 1")
                      .IsUnique();
                entity.HasIndex(e => new { e.ClubId, e.Annee }).IsUnique();
            });

            // Configuration d'ApplicationUser
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.ProfilePictureUrl).HasMaxLength(200);
                entity.Property(e => e.JoinedDate).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();
                entity.Property(e => e.NumeroMembre).IsRequired().HasMaxLength(50);
            });

            // Configuration de TypeReunion
            modelBuilder.Entity<TypeReunion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(100);

                // Index unique pour éviter les doublons de libellé
                entity.HasIndex(e => e.Libelle).IsUnique();

                // Relation one-to-many avec Reunion
                entity.HasMany(e => e.Reunions)
                      .WithOne(e => e.TypeReunion)
                      .HasForeignKey(e => e.TypeReunionId)
                      .OnDelete(DeleteBehavior.Restrict); // Empêche la suppression si des réunions existent
            });

            // Configuration de Reunion
            modelBuilder.Entity<Reunion>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuration de la date (stockée comme DATE en base)
                entity.Property(e => e.Date)
                    .IsRequired()
                    .HasColumnType("date");

                // Configuration de l'heure (stockée comme TIME en base)
                entity.Property(e => e.Heure)
                    .IsRequired()
                    .HasColumnType("time");

                entity.Property(e => e.TypeReunionId).IsRequired();
                entity.Property(e => e.ClubId).IsRequired();

                // Relation avec Club
                entity.HasOne(e => e.Club)
                      .WithMany() // Pas de navigation inverse définie dans Club pour les Reunions
                      .HasForeignKey(e => e.ClubId)
                      .OnDelete(DeleteBehavior.Cascade); // Supprime les réunions si le club est supprimé

                // Relation avec TypeReunion
                entity.HasOne(e => e.TypeReunion)
                      .WithMany(e => e.Reunions)
                      .HasForeignKey(e => e.TypeReunionId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relations one-to-many existantes
                entity.HasMany(e => e.OrdresDuJour)
                      .WithOne(e => e.Reunion)
                      .HasForeignKey(e => e.ReunionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.ListesPresence)
                      .WithOne(e => e.Reunion)
                      .HasForeignKey(e => e.ReunionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Invites)
                      .WithOne(e => e.Reunion)
                      .HasForeignKey(e => e.ReunionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Documents)
                      .WithOne(e => e.Reunion)
                      .HasForeignKey(e => e.ReunionId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.ClubId);
                entity.HasIndex(e => e.TypeReunionId);
                entity.HasIndex(e => new { e.Date, e.Heure });

                // Index composites pour optimiser les requêtes courantes
                entity.HasIndex(e => new { e.ClubId, e.Date });
                entity.HasIndex(e => new { e.ClubId, e.TypeReunionId });
                entity.HasIndex(e => new { e.ClubId, e.Date, e.Heure });

                // Index composite pour éviter les doublons de réunion (même club, date, heure, type)
                entity.HasIndex(e => new { e.ClubId, e.Date, e.Heure, e.TypeReunionId })
                      .IsUnique()
                      .HasDatabaseName("IX_Reunion_ClubId_Date_Heure_TypeReunion_Unique");
            });

            // Configuration d'OrdreDuJour
            modelBuilder.Entity<OrdreDuJour>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.ReunionId).IsRequired();

                // Relation avec Reunion
                entity.HasOne(e => e.Reunion)
                      .WithMany(e => e.OrdresDuJour)
                      .HasForeignKey(e => e.ReunionId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index pour optimiser les requêtes par réunion
                entity.HasIndex(e => e.ReunionId);
            });

            // Configuration de ListePresence
            modelBuilder.Entity<ListePresence>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MembreId).IsRequired();
                entity.Property(e => e.ReunionId).IsRequired();

                // Relation avec ApplicationUser (Membre)
                entity.HasOne(e => e.Membre)
                      .WithMany() // Pas de navigation inverse définie
                      .HasForeignKey(e => e.MembreId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relation avec Reunion
                entity.HasOne(e => e.Reunion)
                      .WithMany(e => e.ListesPresence)
                      .HasForeignKey(e => e.ReunionId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index unique pour éviter qu'un membre soit présent plusieurs fois à la même réunion
                entity.HasIndex(e => new { e.MembreId, e.ReunionId }).IsUnique();

                // Index pour optimiser les requêtes
                entity.HasIndex(e => e.ReunionId);
                entity.HasIndex(e => e.MembreId);
            });

            // Configuration d'InviteReunion
            modelBuilder.Entity<InviteReunion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Prenom).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.Telephone).HasMaxLength(20);
                entity.Property(e => e.Organisation).HasMaxLength(200);
                entity.Property(e => e.ReunionId).IsRequired();

                // Relation avec Reunion
                entity.HasOne(e => e.Reunion)
                      .WithMany(e => e.Invites)
                      .HasForeignKey(e => e.ReunionId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index pour optimiser les requêtes par réunion
                entity.HasIndex(e => e.ReunionId);

                // Index pour optimiser les recherches par nom/prénom
                entity.HasIndex(e => new { e.Nom, e.Prenom });

                // Index pour optimiser les recherches par email
                entity.HasIndex(e => e.Email);
            });

            // ===== CONFIGURATIONS POUR LE MODULE DE FORMATION =====

            // Configuration de DocumentFormation
            modelBuilder.Entity<DocumentFormation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Titre).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.CheminFichier).IsRequired().HasMaxLength(500);
                entity.Property(e => e.UploadePar).IsRequired().HasMaxLength(450);
                entity.Property(e => e.ClubId).IsRequired();
                entity.Property(e => e.Type).IsRequired();

                // Relations
                entity.HasOne(e => e.Uploadeur)
                      .WithMany()
                      .HasForeignKey(e => e.UploadePar)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Club)
                      .WithMany()
                      .HasForeignKey(e => e.ClubId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index
                entity.HasIndex(e => e.ClubId);
                entity.HasIndex(e => e.UploadePar);
                entity.HasIndex(e => e.DateUpload);
                entity.HasIndex(e => new { e.ClubId, e.EstActif });
            });

            // Configuration de ChunkDocument
            modelBuilder.Entity<ChunkDocument>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DocumentFormationId).IsRequired();
                entity.Property(e => e.Contenu).IsRequired();
                entity.Property(e => e.IndexChunk).IsRequired();
                
                // Configuration de la propriété Embedding pour pgvector
                // Temporairement ignorée pour la migration, sera configurée manuellement
                entity.Ignore(e => e.Embedding);

                // Relation avec DocumentFormation
                entity.HasOne(e => e.DocumentFormation)
                      .WithMany(e => e.Chunks)
                      .HasForeignKey(e => e.DocumentFormationId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index
                entity.HasIndex(e => e.DocumentFormationId);
                entity.HasIndex(e => e.IndexChunk);
                entity.HasIndex(e => new { e.DocumentFormationId, e.IndexChunk });
            });

            // Configuration de SessionFormation
            modelBuilder.Entity<SessionFormation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MembreId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.DocumentFormationId).IsRequired();
                entity.Property(e => e.ScoreActuel).HasDefaultValue(0);
                entity.Property(e => e.ScoreObjectif).HasDefaultValue(80);
                entity.Property(e => e.Statut).HasDefaultValue(StatutSession.EnCours);

                // Relations
                entity.HasOne(e => e.Membre)
                      .WithMany()
                      .HasForeignKey(e => e.MembreId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.DocumentFormation)
                      .WithMany(e => e.Sessions)
                      .HasForeignKey(e => e.DocumentFormationId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index
                entity.HasIndex(e => e.MembreId);
                entity.HasIndex(e => e.DocumentFormationId);
                entity.HasIndex(e => e.DateDebut);
                entity.HasIndex(e => new { e.MembreId, e.DocumentFormationId });
            });

            // Configuration de QuestionFormation
            modelBuilder.Entity<QuestionFormation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionFormationId).IsRequired();
                entity.Property(e => e.ChunkDocumentId).IsRequired();
                entity.Property(e => e.TexteQuestion).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.ReponseCorrecte).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Difficulte).HasDefaultValue(1);

                // Relations
                entity.HasOne(e => e.SessionFormation)
                      .WithMany(e => e.Questions)
                      .HasForeignKey(e => e.SessionFormationId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ChunkDocument)
                      .WithMany(e => e.Questions)
                      .HasForeignKey(e => e.ChunkDocumentId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index
                entity.HasIndex(e => e.SessionFormationId);
                entity.HasIndex(e => e.ChunkDocumentId);
                entity.HasIndex(e => e.Difficulte);
            });

            // Configuration de ReponseUtilisateur
            modelBuilder.Entity<ReponseUtilisateur>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.QuestionFormationId).IsRequired();
                entity.Property(e => e.ReponseTexte).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.TempsReponseMs).IsRequired();

                // Relation avec QuestionFormation
                entity.HasOne(e => e.QuestionFormation)
                      .WithMany(e => e.Reponses)
                      .HasForeignKey(e => e.QuestionFormationId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index
                entity.HasIndex(e => e.QuestionFormationId);
                entity.HasIndex(e => e.DateReponse);
            });

            // Configuration de BadgeFormation
            modelBuilder.Entity<BadgeFormation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MembreId).IsRequired().HasMaxLength(450);
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.DocumentFormationId).HasMaxLength(450);
                entity.Property(e => e.PointsGagnes).HasDefaultValue(0);

                // Relation avec ApplicationUser
                entity.HasOne(e => e.Membre)
                      .WithMany()
                      .HasForeignKey(e => e.MembreId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index
                entity.HasIndex(e => e.MembreId);
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.DateObtention);
                entity.HasIndex(e => new { e.MembreId, e.Type, e.DocumentFormationId });
            });
        }
    }
}