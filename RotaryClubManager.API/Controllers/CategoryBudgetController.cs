using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/types-budget/{typeBudgetId}/categories")]
    [ApiController]
    [Authorize]
    public class CategoryBudgetController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoryBudgetController> _logger;

        public CategoryBudgetController(
            ApplicationDbContext context,
            ILogger<CategoryBudgetController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/types-budget/{typeBudgetId}/categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryBudgetDto>>> GetCategoriesBudget(
            Guid typeBudgetId,
            [FromQuery] string? recherche = null)
        {
            try
            {
                // Validation des paramètres
                if (typeBudgetId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du type de budget est invalide");
                }

                // Vérifier que le type de budget existe
                var typeBudget = await _context.TypesBudget.FindAsync(typeBudgetId);
                if (typeBudget == null)
                {
                    return NotFound($"Type de budget avec l'ID {typeBudgetId} introuvable");
                }

                var query = _context.CategoriesBudget
                    .Include(c => c.TypeBudget)
                    .Where(c => c.TypeBudgetId == typeBudgetId);

                // Filtre par terme de recherche
                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(c => c.Libelle.ToLower().Contains(termeLower));
                }

                var categoriesBudget = await query
                    .OrderBy(c => c.Libelle)
                    .Select(c => new CategoryBudgetDto
                    {
                        Id = c.Id,
                        Libelle = c.Libelle,
                        TypeBudgetId = c.TypeBudgetId,
                        TypeBudgetLibelle = c.TypeBudget.Libelle,
                        NombreSousCategories = c.SousCategories.Count()
                    })
                    .ToListAsync();

                return Ok(categoriesBudget);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des catégories de budget du type {TypeBudgetId}", typeBudgetId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des catégories de budget");
            }
        }

        // GET: api/types-budget/{typeBudgetId}/categories/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CategoryBudgetDetailDto>> GetCategoryBudget(Guid typeBudgetId, Guid id)
        {
            try
            {
                // Validation des paramètres
                if (typeBudgetId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du type de budget est invalide");
                }

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la catégorie de budget est invalide");
                }

                var categoryBudget = await _context.CategoriesBudget
                    .Include(c => c.TypeBudget)
                    .Include(c => c.SousCategories)
                        .ThenInclude(sc => sc.Club)
                    .Where(c => c.Id == id && c.TypeBudgetId == typeBudgetId)
                    .Select(c => new CategoryBudgetDetailDto
                    {
                        Id = c.Id,
                        Libelle = c.Libelle,
                        TypeBudgetId = c.TypeBudgetId,
                        TypeBudgetLibelle = c.TypeBudget.Libelle,
                        NombreSousCategories = c.SousCategories.Count(),
                        SousCategories = c.SousCategories.Select(sc => new SousCategoryBudgetResumeDto
                        {
                            Id = sc.Id,
                            Libelle = sc.Libelle,
                            ClubId = sc.ClubId,
                            ClubNom = sc.Club.Name,
                            NombreRubriques = sc.Rubriques.Count()
                        }).OrderBy(sc => sc.ClubNom).ThenBy(sc => sc.Libelle).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (categoryBudget == null)
                {
                    return NotFound($"Catégorie de budget avec l'ID {id} non trouvée pour le type {typeBudgetId}");
                }

                return Ok(categoryBudget);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la catégorie de budget {CategoryBudgetId} du type {TypeBudgetId}", id, typeBudgetId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de la catégorie de budget");
            }
        }

        // POST: api/types-budget/{typeBudgetId}/categories
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<ActionResult<CategoryBudgetDto>> CreateCategoryBudget(
            Guid typeBudgetId,
            [FromBody] CreateCategoryBudgetRequest request)
        {
            try
            {
                // Validation des paramètres
                if (typeBudgetId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du type de budget est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier que le type de budget existe
                var typeBudget = await _context.TypesBudget.FindAsync(typeBudgetId);
                if (typeBudget == null)
                {
                    return NotFound($"Type de budget avec l'ID {typeBudgetId} introuvable");
                }

                // Vérifier l'unicité du libellé dans le type de budget
                var existingCategory = await _context.CategoriesBudget
                    .AnyAsync(c => c.TypeBudgetId == typeBudgetId &&
                                 c.Libelle.ToLower() == request.Libelle.ToLower());

                if (existingCategory)
                {
                    return BadRequest($"Une catégorie de budget avec le libellé '{request.Libelle}' existe déjà pour ce type de budget");
                }

                var categoryBudget = new CategoryBudget
                {
                    Id = Guid.NewGuid(),
                    Libelle = request.Libelle,
                    TypeBudgetId = typeBudgetId
                };

                _context.CategoriesBudget.Add(categoryBudget);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Catégorie de budget '{Libelle}' créée pour le type {TypeBudgetId} avec l'ID {Id}",
                    categoryBudget.Libelle, typeBudgetId, categoryBudget.Id);

                var result = new CategoryBudgetDto
                {
                    Id = categoryBudget.Id,
                    Libelle = categoryBudget.Libelle,
                    TypeBudgetId = categoryBudget.TypeBudgetId,
                    TypeBudgetLibelle = typeBudget.Libelle,
                    NombreSousCategories = 0
                };

                return CreatedAtAction(nameof(GetCategoryBudget),
                    new { typeBudgetId, id = categoryBudget.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la catégorie de budget pour le type {TypeBudgetId}", typeBudgetId);
                return StatusCode(500, "Une erreur est survenue lors de la création de la catégorie de budget");
            }
        }

        // PUT: api/types-budget/{typeBudgetId}/categories/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> UpdateCategoryBudget(
            Guid typeBudgetId,
            Guid id,
            [FromBody] UpdateCategoryBudgetRequest request)
        {
            try
            {
                // Validation des paramètres
                if (typeBudgetId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du type de budget est invalide");
                }

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la catégorie de budget est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var categoryBudget = await _context.CategoriesBudget
                    .FirstOrDefaultAsync(c => c.Id == id && c.TypeBudgetId == typeBudgetId);

                if (categoryBudget == null)
                {
                    return NotFound($"Catégorie de budget avec l'ID {id} non trouvée pour le type {typeBudgetId}");
                }

                // Vérifier l'unicité du libellé si modifié
                if (!string.IsNullOrEmpty(request.Libelle) &&
                    request.Libelle.ToLower() != categoryBudget.Libelle.ToLower())
                {
                    var existingCategory = await _context.CategoriesBudget
                        .AnyAsync(c => c.Id != id &&
                                     c.TypeBudgetId == typeBudgetId &&
                                     c.Libelle.ToLower() == request.Libelle.ToLower());

                    if (existingCategory)
                    {
                        return BadRequest($"Une catégorie de budget avec le libellé '{request.Libelle}' existe déjà pour ce type de budget");
                    }
                }

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.Libelle))
                    categoryBudget.Libelle = request.Libelle;

                _context.Entry(categoryBudget).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Catégorie de budget {Id} mise à jour avec succès", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la catégorie de budget {CategoryBudgetId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de la catégorie de budget");
            }
        }

        // DELETE: api/types-budget/{typeBudgetId}/categories/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> DeleteCategoryBudget(Guid typeBudgetId, Guid id)
        {
            try
            {
                // Validation des paramètres
                if (typeBudgetId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du type de budget est invalide");
                }

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la catégorie de budget est invalide");
                }

                var categoryBudget = await _context.CategoriesBudget
                    .Include(c => c.SousCategories)
                    .FirstOrDefaultAsync(c => c.Id == id && c.TypeBudgetId == typeBudgetId);

                if (categoryBudget == null)
                {
                    return NotFound($"Catégorie de budget avec l'ID {id} non trouvée pour le type {typeBudgetId}");
                }

                // Vérifier si la catégorie est utilisée
                if (categoryBudget.SousCategories.Any())
                {
                    return BadRequest($"Impossible de supprimer la catégorie de budget '{categoryBudget.Libelle}' car elle contient {categoryBudget.SousCategories.Count} sous-catégorie(s). " +
                                    "Veuillez d'abord supprimer toutes les sous-catégories de cette catégorie.");
                }

                _context.CategoriesBudget.Remove(categoryBudget);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Catégorie de budget '{Libelle}' supprimée du type {TypeBudgetId}", categoryBudget.Libelle, typeBudgetId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la catégorie de budget {CategoryBudgetId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de la catégorie de budget");
            }
        }

        // GET: api/types-budget/{typeBudgetId}/categories/{id}/sous-categories
        [HttpGet("{id:guid}/sous-categories")]
        public async Task<ActionResult<IEnumerable<SousCategoryBudgetResumeDto>>> GetSousCategoriesCategory(
            Guid typeBudgetId,
            Guid id,
            [FromQuery] Guid? clubId = null)
        {
            try
            {
                // Validation des paramètres
                if (typeBudgetId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du type de budget est invalide");
                }

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la catégorie de budget est invalide");
                }

                // Vérifier que la catégorie existe
                var categoryExists = await _context.CategoriesBudget
                    .AnyAsync(c => c.Id == id && c.TypeBudgetId == typeBudgetId);

                if (!categoryExists)
                {
                    return NotFound($"Catégorie de budget avec l'ID {id} non trouvée pour le type {typeBudgetId}");
                }

                var query = _context.SousCategoriesBudget
                    .Include(sc => sc.Club)
                    .Where(sc => sc.CategoryBudgetId == id);

                // Filtre optionnel par club
                if (clubId.HasValue)
                {
                    query = query.Where(sc => sc.ClubId == clubId.Value);
                }

                var sousCategories = await query
                    .OrderBy(sc => sc.Club.Name)
                    .ThenBy(sc => sc.Libelle)
                    .Select(sc => new SousCategoryBudgetResumeDto
                    {
                        Id = sc.Id,
                        Libelle = sc.Libelle,
                        ClubId = sc.ClubId,
                        ClubNom = sc.Club.Name,
                        NombreRubriques = sc.Rubriques.Count()
                    })
                    .ToListAsync();

                return Ok(sousCategories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des sous-catégories de la catégorie {CategoryBudgetId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des sous-catégories");
            }
        }

        // GET: api/types-budget/{typeBudgetId}/categories/predefinies
        [HttpGet("predefinies")]
        public ActionResult<IEnumerable<CategoryBudgetPredefiniDto>> GetCategoriesPredefinies(Guid typeBudgetId)
        {
            try
            {
                // Validation des paramètres
                if (typeBudgetId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du type de budget est invalide");
                }

                var categoriesPredefinies = new List<CategoryBudgetPredefiniDto>
                {
                    new() { Libelle = "Fonctionnement", Description = "Frais de fonctionnement du club" },
                    new() { Libelle = "Caritatif", Description = "Actions caritatives et humanitaires" },
                    new() { Libelle = "Formation", Description = "Formation des membres et développement" },
                    new() { Libelle = "Communication", Description = "Communication et relations publiques" },
                    new() { Libelle = "Événements", Description = "Organisation d'événements et manifestations" }
                };

                return Ok(categoriesPredefinies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des catégories prédéfinies");
                return StatusCode(500, "Une erreur est survenue");
            }
        }

        // POST: api/types-budget/{typeBudgetId}/categories/initialiser-predefinies
        [HttpPost("initialiser-predefinies")]
        [Authorize(Roles = "Admin,President")]
        public async Task<ActionResult<IEnumerable<CategoryBudgetDto>>> InitialiserCategoriesPredefinies(Guid typeBudgetId)
        {
            try
            {
                // Validation des paramètres
                if (typeBudgetId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du type de budget est invalide");
                }

                // Vérifier que le type de budget existe
                var typeBudget = await _context.TypesBudget.FindAsync(typeBudgetId);
                if (typeBudget == null)
                {
                    return NotFound($"Type de budget avec l'ID {typeBudgetId} introuvable");
                }

                var categoriesExistantes = await _context.CategoriesBudget
                    .Where(c => c.TypeBudgetId == typeBudgetId)
                    .Select(c => c.Libelle.ToLower())
                    .ToListAsync();

                var categoriesACreer = new List<CategoryBudget>();
                var categoriesPredefinies = new[] { "Fonctionnement", "Caritatif", "Formation", "Communication", "Événements" };

                foreach (var libelle in categoriesPredefinies)
                {
                    if (!categoriesExistantes.Contains(libelle.ToLower()))
                    {
                        categoriesACreer.Add(new CategoryBudget
                        {
                            Id = Guid.NewGuid(),
                            Libelle = libelle,
                            TypeBudgetId = typeBudgetId
                        });
                    }
                }

                if (categoriesACreer.Any())
                {
                    _context.CategoriesBudget.AddRange(categoriesACreer);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("{NombreCategories} catégories de budget prédéfinies créées pour le type {TypeBudgetId}",
                        categoriesACreer.Count, typeBudgetId);
                }

                var result = categoriesACreer.Select(c => new CategoryBudgetDto
                {
                    Id = c.Id,
                    Libelle = c.Libelle,
                    TypeBudgetId = c.TypeBudgetId,
                    TypeBudgetLibelle = typeBudget.Libelle,
                    NombreSousCategories = 0
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'initialisation des catégories prédéfinies pour le type {TypeBudgetId}", typeBudgetId);
                return StatusCode(500, "Une erreur est survenue lors de l'initialisation");
            }
        }
    }

    // DTOs pour les catégories de budget
    public class CategoryBudgetDto
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public Guid TypeBudgetId { get; set; }
        public string TypeBudgetLibelle { get; set; } = string.Empty;
        public int NombreSousCategories { get; set; }
    }

    public class CategoryBudgetDetailDto : CategoryBudgetDto
    {
        public List<SousCategoryBudgetResumeDto> SousCategories { get; set; } = new List<SousCategoryBudgetResumeDto>();
    }

    public class SousCategoryBudgetResumeDto
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public Guid ClubId { get; set; }
        public string ClubNom { get; set; } = string.Empty;
        public int NombreRubriques { get; set; }
    }

    public class CreateCategoryBudgetRequest
    {
        [Required(ErrorMessage = "Le libellé est obligatoire")]
        [MaxLength(100, ErrorMessage = "Le libellé ne peut pas dépasser 100 caractères")]
        public string Libelle { get; set; } = string.Empty;
    }

    public class UpdateCategoryBudgetRequest
    {
        [MaxLength(100, ErrorMessage = "Le libellé ne peut pas dépasser 100 caractères")]
        public string? Libelle { get; set; }
    }

    public class CategoryBudgetPredefiniDto
    {
        public string Libelle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}