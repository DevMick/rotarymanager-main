using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/types-budget")]
    [ApiController]
    [Authorize]
    public class TypeBudgetController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TypeBudgetController> _logger;

        public TypeBudgetController(
            ApplicationDbContext context,
            ILogger<TypeBudgetController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/types-budget
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TypeBudgetDto>>> GetTypesBudget([FromQuery] string? recherche = null)
        {
            try
            {
                var query = _context.TypesBudget.AsQueryable();

                // Filtre par terme de recherche
                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(t => t.Libelle.ToLower().Contains(termeLower));
                }

                var typesBudget = await query
                    .OrderBy(t => t.Libelle)
                    .Select(t => new TypeBudgetDto
                    {
                        Id = t.Id,
                        Libelle = t.Libelle,
                        NombreCategoriesBudget = t.CategoriesBudget.Count()
                    })
                    .ToListAsync();

                return Ok(typesBudget);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des types de budget");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des types de budget");
            }
        }

        // GET: api/types-budget/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TypeBudgetDetailDto>> GetTypeBudget(Guid id)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant du type de budget est invalide");
                }

                var typeBudget = await _context.TypesBudget
                    .Include(t => t.CategoriesBudget)
                    .Where(t => t.Id == id)
                    .Select(t => new TypeBudgetDetailDto
                    {
                        Id = t.Id,
                        Libelle = t.Libelle,
                        NombreCategoriesBudget = t.CategoriesBudget.Count(),
                        CategoriesBudget = t.CategoriesBudget.Select(c => new CategoryBudgetResumeDto
                        {
                            Id = c.Id,
                            Libelle = c.Libelle,
                            NombreSousCategories = c.SousCategories.Count()
                        }).OrderBy(c => c.Libelle).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (typeBudget == null)
                {
                    return NotFound($"Type de budget avec l'ID {id} introuvable");
                }

                return Ok(typeBudget);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du type de budget {TypeBudgetId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du type de budget");
            }
        }

        // POST: api/types-budget
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<ActionResult<TypeBudgetDto>> CreateTypeBudget([FromBody] CreateTypeBudgetRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier l'unicité du libellé
                var existingTypeBudget = await _context.TypesBudget
                    .AnyAsync(t => t.Libelle.ToLower() == request.Libelle.ToLower());

                if (existingTypeBudget)
                {
                    return BadRequest($"Un type de budget avec le libellé '{request.Libelle}' existe déjà");
                }

                var typeBudget = new TypeBudget
                {
                    Id = Guid.NewGuid(),
                    Libelle = request.Libelle
                };

                _context.TypesBudget.Add(typeBudget);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Type de budget '{Libelle}' créé avec l'ID {Id}",
                    typeBudget.Libelle, typeBudget.Id);

                var result = new TypeBudgetDto
                {
                    Id = typeBudget.Id,
                    Libelle = typeBudget.Libelle,
                    NombreCategoriesBudget = 0
                };

                return CreatedAtAction(nameof(GetTypeBudget), new { id = typeBudget.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du type de budget");
                return StatusCode(500, "Une erreur est survenue lors de la création du type de budget");
            }
        }

        // PUT: api/types-budget/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> UpdateTypeBudget(Guid id, [FromBody] UpdateTypeBudgetRequest request)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant du type de budget est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var typeBudget = await _context.TypesBudget.FindAsync(id);
                if (typeBudget == null)
                {
                    return NotFound($"Type de budget avec l'ID {id} introuvable");
                }

                // Vérifier l'unicité du libellé si modifié
                if (!string.IsNullOrEmpty(request.Libelle) &&
                    request.Libelle.ToLower() != typeBudget.Libelle.ToLower())
                {
                    var existingTypeBudget = await _context.TypesBudget
                        .AnyAsync(t => t.Id != id && t.Libelle.ToLower() == request.Libelle.ToLower());

                    if (existingTypeBudget)
                    {
                        return BadRequest($"Un type de budget avec le libellé '{request.Libelle}' existe déjà");
                    }
                }

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.Libelle))
                    typeBudget.Libelle = request.Libelle;

                _context.Entry(typeBudget).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Type de budget {Id} mis à jour avec succès", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du type de budget {TypeBudgetId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour du type de budget");
            }
        }

        // DELETE: api/types-budget/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> DeleteTypeBudget(Guid id)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant du type de budget est invalide");
                }

                var typeBudget = await _context.TypesBudget
                    .Include(t => t.CategoriesBudget)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (typeBudget == null)
                {
                    return NotFound($"Type de budget avec l'ID {id} introuvable");
                }

                // Vérifier si le type de budget est utilisé
                if (typeBudget.CategoriesBudget.Any())
                {
                    return BadRequest($"Impossible de supprimer le type de budget '{typeBudget.Libelle}' car il contient {typeBudget.CategoriesBudget.Count} catégorie(s) de budget. " +
                                    "Veuillez d'abord supprimer ou modifier toutes les catégories de budget de ce type.");
                }

                _context.TypesBudget.Remove(typeBudget);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Type de budget '{Libelle}' supprimé avec l'ID {Id}", typeBudget.Libelle, id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du type de budget {TypeBudgetId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la suppression du type de budget");
            }
        }

        // GET: api/types-budget/{id}/categories
        [HttpGet("{id:guid}/categories")]
        public async Task<ActionResult<IEnumerable<CategoryBudgetResumeDto>>> GetCategoriesTypeBudget(Guid id)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant du type de budget est invalide");
                }

                // Vérifier que le type de budget existe
                var typeBudgetExists = await _context.TypesBudget.AnyAsync(t => t.Id == id);
                if (!typeBudgetExists)
                {
                    return NotFound($"Type de budget avec l'ID {id} introuvable");
                }

                var categories = await _context.CategoriesBudget
                    .Where(c => c.TypeBudgetId == id)
                    .OrderBy(c => c.Libelle)
                    .Select(c => new CategoryBudgetResumeDto
                    {
                        Id = c.Id,
                        Libelle = c.Libelle,
                        NombreSousCategories = c.SousCategories.Count()
                    })
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des catégories du type de budget {TypeBudgetId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des catégories");
            }
        }

        // GET: api/types-budget/statistiques
        [HttpGet("statistiques")]
        public async Task<ActionResult<TypeBudgetStatistiquesDto>> GetStatistiques()
        {
            try
            {
                var statistiques = await _context.TypesBudget
                    .Select(t => new
                    {
                        t.Id,
                        t.Libelle,
                        NombreCategories = t.CategoriesBudget.Count(),
                        NombreSousCategories = t.CategoriesBudget.Sum(c => c.SousCategories.Count()),
                        NombreRubriques = t.CategoriesBudget
                            .SelectMany(c => c.SousCategories)
                            .Sum(sc => sc.Rubriques.Count())
                    })
                    .ToListAsync();

                var result = new TypeBudgetStatistiquesDto
                {
                    NombreTypesBudget = statistiques.Count,
                    NombreTotalCategories = statistiques.Sum(s => s.NombreCategories),
                    NombreTotalSousCategories = statistiques.Sum(s => s.NombreSousCategories),
                    NombreTotalRubriques = statistiques.Sum(s => s.NombreRubriques),
                    TypesDetails = statistiques.Select(s => new TypeBudgetStatistiqueDetailDto
                    {
                        Id = s.Id,
                        Libelle = s.Libelle,
                        NombreCategories = s.NombreCategories,
                        NombreSousCategories = s.NombreSousCategories,
                        NombreRubriques = s.NombreRubriques,
                        PourcentageCategories = statistiques.Sum(x => x.NombreCategories) > 0
                            ? Math.Round((double)s.NombreCategories / statistiques.Sum(x => x.NombreCategories) * 100, 2)
                            : 0
                    }).OrderByDescending(s => s.NombreCategories).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques des types de budget");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
            }
        }

        // GET: api/types-budget/predefinies
        [HttpGet("predefinies")]
        public ActionResult<IEnumerable<TypeBudgetPredefiniDto>> GetTypesBudgetPredefinies()
        {
            try
            {
                var typesPredefinies = new List<TypeBudgetPredefiniDto>
                {
                    new() { Libelle = "Dépenses", Description = "Toutes les sorties d'argent du club" },
                    new() { Libelle = "Recettes", Description = "Toutes les entrées d'argent du club" }
                };

                return Ok(typesPredefinies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des types de budget prédéfinis");
                return StatusCode(500, "Une erreur est survenue");
            }
        }

        // POST: api/types-budget/initialiser-predefinies
        [HttpPost("initialiser-predefinies")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<TypeBudgetDto>>> InitialiserTypesPredefinies()
        {
            try
            {
                var typesExistants = await _context.TypesBudget.Select(t => t.Libelle.ToLower()).ToListAsync();

                var typesACreer = new List<TypeBudget>();
                var typesPredefinies = new[] { "Dépenses", "Recettes" };

                foreach (var libelle in typesPredefinies)
                {
                    if (!typesExistants.Contains(libelle.ToLower()))
                    {
                        typesACreer.Add(new TypeBudget
                        {
                            Id = Guid.NewGuid(),
                            Libelle = libelle
                        });
                    }
                }

                if (typesACreer.Any())
                {
                    _context.TypesBudget.AddRange(typesACreer);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("{NombreTypes} types de budget prédéfinis créés", typesACreer.Count);
                }

                var result = typesACreer.Select(t => new TypeBudgetDto
                {
                    Id = t.Id,
                    Libelle = t.Libelle,
                    NombreCategoriesBudget = 0
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'initialisation des types de budget prédéfinis");
                return StatusCode(500, "Une erreur est survenue lors de l'initialisation");
            }
        }
    }

    // DTOs pour les types de budget
    public class TypeBudgetDto
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public int NombreCategoriesBudget { get; set; }
    }

    public class TypeBudgetDetailDto : TypeBudgetDto
    {
        public List<CategoryBudgetResumeDto> CategoriesBudget { get; set; } = new List<CategoryBudgetResumeDto>();
    }

    public class CategoryBudgetResumeDto
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public int NombreSousCategories { get; set; }
    }

    public class CreateTypeBudgetRequest
    {
        [Required(ErrorMessage = "Le libellé est obligatoire")]
        [MaxLength(50, ErrorMessage = "Le libellé ne peut pas dépasser 50 caractères")]
        public string Libelle { get; set; } = string.Empty;
    }

    public class UpdateTypeBudgetRequest
    {
        [MaxLength(50, ErrorMessage = "Le libellé ne peut pas dépasser 50 caractères")]
        public string? Libelle { get; set; }
    }

    public class TypeBudgetStatistiquesDto
    {
        public int NombreTypesBudget { get; set; }
        public int NombreTotalCategories { get; set; }
        public int NombreTotalSousCategories { get; set; }
        public int NombreTotalRubriques { get; set; }
        public List<TypeBudgetStatistiqueDetailDto> TypesDetails { get; set; } = new List<TypeBudgetStatistiqueDetailDto>();
    }

    public class TypeBudgetStatistiqueDetailDto
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public int NombreCategories { get; set; }
        public int NombreSousCategories { get; set; }
        public int NombreRubriques { get; set; }
        public double PourcentageCategories { get; set; }
    }

    public class TypeBudgetPredefiniDto
    {
        public string Libelle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}