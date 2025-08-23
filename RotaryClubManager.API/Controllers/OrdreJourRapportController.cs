using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Domain.Identity;
using RotaryClubManager.Infrastructure.Data;
using RotaryClubManager.Infrastructure.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/clubs/{clubId}/reunions/{reunionId}/ordres-du-jour/{ordreDuJourId}/rapports")]
    [ApiController]
    [Authorize]
    public class OrdreJourRapportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantService _tenantService;
        private readonly ILogger<OrdreJourRapportController> _logger;

        public OrdreJourRapportController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ITenantService tenantService,
            ILogger<OrdreJourRapportController> logger)
        {
            _context = context;
            _userManager = userManager;
            _tenantService = tenantService;
            _logger = logger;
        }

        // GET: api/clubs/{clubId}/reunions/{reunionId}/ordres-du-jour/{ordreDuJourId}/rapports
        // Récupérer tous les rapports d'un ordre du jour
        [HttpGet]
        public async Task<IActionResult> GetRapports(Guid clubId, Guid reunionId, Guid ordreDuJourId)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (reunionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la réunion est invalide");
                }

                if (ordreDuJourId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'ordre du jour est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que l'ordre du jour existe
                var ordreDuJour = await _context.OrdresDuJour
                    .Include(odj => odj.Reunion)
                        .ThenInclude(r => r.TypeReunion)
                    .FirstOrDefaultAsync(odj => odj.Id == ordreDuJourId &&
                                              odj.ReunionId == reunionId);

                if (ordreDuJour == null)
                {
                    return NotFound("Ordre du jour non trouvé");
                }

                // Récupérer tous les rapports de cet ordre du jour
                var rapports = await _context.OrdreJourRapports
                    .Where(r => r.OrdreDuJourId == ordreDuJourId)
                    .OrderBy(r => r.Texte)
                    .Select(r => new OrdreJourRapportDetailDto
                    {
                        Id = r.Id,
                        OrdreDuJourId = r.OrdreDuJourId,
                        Texte = r.Texte,
                        Divers = r.Divers
                    })
                    .ToListAsync();

                var response = new
                {
                    OrdreDuJour = new
                    {
                        Id = ordreDuJour.Id,
                        Description = ordreDuJour.Description,
                        Reunion = new
                        {
                            Id = ordreDuJour.Reunion.Id,
                            Date = ordreDuJour.Reunion.Date,
                            TypeReunionLibelle = ordreDuJour.Reunion.TypeReunion.Libelle
                        }
                    },
                    Rapports = rapports,
                    Statistiques = new
                    {
                        TotalRapports = rapports.Count,
                        CaracteresTotaux = rapports.Sum(r => r.Texte.Length + (r.Divers?.Length ?? 0))
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des rapports de l'ordre du jour {OrdreDuJourId}",
                    ordreDuJourId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des rapports");
            }
        }

        // GET: api/clubs/{clubId}/reunions/{reunionId}/ordres-du-jour/{ordreDuJourId}/rapports/{rapportId}
        // Récupérer un rapport spécifique
        [HttpGet("{rapportId:guid}")]
        public async Task<IActionResult> GetRapport(Guid clubId, Guid reunionId, Guid ordreDuJourId, Guid rapportId)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (reunionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la réunion est invalide");
                }

                if (ordreDuJourId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'ordre du jour est invalide");
                }

                if (rapportId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du rapport est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                // Récupérer le rapport avec toutes les informations associées
                var rapport = await _context.OrdreJourRapports
                    .Include(r => r.OrdreDuJour)
                        .ThenInclude(odj => odj.Reunion)
                            .ThenInclude(reunion => reunion.TypeReunion)
                    .FirstOrDefaultAsync(r => r.Id == rapportId &&
                                            r.OrdreDuJourId == ordreDuJourId &&
                                            r.OrdreDuJour.ReunionId == reunionId);

                if (rapport == null)
                {
                    return NotFound("Rapport non trouvé");
                }

                var response = new OrdreJourRapportCompletDto
                {
                    Id = rapport.Id,
                    OrdreDuJourId = rapport.OrdreDuJourId,
                    Texte = rapport.Texte,
                    Divers = rapport.Divers,
                    OrdreDuJour = new OrdreJourRapportOrdreDuJourDto
                    {
                        Id = rapport.OrdreDuJour.Id,
                        Description = rapport.OrdreDuJour.Description,
                        Reunion = new OrdreJourRapportReunionDto
                        {
                            Id = rapport.OrdreDuJour.Reunion.Id,
                            Date = rapport.OrdreDuJour.Reunion.Date,
                            TypeReunionLibelle = rapport.OrdreDuJour.Reunion.TypeReunion.Libelle
                        }
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du rapport {RapportId} de l'ordre du jour {OrdreDuJourId}",
                    rapportId, ordreDuJourId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du rapport");
            }
        }

        // POST: api/clubs/{clubId}/reunions/{reunionId}/ordres-du-jour/{ordreDuJourId}/rapports
        // Ajouter un nouveau rapport à un ordre du jour
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> AjouterRapport(
            Guid clubId,
            Guid reunionId,
            Guid ordreDuJourId,
            [FromBody] AjouterOrdreJourRapportRequest request)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (reunionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la réunion est invalide");
                }

                if (ordreDuJourId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'ordre du jour est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que l'ordre du jour existe
                var ordreDuJour = await _context.OrdresDuJour
                    .Include(odj => odj.Reunion)
                        .ThenInclude(r => r.TypeReunion)
                    .FirstOrDefaultAsync(odj => odj.Id == ordreDuJourId &&
                                              odj.ReunionId == reunionId);

                if (ordreDuJour == null)
                {
                    return NotFound("Ordre du jour non trouvé");
                }

                // Créer le nouveau rapport
                var rapport = new OrdreJourRapport
                {
                    Id = Guid.NewGuid(),
                    OrdreDuJourId = ordreDuJourId,
                    Texte = request.Texte.Trim(),
                    Divers = !string.IsNullOrWhiteSpace(request.Divers) ? request.Divers.Trim() : null
                };

                _context.OrdreJourRapports.Add(rapport);
                await _context.SaveChangesAsync();

                var response = new OrdreJourRapportDetailDto
                {
                    Id = rapport.Id,
                    OrdreDuJourId = rapport.OrdreDuJourId,
                    Texte = rapport.Texte,
                    Divers = rapport.Divers
                };

                _logger.LogInformation(
                    "Rapport ajouté avec succès à l'ordre du jour '{Description}' de la réunion {TypeReunion} du {Date} (Rapport ID: {RapportId})",
                    ordreDuJour.Description,
                    ordreDuJour.Reunion.TypeReunion.Libelle,
                    ordreDuJour.Reunion.Date,
                    rapport.Id
                );

                return CreatedAtAction(
                    nameof(GetRapport),
                    new { clubId, reunionId, ordreDuJourId, rapportId = rapport.Id },
                    response
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout du rapport à l'ordre du jour {OrdreDuJourId}",
                    ordreDuJourId);
                return StatusCode(500, "Une erreur est survenue lors de l'ajout du rapport");
            }
        }

        // PUT: api/clubs/{clubId}/reunions/{reunionId}/ordres-du-jour/{ordreDuJourId}/rapports/{rapportId}
        // Modifier un rapport existant
        [HttpPut("{rapportId:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> ModifierRapport(
            Guid clubId,
            Guid reunionId,
            Guid ordreDuJourId,
            Guid rapportId,
            [FromBody] ModifierOrdreJourRapportRequest request)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (reunionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la réunion est invalide");
                }

                if (ordreDuJourId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'ordre du jour est invalide");
                }

                if (rapportId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du rapport est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                // Récupérer le rapport
                var rapport = await _context.OrdreJourRapports
                    .Include(r => r.OrdreDuJour)
                        .ThenInclude(odj => odj.Reunion)
                            .ThenInclude(reunion => reunion.TypeReunion)
                    .FirstOrDefaultAsync(r => r.Id == rapportId &&
                                            r.OrdreDuJourId == ordreDuJourId &&
                                            r.OrdreDuJour.ReunionId == reunionId);

                if (rapport == null)
                {
                    return NotFound("Rapport non trouvé");
                }

                // Sauvegarder les anciennes valeurs pour le log
                var ancienTexte = rapport.Texte;
                var ancienDivers = rapport.Divers;

                // Mettre à jour les propriétés
                rapport.Texte = request.Texte.Trim();
                rapport.Divers = !string.IsNullOrWhiteSpace(request.Divers) ? request.Divers.Trim() : null;

                _context.Entry(rapport).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Rapport modifié avec succès (ID: {RapportId}) pour l'ordre du jour '{Description}' de la réunion {TypeReunion} du {Date}",
                    rapportId,
                    rapport.OrdreDuJour.Description,
                    rapport.OrdreDuJour.Reunion.TypeReunion.Libelle,
                    rapport.OrdreDuJour.Reunion.Date
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification du rapport {RapportId} de l'ordre du jour {OrdreDuJourId}",
                    rapportId, ordreDuJourId);
                return StatusCode(500, "Une erreur est survenue lors de la modification du rapport");
            }
        }

        // DELETE: api/clubs/{clubId}/reunions/{reunionId}/ordres-du-jour/{ordreDuJourId}/rapports/{rapportId}
        // Supprimer un rapport
        [HttpDelete("{rapportId:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> SupprimerRapport(Guid clubId, Guid reunionId, Guid ordreDuJourId, Guid rapportId)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (reunionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la réunion est invalide");
                }

                if (ordreDuJourId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'ordre du jour est invalide");
                }

                if (rapportId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du rapport est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                // Récupérer le rapport avec les informations associées
                var rapport = await _context.OrdreJourRapports
                    .Include(r => r.OrdreDuJour)
                        .ThenInclude(odj => odj.Reunion)
                            .ThenInclude(reunion => reunion.TypeReunion)
                    .FirstOrDefaultAsync(r => r.Id == rapportId &&
                                            r.OrdreDuJourId == ordreDuJourId &&
                                            r.OrdreDuJour.ReunionId == reunionId);

                if (rapport == null)
                {
                    return NotFound("Rapport non trouvé");
                }

                // Sauvegarder les informations pour le log
                var texteRapport = rapport.Texte;
                var infoOrdreDuJour = rapport.OrdreDuJour.Description;
                var infoReunion = $"{rapport.OrdreDuJour.Reunion.TypeReunion.Libelle} du {rapport.OrdreDuJour.Reunion.Date:dd/MM/yyyy}";

                // Supprimer le rapport
                _context.OrdreJourRapports.Remove(rapport);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Rapport supprimé avec succès (ID: {RapportId}) de l'ordre du jour '{OrdreDuJour}' de la réunion {Reunion}",
                    rapportId,
                    infoOrdreDuJour,
                    infoReunion
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du rapport {RapportId} de l'ordre du jour {OrdreDuJourId}",
                    rapportId, ordreDuJourId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression du rapport");
            }
        }

        // Méthodes d'aide pour vérifier les autorisations
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

            return User.IsInRole("President") || User.IsInRole("Secretary");
        }
    }

    // DTOs pour les rapports d'ordre du jour
    public class OrdreJourRapportDetailDto
    {
        public Guid Id { get; set; }
        public Guid OrdreDuJourId { get; set; }
        public string Texte { get; set; } = string.Empty;
        public string? Divers { get; set; }
    }

    public class OrdreJourRapportCompletDto : OrdreJourRapportDetailDto
    {
        public OrdreJourRapportOrdreDuJourDto OrdreDuJour { get; set; } = null!;
    }

    public class OrdreJourRapportOrdreDuJourDto
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public OrdreJourRapportReunionDto Reunion { get; set; } = null!;
    }

    public class OrdreJourRapportReunionDto
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string TypeReunionLibelle { get; set; } = string.Empty;
    }

    public class AjouterOrdreJourRapportRequest
    {
        [Required]
        public string Texte { get; set; } = string.Empty;

        public string? Divers { get; set; }
    }

    public class ModifierOrdreJourRapportRequest
    {
        [Required]
        public string Texte { get; set; } = string.Empty;

        public string? Divers { get; set; }
    }
}