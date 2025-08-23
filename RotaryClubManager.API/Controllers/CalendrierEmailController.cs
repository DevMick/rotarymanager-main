using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Application.Services;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Domain.Identity;
using RotaryClubManager.Infrastructure.Data;
using RotaryClubManager.Infrastructure.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting("EmailPolicy")]
    public class CalendrierEmailController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ITenantService _tenantService;
        private readonly ILogger<CalendrierEmailController> _logger;

        public CalendrierEmailController(
            ApplicationDbContext context,
            IEmailService emailService,
            ITenantService tenantService,
            ILogger<CalendrierEmailController> logger)
        {
            _context = context;
            _emailService = emailService;
            _tenantService = tenantService;
            _logger = logger;
        }

        /// <summary>
        /// Envoie le calendrier du mois par email aux membres du club
        /// </summary>
        /// <param name="request">Données de l'envoi du calendrier</param>
        /// <returns>Résultat de l'envoi</returns>
        [HttpPost("envoyer-calendrier")]
        [ProducesResponseType(typeof(CalendrierEmailResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 403)]
        [ProducesResponseType(typeof(ErrorResponse), 429)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> EnvoyerCalendrierParEmail([FromBody] EnvoyerCalendrierRequest request)
        {
            try
            {
                _logger.LogInformation("Tentative d'envoi du calendrier pour le club {ClubId} et le mois {Mois}",
                    request.ClubId, request.Mois);

                // Validation de la requête
                if (request.ClubId == Guid.Empty)
                    return BadRequest(new ErrorResponse
                    {
                        Success = false,
                        Message = "L'identifiant du club est invalide."
                    });

                if (request.Mois < 1 || request.Mois > 12)
                    return BadRequest(new ErrorResponse
                    {
                        Success = false,
                        Message = "Le mois doit être compris entre 1 et 12."
                    });

                // Vérifier l'accès au club
                if (!await CanAccessClub(request.ClubId))
                    return Forbid("Accès non autorisé à ce club.");

                _tenantService.SetCurrentTenantId(request.ClubId);

                // Récupérer les informations du club
                var club = await _context.Clubs
                    .FirstOrDefaultAsync(c => c.Id == request.ClubId);

                if (club == null)
                    return NotFound(new ErrorResponse
                    {
                        Success = false,
                        Message = "Club non trouvé."
                    });

                // Récupérer le calendrier du mois
                var calendrier = await GetCalendrierDuMois(request.ClubId, request.Mois);

                if (!calendrier.Any())
                {
                    return BadRequest(new ErrorResponse
                    {
                        Success = false,
                        Message = $"Aucun événement trouvé pour le mois {request.Mois}."
                    });
                }

                // Déterminer la liste des destinataires
                List<string> destinataires = new List<string>();

                if (request.EnvoyerATousLesMembres)
                {
                    // Récupérer tous les membres du club avec leurs emails
                    var membres = await _context.UserClubs
                        .Where(uc => uc.ClubId == request.ClubId)
                        .Include(uc => uc.User)
                        .Where(uc => uc.User.IsActive && !string.IsNullOrEmpty(uc.User.Email))
                        .Select(uc => uc.User.Email)
                        .ToListAsync();

                    destinataires.AddRange(membres);
                }

                // Ajouter les emails spécifiques si fournis
                if (request.EmailsDestinataires?.Any() == true)
                {
                    // Valider les emails fournis
                    var emailsValides = request.EmailsDestinataires
                        .Where(email => !string.IsNullOrWhiteSpace(email) && IsValidEmail(email))
                        .Distinct()
                        .ToList();

                    destinataires.AddRange(emailsValides);
                }

                // Supprimer les doublons
                destinataires = destinataires.Distinct().ToList();

                if (!destinataires.Any())
                {
                    return BadRequest(new ErrorResponse
                    {
                        Success = false,
                        Message = "Aucun destinataire valide trouvé. Vérifiez les emails fournis ou les membres du club."
                    });
                }

                // Créer le contenu HTML du calendrier - Compatible clients email
                var nomMois = GetNomMois(request.Mois);
                var annee = DateTime.Now.Year;
                var evenementsHtml = "";

                foreach (var evenement in calendrier)
                {
                    var dateFormatee = evenement.Date.ToString("dd/MM/yyyy", new System.Globalization.CultureInfo("fr-FR"));
                    var heureFormatee = evenement.Date.ToString("HH:mm");

                    evenementsHtml += $@"
            <tr>
                <td style=""padding: 8px 0; color: #666; font-size: 14px; width: 35%; vertical-align: top;"">📅 {dateFormatee} à {heureFormatee} :</td>
                <td style=""padding: 8px 0; font-weight: bold; color: #333; vertical-align: top;"">{evenement.Libelle}</td>
            </tr>";
                }

                var messagePersonnaliseHtml = !string.IsNullOrEmpty(request.MessagePersonnalise)
                    ? $@"
        <table cellpadding=""0"" cellspacing=""0"" border=""0"" style=""width: 100%; margin: 20px 0;"">
            <tr>
                <td style=""background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px;"">
                    <p style=""margin: 0; color: #856404; font-weight: bold;"">
                        💬 {request.MessagePersonnalise}
                    </p>
                </td>
            </tr>
        </table>"
                    : "";

                var contenuHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Calendrier {nomMois} - {club.Name}</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f5f5;"">
    <table cellpadding=""0"" cellspacing=""0"" border=""0"" style=""width: 100%; max-width: 600px; margin: 0 auto; background-color: #ffffff;"">
        <!-- Header -->
        <tr>
            <td style=""background-color: #3f5aa6; padding: 15px; text-align: center;"">
                <h2 style=""color: white; margin: 0; font-size: 18px; font-weight: 600;"">📅 Calendrier {nomMois} {annee}</h2>
                <p style=""color: white; margin: 8px 0 0 0; font-size: 14px;"">{club.Name}</p>
            </td>
        </tr>
        
        <!-- Contenu principal -->
        <tr>
            <td style=""background-color: #f8f9fa; padding: 20px;"">
                <p style=""margin: 0 0 15px 0; font-weight: bold; color: #333; font-size: 16px;"">Événements du mois de {nomMois} :</p>
                
                {messagePersonnaliseHtml}
                
                <!-- Liste des événements -->
                <table cellpadding=""0"" cellspacing=""0"" border=""0"" style=""width: 100%; margin-bottom: 20px;"">
                    {evenementsHtml}
                </table>
                
                <!-- Footer information -->
                <table cellpadding=""0"" cellspacing=""0"" border=""0"" style=""width: 100%; margin-top: 30px; border-top: 1px solid #dee2e6; padding-top: 15px;"">
                    <tr>
                        <td style=""color: #6c757d; font-size: 14px; padding-top: 15px;"">
                            <p style=""margin: 0 0 10px 0;"">Ce calendrier contient {calendrier.Count} événement(s) pour le mois de {nomMois}.</p>
                            <p style=""margin: 0 0 5px 0; font-style: italic;"">Cordialement,</p>
                            <p style=""margin: 0; font-style: italic;"">Équipe {club.Name}</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
        
        <!-- Message automatique -->
        <tr>
            <td style=""text-align: center; padding: 15px; font-size: 12px; color: #6c757d;"">
                <p style=""margin: 0;"">Ceci est un message automatique, merci de ne pas y répondre.</p>
            </td>
        </tr>
    </table>
</body>
</html>";

                // Préparer la requête d'email
                var emailRequest = new EmailRequest
                {
                    Subject = $"📅 Calendrier {nomMois} - {club.Name}",
                    Message = contenuHtml,
                    Recipients = destinataires,
                    IsUrgent = false,
                    SendCopy = true
                };

                // Envoyer l'email via la nouvelle méthode sans template professionnel
                var result = await _emailService.SendSimpleEmailAsync(emailRequest);

                if (result.Success)
                {
                    _logger.LogInformation("Calendrier envoyé avec succès - ID: {EmailId}, Destinataires: {Count}",
                        result.EmailId, result.RecipientsSent);

                    return Ok(new CalendrierEmailResponse
                    {
                        Success = true,
                        Message = $"Calendrier envoyé avec succès à {result.RecipientsSent} membre(s).",
                        EmailId = result.EmailId,
                        NombreDestinataires = result.RecipientsSent,
                        NombreEvenements = calendrier.Count,
                        Mois = request.Mois,
                        NomMois = GetNomMois(request.Mois),
                        ClubNom = club.Name
                    });
                }
                else
                {
                    _logger.LogError("Échec de l'envoi du calendrier: {Error}", result.ErrorMessage);

                    return StatusCode(500, new ErrorResponse
                    {
                        Success = false,
                        Message = "Erreur lors de l'envoi du calendrier.",
                        Errors = new[] { result.ErrorMessage }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi du calendrier pour le club {ClubId} et le mois {Mois}",
                    request.ClubId, request.Mois);

                return StatusCode(500, new ErrorResponse
                {
                    Success = false,
                    Message = "Une erreur inattendue est survenue."
                });
            }
        }

        /// <summary>
        /// Récupère le calendrier du mois pour un club
        /// </summary>
        private async Task<List<ItemCalendrierDto>> GetCalendrierDuMois(Guid clubId, int mois)
        {
            // 1. Récupérer les réunions du mois
            var reunions = await _context.Reunions
                .Where(r => r.ClubId == clubId && r.Date.Month == mois)
                .Include(r => r.TypeReunion)
                .Select(r => new ItemCalendrierDto
                {
                    Libelle = r.TypeReunion.Libelle,
                    Date = r.DateTimeComplete
                })
                .ToListAsync();

            // 2. Récupérer les événements du mois
            var evenements = await _context.Evenements
                .Where(e => e.ClubId == clubId && e.Date.Month == mois)
                .Select(e => new ItemCalendrierDto
                {
                    Libelle = e.Libelle,
                    Date = e.Date
                })
                .ToListAsync();

            // 3. Récupérer les anniversaires des membres du club pour le mois
            var anniversaires = await _context.UserClubs
                .Where(uc => uc.ClubId == clubId && uc.User.DateAnniversaire != default && uc.User.DateAnniversaire.Month == mois)
                .Select(uc => new ItemCalendrierDto
                {
                    Libelle = $"Anniversaire de {uc.User.FirstName} {uc.User.LastName}",
                    Date = new DateTime(DateTime.Now.Year, uc.User.DateAnniversaire.Month, uc.User.DateAnniversaire.Day)
                })
                .ToListAsync();

            // 4. Agréger les listes et les trier par date
            return reunions
                .Concat(evenements)
                .Concat(anniversaires)
                .OrderBy(item => item.Date)
                .ToList();
        }

        /// <summary>
        /// Vérifie si l'utilisateur peut accéder au club
        /// </summary>
        private async Task<bool> CanAccessClub(Guid clubId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return false;

            return await _context.UserClubs
                .AnyAsync(uc => uc.UserId == userId && uc.ClubId == clubId);
        }

        /// <summary>
        /// Valide le format d'un email
        /// </summary>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Retourne le nom du mois en français
        /// </summary>
        private string GetNomMois(int mois)
        {
            return mois switch
            {
                1 => "Janvier",
                2 => "Février",
                3 => "Mars",
                4 => "Avril",
                5 => "Mai",
                6 => "Juin",
                7 => "Juillet",
                8 => "Août",
                9 => "Septembre",
                10 => "Octobre",
                11 => "Novembre",
                12 => "Décembre",
                _ => "Mois inconnu"
            };
        }
    }

    // DTOs pour les requêtes et réponses
    public class EnvoyerCalendrierRequest
    {
        [Required(ErrorMessage = "L'identifiant du club est obligatoire")]
        public Guid ClubId { get; set; }

        [Required(ErrorMessage = "Le mois est obligatoire")]
        [Range(1, 12, ErrorMessage = "Le mois doit être compris entre 1 et 12")]
        public int Mois { get; set; }

        [StringLength(1000, ErrorMessage = "Le message personnalisé ne peut pas dépasser 1000 caractères")]
        public string? MessagePersonnalise { get; set; }

        public List<string>? EmailsDestinataires { get; set; } = new List<string>();

        public bool EnvoyerATousLesMembres { get; set; } = true;
    }

    public class CalendrierEmailResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string EmailId { get; set; } = string.Empty;
        public int NombreDestinataires { get; set; }
        public int NombreEvenements { get; set; }
        public int Mois { get; set; }
        public string NomMois { get; set; } = string.Empty;
        public string ClubNom { get; set; } = string.Empty;
    }

    public class ErrorResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string[] Errors { get; set; } = Array.Empty<string>();
    }

    public class ItemCalendrierDto
    {
        public string Libelle { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}