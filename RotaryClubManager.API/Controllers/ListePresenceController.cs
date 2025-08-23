using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Domain.Identity;
using RotaryClubManager.Infrastructure.Data;
using RotaryClubManager.Infrastructure.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/clubs/{clubId}/reunions/{reunionId}/presences")]
    [ApiController]
    [Authorize]
    public class ListePresenceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantService _tenantService;
        private readonly ILogger<ListePresenceController> _logger;

        public ListePresenceController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ITenantService tenantService,
            ILogger<ListePresenceController> logger)
        {
            _context = context;
            _userManager = userManager;
            _tenantService = tenantService;
            _logger = logger;
        }

        // GET: api/clubs/{clubId}/reunions/{reunionId}/presences
        // Récupérer la liste de présence d'une réunion
        [HttpGet]
        public async Task<IActionResult> GetListePresence(Guid clubId, Guid reunionId)
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

                // Récupérer la liste de présence
                var presences = await _context.ListesPresence
                    .Include(lp => lp.Membre)
                    .Where(lp => lp.ReunionId == reunionId)
                    .OrderBy(lp => lp.Membre.LastName)
                    .ThenBy(lp => lp.Membre.FirstName)
                    .Select(lp => new PresenceDetailDto
                    {
                        Id = lp.Id,
                        MembreId = lp.MembreId,
                        NomCompletMembre = $"{lp.Membre.FirstName} {lp.Membre.LastName}",
                        EmailMembre = lp.Membre.Email,
                        EstActifMembre = lp.Membre.IsActive,
                        ReunionId = lp.ReunionId
                    })
                    .ToListAsync();

                // Récupérer les membres du club qui ne sont pas présents
                var membresPresents = presences.Select(p => p.MembreId).ToList();
                var membresAbsents = await _context.Users
                    .Where(u => _context.UserClubs.Any(uc => uc.UserId == u.Id && uc.ClubId == clubId) &&
                              u.IsActive &&
                              !membresPresents.Contains(u.Id))
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .Select(u => new MembreAbsentDto
                    {
                        Id = u.Id,
                        NomComplet = $"{u.FirstName} {u.LastName}",
                        Email = u.Email
                    })
                    .ToListAsync();

                var totalMembresActifs = await _context.Users
                    .CountAsync(u => _context.UserClubs.Any(uc => uc.UserId == u.Id && uc.ClubId == clubId) && u.IsActive);

                var response = new
                {
                    Reunion = new
                    {
                        Id = reunion.Id,
                        Date = reunion.Date,
                        TypeReunionLibelle = reunion.TypeReunion.Libelle
                    },
                    Presences = presences,
                    MembresAbsents = membresAbsents,
                    Statistiques = new
                    {
                        TotalMembresActifs = totalMembresActifs,
                        NombrePresents = presences.Count,
                        NombreAbsents = membresAbsents.Count,
                        TauxPresence = totalMembresActifs > 0
                            ? Math.Round((double)presences.Count / totalMembresActifs * 100, 1)
                            : 0.0
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la liste de présence de la réunion {ReunionId} du club {ClubId}",
                    reunionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de la liste de présence");
            }
        }

        // GET: api/clubs/{clubId}/reunions/{reunionId}/presences/{presenceId}
        // Récupérer une présence spécifique
        [HttpGet("{presenceId:guid}")]
        public async Task<IActionResult> GetPresence(Guid clubId, Guid reunionId, Guid presenceId)
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

                if (presenceId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la présence est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                // Récupérer la présence avec les détails
                var presence = await _context.ListesPresence
                    .Include(lp => lp.Membre)
                    .Include(lp => lp.Reunion)
                        .ThenInclude(r => r.TypeReunion)
                    .FirstOrDefaultAsync(lp => lp.Id == presenceId &&
                                             lp.ReunionId == reunionId);

                if (presence == null)
                {
                    return NotFound("Présence non trouvée");
                }

                var response = new PresenceCompletDto
                {
                    Id = presence.Id,
                    MembreId = presence.MembreId,
                    NomCompletMembre = $"{presence.Membre.FirstName} {presence.Membre.LastName}",
                    EmailMembre = presence.Membre.Email,
                    EstActifMembre = presence.Membre.IsActive,
                    ReunionId = presence.ReunionId,
                    Reunion = new ReunionBasicDto
                    {
                        Id = presence.Reunion.Id,
                        Date = presence.Reunion.Date,
                        TypeReunionLibelle = presence.Reunion.TypeReunion.Libelle
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la présence {PresenceId} de la réunion {ReunionId}",
                    presenceId, reunionId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de la présence");
            }
        }

        // POST: api/clubs/{clubId}/reunions/{reunionId}/presences
        // Marquer un membre comme présent
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> MarquerPresence(
            Guid clubId,
            Guid reunionId,
            [FromBody] MarquerPresenceRequest request)
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

                // Vérifier que le membre existe et appartient au club
                var membre = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == request.MembreId &&
                                            _context.UserClubs.Any(uc => uc.UserId == u.Id && uc.ClubId == clubId) &&
                                            u.IsActive);

                if (membre == null)
                {
                    return NotFound("Membre non trouvé dans ce club ou membre inactif");
                }

                // Vérifier si le membre n'est pas déjà marqué comme présent
                var presenceExistante = await _context.ListesPresence
                    .FirstOrDefaultAsync(lp => lp.MembreId == request.MembreId &&
                                             lp.ReunionId == reunionId);

                if (presenceExistante != null)
                {
                    return BadRequest($"Le membre {membre.FirstName} {membre.LastName} est déjà marqué comme présent à cette réunion");
                }

                // Créer la nouvelle présence
                var presence = new ListePresence
                {
                    Id = Guid.NewGuid(),
                    MembreId = request.MembreId,
                    ReunionId = reunionId
                };

                _context.ListesPresence.Add(presence);
                await _context.SaveChangesAsync();

                var response = new PresenceDetailDto
                {
                    Id = presence.Id,
                    MembreId = presence.MembreId,
                    NomCompletMembre = $"{membre.FirstName} {membre.LastName}",
                    EmailMembre = membre.Email,
                    EstActifMembre = membre.IsActive,
                    ReunionId = presence.ReunionId
                };

                _logger.LogInformation(
                    "Présence marquée avec succès : {MembreNom} à la réunion {TypeReunion} du {Date} (Présence ID: {PresenceId})",
                    $"{membre.FirstName} {membre.LastName}",
                    reunion.TypeReunion.Libelle,
                    reunion.Date,
                    presence.Id
                );

                return CreatedAtAction(
                    nameof(GetPresence),
                    new { clubId, reunionId, presenceId = presence.Id },
                    response
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du marquage de présence pour la réunion {ReunionId} du club {ClubId}",
                    reunionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors du marquage de présence");
            }
        }

        // DELETE: api/clubs/{clubId}/reunions/{reunionId}/presences/{presenceId}
        // Supprimer une présence (marquer comme absent)
        [HttpDelete("{presenceId:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> SupprimerPresence(Guid clubId, Guid reunionId, Guid presenceId)
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

                if (presenceId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la présence est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                // Récupérer la présence avec les détails
                var presence = await _context.ListesPresence
                    .Include(lp => lp.Membre)
                    .Include(lp => lp.Reunion)
                        .ThenInclude(r => r.TypeReunion)
                    .FirstOrDefaultAsync(lp => lp.Id == presenceId &&
                                             lp.ReunionId == reunionId);

                if (presence == null)
                {
                    return NotFound("Présence non trouvée");
                }

                // Sauvegarder les informations pour le log
                var membreNom = $"{presence.Membre.FirstName} {presence.Membre.LastName}";
                var infoReunion = $"{presence.Reunion.TypeReunion.Libelle} du {presence.Reunion.Date:dd/MM/yyyy HH:mm}";

                // Supprimer la présence
                _context.ListesPresence.Remove(presence);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Présence supprimée avec succès : {MembreNom} retiré de la réunion {InfoReunion} (Présence ID: {PresenceId})",
                    membreNom,
                    infoReunion,
                    presenceId
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la présence {PresenceId} de la réunion {ReunionId}",
                    presenceId, reunionId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de la présence");
            }
        }

        // DELETE: api/clubs/{clubId}/reunions/{reunionId}/presences/by-membre/{membreId}
        // Supprimer une présence par ID de membre
        [HttpDelete("by-membre/{membreId}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> SupprimerPresenceParMembre(Guid clubId, Guid reunionId, string membreId)
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

                if (string.IsNullOrEmpty(membreId))
                {
                    return BadRequest("L'identifiant du membre est requis");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                // Récupérer la présence
                var presence = await _context.ListesPresence
                    .Include(lp => lp.Membre)
                    .Include(lp => lp.Reunion)
                        .ThenInclude(r => r.TypeReunion)
                    .FirstOrDefaultAsync(lp => lp.MembreId == membreId &&
                                             lp.ReunionId == reunionId);

                if (presence == null)
                {
                    return NotFound("Aucune présence trouvée pour ce membre à cette réunion");
                }

                // Sauvegarder les informations pour le log
                var membreNom = $"{presence.Membre.FirstName} {presence.Membre.LastName}";
                var infoReunion = $"{presence.Reunion.TypeReunion.Libelle} du {presence.Reunion.Date:dd/MM/yyyy HH:mm}";

                // Supprimer la présence
                _context.ListesPresence.Remove(presence);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Présence supprimée par membre ID : {MembreNom} retiré de la réunion {InfoReunion}",
                    membreNom,
                    infoReunion
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la présence du membre {MembreId} de la réunion {ReunionId}",
                    membreId, reunionId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de la présence");
            }
        }

        // POST: api/clubs/{clubId}/reunions/{reunionId}/presences/batch
        // Marquer plusieurs membres comme présents en une seule opération
        [HttpPost("batch")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> MarquerPresencesBatch(
            Guid clubId,
            Guid reunionId,
            [FromBody] MarquerPresencesBatchRequest request)
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

                if (request.MembresIds == null || !request.MembresIds.Any())
                {
                    return BadRequest("Au moins un ID de membre est requis");
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

                // Récupérer les IDs uniques
                var membresIdsUniques = request.MembresIds.Distinct().ToList();

                // Vérifier que tous les membres existent et appartiennent au club
                var membres = await _context.Users
                    .Where(u => membresIdsUniques.Contains(u.Id) &&
                              _context.UserClubs.Any(uc => uc.UserId == u.Id && uc.ClubId == clubId) &&
                              u.IsActive)
                    .ToListAsync();

                var membresInvalides = membresIdsUniques.Except(membres.Select(m => m.Id)).ToList();

                // Récupérer les présences déjà existantes
                var presencesExistantes = await _context.ListesPresence
                    .Where(lp => lp.ReunionId == reunionId &&
                               membresIdsUniques.Contains(lp.MembreId))
                    .Select(lp => lp.MembreId)
                    .ToListAsync();

                // Déterminer les nouveaux membres à marquer comme présents
                var nouveauxPresents = membres
                    .Where(m => !presencesExistantes.Contains(m.Id))
                    .ToList();

                // Créer les nouvelles présences
                var nouvellesPresences = nouveauxPresents.Select(membre => new ListePresence
                {
                    Id = Guid.NewGuid(),
                    MembreId = membre.Id,
                    ReunionId = reunionId
                }).ToList();

                _context.ListesPresence.AddRange(nouvellesPresences);
                await _context.SaveChangesAsync();

                var response = new
                {
                    PresencesAjoutees = nouvellesPresences.Select(p => new PresenceDetailDto
                    {
                        Id = p.Id,
                        MembreId = p.MembreId,
                        NomCompletMembre = $"{membres.First(m => m.Id == p.MembreId).FirstName} {membres.First(m => m.Id == p.MembreId).LastName}",
                        EmailMembre = membres.First(m => m.Id == p.MembreId).Email,
                        EstActifMembre = membres.First(m => m.Id == p.MembreId).IsActive,
                        ReunionId = p.ReunionId
                    }).ToList(),
                    Statistiques = new
                    {
                        MembresDemandesTraitement = membresIdsUniques.Count,
                        PresencesAjoutees = nouvellesPresences.Count,
                        DejaPresents = presencesExistantes.Count,
                        MembresInvalides = membresInvalides.Count,
                        MembresInvalidesIds = membresInvalides
                    }
                };

                _logger.LogInformation(
                    "Marquage de présences en lot : {NombreAjoutes} nouvelles présences ajoutées à la réunion {TypeReunion} du {Date}",
                    nouvellesPresences.Count,
                    reunion.TypeReunion.Libelle,
                    reunion.Date
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du marquage de présences en lot pour la réunion {ReunionId} du club {ClubId}",
                    reunionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors du marquage des présences");
            }
        }

        // GET: api/clubs/{clubId}/reunions/{reunionId}/presences/statistiques
        // Récupérer les statistiques de présence pour une réunion
        [HttpGet("statistiques")]
        public async Task<IActionResult> GetStatistiquesPresence(Guid clubId, Guid reunionId)
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

                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que la réunion existe
                var reunion = await _context.Reunions
                    .Include(r => r.TypeReunion)
                    .FirstOrDefaultAsync(r => r.Id == reunionId);

                if (reunion == null)
                {
                    return NotFound("Réunion non trouvée");
                }

                // Calculer les statistiques
                var totalMembresActifs = await _context.Users
                    .CountAsync(u => _context.UserClubs.Any(uc => uc.UserId == u.Id && uc.ClubId == clubId) && u.IsActive);

                var nombrePresents = await _context.ListesPresence
                    .CountAsync(lp => lp.ReunionId == reunionId);

                // Récupérer les statistiques de présence historiques pour ce type de réunion
                var presenceMoyenneTypeReunion = await _context.Reunions
                    .Where(r => r.TypeReunionId == reunion.TypeReunionId && r.Id != reunionId)
                    .Select(r => r.ListesPresence.Count())
                    .DefaultIfEmpty(0)
                    .AverageAsync();

                var statistiques = new
                {
                    Reunion = new
                    {
                        Id = reunion.Id,
                        Date = reunion.Date,
                        TypeReunionLibelle = reunion.TypeReunion.Libelle
                    },
                    TotalMembresActifs = totalMembresActifs,
                    NombrePresents = nombrePresents,
                    NombreAbsents = totalMembresActifs - nombrePresents,
                    TauxPresence = totalMembresActifs > 0
                        ? Math.Round((double)nombrePresents / totalMembresActifs * 100, 1)
                        : 0.0,
                    PresenceMoyenneTypeReunion = Math.Round(presenceMoyenneTypeReunion, 1),
                    PerformanceVsMoyenne = presenceMoyenneTypeReunion > 0
                        ? Math.Round((nombrePresents - presenceMoyenneTypeReunion) / presenceMoyenneTypeReunion * 100, 1)
                        : 0.0
                };

                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques de présence pour la réunion {ReunionId}",
                    reunionId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
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

    // DTOs pour les listes de présence
    public class PresenceDetailDto
    {
        public Guid Id { get; set; }
        public string MembreId { get; set; } = string.Empty;
        public string NomCompletMembre { get; set; } = string.Empty;
        public string EmailMembre { get; set; } = string.Empty;
        public bool EstActifMembre { get; set; }
        public Guid ReunionId { get; set; }
    }

    public class PresenceCompletDto : PresenceDetailDto
    {
        public ReunionBasicDto Reunion { get; set; } = null!;
    }

    public class ReunionBasicDto
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string TypeReunionLibelle { get; set; } = string.Empty;
    }

    public class MembreAbsentDto
    {
        public string Id { get; set; } = string.Empty;
        public string NomComplet { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class MarquerPresenceRequest
    {
        [Required(ErrorMessage = "L'identifiant du membre est obligatoire")]
        public string MembreId { get; set; } = string.Empty;
    }

    public class MarquerPresencesBatchRequest
    {
        [Required(ErrorMessage = "Au moins un ID de membre est requis")]
        public IEnumerable<string> MembresIds { get; set; } = new List<string>();
    }
}