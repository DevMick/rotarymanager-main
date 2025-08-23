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
    [Route("api/clubs/{clubId}/types-reunion")]
    [ApiController]
    [Authorize]
    public class TypeReunionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantService _tenantService;
        private readonly ILogger<TypeReunionController> _logger;

        public TypeReunionController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ITenantService tenantService,
            ILogger<TypeReunionController> logger)
        {
            _context = context;
            _userManager = userManager;
            _tenantService = tenantService;
            _logger = logger;
        }

        // GET: api/clubs/{clubId}/types-reunion
        // Récupérer tous les types de réunion
        [HttpGet]
        public async Task<IActionResult> GetTypesReunion(Guid clubId)
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

                // Récupérer tous les types de réunion
                var typesReunion = await _context.TypesReunion
                    .OrderBy(tr => tr.Libelle)
                    .Select(tr => new TypeReunionDto
                    {
                        Id = tr.Id,
                        Libelle = tr.Libelle,
                        NombreReunions = tr.Reunions.Count()
                    })
                    .ToListAsync();

                var response = new
                {
                    TypesReunion = typesReunion,
                    Statistiques = new
                    {
                        TotalTypes = typesReunion.Count,
                        TypesAvecReunions = typesReunion.Count(tr => tr.NombreReunions > 0),
                        TypesSansReunions = typesReunion.Count(tr => tr.NombreReunions == 0)
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des types de réunion pour le club {ClubId}", clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des types de réunion");
            }
        }

        // GET: api/clubs/{clubId}/types-reunion/{typeReunionId}
        // Récupérer un type de réunion spécifique avec ses détails
        [HttpGet("{typeReunionId:guid}")]
        public async Task<IActionResult> GetTypeReunion(Guid clubId, Guid typeReunionId)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (typeReunionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du type de réunion est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                // Récupérer le type de réunion avec ses réunions
                var typeReunion = await _context.TypesReunion
                    .Include(tr => tr.Reunions)
                    .FirstOrDefaultAsync(tr => tr.Id == typeReunionId);

                if (typeReunion == null)
                {
                    return NotFound("Type de réunion non trouvé");
                }

                var response = new TypeReunionDetailDto
                {
                    Id = typeReunion.Id,
                    Libelle = typeReunion.Libelle,
                    NombreReunions = typeReunion.Reunions.Count,
                    DerniereReunion = typeReunion.Reunions.Any()
                        ? typeReunion.Reunions.OrderByDescending(r => r.Date).First().Date
                        : (DateTime?)null,
                    ProchaineReunion = typeReunion.Reunions.Any(r => r.Date > DateTime.Now)
                        ? typeReunion.Reunions.Where(r => r.Date > DateTime.Now).OrderBy(r => r.Date).First().Date
                        : (DateTime?)null
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du type de réunion {TypeReunionId} pour le club {ClubId}",
                    typeReunionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du type de réunion");
            }
        }

        // POST: api/clubs/{clubId}/types-reunion
        // Créer un nouveau type de réunion
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> CreerTypeReunion(
            Guid clubId,
            [FromBody] CreerTypeReunionRequest request)
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

                // Vérifier l'unicité du libellé
                var typeExistant = await _context.TypesReunion
                    .FirstOrDefaultAsync(tr => tr.Libelle.ToLower() == request.Libelle.ToLower());

                if (typeExistant != null)
                {
                    return BadRequest($"Un type de réunion avec le libellé '{request.Libelle}' existe déjà");
                }

                // Créer le nouveau type de réunion
                var typeReunion = new TypeReunion
                {
                    Id = Guid.NewGuid(),
                    Libelle = request.Libelle.Trim()
                };

                _context.TypesReunion.Add(typeReunion);
                await _context.SaveChangesAsync();

                var response = new TypeReunionDto
                {
                    Id = typeReunion.Id,
                    Libelle = typeReunion.Libelle,
                    NombreReunions = 0
                };

                _logger.LogInformation(
                    "Type de réunion '{Libelle}' créé avec succès pour le club {ClubId} par l'utilisateur {UserId}",
                    request.Libelle,
                    clubId,
                    User.FindFirstValue(ClaimTypes.NameIdentifier)
                );

                return CreatedAtAction(
                    nameof(GetTypeReunion),
                    new { clubId, typeReunionId = typeReunion.Id },
                    response
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du type de réunion '{Libelle}' pour le club {ClubId}",
                    request.Libelle, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la création du type de réunion");
            }
        }

        // PUT: api/clubs/{clubId}/types-reunion/{typeReunionId}
        // Modifier un type de réunion existant
        [HttpPut("{typeReunionId:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> ModifierTypeReunion(
            Guid clubId,
            Guid typeReunionId,
            [FromBody] ModifierTypeReunionRequest request)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (typeReunionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du type de réunion est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                // Récupérer le type de réunion
                var typeReunion = await _context.TypesReunion
                    .FirstOrDefaultAsync(tr => tr.Id == typeReunionId);

                if (typeReunion == null)
                {
                    return NotFound("Type de réunion non trouvé");
                }

                // Vérifier l'unicité du nouveau libellé (si différent)
                if (!string.Equals(typeReunion.Libelle, request.Libelle, StringComparison.OrdinalIgnoreCase))
                {
                    var typeExistant = await _context.TypesReunion
                        .AnyAsync(tr => tr.Libelle.ToLower() == request.Libelle.ToLower() && tr.Id != typeReunionId);

                    if (typeExistant)
                    {
                        return BadRequest($"Un autre type de réunion avec le libellé '{request.Libelle}' existe déjà");
                    }
                }

                // Mettre à jour les propriétés
                var ancienLibelle = typeReunion.Libelle;
                typeReunion.Libelle = request.Libelle.Trim();

                _context.Entry(typeReunion).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Type de réunion modifié avec succès : '{AncienLibelle}' -> '{NouveauLibelle}' (ID: {TypeReunionId}) pour le club {ClubId}",
                    ancienLibelle,
                    typeReunion.Libelle,
                    typeReunionId,
                    clubId
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification du type de réunion {TypeReunionId} pour le club {ClubId}",
                    typeReunionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la modification du type de réunion");
            }
        }

        // DELETE: api/clubs/{clubId}/types-reunion/{typeReunionId}
        // Supprimer un type de réunion
        [HttpDelete("{typeReunionId:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> SupprimerTypeReunion(Guid clubId, Guid typeReunionId)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (typeReunionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du type de réunion est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                // Récupérer le type de réunion avec ses réunions
                var typeReunion = await _context.TypesReunion
                    .Include(tr => tr.Reunions)
                    .FirstOrDefaultAsync(tr => tr.Id == typeReunionId);

                if (typeReunion == null)
                {
                    return NotFound("Type de réunion non trouvé");
                }

                // Vérifier s'il y a des réunions associées
                if (typeReunion.Reunions.Any())
                {
                    return BadRequest($"Impossible de supprimer le type de réunion '{typeReunion.Libelle}' car {typeReunion.Reunions.Count} réunion(s) y sont associées. " +
                                    "Veuillez d'abord supprimer ou réassigner ces réunions.");
                }

                // Supprimer le type de réunion
                var libelleSupprimer = typeReunion.Libelle;
                _context.TypesReunion.Remove(typeReunion);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Type de réunion '{Libelle}' (ID: {TypeReunionId}) supprimé avec succès pour le club {ClubId}",
                    libelleSupprimer,
                    typeReunionId,
                    clubId
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du type de réunion {TypeReunionId} pour le club {ClubId}",
                    typeReunionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression du type de réunion");
            }
        }

        // GET: api/clubs/{clubId}/types-reunion/{typeReunionId}/reunions
        // Récupérer toutes les réunions d'un type spécifique
        [HttpGet("{typeReunionId:guid}/reunions")]
        public async Task<IActionResult> GetReunionsParType(
            Guid clubId,
            Guid typeReunionId,
            [FromQuery] DateTime? dateDebut = null,
            [FromQuery] DateTime? dateFin = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (typeReunionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du type de réunion est invalide");
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 50;

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que le type de réunion existe
                var typeReunion = await _context.TypesReunion
                    .FirstOrDefaultAsync(tr => tr.Id == typeReunionId);

                if (typeReunion == null)
                {
                    return NotFound("Type de réunion non trouvé");
                }

                // Construire la requête des réunions
                var query = _context.Reunions
                    .Where(r => r.TypeReunionId == typeReunionId);

                // Appliquer les filtres de date
                if (dateDebut.HasValue)
                {
                    query = query.Where(r => r.Date >= dateDebut.Value);
                }

                if (dateFin.HasValue)
                {
                    query = query.Where(r => r.Date <= dateFin.Value);
                }

                // Calculer le total pour la pagination
                var total = await query.CountAsync();

                // Appliquer la pagination et récupérer les données
                var reunions = await query
                    .OrderByDescending(r => r.Date)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new ReunionSummaryDto
                    {
                        Id = r.Id,
                        Date = r.Date,
                        TypeReunionLibelle = r.TypeReunion.Libelle,
                        NombreOrdresDuJour = r.OrdresDuJour.Count(),
                        NombrePresences = r.ListesPresence.Count(),
                        NombreInvites = r.Invites.Count(),
                        NombreDocuments = r.Documents.Count()
                    })
                    .ToListAsync();

                var response = new
                {
                    TypeReunion = new
                    {
                        Id = typeReunion.Id,
                        Libelle = typeReunion.Libelle
                    },
                    Reunions = reunions,
                    Pagination = new
                    {
                        Page = page,
                        PageSize = pageSize,
                        Total = total,
                        TotalPages = (int)Math.Ceiling((double)total / pageSize)
                    },
                    Statistiques = new
                    {
                        TotalReunions = total,
                        ReunionsPassees = reunions.Count(r => r.Date < DateTime.Now),
                        ReunionsFutures = reunions.Count(r => r.Date >= DateTime.Now)
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réunions du type {TypeReunionId} pour le club {ClubId}",
                    typeReunionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des réunions");
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

    // DTOs pour les types de réunion
    public class TypeReunionDto
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public int NombreReunions { get; set; }
    }

    public class TypeReunionDetailDto : TypeReunionDto
    {
        public DateTime? DerniereReunion { get; set; }
        public DateTime? ProchaineReunion { get; set; }
    }

    public class CreerTypeReunionRequest
    {
        [Required(ErrorMessage = "Le libellé est obligatoire")]
        [MaxLength(100, ErrorMessage = "Le libellé ne peut pas dépasser 100 caractères")]
        [MinLength(3, ErrorMessage = "Le libellé doit contenir au moins 3 caractères")]
        public string Libelle { get; set; } = string.Empty;
    }

    public class ModifierTypeReunionRequest
    {
        [Required(ErrorMessage = "Le libellé est obligatoire")]
        [MaxLength(100, ErrorMessage = "Le libellé ne peut pas dépasser 100 caractères")]
        [MinLength(3, ErrorMessage = "Le libellé doit contenir au moins 3 caractères")]
        public string Libelle { get; set; } = string.Empty;
    }

    public class ReunionSummaryDto
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string TypeReunionLibelle { get; set; } = string.Empty;
        public int NombreOrdresDuJour { get; set; }
        public int NombrePresences { get; set; }
        public int NombreInvites { get; set; }
        public int NombreDocuments { get; set; }
    }
}