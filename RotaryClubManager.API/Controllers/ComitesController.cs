using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using RotaryClubManager.Infrastructure.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/clubs/{clubId}/comites")]
    [ApiController]
    [Authorize]
    public class ComitesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantService _tenantService;
        private readonly ILogger<ComitesController> _logger;

        public ComitesController(
            ApplicationDbContext context,
            ITenantService tenantService,
            ILogger<ComitesController> logger)
        {
            _context = context;
            _tenantService = tenantService;
            _logger = logger;
        }

        // GET: api/clubs/{clubId}/comites
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ComiteDetailDto>>> GetComites(Guid clubId, [FromQuery] Guid? mandatId = null)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                // Construire la requête avec la relation directe au club
                var query = _context.Comites
                    .Include(c => c.Mandat)
                    .Include(c => c.Club)
                    .Where(c => c.ClubId == clubId);

                // Filtrer par mandat si spécifié
                if (mandatId.HasValue)
                {
                    query = query.Where(c => c.MandatId == mandatId.Value);
                }

                var comites = await query
                    .OrderByDescending(c => c.Mandat.Annee)
                    .ThenBy(c => c.Nom)
                    .Select(c => new ComiteDetailDto
                    {
                        Id = c.Id,
                        Nom = c.Nom,
                        ClubId = c.ClubId,
                        ClubNom = c.Club.Name,
                        MandatId = c.MandatId,
                        MandatAnnee = c.Mandat.Annee,
                        MandatDescription = c.Mandat.Description,
                        MandatPeriodeComplete = c.Mandat.PeriodeComplete
                    })
                    .ToListAsync();

                return Ok(comites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des comités du club {ClubId}", clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des comités");
            }
        }

        // GET: api/clubs/{clubId}/comites/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ComiteDetailDto>> GetComite(Guid clubId, Guid id)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant du comité est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                var comite = await _context.Comites
                    .Include(c => c.Mandat)
                    .Include(c => c.Club)
                    .Where(c => c.Id == id && c.ClubId == clubId)
                    .Select(c => new ComiteDetailDto
                    {
                        Id = c.Id,
                        Nom = c.Nom,
                        ClubId = c.ClubId,
                        ClubNom = c.Club.Name,
                        MandatId = c.MandatId,
                        MandatAnnee = c.Mandat.Annee,
                        MandatDescription = c.Mandat.Description,
                        MandatPeriodeComplete = c.Mandat.PeriodeComplete
                    })
                    .FirstOrDefaultAsync();

                if (comite == null)
                {
                    return NotFound($"Comité avec l'ID {id} non trouvé dans le club {clubId}");
                }

                return Ok(comite);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du comité {ComiteId} du club {ClubId}", id, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du comité");
            }
        }

        // POST: api/clubs/{clubId}/comites
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<ActionResult<ComiteDetailDto>> PostComite(Guid clubId, [FromBody] CreateComiteRequest request)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que le club existe
                var club = await _context.Clubs.FindAsync(clubId);
                if (club == null)
                {
                    return NotFound("Club non trouvé");
                }

                // Vérifier que le mandat existe et appartient au club
                var mandat = await _context.Mandats
                    .FirstOrDefaultAsync(m => m.Id == request.MandatId && m.ClubId == clubId);

                if (mandat == null)
                {
                    return BadRequest("Mandat non trouvé pour ce club");
                }

                // Vérifier l'unicité du nom dans le club/mandat
                var existingComite = await _context.Comites
                    .AnyAsync(c => c.Nom.ToLower() == request.Nom.ToLower() &&
                                 c.ClubId == clubId &&
                                 c.MandatId == request.MandatId);

                if (existingComite)
                {
                    return BadRequest($"Un comité avec le nom '{request.Nom}' existe déjà pour ce mandat dans ce club");
                }

                // Créer le comité
                var comite = new Comite
                {
                    Id = Guid.NewGuid(),
                    Nom = request.Nom,
                    ClubId = clubId,
                    MandatId = request.MandatId
                };

                _context.Comites.Add(comite);
                await _context.SaveChangesAsync();

                // Charger les données pour la réponse
                var comiteCree = await _context.Comites
                    .Include(c => c.Mandat)
                    .Include(c => c.Club)
                    .Where(c => c.Id == comite.Id)
                    .Select(c => new ComiteDetailDto
                    {
                        Id = c.Id,
                        Nom = c.Nom,
                        ClubId = c.ClubId,
                        ClubNom = c.Club.Name,
                        MandatId = c.MandatId,
                        MandatAnnee = c.Mandat.Annee,
                        MandatDescription = c.Mandat.Description,
                        MandatPeriodeComplete = c.Mandat.PeriodeComplete
                    })
                    .FirstOrDefaultAsync();

                _logger.LogInformation("Comité {ComiteNom} créé pour le club {ClubId} dans le mandat {MandatAnnee}",
                    request.Nom, clubId, mandat.Annee);

                return CreatedAtAction(
                    nameof(GetComite),
                    new { clubId, id = comite.Id },
                    comiteCree
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du comité pour le club {ClubId}", clubId);
                return StatusCode(500, "Une erreur est survenue lors de la création du comité");
            }
        }

        // PUT: api/clubs/{clubId}/comites/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> PutComite(Guid clubId, Guid id, [FromBody] UpdateComiteRequest request)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant du comité est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                var comite = await _context.Comites
                    .Include(c => c.Mandat)
                    .Include(c => c.Club)
                    .FirstOrDefaultAsync(c => c.Id == id && c.ClubId == clubId);

                if (comite == null)
                {
                    return NotFound($"Comité avec l'ID {id} non trouvé dans le club {clubId}");
                }

                // Vérifier l'unicité du nom si modifié
                if (!string.IsNullOrEmpty(request.Nom) &&
                    request.Nom.ToLower() != comite.Nom.ToLower())
                {
                    var existingComite = await _context.Comites
                        .AnyAsync(c => c.Nom.ToLower() == request.Nom.ToLower() &&
                                     c.ClubId == clubId &&
                                     c.MandatId == comite.MandatId &&
                                     c.Id != id);

                    if (existingComite)
                    {
                        return BadRequest($"Un comité avec le nom '{request.Nom}' existe déjà pour ce mandat dans ce club");
                    }
                }

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.Nom))
                    comite.Nom = request.Nom;

                _context.Entry(comite).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Comité {ComiteId} mis à jour avec succès", id);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ComiteExists(id, clubId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du comité {ComiteId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour du comité");
            }
        }

        // DELETE: api/clubs/{clubId}/comites/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> DeleteComite(Guid clubId, Guid id)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant du comité est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                var comite = await _context.Comites
                    .FirstOrDefaultAsync(c => c.Id == id && c.ClubId == clubId);

                if (comite == null)
                {
                    return NotFound($"Comité avec l'ID {id} non trouvé dans le club {clubId}");
                }

                _context.Comites.Remove(comite);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Comité {ComiteId} supprimé du club {ClubId}", id, clubId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du comité {ComiteId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la suppression du comité");
            }
        }

        // GET: api/clubs/{clubId}/comites/by-mandat/{mandatId}
        [HttpGet("by-mandat/{mandatId:guid}")]
        public async Task<ActionResult<IEnumerable<ComiteDetailDto>>> GetComitesByMandat(Guid clubId, Guid mandatId)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (mandatId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du mandat est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que le mandat appartient au club
                var mandatExists = await _context.Mandats
                    .AnyAsync(m => m.Id == mandatId && m.ClubId == clubId);

                if (!mandatExists)
                {
                    return NotFound($"Mandat avec l'ID {mandatId} non trouvé dans le club {clubId}");
                }

                var comites = await _context.Comites
                    .Include(c => c.Mandat)
                    .Include(c => c.Club)
                    .Where(c => c.ClubId == clubId && c.MandatId == mandatId)
                    .OrderBy(c => c.Nom)
                    .Select(c => new ComiteDetailDto
                    {
                        Id = c.Id,
                        Nom = c.Nom,
                        ClubId = c.ClubId,
                        ClubNom = c.Club.Name,
                        MandatId = c.MandatId,
                        MandatAnnee = c.Mandat.Annee,
                        MandatDescription = c.Mandat.Description,
                        MandatPeriodeComplete = c.Mandat.PeriodeComplete
                    })
                    .ToListAsync();

                return Ok(comites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des comités du mandat {MandatId}", mandatId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des comités");
            }
        }

        // Méthodes d'aide
        private async Task<bool> ComiteExists(Guid id, Guid clubId)
        {
            return await _context.Comites
                .AnyAsync(c => c.Id == id && c.ClubId == clubId);
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

            return User.IsInRole("President") || User.IsInRole("Secretary");
        }
    }

    // DTOs mis à jour
    public class ComiteDetailDto
    {
        public Guid Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public Guid ClubId { get; set; }
        public string ClubNom { get; set; } = string.Empty;
        public Guid MandatId { get; set; }
        public int MandatAnnee { get; set; }
        public string? MandatDescription { get; set; }
        public string MandatPeriodeComplete { get; set; } = string.Empty;
    }

    public class CreateComiteRequest
    {
        [Required]
        [MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Required]
        public Guid MandatId { get; set; }
    }

    public class UpdateComiteRequest
    {
        [MaxLength(100)]
        public string? Nom { get; set; }
    }
}