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
    public class EmailCompteRenduController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailCompteRenduController> _logger;

        public EmailCompteRenduController(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<EmailCompteRenduController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Envoie le compte rendu d'une réunion par email aux membres sélectionnés
        /// </summary>
        /// <param name="dto">Données pour l'envoi du compte rendu</param>
        /// <returns>Résultat de l'envoi</returns>
        [HttpPost("send-compte-rendu")]
        public async Task<ActionResult<object>> SendCompteRendu([FromBody] SendCompteRenduDto dto)
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

                // Vérifier la limite de destinataires
                if (dto.MembresIds.Count > 100)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Le nombre de destinataires ({dto.MembresIds.Count}) dépasse la limite autorisée (100)"
                    });
                }

                // Récupérer les informations de la réunion
                var reunion = await _context.Reunions
                    .Include(r => r.TypeReunion)
                    .Include(r => r.Club)
                    .FirstOrDefaultAsync(r => r.Id == dto.ReunionId && r.ClubId == dto.ClubId);

                if (reunion == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Réunion non trouvée"
                    });
                }

                // Récupérer les ordres du jour
                var ordresDuJour = await _context.OrdresDuJour
                    .Where(odj => odj.ReunionId == dto.ReunionId)
                    .ToListAsync();

                // Récupérer les rapports pour tous les ordres du jour
                var rapports = await _context.OrdreJourRapports
                    .Where(r => ordresDuJour.Select(odj => odj.Id).Contains(r.OrdreDuJourId))
                    .ToListAsync();

                // Récupérer les présences
                var presences = await _context.ListesPresence
                    .Include(p => p.Membre)
                    .Where(p => p.ReunionId == dto.ReunionId)
                    .Select(p => new
                    {
                        NomComplet = $"{p.Membre.FirstName} {p.Membre.LastName}"
                    })
                    .ToListAsync();

                // Récupérer les invités
                var invites = await _context.InvitesReunion
                    .Where(i => i.ReunionId == dto.ReunionId)
                    .Select(i => new
                    {
                        NomComplet = $"{i.Prenom} {i.Nom}",
                        Organisation = i.Organisation
                    })
                    .ToListAsync();

                var resultats = new List<EmailResult>();
                var emailsEnvoyes = 0;
                var emailsEchoues = 0;

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

                        // Générer le contenu HTML du compte rendu
                        var htmlContent = GenerateCompteRenduEmailHtml(reunion, ordresDuJour, rapports, presences, invites);

                        // Préparer la requête d'email
                        var emailRequest = new EmailRequest
                        {
                            Subject = $"📋 Compte rendu - {reunion.TypeReunion.Libelle} du {reunion.Date:dd/MM/yyyy} - {reunion.Club.Name}",
                            Message = htmlContent,
                            Recipients = new List<string> { membre.Email },
                            IsUrgent = false,
                            SendCopy = false
                        };

                        // Envoyer l'email via la méthode sans template
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

                _logger.LogInformation("Envoi en masse du compte rendu terminé: {Envoyes} réussis, {Echoues} échoués",
                    emailsEnvoyes, emailsEchoues);

                return Ok(new
                {
                    success = true,
                    message = $"Envoi terminé: {emailsEnvoyes} email(s) envoyé(s), {emailsEchoues} échec(s)",
                    reunion = new
                    {
                        date = reunion.Date,
                        typeReunion = reunion.TypeReunion.Libelle,
                        club = reunion.Club.Name
                    },
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
                _logger.LogError(ex, "Erreur lors de l'envoi en masse des comptes rendus");
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors de l'envoi en masse des comptes rendus",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Envoie le compte rendu à tous les membres du club
        /// </summary>
        /// <param name="dto">Données pour l'envoi à tous les membres</param>
        /// <returns>Résultat des envois</returns>
        [HttpPost("send-to-all-club-members")]
        [Authorize(Roles = "Admin,President,Secretary")] // Seuls les responsables peuvent envoyer à tous
        public async Task<ActionResult<object>> SendCompteRenduToAllClubMembers([FromBody] SendCompteRenduToAllDto dto)
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

                var sendDto = new SendCompteRenduDto
                {
                    ClubId = dto.ClubId,
                    ReunionId = dto.ReunionId,
                    MembresIds = membresIds
                };

                // Réutiliser la méthode d'envoi multiple
                return await SendCompteRendu(sendDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi du compte rendu à tous les membres du club {ClubId}", dto.ClubId);
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors de l'envoi du compte rendu à tous les membres du club",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Teste l'envoi d'un compte rendu à un email spécifique
        /// </summary>
        /// <param name="dto">Paramètres du test</param>
        /// <returns>Résultat du test</returns>
        [HttpPost("test-compte-rendu")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<ActionResult<object>> TestCompteRenduEmail([FromBody] TestCompteRenduDto dto)
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

                // Récupérer les informations de la réunion pour le test
                var reunion = await _context.Reunions
                    .Include(r => r.TypeReunion)
                    .Include(r => r.Club)
                    .FirstOrDefaultAsync(r => r.Id == dto.ReunionId && r.ClubId == dto.ClubId);

                if (reunion == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Réunion non trouvée"
                    });
                }

                // Générer un contenu de test
                var testHtml = GenerateTestCompteRenduHtml(reunion);

                // Préparer la requête d'email de test
                var emailRequest = new EmailRequest
                {
                    Subject = $"TEST - Compte rendu - {reunion.TypeReunion.Libelle} du {reunion.Date:dd/MM/yyyy} - {reunion.Club.Name}",
                    Message = testHtml,
                    Recipients = new List<string> { dto.TestEmail },
                    IsUrgent = false,
                    SendCopy = false
                };

                // Envoyer le test
                var result = await _emailService.SendSimpleEmailAsync(emailRequest);

                if (result.Success)
                {
                    _logger.LogInformation("Test compte rendu email réussi vers {Email}", dto.TestEmail);

                    return Ok(new
                    {
                        success = true,
                        message = "Test compte rendu envoyé avec succès ! Vérifiez votre boîte mail.",
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
                        message = "Échec du test compte rendu",
                        error = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du test compte rendu email");
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors du test compte rendu",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Génère le contenu HTML du compte rendu pour l'email
        /// </summary>
        private string GenerateCompteRenduEmailHtml(dynamic reunion, List<OrdreDuJour> ordresDuJour, List<OrdreJourRapport> rapports, dynamic presences, dynamic invites)
        {
            var dateReunion = ((DateTime)reunion.Date).ToString("dd/MM/yyyy");
            var heureReunion = ((TimeSpan)reunion.Heure).ToString(@"hh\:mm");

            // Générer la liste des présences
            var presencesHtml = "";
            foreach (var presence in presences)
            {
                presencesHtml += $"<li style='margin-bottom: 4px;'>{presence.NomComplet}</li>";
            }

            // Générer la liste des invités
            var invitesHtml = "";
            foreach (var invite in invites)
            {
                var organisation = !string.IsNullOrEmpty(invite.Organisation) ? $" ({invite.Organisation})" : "";
                invitesHtml += $"<li style='margin-bottom: 4px;'>{invite.NomComplet}{organisation}</li>";
            }

            // Générer les ordres du jour avec rapports
            var ordresHtml = "";
            var numeroOrdre = 1;
            foreach (var ordre in ordresDuJour)
            {
                var rapportsTexte = "";

                // Récupérer les rapports pour cet ordre du jour
                var rapportsOrdre = rapports.Where(r => r.OrdreDuJourId == ordre.Id).ToList();

                if (rapportsOrdre.Any())
                {
                    foreach (var rapport in rapportsOrdre)
                    {
                        rapportsTexte += $"<p style='margin: 8px 0; line-height: 1.6;'>{rapport.Texte}</p>";
                        if (!string.IsNullOrEmpty(rapport.Divers))
                        {
                            rapportsTexte += $"<p style='margin: 8px 0; font-style: italic; color: #666;'>{rapport.Divers}</p>";
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(ordre.Rapport))
                {
                    // Utiliser le rapport de l'ordre du jour si pas de rapports séparés
                    rapportsTexte = $"<p style='margin: 8px 0; line-height: 1.6;'>{ordre.Rapport}</p>";
                }
                else
                {
                    rapportsTexte = "<p style='margin: 8px 0; color: #888; font-style: italic;'>Aucun rapport disponible</p>";
                }

                ordresHtml += $@"
                <div style='margin-bottom: 25px; border-left: 4px solid #1f4788; padding-left: 15px;'>
                    <h3 style='margin: 0 0 10px 0; color: #1f4788; font-size: 16px;'>{numeroOrdre}. {ordre.Description}</h3>
                    <div style='background-color: #f8fafc; padding: 15px; border-radius: 6px;'>
                        {rapportsTexte}
                    </div>
                </div>";
                numeroOrdre++;
            }

            return $@"
<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin: 0; padding: 20px; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <div style='max-width: 700px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);'>
        
        <!-- Header -->
        <div style='background: linear-gradient(135deg, #1f4788 0%, #2d5fa3 100%); color: white; padding: 25px; text-align: center;'>
            <h1 style='margin: 0; font-size: 26px; font-weight: 600;'>📋 Compte Rendu de Réunion</h1>
            <p style='margin: 8px 0 0 0; opacity: 0.9; font-size: 18px;'>{reunion.Club.Name}</p>
        </div>
        
        <!-- Informations de la réunion -->
        <div style='background-color: #f8fafc; padding: 25px; border-left: 4px solid #1f4788;'>
            <h2 style='margin: 0 0 15px 0; color: #2d3748; font-size: 20px;'>{reunion.TypeReunion.Libelle}</h2>
            <div style='display: flex; gap: 20px; flex-wrap: wrap;'>
                <div style='color: #666; font-size: 14px;'>
                    <strong>📅 Date :</strong> {dateReunion}
                </div>
                <div style='color: #666; font-size: 14px;'>
                    <strong>🕐 Heure :</strong> {heureReunion}
                </div>
            </div>
        </div>

        <!-- Participants -->
        <div style='padding: 25px;'>
            <h3 style='margin: 0 0 15px 0; color: #1f4788; font-size: 18px; border-bottom: 2px solid #e2e8f0; padding-bottom: 8px;'>👥 Participants</h3>
            
            <div style='margin-bottom: 20px;'>
                <h4 style='margin: 0 0 10px 0; color: #2d3748; font-size: 16px;'>Membres présents ({presences.Count})</h4>
                <ul style='margin: 0; padding-left: 20px; color: #4a5568;'>
                    {presencesHtml}
                </ul>
            </div>

            {(invites.Count > 0 ? $@"
            <div style='margin-bottom: 20px;'>
                <h4 style='margin: 0 0 10px 0; color: #2d3748; font-size: 16px;'>Invités ({invites.Count})</h4>
                <ul style='margin: 0; padding-left: 20px; color: #4a5568;'>
                    {invitesHtml}
                </ul>
            </div>" : "")}
        </div>

        <!-- Ordre du jour -->
        <div style='padding: 0 25px 25px 25px;'>
            <h3 style='margin: 0 0 20px 0; color: #1f4788; font-size: 18px; border-bottom: 2px solid #e2e8f0; padding-bottom: 8px;'>📝 Ordre du Jour</h3>
            {ordresHtml}
        </div>

        <!-- Footer -->
        <div style='background-color: #1f4788; color: white; padding: 20px; text-align: center;'>
            <p style='margin: 0; font-size: 13px; opacity: 0.9;'>
                Compte rendu généré automatiquement le {DateTime.Now:dd/MM/yyyy à HH:mm} <br>
                © {DateTime.Now.Year} {reunion.Club.Name} - Service Above Self
            </p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Génère un contenu HTML de test pour le compte rendu
        /// </summary>
        private string GenerateTestCompteRenduHtml(dynamic reunion)
        {
            return $@"
<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin: 0; padding: 20px; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);'>
        <div style='background: linear-gradient(135deg, #4CAF50 0%, #45a049 100%); color: white; padding: 20px; text-align: center;'>
            <h1 style='margin: 0; font-size: 24px; font-weight: 600;'>✅ Test Compte Rendu</h1>
            <p style='margin: 8px 0 0 0; opacity: 0.9; font-size: 16px;'>Rotary Club Manager</p>
        </div>
        <div style='padding: 30px; text-align: center;'>
            <div style='background-color: #f9fff9; border: 2px solid #4CAF50; border-radius: 8px; padding: 20px; margin-bottom: 20px;'>
                <p style='margin: 0; color: #2e7d32; font-size: 16px; font-weight: 500;'>
                    Test d'envoi de compte rendu pour la réunion :<br>
                    <strong>{reunion.TypeReunion.Libelle}</strong><br>
                    du {((DateTime)reunion.Date):dd/MM/yyyy} - {reunion.Club.Name}
                </p>
            </div>
            <div style='color: #666; margin-top: 20px;'>
                <p><strong>Date du test :</strong> {DateTime.Now:dd/MM/yyyy à HH:mm:ss}</p>
            </div>
        </div>
        <div style='background-color: #1f4788; color: white; padding: 15px; text-align: center;'>
            <p style='margin: 0; font-size: 13px; opacity: 0.9;'>
                © {DateTime.Now.Year} Rotary Club Manager - Service Above Self
            </p>
        </div>
    </div>
</body>
</html>";
        }
    }

    // DTOs pour les requêtes
    public class SendCompteRenduDto
    {
        [Required(ErrorMessage = "L'ID du club est requis")]
        public Guid ClubId { get; set; }

        [Required(ErrorMessage = "L'ID de la réunion est requis")]
        public Guid ReunionId { get; set; }

        [Required(ErrorMessage = "La liste des IDs des membres est requise")]
        [MinLength(1, ErrorMessage = "Au moins un membre doit être spécifié")]
        public List<string> MembresIds { get; set; } = new();
    }

    public class SendCompteRenduToAllDto
    {
        [Required(ErrorMessage = "L'ID du club est requis")]
        public Guid ClubId { get; set; }

        [Required(ErrorMessage = "L'ID de la réunion est requis")]
        public Guid ReunionId { get; set; }

        public bool IncludeInactive { get; set; } = false;
    }

    public class TestCompteRenduDto
    {
        [Required(ErrorMessage = "L'ID du club est requis")]
        public Guid ClubId { get; set; }

        [Required(ErrorMessage = "L'ID de la réunion est requis")]
        public Guid ReunionId { get; set; }

        [Required(ErrorMessage = "L'adresse email de test est requise")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string TestEmail { get; set; } = string.Empty;
    }
}