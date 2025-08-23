using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/clubs/{clubId}/categories/{categoryBudgetId}/sous-categories")]
    [ApiController]
    [Authorize]
    public class SousCategoryBudgetController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SousCategoryBudgetController> _logger;

        public SousCategoryBudgetController(
            ApplicationDbContext context,
            ILogger<SousCategoryBudgetController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/clubs/{clubId}/categories/{categoryBudgetId}/sous-categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SousCategoryBudgetDto>>> GetSousCategories(
            Guid clubId,
            Guid categoryBudgetId,
            [FromQuery] string? recherche = null)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (categoryBudgetId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la catégorie de budget est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                // Vérifier que la catégorie de budget existe
                var categoryBudget = await _context.CategoriesBudget
                    .Include(c => c.TypeBudget)
                    .FirstOrDefaultAsync(c => c.Id == categoryBudgetId);

                if (categoryBudget == null)
                {
                    return NotFound($"Catégorie de budget avec l'ID {categoryBudgetId} introuvable");
                }

                var query = _context.SousCategoriesBudget
                    .Include(sc => sc.CategoryBudget)
                        .ThenInclude(c => c.TypeBudget)
                    .Include(sc => sc.Club)
                    .Where(sc => sc.ClubId == clubId && sc.CategoryBudgetId == categoryBudgetId);

                // Filtre par terme de recherche
                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(sc => sc.Libelle.ToLower().Contains(termeLower));
                }

                var sousCategories = await query
                    .OrderBy(sc => sc.Libelle)
                    .Select(sc => new SousCategoryBudgetDto
                    {
                        Id = sc.Id,
                        Libelle = sc.Libelle,
                        CategoryBudgetId = sc.CategoryBudgetId,
                        CategoryBudgetLibelle = sc.CategoryBudget.Libelle,
                        TypeBudgetLibelle = sc.CategoryBudget.TypeBudget.Libelle,
                        ClubId = sc.ClubId,
                        ClubNom = sc.Club.Name,
                        NombreRubriques = sc.Rubriques.Count()
                    })
                    .ToListAsync();

                return Ok(sousCategories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des sous-catégories de budget du club {ClubId}", clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des sous-catégories de budget");
            }
        }

        // GET: api/clubs/{clubId}/categories/{categoryBudgetId}/sous-categories/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<SousCategoryBudgetDetailDto>> GetSousCategory(
            Guid clubId,
            Guid categoryBudgetId,
            Guid id)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (categoryBudgetId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la catégorie de budget est invalide");
                }

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la sous-catégorie est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                var sousCategory = await _context.SousCategoriesBudget
                    .Include(sc => sc.CategoryBudget)
                        .ThenInclude(c => c.TypeBudget)
                    .Include(sc => sc.Club)
                    .Include(sc => sc.Rubriques)
                        .ThenInclude(r => r.Mandat)
                    .Where(sc => sc.Id == id && sc.ClubId == clubId && sc.CategoryBudgetId == categoryBudgetId)
                    .Select(sc => new SousCategoryBudgetDetailDto
                    {
                        Id = sc.Id,
                        Libelle = sc.Libelle,
                        CategoryBudgetId = sc.CategoryBudgetId,
                        CategoryBudgetLibelle = sc.CategoryBudget.Libelle,
                        TypeBudgetLibelle = sc.CategoryBudget.TypeBudget.Libelle,
                        ClubId = sc.ClubId,
                        ClubNom = sc.Club.Name,
                        NombreRubriques = sc.Rubriques.Count(),
                        Rubriques = sc.Rubriques.Select(r => new RubriqueBudgetResumeDto
                        {
                            Id = r.Id,
                            Libelle = r.Libelle,
                            PrixUnitaire = r.PrixUnitaire,
                            Quantite = r.Quantite,
                            MontantTotal = r.MontantTotal,
                            MandatId = r.MandatId,
                            MandatAnnee = r.Mandat.Annee,
                            NombreRealisations = r.Realisations.Count()
                        }).OrderByDescending(r => r.MandatAnnee).ThenBy(r => r.Libelle).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (sousCategory == null)
                {
                    return NotFound($"Sous-catégorie avec l'ID {id} non trouvée pour le club {clubId} et la catégorie {categoryBudgetId}");
                }

                return Ok(sousCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la sous-catégorie {SousCategoryId} du club {ClubId}", id, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de la sous-catégorie");
            }
        }

        // POST: api/clubs/{clubId}/categories/{categoryBudgetId}/sous-categories
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary,Treasurer")]
        public async Task<ActionResult<SousCategoryBudgetDto>> CreateSousCategory(
            Guid clubId,
            Guid categoryBudgetId,
            [FromBody] CreateSousCategoryBudgetRequest request)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (categoryBudgetId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la catégorie de budget est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                // Vérifier que le club existe
                var club = await _context.Clubs.FindAsync(clubId);
                if (club == null)
                {
                    return NotFound("Club non trouvé");
                }

                // Vérifier que la catégorie de budget existe
                var categoryBudget = await _context.CategoriesBudget
                    .Include(c => c.TypeBudget)
                    .FirstOrDefaultAsync(c => c.Id == categoryBudgetId);

                if (categoryBudget == null)
                {
                    return NotFound("Catégorie de budget non trouvée");
                }

                // Vérifier l'unicité du libellé dans le club/catégorie
                var existingSousCategory = await _context.SousCategoriesBudget
                    .AnyAsync(sc => sc.ClubId == clubId &&
                                   sc.CategoryBudgetId == categoryBudgetId &&
                                   sc.Libelle.ToLower() == request.Libelle.ToLower());

                if (existingSousCategory)
                {
                    return BadRequest($"Une sous-catégorie avec le libellé '{request.Libelle}' existe déjà pour cette catégorie dans ce club");
                }

                var sousCategory = new SousCategoryBudget
                {
                    Id = Guid.NewGuid(),
                    Libelle = request.Libelle,
                    CategoryBudgetId = categoryBudgetId,
                    ClubId = clubId
                };

                _context.SousCategoriesBudget.Add(sousCategory);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Sous-catégorie '{Libelle}' créée pour le club {ClubId} et la catégorie {CategoryBudgetId} avec l'ID {Id}",
                    sousCategory.Libelle, clubId, categoryBudgetId, sousCategory.Id);

                var result = new SousCategoryBudgetDto
                {
                    Id = sousCategory.Id,
                    Libelle = sousCategory.Libelle,
                    CategoryBudgetId = sousCategory.CategoryBudgetId,
                    CategoryBudgetLibelle = categoryBudget.Libelle,
                    TypeBudgetLibelle = categoryBudget.TypeBudget.Libelle,
                    ClubId = sousCategory.ClubId,
                    ClubNom = club.Name,
                    NombreRubriques = 0
                };

                return CreatedAtAction(nameof(GetSousCategory),
                    new { clubId, categoryBudgetId, id = sousCategory.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la sous-catégorie pour le club {ClubId}", clubId);
                return StatusCode(500, "Une erreur est survenue lors de la création de la sous-catégorie");
            }
        }

        // PUT: api/clubs/{clubId}/categories/{categoryBudgetId}/sous-categories/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary,Treasurer")]
        public async Task<IActionResult> UpdateSousCategory(
            Guid clubId,
            Guid categoryBudgetId,
            Guid id,
            [FromBody] UpdateSousCategoryBudgetRequest request)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (categoryBudgetId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la catégorie de budget est invalide");
                }

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la sous-catégorie est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                var sousCategory = await _context.SousCategoriesBudget
                    .FirstOrDefaultAsync(sc => sc.Id == id &&
                                             sc.ClubId == clubId &&
                                             sc.CategoryBudgetId == categoryBudgetId);

                if (sousCategory == null)
                {
                    return NotFound($"Sous-catégorie avec l'ID {id} non trouvée pour le club {clubId} et la catégorie {categoryBudgetId}");
                }

                // Vérifier l'unicité du libellé si modifié
                if (!string.IsNullOrEmpty(request.Libelle) &&
                    request.Libelle.ToLower() != sousCategory.Libelle.ToLower())
                {
                    var existingSousCategory = await _context.SousCategoriesBudget
                        .AnyAsync(sc => sc.Id != id &&
                                       sc.ClubId == clubId &&
                                       sc.CategoryBudgetId == categoryBudgetId &&
                                       sc.Libelle.ToLower() == request.Libelle.ToLower());

                    if (existingSousCategory)
                    {
                        return BadRequest($"Une sous-catégorie avec le libellé '{request.Libelle}' existe déjà pour cette catégorie dans ce club");
                    }
                }

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.Libelle))
                    sousCategory.Libelle = request.Libelle;

                _context.Entry(sousCategory).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Sous-catégorie {Id} mise à jour dans le club {ClubId}", id, clubId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la sous-catégorie {SousCategoryId} du club {ClubId}", id, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de la sous-catégorie");
            }
        }

        // DELETE: api/clubs/{clubId}/categories/{categoryBudgetId}/sous-categories/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> DeleteSousCategory(
            Guid clubId,
            Guid categoryBudgetId,
            Guid id)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (categoryBudgetId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la catégorie de budget est invalide");
                }

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la sous-catégorie est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                var sousCategory = await _context.SousCategoriesBudget
                    .Include(sc => sc.Rubriques)
                    .FirstOrDefaultAsync(sc => sc.Id == id &&
                                             sc.ClubId == clubId &&
                                             sc.CategoryBudgetId == categoryBudgetId);

                if (sousCategory == null)
                {
                    return NotFound($"Sous-catégorie avec l'ID {id} non trouvée pour le club {clubId} et la catégorie {categoryBudgetId}");
                }

                // Vérifier si la sous-catégorie est utilisée
                if (sousCategory.Rubriques.Any())
                {
                    return BadRequest($"Impossible de supprimer la sous-catégorie '{sousCategory.Libelle}' car elle contient {sousCategory.Rubriques.Count} rubrique(s). " +
                                    "Veuillez d'abord supprimer toutes les rubriques de cette sous-catégorie.");
                }

                _context.SousCategoriesBudget.Remove(sousCategory);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Sous-catégorie '{Libelle}' supprimée du club {ClubId}", sousCategory.Libelle, clubId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la sous-catégorie {SousCategoryId} du club {ClubId}", id, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de la sous-catégorie");
            }
        }

        // GET: api/clubs/{clubId}/categories/{categoryBudgetId}/sous-categories/{id}/rubriques
        [HttpGet("{id:guid}/rubriques")]
        public async Task<ActionResult<IEnumerable<RubriqueBudgetResumeDto>>> GetRubriquesSousCategory(
            Guid clubId,
            Guid categoryBudgetId,
            Guid id,
            [FromQuery] Guid? mandatId = null)
        {
            try
            {
                // Validation et autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                // Vérifier que la sous-catégorie existe
                var sousCategoryExists = await _context.SousCategoriesBudget
                    .AnyAsync(sc => sc.Id == id && sc.ClubId == clubId && sc.CategoryBudgetId == categoryBudgetId);

                if (!sousCategoryExists)
                {
                    return NotFound($"Sous-catégorie avec l'ID {id} non trouvée");
                }

                var query = _context.RubriquesBudget
                    .Include(r => r.Mandat)
                    .Where(r => r.SousCategoryBudgetId == id && r.ClubId == clubId);

                // Filtre optionnel par mandat
                if (mandatId.HasValue)
                {
                    query = query.Where(r => r.MandatId == mandatId.Value);
                }

                var rubriques = await query
                    .OrderByDescending(r => r.Mandat.Annee)
                    .ThenBy(r => r.Libelle)
                    .Select(r => new RubriqueBudgetResumeDto
                    {
                        Id = r.Id,
                        Libelle = r.Libelle,
                        PrixUnitaire = r.PrixUnitaire,
                        Quantite = r.Quantite,
                        MontantTotal = r.MontantTotal,
                        MandatId = r.MandatId,
                        MandatAnnee = r.Mandat.Annee,
                        NombreRealisations = r.Realisations.Count()
                    })
                    .ToListAsync();

                return Ok(rubriques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des rubriques de la sous-catégorie {SousCategoryId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des rubriques");
            }
        }

        // GET: api/clubs/{clubId}/categories/{categoryBudgetId}/sous-categories/statistiques
        [HttpGet("statistiques")]
        public async Task<ActionResult<SousCategoryBudgetStatistiquesDto>> GetStatistiques(
            Guid clubId,
            Guid categoryBudgetId)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                var statistiques = await _context.SousCategoriesBudget
                    .Where(sc => sc.ClubId == clubId && sc.CategoryBudgetId == categoryBudgetId)
                    .Select(sc => new
                    {
                        sc.Id,
                        sc.Libelle,
                        NombreRubriques = sc.Rubriques.Count(),
                        MontantTotalBudget = sc.Rubriques.Sum(r => r.PrixUnitaire * r.Quantite),
                        MontantTotalRealise = sc.Rubriques
                            .SelectMany(r => r.Realisations)
                            .Sum(real => real.Montant)
                    })
                    .ToListAsync();

                var result = new SousCategoryBudgetStatistiquesDto
                {
                    ClubId = clubId,
                    CategoryBudgetId = categoryBudgetId,
                    NombreSousCategories = statistiques.Count,
                    NombreTotalRubriques = statistiques.Sum(s => s.NombreRubriques),
                    MontantTotalBudget = statistiques.Sum(s => s.MontantTotalBudget),
                    MontantTotalRealise = statistiques.Sum(s => s.MontantTotalRealise),
                    SousCategoriesDetails = statistiques.Select(s => new SousCategoryBudgetStatistiqueDetailDto
                    {
                        Id = s.Id,
                        Libelle = s.Libelle,
                        NombreRubriques = s.NombreRubriques,
                        MontantTotalBudget = s.MontantTotalBudget,
                        MontantTotalRealise = s.MontantTotalRealise,
                        EcartBudgetRealise = s.MontantTotalRealise - s.MontantTotalBudget
                    }).OrderByDescending(s => s.MontantTotalBudget).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques des sous-catégories du club {ClubId}", clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
            }
        }

        // Méthodes d'aide
        private async Task<bool> CanAccessClub(Guid clubId)
        {
            if (User.IsInRole("Admin"))
                return true;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return false;

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

            var userInClub = await _context.UserClubs
                .AnyAsync(uc => uc.UserId == userId && uc.ClubId == clubId);

            if (!userInClub)
                return false;

            return User.IsInRole("President") || User.IsInRole("Secretary") || User.IsInRole("Treasurer");
        }
    }

    // DTOs pour les sous-catégories de budget
    public class SousCategoryBudgetDto
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public Guid CategoryBudgetId { get; set; }
        public string CategoryBudgetLibelle { get; set; } = string.Empty;
        public string TypeBudgetLibelle { get; set; } = string.Empty;
        public Guid ClubId { get; set; }
        public string ClubNom { get; set; } = string.Empty;
        public int NombreRubriques { get; set; }
    }

    public class SousCategoryBudgetDetailDto : SousCategoryBudgetDto
    {
        public List<RubriqueBudgetResumeDto> Rubriques { get; set; } = new List<RubriqueBudgetResumeDto>();
    }

    public class RubriqueBudgetResumeDto
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public decimal PrixUnitaire { get; set; }
        public int Quantite { get; set; }
        public decimal MontantTotal { get; set; }
        public Guid MandatId { get; set; }
        public int MandatAnnee { get; set; }
        public int NombreRealisations { get; set; }
    }

    public class CreateSousCategoryBudgetRequest
    {
        [Required(ErrorMessage = "Le libellé est obligatoire")]
        [MaxLength(150, ErrorMessage = "Le libellé ne peut pas dépasser 150 caractères")]
        public string Libelle { get; set; } = string.Empty;
    }

    public class UpdateSousCategoryBudgetRequest
    {
        [MaxLength(150, ErrorMessage = "Le libellé ne peut pas dépasser 150 caractères")]
        public string? Libelle { get; set; }
    }

    public class SousCategoryBudgetStatistiquesDto
    {
        public Guid ClubId { get; set; }
        public Guid CategoryBudgetId { get; set; }
        public int NombreSousCategories { get; set; }
        public int NombreTotalRubriques { get; set; }
        public decimal MontantTotalBudget { get; set; }
        public decimal MontantTotalRealise { get; set; }
        public List<SousCategoryBudgetStatistiqueDetailDto> SousCategoriesDetails { get; set; } = new List<SousCategoryBudgetStatistiqueDetailDto>();
    }

    public class SousCategoryBudgetStatistiqueDetailDto
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public int NombreRubriques { get; set; }
        public decimal MontantTotalBudget { get; set; }
        public decimal MontantTotalRealise { get; set; }
        public decimal EcartBudgetRealise { get; set; }
    }
}