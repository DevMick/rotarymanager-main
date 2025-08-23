using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.API.DTOs.Authentication;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Domain.Identity;
using RotaryClubManager.Infrastructure.Data;
using RotaryClubManager.Infrastructure.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClubsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly ILogger<ClubsController> _logger;

        public ClubsController(ApplicationDbContext context, ITenantService tenantService, ILogger<ClubsController> logger)
        {
            _context = context;
            _tenantService = tenantService;
            _logger = logger;
        }

        // Méthodes d'aide pour les validations supplémentaires
        private static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return true; // Email optionnel maintenant

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

        private static bool IsValidPhoneNumber(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return true; // Téléphone optionnel maintenant

            // Regex simple pour valider un numéro de téléphone international
            var phoneRegex = new Regex(@"^[\+]?[0-9\s\-\(\)]{7,20}$");
            return phoneRegex.IsMatch(phoneNumber);
        }

        private static bool IsValidJourReunion(string? jour)
        {
            if (string.IsNullOrWhiteSpace(jour))
                return true; // Jour optionnel maintenant

            var joursValides = new[] { "Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche" };
            return joursValides.Contains(jour, StringComparer.OrdinalIgnoreCase);
        }

        // Récupérer tous les clubs (uniquement pour les administrateurs)
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Club>>> GetClubs()
        {
            try
            {
                var clubs = await _context.Clubs
                    .OrderBy(c => c.Name)
                    .ToListAsync();
                return Ok(clubs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des clubs");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des clubs");
            }
        }

        // Récupérer un club spécifique par ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Club>> GetClub(Guid id)
        {
            try
            {
                // Pour les non-administrateurs, vérifier si l'utilisateur appartient au club
                if (!User.IsInRole("Admin"))
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (string.IsNullOrEmpty(userId))
                    {
                        return Unauthorized();
                    }

                    // Vérifier via UserClubs si l'utilisateur appartient au club
                    var hasAccess = await _context.UserClubs
                        .AnyAsync(uc => uc.UserId == userId && uc.ClubId == id);

                    if (!hasAccess)
                    {
                        return Forbid();
                    }
                }

                var club = await _context.Clubs.FindAsync(id);

                if (club == null)
                {
                    return NotFound();
                }

                return club;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du club {ClubId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du club");
            }
        }

        // Récupérer le club principal de l'utilisateur courant
        [HttpGet("my-club")]
        [Authorize]
        public async Task<ActionResult<object>> GetMyClub()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Récupérer le club principal via UserClubs (première relation active)
                var primaryUserClub = await _context.UserClubs
                    .Include(uc => uc.Club)
                    .Where(uc => uc.UserId == userId)
                    .OrderBy(uc => uc.JoinedDate)
                    .FirstOrDefaultAsync();

                if (primaryUserClub?.Club == null)
                {
                    return NotFound(new { Message = "Aucun club principal défini pour cet utilisateur" });
                }

                var club = primaryUserClub.Club;
                var result = new
                {
                    club.Id,
                    club.Name,
                    club.DateCreation,
                    club.NumeroClub,
                    club.NumeroTelephone,
                    club.Email,
                    club.LieuReunion,
                    club.ParrainePar,
                    club.JourReunion,
                    club.HeureReunion,
                    club.Frequence,
                    club.Adresse,
                    UserJoinedDate = primaryUserClub.JoinedDate,
                    IsPrimary = true
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du club principal de l'utilisateur");
                return StatusCode(500, "Une erreur est survenue lors de la récupération du club");
            }
        }

        // Créer un nouveau club (administrateurs uniquement)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> CreateClub(CreateClubRequest request)
        {
            try
            {
                // Validations personnalisées supplémentaires
                var validationErrors = new List<string>();

                if (!string.IsNullOrWhiteSpace(request.Email) && !IsValidEmail(request.Email))
                {
                    validationErrors.Add("L'adresse email n'est pas valide");
                }

                if (!string.IsNullOrWhiteSpace(request.NumeroTelephone) && !IsValidPhoneNumber(request.NumeroTelephone))
                {
                    validationErrors.Add("Le numéro de téléphone n'est pas valide");
                }

                if (!string.IsNullOrWhiteSpace(request.JourReunion) && !IsValidJourReunion(request.JourReunion))
                {
                    validationErrors.Add("Le jour de réunion doit être un jour de la semaine valide");
                }

                // Validation du numéro de club (seulement s'il est fourni)
                if (request.NumeroClub.HasValue && request.NumeroClub <= 0)
                {
                    validationErrors.Add("Le numéro de club doit être un nombre positif");
                }

                if (validationErrors.Any())
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Erreurs de validation",
                        Errors = validationErrors
                    });
                }

                // Vérifier si le numéro du club est unique (seulement s'il est fourni)
                if (request.NumeroClub.HasValue)
                {
                    var existingClubWithNumber = await _context.Clubs
                        .Where(c => c.NumeroClub == request.NumeroClub)
                        .FirstOrDefaultAsync();

                    if (existingClubWithNumber != null)
                    {
                        return BadRequest(new
                        {
                            Success = false,
                            Message = $"Un club utilise déjà le numéro '{request.NumeroClub}'"
                        });
                    }
                }

                // Vérifier si l'email est unique (seulement s'il est fourni)
                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    var existingClubWithEmail = await _context.Clubs
                        .Where(c => c.Email == request.Email.Trim().ToLower())
                        .FirstOrDefaultAsync();

                    if (existingClubWithEmail != null)
                    {
                        return BadRequest(new
                        {
                            Success = false,
                            Message = $"Un club utilise déjà l'email '{request.Email}'"
                        });
                    }
                }

                // Créer le nouveau club
                var club = new Club
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name.Trim(),
                    DateCreation = request.DateCreation?.Trim(),
                    NumeroClub = request.NumeroClub,
                    NumeroTelephone = request.NumeroTelephone?.Trim(),
                    Email = request.Email?.Trim().ToLower(),
                    LieuReunion = request.LieuReunion?.Trim(),
                    ParrainePar = request.ParrainePar?.Trim(),
                    JourReunion = request.JourReunion?.Trim(),
                    HeureReunion = request.HeureReunion,
                    Frequence = request.Frequence?.Trim(),
                    Adresse = request.Adresse?.Trim()
                };

                _context.Clubs.Add(club);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Club '{ClubName}' créé avec succès par l'utilisateur {UserId}",
                    club.Name, User.FindFirstValue(ClaimTypes.NameIdentifier));

                return CreatedAtAction(nameof(GetClub), new { id = club.Id }, new
                {
                    Success = true,
                    Message = "Club créé avec succès.",
                    Club = club
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erreur de base de données lors de la création du club");
                return BadRequest(new
                {
                    Success = false,
                    Message = "Erreur lors de la sauvegarde en base de données.",
                    DetailedError = dbEx.InnerException?.Message ?? dbEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du club");
                return BadRequest(new
                {
                    Success = false,
                    Message = "Une erreur inattendue s'est produite lors de la création du club.",
                    DetailedError = ex.Message
                });
            }
        }

        // Mettre à jour les informations complètes d'un club
        [HttpPut("{id}/info")]
        [Authorize]
        public async Task<IActionResult> UpdateClubInfo(Guid id, UpdateClubInfoRequest request)
        {
            try
            {
                // Validations personnalisées supplémentaires
                var validationErrors = new List<string>();

                if (!string.IsNullOrWhiteSpace(request.Email) && !IsValidEmail(request.Email))
                {
                    validationErrors.Add("L'adresse email n'est pas valide");
                }

                if (!string.IsNullOrWhiteSpace(request.NumeroTelephone) && !IsValidPhoneNumber(request.NumeroTelephone))
                {
                    validationErrors.Add("Le numéro de téléphone n'est pas valide");
                }

                if (!string.IsNullOrWhiteSpace(request.JourReunion) && !IsValidJourReunion(request.JourReunion))
                {
                    validationErrors.Add("Le jour de réunion doit être un jour de la semaine valide");
                }

                // Validation du numéro de club (seulement s'il est fourni)
                if (request.NumeroClub.HasValue && request.NumeroClub <= 0)
                {
                    validationErrors.Add("Le numéro de club doit être un nombre positif");
                }

                if (validationErrors.Any())
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Erreurs de validation",
                        Errors = validationErrors
                    });
                }

                // Vérifier si le club existe
                var club = await _context.Clubs.FindAsync(id);
                if (club == null)
                {
                    return NotFound(new { Success = false, Message = "Club non trouvé." });
                }

                // Vérifier les permissions
                if (!await CanManageClub(id))
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Seuls les administrateurs et les présidents peuvent modifier les informations du club."
                    });
                }

                // Vérifier si le numéro du club est unique (seulement s'il est fourni et différent de l'actuel)
                if (request.NumeroClub.HasValue && request.NumeroClub != club.NumeroClub)
                {
                    var existingClubWithNumber = await _context.Clubs
                        .Where(c => c.NumeroClub == request.NumeroClub && c.Id != id)
                        .FirstOrDefaultAsync();

                    if (existingClubWithNumber != null)
                    {
                        return BadRequest(new
                        {
                            Success = false,
                            Message = $"Un autre club utilise déjà le numéro '{request.NumeroClub}'"
                        });
                    }
                }

                // Vérifier si l'email est unique (seulement s'il est fourni et différent de l'actuel)
                if (!string.IsNullOrWhiteSpace(request.Email) &&
                    !string.Equals(request.Email.Trim(), club.Email, StringComparison.OrdinalIgnoreCase))
                {
                    var existingClubWithEmail = await _context.Clubs
                        .Where(c => c.Email == request.Email.Trim().ToLower() && c.Id != id)
                        .FirstOrDefaultAsync();

                    if (existingClubWithEmail != null)
                    {
                        return BadRequest(new
                        {
                            Success = false,
                            Message = $"Un autre club utilise déjà l'email '{request.Email}'"
                        });
                    }
                }

                // Mettre à jour toutes les informations du club
                club.Name = request.Name.Trim();
                club.DateCreation = request.DateCreation?.Trim();
                club.NumeroClub = request.NumeroClub;
                club.NumeroTelephone = request.NumeroTelephone?.Trim();
                club.Email = request.Email?.Trim().ToLower();
                club.LieuReunion = request.LieuReunion?.Trim();
                club.ParrainePar = request.ParrainePar?.Trim();
                club.JourReunion = request.JourReunion?.Trim();
                club.HeureReunion = request.HeureReunion;
                club.Frequence = request.Frequence?.Trim();
                club.Adresse = request.Adresse?.Trim();

                await _context.SaveChangesAsync();

                _logger.LogInformation("Informations du club {ClubId} mises à jour avec succès", id);

                return Ok(new
                {
                    Success = true,
                    Message = "Informations du club mises à jour avec succès.",
                    Club = club
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClubExists(id))
                {
                    return NotFound(new { Success = false, Message = "Club non trouvé lors de la sauvegarde." });
                }
                else
                {
                    return Conflict(new
                    {
                        Success = false,
                        Message = "Le club a été modifié par un autre utilisateur. Veuillez recharger et réessayer."
                    });
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erreur de base de données lors de la mise à jour du club {ClubId}", id);
                return BadRequest(new
                {
                    Success = false,
                    Message = "Erreur lors de la sauvegarde en base de données.",
                    DetailedError = dbEx.InnerException?.Message ?? dbEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour des informations du club {ClubId}", id);
                return BadRequest(new
                {
                    Success = false,
                    Message = "Une erreur inattendue s'est produite lors de la mise à jour du club.",
                    DetailedError = ex.Message
                });
            }
        }

        // Supprimer un club (seulement pour les administrateurs)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteClub(Guid id)
        {
            try
            {
                var club = await _context.Clubs.FindAsync(id);
                if (club == null)
                {
                    return NotFound();
                }

                // Vérifier si des utilisateurs sont liés à ce club via UserClubs
                var hasUsers = await _context.UserClubs.AnyAsync(uc => uc.ClubId == id);
                if (hasUsers)
                {
                    return BadRequest("Impossible de supprimer un club qui a des membres. Veuillez d'abord désaffecter tous les membres.");
                }

                // Vérifier si des commissions sont liées à ce club
                var hasCommissions = await _context.Commissions.AnyAsync(c => c.ClubId == id);
                if (hasCommissions)
                {
                    return BadRequest("Impossible de supprimer un club qui a des commissions. Veuillez d'abord supprimer toutes les commissions.");
                }

                _context.Clubs.Remove(club);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Club {ClubId} supprimé avec succès", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du club {ClubId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la suppression du club");
            }
        }

        // Méthodes privées d'aide
        private bool ClubExists(Guid id)
        {
            return _context.Clubs.Any(e => e.Id == id);
        }

        private async Task<bool> CanAccessClub(Guid clubId)
        {
            if (User.IsInRole("Admin"))
                return true;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return false;

            // Vérifier si l'utilisateur a une relation active avec ce club
            var hasAccess = await _context.UserClubs
                .AnyAsync(uc => uc.UserId == userId && uc.ClubId == clubId);

            return hasAccess;
        }

        private async Task<bool> CanManageClub(Guid clubId)
        {
            if (User.IsInRole("Admin"))
                return true;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return false;

            // Vérifier si l'utilisateur appartient au club
            var userInClub = await _context.UserClubs
                .AnyAsync(uc => uc.UserId == userId && uc.ClubId == clubId);

            if (!userInClub)
                return false;

            // Vérifier si l'utilisateur a le rôle Président
            return User.IsInRole("President");
        }
    }

    // DTOs pour les requêtes
    public class CreateClubRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? DateCreation { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Le numéro de club doit être un nombre positif")]
        public int? NumeroClub { get; set; }

        public string? NumeroTelephone { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? LieuReunion { get; set; }

        public string? ParrainePar { get; set; }

        public string? JourReunion { get; set; }

        public TimeSpan? HeureReunion { get; set; }

        public string? Frequence { get; set; }

        public string? Adresse { get; set; }
    }

    public class UpdateClubInfoRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? DateCreation { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Le numéro de club doit être un nombre positif")]
        public int? NumeroClub { get; set; }

        public string? NumeroTelephone { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? LieuReunion { get; set; }

        public string? ParrainePar { get; set; }

        public string? JourReunion { get; set; }

        public TimeSpan? HeureReunion { get; set; }

        public string? Frequence { get; set; }

        public string? Adresse { get; set; }
    }
}