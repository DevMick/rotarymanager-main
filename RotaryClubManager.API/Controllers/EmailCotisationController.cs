using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Application.Services;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all endpoints
    [EnableRateLimiting("EmailPolicy")]
    public class EmailCotisationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailCotisationController> _logger;

        public EmailCotisationController(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<EmailCotisationController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Envoie la situation de cotisation par email à un membre spécifique
        /// </summary>
        /// <param name="dto">Données pour l'envoi d'email</param>
        /// <returns>Résultat de l'envoi</returns>
        [HttpPost("send-to-member")]
        public async Task<ActionResult<object>> SendSituationToMember([FromBody] SendSituationToMemberDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Données invalides",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                // Récupérer les informations du membre et sa situation
                var membre = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == dto.MembreId);

                if (membre == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Membre non trouvé"
                    });
                }

                if (string.IsNullOrEmpty(membre.Email))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Adresse email non disponible pour ce membre"
                    });
                }

                // Récupérer la situation du membre
                var situation = await GetSituationMembre(dto.MembreId, dto.ClubId);
                if (situation == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Impossible de récupérer la situation du membre"
                    });
                }

                // Générer le contenu HTML de l'email (simple et joli)
                var htmlContent = GenerateSimpleEmailHtml(membre, situation);

                // Préparer la requête d'email en utilisant la nouvelle méthode sans template
                var emailRequest = new EmailRequest
                {
                    Subject = $"💳 Situation de Cotisation - {situation.ClubName}",
                    Message = htmlContent,
                    Recipients = new List<string> { membre.Email },
                    IsUrgent = false,
                    SendCopy = false
                };

                // Envoyer l'email via la nouvelle méthode sans template
                var result = await _emailService.SendSimpleEmailAsync(emailRequest);

                if (result.Success)
                {
                    _logger.LogInformation("Email de situation envoyé avec succès à {Email} pour le membre {MembreId}",
                        membre.Email, dto.MembreId);

                    return Ok(new
                    {
                        success = true,
                        message = $"Email envoyé avec succès à {membre.Email}",
                        emailId = result.EmailId,
                        destinataire = new
                        {
                            membreId = membre.Id,
                            nomComplet = $"{membre.FirstName} {membre.LastName}",
                            email = membre.Email
                        }
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Erreur lors de l'envoi de l'email",
                        error = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'email de situation au membre {MembreId}", dto.MembreId);
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors de l'envoi de l'email",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Envoie la situation de cotisation par email à plusieurs membres
        /// </summary>
        /// <param name="dto">Données pour l'envoi d'emails en masse</param>
        /// <returns>Résultat des envois</returns>
        [HttpPost("send-to-multiple-members")]
        public async Task<ActionResult<object>> SendSituationToMultipleMembers([FromBody] SendSituationToMultipleMembersDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Données invalides",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                // Vérifier la limite de destinataires (100 max selon votre config)
                if (dto.MembresIds.Count > 100)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Le nombre de destinataires ({dto.MembresIds.Count}) dépasse la limite autorisée (100)"
                    });
                }

                var resultats = new List<EmailResult>();
                var emailsEnvoyes = 0;
                var emailsEchoues = 0;

                // Récupérer les informations du club pour le nom
                var club = await _context.Clubs.FirstOrDefaultAsync(c => c.Id == dto.ClubId);
                var clubName = club?.Name ?? "Club";

                foreach (var membreId in dto.MembresIds)
                {
                    try
                    {
                        // Récupérer les informations du membre
                        var membre = await _context.Users
                            .FirstOrDefaultAsync(u => u.Id == membreId);

                        if (membre == null)
                        {
                            resultats.Add(new EmailResult
                            {
                                MembreId = membreId,
                                Email = "N/A",
                                NomComplet = "N/A",
                                Success = false,
                                Message = "Membre non trouvé"
                            });
                            emailsEchoues++;
                            continue;
                        }

                        if (string.IsNullOrEmpty(membre.Email))
                        {
                            resultats.Add(new EmailResult
                            {
                                MembreId = membreId,
                                Email = "N/A",
                                NomComplet = $"{membre.FirstName} {membre.LastName}",
                                Success = false,
                                Message = "Adresse email non disponible"
                            });
                            emailsEchoues++;
                            continue;
                        }

                        // Récupérer la situation du membre
                        var situation = await GetSituationMembre(membreId, dto.ClubId);
                        if (situation == null)
                        {
                            resultats.Add(new EmailResult
                            {
                                MembreId = membreId,
                                Email = membre.Email,
                                NomComplet = $"{membre.FirstName} {membre.LastName}",
                                Success = false,
                                Message = "Impossible de récupérer la situation"
                            });
                            emailsEchoues++;
                            continue;
                        }

                        // Générer le contenu HTML de l'email (simple et joli)
                        var htmlContent = GenerateSimpleEmailHtml(membre, situation);

                        // Préparer la requête d'email
                        var emailRequest = new EmailRequest
                        {
                            Subject = $"💳 Situation de Cotisation - {situation.ClubName}",
                            Message = htmlContent,
                            Recipients = new List<string> { membre.Email },
                            IsUrgent = false,
                            SendCopy = false
                        };

                        // Envoyer l'email via la nouvelle méthode sans template
                        var result = await _emailService.SendSimpleEmailAsync(emailRequest);

                        if (result.Success)
                        {
                            resultats.Add(new EmailResult
                            {
                                MembreId = membreId,
                                Email = membre.Email,
                                NomComplet = $"{membre.FirstName} {membre.LastName}",
                                Success = true,
                                Message = "Email envoyé avec succès",
                                EmailId = result.EmailId
                            });
                            emailsEnvoyes++;
                        }
                        else
                        {
                            resultats.Add(new EmailResult
                            {
                                MembreId = membreId,
                                Email = membre.Email,
                                NomComplet = $"{membre.FirstName} {membre.LastName}",
                                Success = false,
                                Message = result.ErrorMessage ?? "Erreur lors de l'envoi"
                            });
                            emailsEchoues++;
                        }

                        // Délai entre les envois pour éviter le spam
                        await Task.Delay(1000); // 1 seconde entre chaque envoi
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors de l'envoi de l'email au membre {MembreId}", membreId);
                        resultats.Add(new EmailResult
                        {
                            MembreId = membreId,
                            Email = "N/A",
                            NomComplet = "N/A",
                            Success = false,
                            Message = $"Erreur: {ex.Message}"
                        });
                        emailsEchoues++;
                    }
                }

                _logger.LogInformation("Envoi en masse terminé: {Envoyes} réussis, {Echoues} échoués",
                    emailsEnvoyes, emailsEchoues);

                return Ok(new
                {
                    success = true,
                    message = $"Envoi terminé: {emailsEnvoyes} email(s) envoyé(s), {emailsEchoues} échec(s)",
                    statistiques = new
                    {
                        totalMembres = dto.MembresIds.Count,
                        emailsEnvoyes = emailsEnvoyes,
                        emailsEchoues = emailsEchoues,
                        tauxReussite = dto.MembresIds.Count > 0
                            ? Math.Round((double)emailsEnvoyes / dto.MembresIds.Count * 100, 2)
                            : 0
                    },
                    resultats = resultats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi en masse des emails de situation");
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors de l'envoi en masse des emails",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Envoie la situation à tous les membres d'un club
        /// </summary>
        /// <param name="dto">Données pour l'envoi à tous les membres du club</param>
        /// <returns>Résultat des envois</returns>
        [HttpPost("send-to-all-club-members")]
        [Authorize(Roles = "Admin")] // Seuls les admins peuvent envoyer à tous les membres
        public async Task<ActionResult<object>> SendSituationToAllClubMembers([FromBody] SendSituationToAllClubMembersDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Données invalides",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                // Récupérer tous les membres du club
                var membresQuery = _context.UserClubs
                    .Include(uc => uc.User)
                    .Where(uc => uc.ClubId == dto.ClubId);

                if (!dto.IncludeInactive)
                {
                    membresQuery = membresQuery.Where(uc => uc.User.IsActive);
                }

                var membresClub = await membresQuery
                    .Select(uc => uc.User)
                    .Where(u => !string.IsNullOrEmpty(u.Email)) // Seulement les membres avec email
                    .ToListAsync();

                if (!membresClub.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Aucun membre avec adresse email trouvé dans ce club"
                    });
                }

                // Convertir en liste d'IDs pour réutiliser la méthode existante
                var membresIds = membresClub.Select(m => m.Id).ToList();

                var sendDto = new SendSituationToMultipleMembersDto
                {
                    ClubId = dto.ClubId,
                    MembresIds = membresIds
                };

                // Réutiliser la méthode d'envoi multiple
                return await SendSituationToMultipleMembers(sendDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi à tous les membres du club {ClubId}", dto.ClubId);
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors de l'envoi à tous les membres du club",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Teste la configuration email en envoyant un email de test
        /// </summary>
        /// <param name="dto">Paramètres du test</param>
        /// <returns>Résultat du test</returns>
        [HttpPost("test-email")]
        [Authorize(Roles = "Admin")] // Seuls les admins peuvent tester
        public async Task<ActionResult<object>> TestEmailConfiguration([FromBody] TestEmailDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Données invalides",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                // Créer un message de test simple
                var testSubject = "Test de configuration Email - Rotary Club Manager";
                var testBody = @"
<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin: 0; padding: 20px; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);'>
        <div style='background: linear-gradient(135deg, #4CAF50 0%, #45a049 100%); color: white; padding: 20px; text-align: center;'>
            <h1 style='margin: 0; font-size: 24px; font-weight: 600;'>✅ Test Email Réussi !</h1>
            <p style='margin: 8px 0 0 0; opacity: 0.9; font-size: 16px;'>Rotary Club Manager</p>
        </div>
        <div style='padding: 30px; text-align: center;'>
            <div style='background-color: #f9fff9; border: 2px solid #4CAF50; border-radius: 8px; padding: 20px; margin-bottom: 20px;'>
                <p style='margin: 0; color: #2e7d32; font-size: 16px; font-weight: 500;'>
                    Ce message confirme que la configuration email de votre Rotary Club Manager fonctionne correctement.
                </p>
            </div>
            <div style='color: #666; margin-top: 20px;'>
                <p><strong>Date du test :</strong> " + DateTime.Now.ToString("dd/MM/yyyy à HH:mm:ss") + @"</p>
            </div>
        </div>
        <div style='background-color: #1f4788; color: white; padding: 15px; text-align: center;'>
            <p style='margin: 0; font-size: 13px; opacity: 0.9;'>
                © " + DateTime.Now.Year + @" Rotary Club Manager - Service Above Self
            </p>
        </div>
    </div>
</body>
</html>";

                // Préparer la requête d'email
                var emailRequest = new EmailRequest
                {
                    Subject = testSubject,
                    Message = testBody,
                    Recipients = new List<string> { dto.TestEmail },
                    IsUrgent = false,
                    SendCopy = false
                };

                // Envoyer le test via la nouvelle méthode sans template
                var result = await _emailService.SendSimpleEmailAsync(emailRequest);

                if (result.Success)
                {
                    _logger.LogInformation("Test email réussi vers {Email}", dto.TestEmail);

                    return Ok(new
                    {
                        success = true,
                        message = "Test email réussi ! Vérifiez votre boîte mail.",
                        emailId = result.EmailId,
                        destinataire = dto.TestEmail,
                        dateTest = DateTime.Now
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Échec du test email",
                        error = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du test email");
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors du test email",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Récupère la situation d'un membre pour un club donné
        /// </summary>
        private async Task<SituationMembre?> GetSituationMembre(string membreId, Guid clubId)
        {
            try
            {
                // Récupérer le club
                var club = await _context.Clubs.FirstOrDefaultAsync(c => c.Id == clubId);
                if (club == null) return null;

                // Récupérer le membre
                var membre = await _context.Users.FirstOrDefaultAsync(u => u.Id == membreId);
                if (membre == null) return null;

                // Cotisations du membre pour ce club
                var cotisationsMembre = await _context.Cotisations
                    .Include(c => c.Mandat)
                    .Where(c => c.MembreId == membreId && c.Mandat.ClubId == clubId)
                    .ToListAsync();

                // Paiements du membre pour ce club
                var paiementsMembre = await _context.PaiementCotisations
                    .Where(p => p.MembreId == membreId && p.ClubId == clubId)
                    .ToListAsync();

                var montantTotalCotisations = cotisationsMembre.Sum(c => c.Montant);
                var montantTotalPaiements = paiementsMembre.Sum(p => p.Montant);
                var solde = montantTotalCotisations - montantTotalPaiements;

                return new SituationMembre
                {
                    MembreId = membreId,
                    NomComplet = $"{membre.FirstName} {membre.LastName}",
                    Email = membre.Email ?? "",
                    ClubId = clubId,
                    ClubName = club.Name,
                    MontantTotalCotisations = montantTotalCotisations,
                    MontantTotalPaiements = montantTotalPaiements,
                    Solde = solde,
                    TauxRecouvrement = montantTotalCotisations > 0
                        ? Math.Round((double)montantTotalPaiements / montantTotalCotisations * 100, 2)
                        : 0,
                    Statut = DeterminerStatutMembre(montantTotalCotisations, montantTotalPaiements, solde)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la situation du membre {MembreId}", membreId);
                return null;
            }
        }

        /// <summary>
        /// Génère le contenu HTML simple et joli pour l'email de situation (sans template professionnel)
        /// </summary>
        private string GenerateSimpleEmailHtml(dynamic membre, SituationMembre situation)
        {
            var statutColor = situation.Statut switch
            {
                "À jour" => "#10B981", // Vert
                "Partiellement payé" => "#F59E0B", // Orange
                "En retard" => "#EF4444", // Rouge
                _ => "#6B7280" // Gris
            };

            var statutIcon = situation.Statut switch
            {
                "À jour" => "✅",
                "Partiellement payé" => "⚠️",
                "En retard" => "❌",
                _ => "ℹ️"
            };

            return $@"
<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin: 0; padding: 20px; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);'>
        
        <!-- Header -->
        <div style='background: linear-gradient(135deg, #1f4788 0%, #2d5fa3 100%); color: white; padding: 20px; text-align: center;'>
            <h1 style='margin: 0; font-size: 24px; font-weight: 600;'>💳 Situation de Cotisation</h1>
            <p style='margin: 8px 0 0 0; opacity: 0.9; font-size: 16px;'>{situation.ClubName}</p>
        </div>
        
        <!-- Informations du membre -->
        <div style='background-color: #f8fafc; padding: 20px; border-left: 4px solid #1f4788;'>
            <div style='font-size: 20px; font-weight: 600; color: #2d3748; margin: 0 0 8px 0;'>{situation.NomComplet}</div>
            <div style='display: inline-flex; align-items: center; gap: 8px; padding: 6px 12px; border-radius: 20px; font-size: 14px; font-weight: 500; background-color: {statutColor}20; color: {statutColor}; border: 1px solid {statutColor}40;'>
                <span>{statutIcon}</span>
                <span>{situation.Statut}</span>
            </div>
        </div>

        <!-- Tableau de situation -->
        <div style='margin: 20px; border-radius: 8px; overflow: hidden; border: 1px solid #e2e8f0;'>
            <table style='width: 100%; border-collapse: collapse;'>
                <thead>
                    <tr style='background-color: #1f4788; color: white;'>
                        <th style='padding: 12px 15px; text-align: left; font-size: 14px; font-weight: 600;'>Description</th>
                        <th style='padding: 12px 15px; text-align: right; font-size: 14px; font-weight: 600;'>Montant</th>
                    </tr>
                </thead>
                <tbody>
                    <tr style='border-bottom: 1px solid #e2e8f0; background-color: white;'>
                        <td style='padding: 16px 15px; font-weight: 500;'>Montant dû</td>
                        <td style='padding: 16px 15px; text-align: right; font-weight: 600; font-size: 16px; color: #6b7280;'>
                            {situation.MontantTotalCotisations:N0} <span style='font-size: 12px; color: #64748b; font-weight: normal;'>FCFA</span>
                        </td>
                    </tr>
                    <tr style='border-bottom: 1px solid #e2e8f0; background-color: white;'>
                        <td style='padding: 16px 15px; font-weight: 500;'>Montant payé</td>
                        <td style='padding: 16px 15px; text-align: right; font-weight: 600; font-size: 16px; color: #10b981;'>
                            {situation.MontantTotalPaiements:N0} <span style='font-size: 12px; color: #64748b; font-weight: normal;'>FCFA</span>
                        </td>
                    </tr>
                    <tr style='background-color: white;'>
                        <td style='padding: 16px 15px; font-weight: 500;'>Solde restant</td>
                        <td style='padding: 16px 15px; text-align: right; font-weight: 600; font-size: 16px; color: {(situation.Solde <= 0 ? "#10b981" : "#ef4444")};'>
                            {situation.Solde:N0} <span style='font-size: 12px; color: #64748b; font-weight: normal;'>FCFA</span>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>

        <!-- Barre de progression -->
        <div style='margin: 20px; padding: 20px; background-color: #f8fafc; border-radius: 8px;'>
            <div style='font-size: 14px; color: #64748b; margin-bottom: 8px;'>Progression des paiements</div>
            <div style='width: 100%; height: 8px; background-color: #e2e8f0; border-radius: 4px; overflow: hidden;'>
                <div style='height: 100%; background: linear-gradient(90deg, #10b981 0%, #059669 100%); width: {Math.Min(situation.TauxRecouvrement, 100)}%; transition: width 0.3s ease;'></div>
            </div>
            <div style='text-align: center; margin-top: 8px; font-weight: 600; color: #2d3748;'>{situation.TauxRecouvrement:F1}%</div>
        </div>

        <!-- Footer -->
        <div style='background-color: #1f4788; color: white; padding: 15px; text-align: center;'>
            <p style='margin: 0; font-size: 13px; opacity: 0.9;'>
                Ce relevé a été généré automatiquement le {DateTime.Now:dd/MM/yyyy à HH:mm} <br>
                © {DateTime.Now.Year} {situation.ClubName} - Service Above Self
            </p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Détermine le statut d'un membre
        /// </summary>
        private static string DeterminerStatutMembre(int montantCotisations, int montantPaiements, int solde)
        {
            if (montantCotisations == 0)
                return "Aucune cotisation";

            if (solde <= 0)
                return "À jour";

            if (montantPaiements > 0)
                return "Partiellement payé";

            return "En retard";
        }
    }

    // DTOs pour les requêtes
    public class SendSituationToMemberDto
    {
        [Required(ErrorMessage = "L'ID du membre est requis")]
        public string MembreId { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'ID du club est requis")]
        public Guid ClubId { get; set; }
    }

    public class SendSituationToMultipleMembersDto
    {
        [Required(ErrorMessage = "L'ID du club est requis")]
        public Guid ClubId { get; set; }

        [Required(ErrorMessage = "La liste des IDs des membres est requise")]
        [MinLength(1, ErrorMessage = "Au moins un membre doit être spécifié")]
        public List<string> MembresIds { get; set; } = new();
    }

    public class SendSituationToAllClubMembersDto
    {
        [Required(ErrorMessage = "L'ID du club est requis")]
        public Guid ClubId { get; set; }

        public bool IncludeInactive { get; set; } = false;
    }

    public class TestEmailDto
    {
        [Required(ErrorMessage = "L'adresse email de test est requise")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string TestEmail { get; set; } = string.Empty;
    }

    // DTOs pour les réponses
    public class EmailResult
    {
        public string MembreId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string NomComplet { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? EmailId { get; set; }
    }

    public class SituationMembre
    {
        public string MembreId { get; set; } = string.Empty;
        public string NomComplet { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Guid ClubId { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public int MontantTotalCotisations { get; set; }
        public int MontantTotalPaiements { get; set; }
        public int Solde { get; set; }
        public double TauxRecouvrement { get; set; }
        public string Statut { get; set; } = string.Empty;
    }
}