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
    [Route("api/clubs/{clubId}/reunions/{reunionId}/ordres-du-jour")]
    [ApiController]
    [Authorize]
    public class OrdreDuJourController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantService _tenantService;
        private readonly ILogger<OrdreDuJourController> _logger;

        public OrdreDuJourController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ITenantService tenantService,
            ILogger<OrdreDuJourController> logger)
        {
            _context = context;
            _userManager = userManager;
            _tenantService = tenantService;
            _logger = logger;
        }

        // GET: api/clubs/{clubId}/reunions/{reunionId}/ordres-du-jour
        // Récupérer tous les ordres du jour d'une réunion
        [HttpGet]
        public async Task<IActionResult> GetOrdresDuJour(Guid clubId, Guid reunionId)
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

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que la réunion existe
                var reunion = await _context.Reunions
                    .Include(r => r.TypeReunion)
                    .FirstOrDefaultAsync(r => r.Id == reunionId);

                if (reunion == null)
                {
                    return NotFound("Réunion non trouvée");
                }

                // Récupérer tous les ordres du jour de cette réunion
                var ordresDuJour = await _context.OrdresDuJour
                    .Where(odj => odj.ReunionId == reunionId)
                    .OrderBy(odj => odj.Description)
                    .Select(odj => new OrdreDuJourDetailDto
                    {
                        Id = odj.Id,
                        Description = odj.Description,
                        ReunionId = odj.ReunionId,
                        Rapport = odj.Rapport
                    })
                    .ToListAsync();

                var response = new
                {
                    Reunion = new
                    {
                        Id = reunion.Id,
                        Date = reunion.Date,
                        TypeReunionLibelle = reunion.TypeReunion.Libelle
                    },
                    OrdresDuJour = ordresDuJour,
                    Statistiques = new
                    {
                        TotalOrdres = ordresDuJour.Count,
                        CaracteresTotaux = ordresDuJour.Sum(o => o.Description.Length)
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des ordres du jour de la réunion {ReunionId} du club {ClubId}",
                    reunionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des ordres du jour");
            }
        }

        // GET: api/clubs/{clubId}/reunions/{reunionId}/ordres-du-jour/{ordreDuJourId}
        // Récupérer un ordre du jour spécifique
        [HttpGet("{ordreDuJourId:guid}")]
        public async Task<IActionResult> GetOrdreDuJour(Guid clubId, Guid reunionId, Guid ordreDuJourId)
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

                _tenantService.SetCurrentTenantId(clubId);

                // Récupérer l'ordre du jour avec la réunion associée
                var ordreDuJour = await _context.OrdresDuJour
                    .Include(odj => odj.Reunion)
                        .ThenInclude(r => r.TypeReunion)
                    .FirstOrDefaultAsync(odj => odj.Id == ordreDuJourId &&
                                              odj.ReunionId == reunionId);

                if (ordreDuJour == null)
                {
                    return NotFound("Ordre du jour non trouvé");
                }

                var response = new OrdreDuJourCompletDto
                {
                    Id = ordreDuJour.Id,
                    Description = ordreDuJour.Description,
                    ReunionId = ordreDuJour.ReunionId,
                    Reunion = new OrdreDuJourReunionDto
                    {
                        Id = ordreDuJour.Reunion.Id,
                        Date = ordreDuJour.Reunion.Date,
                        TypeReunionLibelle = ordreDuJour.Reunion.TypeReunion.Libelle
                    },
                    Rapport = ordreDuJour.Rapport
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'ordre du jour {OrdreDuJourId} de la réunion {ReunionId}",
                    ordreDuJourId, reunionId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de l'ordre du jour");
            }
        }

        // POST: api/clubs/{clubId}/reunions/{reunionId}/ordres-du-jour
        // Ajouter un nouvel ordre du jour à une réunion
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> AjouterOrdreDuJour(
            Guid clubId,
            Guid reunionId,
            [FromBody] AjouterOrdreDuJourRequest request)
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

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que la réunion existe
                var reunion = await _context.Reunions
                    .Include(r => r.TypeReunion)
                    .FirstOrDefaultAsync(r => r.Id == reunionId);

                if (reunion == null)
                {
                    return NotFound("Réunion non trouvée");
                }

                // Vérifier qu'un ordre du jour identique n'existe pas déjà
                var ordreExistant = await _context.OrdresDuJour
                    .AnyAsync(odj => odj.ReunionId == reunionId &&
                                   odj.Description.ToLower().Trim() == request.Description.ToLower().Trim());

                if (ordreExistant)
                {
                    return BadRequest("Un ordre du jour avec cette description existe déjà pour cette réunion");
                }

                // Créer le nouvel ordre du jour
                var ordreDuJour = new OrdreDuJour
                {
                    Id = Guid.NewGuid(),
                    Description = request.Description.Trim(),
                    ReunionId = reunionId,
                    Rapport = request.Rapport
                };

                _context.OrdresDuJour.Add(ordreDuJour);
                await _context.SaveChangesAsync();

                var response = new OrdreDuJourDetailDto
                {
                    Id = ordreDuJour.Id,
                    Description = ordreDuJour.Description,
                    ReunionId = ordreDuJour.ReunionId,
                    Rapport = ordreDuJour.Rapport
                };

                _logger.LogInformation(
                    "Ordre du jour ajouté avec succès à la réunion {TypeReunion} du {Date} (Réunion ID: {ReunionId}, Ordre ID: {OrdreId})",
                    reunion.TypeReunion.Libelle,
                    reunion.Date,
                    reunionId,
                    ordreDuJour.Id
                );

                return CreatedAtAction(
                    nameof(GetOrdreDuJour),
                    new { clubId, reunionId, ordreDuJourId = ordreDuJour.Id },
                    response
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout de l'ordre du jour à la réunion {ReunionId} du club {ClubId}",
                    reunionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de l'ajout de l'ordre du jour");
            }
        }

        // PUT: api/clubs/{clubId}/reunions/{reunionId}/ordres-du-jour/{ordreDuJourId}
        // Modifier un ordre du jour existant
        [HttpPut("{ordreDuJourId:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> ModifierOrdreDuJour(
            Guid clubId,
            Guid reunionId,
            Guid ordreDuJourId,
            [FromBody] ModifierOrdreDuJourRequest request)
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

                _tenantService.SetCurrentTenantId(clubId);

                // Récupérer l'ordre du jour
                var ordreDuJour = await _context.OrdresDuJour
                    .Include(odj => odj.Reunion)
                        .ThenInclude(r => r.TypeReunion)
                    .FirstOrDefaultAsync(odj => odj.Id == ordreDuJourId &&
                                              odj.ReunionId == reunionId);

                if (ordreDuJour == null)
                {
                    return NotFound("Ordre du jour non trouvé");
                }

                // Vérifier l'unicité de la nouvelle description (si différente)
                if (!string.Equals(ordreDuJour.Description.Trim(), request.Description.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    var ordreExistant = await _context.OrdresDuJour
                        .AnyAsync(odj => odj.ReunionId == reunionId &&
                                       odj.Description.ToLower().Trim() == request.Description.ToLower().Trim() &&
                                       odj.Id != ordreDuJourId);

                    if (ordreExistant)
                    {
                        return BadRequest("Un autre ordre du jour avec cette description existe déjà pour cette réunion");
                    }
                }

                // Mettre à jour la description
                var ancienneDescription = ordreDuJour.Description;
                ordreDuJour.Description = request.Description.Trim();

                // Mettre à jour le rapport si différent
                if (!string.Equals(ordreDuJour.Rapport, request.Rapport))
                {
                    ordreDuJour.Rapport = request.Rapport;
                }

                _context.Entry(ordreDuJour).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Ordre du jour modifié avec succès (ID: {OrdreId}) pour la réunion {TypeReunion} du {Date} : '{AncienneDescription}' -> '{NouvelleDescription}'",
                    ordreDuJourId,
                    ordreDuJour.Reunion.TypeReunion.Libelle,
                    ordreDuJour.Reunion.Date,
                    ancienneDescription,
                    ordreDuJour.Description
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification de l'ordre du jour {OrdreDuJourId} de la réunion {ReunionId}",
                    ordreDuJourId, reunionId);
                return StatusCode(500, "Une erreur est survenue lors de la modification de l'ordre du jour");
            }
        }

        // DELETE: api/clubs/{clubId}/reunions/{reunionId}/ordres-du-jour/{ordreDuJourId}
        // Supprimer un ordre du jour
        [HttpDelete("{ordreDuJourId:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> SupprimerOrdreDuJour(Guid clubId, Guid reunionId, Guid ordreDuJourId)
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

                _tenantService.SetCurrentTenantId(clubId);

                // Récupérer l'ordre du jour avec les informations de la réunion
                var ordreDuJour = await _context.OrdresDuJour
                    .Include(odj => odj.Reunion)
                        .ThenInclude(r => r.TypeReunion)
                    .FirstOrDefaultAsync(odj => odj.Id == ordreDuJourId &&
                                              odj.ReunionId == reunionId);

                if (ordreDuJour == null)
                {
                    return NotFound("Ordre du jour non trouvé");
                }

                // Sauvegarder les informations pour le log
                var descriptionSupprimer = ordreDuJour.Description;
                var infoReunion = $"{ordreDuJour.Reunion.TypeReunion.Libelle} du {ordreDuJour.Reunion.Date:dd/MM/yyyy HH:mm}";

                // Supprimer l'ordre du jour
                _context.OrdresDuJour.Remove(ordreDuJour);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Ordre du jour supprimé avec succès : '{Description}' (ID: {OrdreId}) de la réunion {InfoReunion}",
                    descriptionSupprimer,
                    ordreDuJourId,
                    infoReunion
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'ordre du jour {OrdreDuJourId} de la réunion {ReunionId}",
                    ordreDuJourId, reunionId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de l'ordre du jour");
            }
        }

        // POST: api/clubs/{clubId}/reunions/{reunionId}/ordres-du-jour/batch
        // Ajouter plusieurs ordres du jour en une seule opération
        [HttpPost("batch")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> AjouterOrdresDuJourBatch(
            Guid clubId,
            Guid reunionId,
            [FromBody] AjouterOrdresDuJourBatchRequest request)
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

                if (request.Descriptions == null || !request.Descriptions.Any())
                {
                    return BadRequest("Au moins une description d'ordre du jour est requise");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que la réunion existe
                var reunion = await _context.Reunions
                    .Include(r => r.TypeReunion)
                    .FirstOrDefaultAsync(r => r.Id == reunionId);

                if (reunion == null)
                {
                    return NotFound("Réunion non trouvée");
                }

                // Nettoyer les descriptions et vérifier les doublons dans la requête
                var descriptionsNettoyees = request.Descriptions
                    .Select(d => d.Trim())
                    .Where(d => !string.IsNullOrEmpty(d))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (!descriptionsNettoyees.Any())
                {
                    return BadRequest("Aucune description valide fournie");
                }

                // Vérifier les ordres du jour existants
                var ordresExistants = await _context.OrdresDuJour
                    .Where(odj => odj.ReunionId == reunionId)
                    .Select(odj => odj.Description.ToLower().Trim())
                    .ToListAsync();

                var descriptionsNouvelles = descriptionsNettoyees
                    .Where(d => !ordresExistants.Contains(d.ToLower()))
                    .ToList();

                if (!descriptionsNouvelles.Any())
                {
                    return BadRequest("Tous les ordres du jour fournis existent déjà pour cette réunion");
                }

                // Créer les nouveaux ordres du jour
                var nouveauxOrdres = descriptionsNouvelles.Select(description => new OrdreDuJour
                {
                    Id = Guid.NewGuid(),
                    Description = description,
                    ReunionId = reunionId
                }).ToList();

                _context.OrdresDuJour.AddRange(nouveauxOrdres);
                await _context.SaveChangesAsync();

                var response = new
                {
                    OrdresAjoutes = nouveauxOrdres.Select(o => new OrdreDuJourDetailDto
                    {
                        Id = o.Id,
                        Description = o.Description,
                        ReunionId = o.ReunionId
                    }).ToList(),
                    Statistiques = new
                    {
                        NombreAjoutes = nouveauxOrdres.Count,
                        NombreIgnores = request.Descriptions.Count() - nouveauxOrdres.Count,
                        TotalOrdresReunion = await _context.OrdresDuJour.CountAsync(odj => odj.ReunionId == reunionId)
                    }
                };

                _logger.LogInformation(
                    "Ajout en lot de {NombreAjoutes} ordres du jour à la réunion {TypeReunion} du {Date} (Réunion ID: {ReunionId})",
                    nouveauxOrdres.Count,
                    reunion.TypeReunion.Libelle,
                    reunion.Date,
                    reunionId
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout en lot des ordres du jour à la réunion {ReunionId} du club {ClubId}",
                    reunionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de l'ajout des ordres du jour");
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

    // DTOs pour les ordres du jour
    public class OrdreDuJourDetailDto
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public Guid ReunionId { get; set; }
        public string? Rapport { get; set; } // Ajouté
    }

    public class OrdreDuJourCompletDto : OrdreDuJourDetailDto
    {
        public OrdreDuJourReunionDto Reunion { get; set; } = null!;
    }

    public class OrdreDuJourReunionDto
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string TypeReunionLibelle { get; set; } = string.Empty;
    }

    public class AjouterOrdreDuJourRequest
    {
        public string Description { get; set; } = string.Empty;
        public string? Rapport { get; set; } // Ajouté
    }

    public class ModifierOrdreDuJourRequest
    {
        public string Description { get; set; } = string.Empty;
        public string? Rapport { get; set; } // Ajouté
    }

    public class AjouterOrdresDuJourBatchRequest
    {
        public IEnumerable<string> Descriptions { get; set; } = new List<string>();
    }
}