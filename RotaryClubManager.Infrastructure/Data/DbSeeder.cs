using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RotaryClubManager.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RotaryClubManager.Domain.Identity;

namespace RotaryClubManager.Infrastructure.Data
{
    public static class DbSeeder
    {
        public static async Task SeedDefaultDataAsync(IHost host)
        {
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<ApplicationDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                // Appliquer les migrations si elles ne sont pas déjà appliquées
                await context.Database.MigrateAsync();

                // Créer les rôles par défaut
                await SeedRolesAsync(roleManager, logger);

                // Créer un club par défaut si aucun n'existe
                await SeedDefaultClubAsync(context, logger);

                // Créer les commissions par défaut pour le club
                await SeedDefaultCommissionsAsync(context, logger);

                // Créer un admin par défaut si aucun n'existe
                await SeedDefaultAdminAsync(userManager, context, logger);

                // Créer des données de test si en mode développement
                await SeedTestDataIfNeededAsync(context, userManager, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Une erreur est survenue pendant le seeding de la base de données");
                throw;
            }
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            // Définir les rôles par défaut (alignés avec notre système de bureau)
            var roles = new[]
            {
                "Admin",
                "President",
                "VicePresident",
                "Secretary",
                "Treasurer",
                "PastPresident",
                "BoardMember",
                "Member"
            };

            logger.LogInformation("Création des rôles par défaut...");

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(role));
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Rôle '{Role}' créé avec succès", role);
                    }
                    else
                    {
                        logger.LogError("Erreur lors de la création du rôle '{Role}': {Errors}",
                            role, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
        }

        private static async Task SeedDefaultClubAsync(ApplicationDbContext context, ILogger logger)
        {
            if (!await context.Clubs.AnyAsync())
            {
                logger.LogInformation("Création du club par défaut...");

                // Créer un club par défaut
                var defaultClub = new Club
                {
                    Id = Guid.NewGuid(),
                    Name = "Club Rotary International",
                    DateCreation = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    NumeroClub = 155,
                    NumeroTelephone = "+1234567890",
                    Email = "contact@rotaryclub.org",
                    LieuReunion = "Centre Communautaire Rotary",
                    ParrainePar = "Rotary International",
                    JourReunion = "Mercredi",
                    HeureReunion = new TimeSpan(18, 30, 0), // 18h30
                    Frequence = "Hebdomadaire",
                    Adresse = "123 Rotary Street, Rotary City, World"
                };

                context.Clubs.Add(defaultClub);
                await context.SaveChangesAsync();

                // Créer un mandat par défaut
                await CreateDefaultMandatForClub(context, defaultClub.Id, logger);

                logger.LogInformation("Club par défaut '{ClubName}' créé avec ID {ClubId}",
                    defaultClub.Name, defaultClub.Id);
            }
        }

        private static async Task SeedDefaultCommissionsAsync(ApplicationDbContext context, ILogger logger)
        {
            // Récupérer le premier club (ou tous les clubs)
            var club = await context.Clubs.FirstOrDefaultAsync();
            if (club == null)
            {
                logger.LogWarning("Aucun club trouvé pour créer les commissions");
                return;
            }

            // Vérifier si des commissions existent déjà pour ce club
            if (await context.Commissions.AnyAsync(c => c.ClubId == club.Id))
            {
                logger.LogInformation("Des commissions existent déjà pour le club {ClubName}", club.Name);
                return;
            }

            logger.LogInformation("Création des commissions par défaut pour le club {ClubName}...", club.Name);

            var defaultCommissions = new[]
            {
                new Commission
                {
                    Id = Guid.NewGuid(),
                    ClubId = club.Id,
                    Nom = "Action",
                    Description = "Commission Action Rotary et Actions Humanitaires"
                },
                new Commission
                {
                    Id = Guid.NewGuid(),
                    ClubId = club.Id,
                    Nom = "Administration",
                    Description = "Commission Administration du club"
                },
                new Commission
                {
                    Id = Guid.NewGuid(),
                    ClubId = club.Id,
                    Nom = "Effectif",
                    Description = "Commission Effectif et Parrainage"
                },
                new Commission
                {
                    Id = Guid.NewGuid(),
                    ClubId = club.Id,
                    Nom = "Relations Publiques",
                    Description = "Commission Relations Publiques et Communication"
                },
                new Commission
                {
                    Id = Guid.NewGuid(),
                    ClubId = club.Id,
                    Nom = "Fondation Rotary",
                    Description = "Commission Fondation Rotary"
                },
                new Commission
                {
                    Id = Guid.NewGuid(),
                    ClubId = club.Id,
                    Nom = "Formation",
                    Description = "Commission Formation et Leadership"
                },
                new Commission
                {
                    Id = Guid.NewGuid(),
                    ClubId = club.Id,
                    Nom = "Jeunesse",
                    Description = "Commission des programmes Jeunesse"
                },
                new Commission
                {
                    Id = Guid.NewGuid(),
                    ClubId = club.Id,
                    Nom = "Rotary International",
                    Description = "Commission Rotary International"
                }
            };

            context.Commissions.AddRange(defaultCommissions);
            await context.SaveChangesAsync();

            logger.LogInformation("{Count} commissions créées pour le club {ClubName}",
                defaultCommissions.Length, club.Name);
        }

        private static async Task CreateDefaultMandatForClub(ApplicationDbContext context, Guid clubId, ILogger logger)
        {
            if (!await context.Mandats.AnyAsync(m => m.ClubId == clubId))
            {
                logger.LogInformation("Création du mandat par défaut pour le club...");

                var currentYear = DateTime.Now.Year;
                var mandat = new Mandat
                {
                    Id = Guid.NewGuid(),
                    ClubId = clubId,
                    Annee = currentYear,
                    DateDebut = new DateTime(currentYear, 7, 1), // Année Rotary commence en juillet
                    DateFin = new DateTime(currentYear + 1, 6, 30), // Se termine en juin
                    EstActuel = true,
                    Description = $"Mandat {currentYear}-{currentYear + 1} (par défaut)"
                };

                context.Mandats.Add(mandat);
                await context.SaveChangesAsync();

                logger.LogInformation("Mandat par défaut {Annee} créé pour le club", mandat.Annee);
            }
        }

        private static async Task SeedDefaultAdminAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context, ILogger logger)
        {
            // Vérifier si un admin existe déjà
            if (await userManager.Users.AnyAsync(u => u.Email == "admin@rotaryclub.org"))
            {
                logger.LogInformation("L'administrateur par défaut existe déjà");
                return;
            }

            // Récupérer le premier club pour l'administrateur
            var club = await context.Clubs.FirstOrDefaultAsync();
            if (club == null)
            {
                logger.LogWarning("Aucun club disponible pour créer l'administrateur");
                return;
            }

            logger.LogInformation("Création de l'administrateur par défaut...");

            // Créer l'administrateur par défaut
            var adminUser = new ApplicationUser
            {
                UserName = "admin@rotaryclub.org",
                Email = "admin@rotaryclub.org",
                FirstName = "Admin",
                LastName = "Rotary",
                EmailConfirmed = true,
                PhoneNumber = "+1234567890",
                PhoneNumberConfirmed = true,
                JoinedDate = DateTime.UtcNow,
                IsActive = true,
                NumeroMembre = "ADMIN-0001"
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123456");
            if (result.Succeeded)
            {
                // Ajouter l'utilisateur au rôle Admin
                await userManager.AddToRoleAsync(adminUser, "Admin");

                // Créer la relation UserClub pour l'admin
                var userClub = new UserClub
                {
                    Id = Guid.NewGuid(),
                    UserId = adminUser.Id,
                    ClubId = club.Id,
                    JoinedDate = DateTime.UtcNow
                };

                context.UserClubs.Add(userClub);
                await context.SaveChangesAsync();

                logger.LogInformation("Administrateur par défaut créé avec succès. Email: {Email}",
                    adminUser.Email);
                logger.LogInformation("Mot de passe par défaut: Admin@123456");
            }
            else
            {
                logger.LogError("Erreur lors de la création de l'administrateur: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        private static async Task SeedTestDataIfNeededAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger logger)
        {
            // Cette méthode peut être appelée pour créer des données de test en mode développement
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (environment == "Development" && !await userManager.Users.AnyAsync(u => u.Email != "admin@rotaryclub.org"))
            {
                logger.LogInformation("Création de données de test pour l'environnement de développement...");

                var club = await context.Clubs.FirstOrDefaultAsync();
                if (club == null) return;

                var mandat = await context.Mandats.FirstOrDefaultAsync(m => m.ClubId == club.Id);
                if (mandat == null) return;

                // Créer quelques utilisateurs de test
                var testUsers = new[]
                {
                    new { Email = "president@rotaryclub.org", FirstName = "Jean", LastName = "Dupont", Role = "President" },
                    new { Email = "secretary@rotaryclub.org", FirstName = "Marie", LastName = "Martin", Role = "Secretary" },
                    new { Email = "treasurer@rotaryclub.org", FirstName = "Pierre", LastName = "Durand", Role = "Treasurer" },
                    new { Email = "member1@rotaryclub.org", FirstName = "Sophie", LastName = "Bernard", Role = "Member" },
                    new { Email = "member2@rotaryclub.org", FirstName = "Michel", LastName = "Petit", Role = "Member" },
                    new { Email = "thomas.dubois@rotaryclub.org", FirstName = "Thomas", LastName = "Dubois", Role = "Member" }
                };

                foreach (var userData in testUsers)
                {
                    var user = new ApplicationUser
                    {
                        UserName = userData.Email,
                        Email = userData.Email,
                        FirstName = userData.FirstName,
                        LastName = userData.LastName,
                        EmailConfirmed = true,
                        JoinedDate = DateTime.UtcNow.AddMonths(-6), // Membres depuis 6 mois
                        IsActive = true,
                        NumeroMembre = $"MEMBER-000{Array.IndexOf(testUsers, userData) + 1:D2}"
                    };

                    var result = await userManager.CreateAsync(user, "Test@123456");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, userData.Role);

                        // Créer la relation UserClub pour chaque utilisateur
                        var userClub = new UserClub
                        {
                            Id = Guid.NewGuid(),
                            UserId = user.Id,
                            ClubId = club.Id,
                            JoinedDate = DateTime.UtcNow.AddMonths(-6)
                        };

                        context.UserClubs.Add(userClub);

                        logger.LogInformation("Utilisateur de test créé: {Email} avec le rôle {Role}",
                            userData.Email, userData.Role);
                    }
                }

                await context.SaveChangesAsync();

                // Ajouter quelques membres aux commissions
                await SeedTestCommissionMemberships(context, club.Id, mandat.Id, logger);
            }
        }

        private static async Task SeedTestCommissionMemberships(ApplicationDbContext context, Guid clubId, Guid mandatId, ILogger logger)
        {
            // Récupérer tous les utilisateurs non-admin via UserClubs
            var userClubs = await context.UserClubs
                .Include(uc => uc.User)
                .Where(uc => uc.ClubId == clubId && uc.User.Email != "admin@rotaryclub.org")
                .ToListAsync();

            // Récupérer les commissions de ce club
            var commissions = await context.Commissions
                .Where(c => c.ClubId == clubId)
                .ToListAsync();

            if (userClubs.Any() && commissions.Any())
            {
                logger.LogInformation("Création d'affiliations de test aux commissions...");
                var random = new Random();

                // Assigner chaque membre à 1-3 commissions aléatoirement
                foreach (var userClub in userClubs.Take(5)) // Prendre seulement les 5 premiers
                {
                    var numberOfCommissions = random.Next(1, 4); // 1 à 3 commissions
                    var selectedCommissions = commissions.OrderBy(x => random.Next()).Take(numberOfCommissions);

                    foreach (var commission in selectedCommissions)
                    {
                        // Vérifier si cette affectation n'existe pas déjà
                        var existingMembership = await context.MembresCommission
                            .FirstOrDefaultAsync(mc => mc.MembreId == userClub.UserId &&
                                                      mc.CommissionId == commission.Id &&
                                                      mc.MandatId == mandatId);

                        if (existingMembership == null)
                        {
                            var membreCommission = new MembreCommission
                            {
                                Id = Guid.NewGuid(),
                                MembreId = userClub.UserId,
                                CommissionId = commission.Id, // Relation directe avec Commission
                                MandatId = mandatId,
                                EstResponsable = random.Next(1, 5) == 1, // 25% de chance d'être responsable
                                DateNomination = DateTime.UtcNow.AddMonths(-random.Next(1, 6)),
                                EstActif = true,
                                Commentaires = "Affiliation de test"
                            };
                            context.MembresCommission.Add(membreCommission);
                        }
                    }
                }

                await context.SaveChangesAsync();
                logger.LogInformation("Affiliations de test créées");
            }
        }
    }
}