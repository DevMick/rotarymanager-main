using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/categories")]
    [ApiController]
    [Authorize]
    public class CategorieController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategorieController> _logger;

        public CategorieController(
            ApplicationDbContext context,
            ILogger<CategorieController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategorieDto>>> GetCategories([FromQuery] string? recherche = null)
        {
            try
            {
                var query = _context.Categories.AsQueryable();

                // Filtre par terme de recherche
                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(c => c.Libelle.ToLower().Contains(termeLower));
                }

                var categories = await query
                    .OrderBy(c => c.Libelle)
                    .Select(c => new CategorieDto
                    {
                        Id = c.Id,
                        Libelle = c.Libelle,
                        NombreDocuments = c.Documents.Count()
                    })
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des catégories");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des catégories");
            }
        }

        // GET: api/categories/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CategorieDetailDto>> GetCategorie(Guid id)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la catégorie est invalide");
                }

                var categorie = await _context.Categories
                    .Include(c => c.Documents)
                        .ThenInclude(d => d.Club)
                    .Include(c => c.Documents)
                        .ThenInclude(d => d.TypeDocument)
                    .Where(c => c.Id == id)
                    .Select(c => new CategorieDetailDto
                    {
                        Id = c.Id,
                        Libelle = c.Libelle,
                        NombreDocuments = c.Documents.Count(),
                        Documents = c.Documents.Select(d => new DocumentSummaireDto
                        {
                            Id = d.Id,
                            Nom = d.Nom,
                            Description = d.Description,
                            TypeDocumentLibelle = d.TypeDocument.Libelle,
                            ClubNom = d.Club.Name,
                            TailleFichier = d.Fichier.Length
                        }).OrderBy(d => d.Nom).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (categorie == null)
                {
                    return NotFound($"Catégorie avec l'ID {id} introuvable");
                }

                return Ok(categorie);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la catégorie {CategorieId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de la catégorie");
            }
        }

        // POST: api/categories
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<ActionResult<CategorieDto>> CreateCategorie([FromBody] CreateCategorieRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier l'unicité du libellé
                var existingCategorie = await _context.Categories
                    .AnyAsync(c => c.Libelle.ToLower() == request.Libelle.ToLower());

                if (existingCategorie)
                {
                    return BadRequest($"Une catégorie avec le libellé '{request.Libelle}' existe déjà");
                }

                var categorie = new Categorie
                {
                    Id = Guid.NewGuid(),
                    Libelle = request.Libelle
                };

                _context.Categories.Add(categorie);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Catégorie '{Libelle}' créée avec l'ID {Id}",
                    categorie.Libelle, categorie.Id);

                var result = new CategorieDto
                {
                    Id = categorie.Id,
                    Libelle = categorie.Libelle,
                    NombreDocuments = 0
                };

                return CreatedAtAction(nameof(GetCategorie), new { id = categorie.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la catégorie");
                return StatusCode(500, "Une erreur est survenue lors de la création de la catégorie");
            }
        }

        // PUT: api/categories/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> UpdateCategorie(Guid id, [FromBody] UpdateCategorieRequest request)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la catégorie est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var categorie = await _context.Categories.FindAsync(id);
                if (categorie == null)
                {
                    return NotFound($"Catégorie avec l'ID {id} introuvable");
                }

                // Vérifier l'unicité du libellé si modifié
                if (!string.IsNullOrEmpty(request.Libelle) &&
                    request.Libelle.ToLower() != categorie.Libelle.ToLower())
                {
                    var existingCategorie = await _context.Categories
                        .AnyAsync(c => c.Id != id && c.Libelle.ToLower() == request.Libelle.ToLower());

                    if (existingCategorie)
                    {
                        return BadRequest($"Une catégorie avec le libellé '{request.Libelle}' existe déjà");
                    }
                }

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.Libelle))
                    categorie.Libelle = request.Libelle;

                _context.Entry(categorie).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Catégorie {Id} mise à jour avec succès", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la catégorie {CategorieId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de la catégorie");
            }
        }

        // DELETE: api/categories/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> DeleteCategorie(Guid id)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la catégorie est invalide");
                }

                var categorie = await _context.Categories
                    .Include(c => c.Documents)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (categorie == null)
                {
                    return NotFound($"Catégorie avec l'ID {id} introuvable");
                }

                // Vérifier si la catégorie est utilisée
                if (categorie.Documents.Any())
                {
                    return BadRequest($"Impossible de supprimer la catégorie '{categorie.Libelle}' car elle contient {categorie.Documents.Count} document(s). " +
                                    "Veuillez d'abord supprimer ou déplacer tous les documents de cette catégorie.");
                }

                _context.Categories.Remove(categorie);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Catégorie '{Libelle}' supprimée avec l'ID {Id}", categorie.Libelle, id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la catégorie {CategorieId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de la catégorie");
            }
        }

        // GET: api/categories/{id}/documents
        [HttpGet("{id:guid}/documents")]
        public async Task<ActionResult<IEnumerable<DocumentSummaireDto>>> GetDocumentsCategorie(
            Guid id,
            [FromQuery] Guid? clubId = null,
            [FromQuery] Guid? typeDocumentId = null)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la catégorie est invalide");
                }

                // Vérifier que la catégorie existe
                var categorieExists = await _context.Categories.AnyAsync(c => c.Id == id);
                if (!categorieExists)
                {
                    return NotFound($"Catégorie avec l'ID {id} introuvable");
                }

                var query = _context.Documents
                    .Include(d => d.TypeDocument)
                    .Include(d => d.Club)
                    .Where(d => d.CategorieId == id);

                // Filtres optionnels
                if (clubId.HasValue)
                {
                    query = query.Where(d => d.ClubId == clubId.Value);
                }

                if (typeDocumentId.HasValue)
                {
                    query = query.Where(d => d.TypeDocumentId == typeDocumentId.Value);
                }

                var documents = await query
                    .OrderBy(d => d.Nom)
                    .Select(d => new DocumentSummaireDto
                    {
                        Id = d.Id,
                        Nom = d.Nom,
                        Description = d.Description,
                        TypeDocumentLibelle = d.TypeDocument.Libelle,
                        ClubNom = d.Club.Name,
                        TailleFichier = d.Fichier.Length
                    })
                    .ToListAsync();

                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des documents de la catégorie {CategorieId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des documents");
            }
        }

        // GET: api/categories/statistiques
        [HttpGet("statistiques")]
        public async Task<ActionResult<CategorieStatistiquesDto>> GetStatistiques()
        {
            try
            {
                var statistiques = await _context.Categories
                    .Select(c => new
                    {
                        c.Id,
                        c.Libelle,
                        NombreDocuments = c.Documents.Count(),
                        TailleTotale = c.Documents.Sum(d => (long)d.Fichier.Length)
                    })
                    .ToListAsync();

                var result = new CategorieStatistiquesDto
                {
                    NombreCategories = statistiques.Count,
                    NombreTotalDocuments = statistiques.Sum(s => s.NombreDocuments),
                    TailleTotaleDocuments = statistiques.Sum(s => s.TailleTotale),
                    CategoriesDetails = statistiques.Select(s => new CategorieStatistiqueDetailDto
                    {
                        Id = s.Id,
                        Libelle = s.Libelle,
                        NombreDocuments = s.NombreDocuments,
                        TailleTotale = s.TailleTotale,
                        PourcentageDocuments = statistiques.Sum(x => x.NombreDocuments) > 0
                            ? Math.Round((double)s.NombreDocuments / statistiques.Sum(x => x.NombreDocuments) * 100, 2)
                            : 0
                    }).OrderByDescending(s => s.NombreDocuments).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques des catégories");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
            }
        }
    }

    // DTOs pour les catégories
    public class CategorieDto
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public int NombreDocuments { get; set; }
    }

    public class CategorieDetailDto : CategorieDto
    {
        public List<DocumentSummaireDto> Documents { get; set; } = new List<DocumentSummaireDto>();
    }

    public class DocumentSummaireDto
    {
        public Guid Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string TypeDocumentLibelle { get; set; } = string.Empty;
        public string ClubNom { get; set; } = string.Empty;
        public long TailleFichier { get; set; }
    }

    public class CreateCategorieRequest
    {
        [Required(ErrorMessage = "Le libellé est obligatoire")]
        [MaxLength(100, ErrorMessage = "Le libellé ne peut pas dépasser 100 caractères")]
        public string Libelle { get; set; } = string.Empty;
    }

    public class UpdateCategorieRequest
    {
        [MaxLength(100, ErrorMessage = "Le libellé ne peut pas dépasser 100 caractères")]
        public string? Libelle { get; set; }
    }

    public class CategorieStatistiquesDto
    {
        public int NombreCategories { get; set; }
        public int NombreTotalDocuments { get; set; }
        public long TailleTotaleDocuments { get; set; }
        public List<CategorieStatistiqueDetailDto> CategoriesDetails { get; set; } = new List<CategorieStatistiqueDetailDto>();
    }

    public class CategorieStatistiqueDetailDto
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public int NombreDocuments { get; set; }
        public long TailleTotale { get; set; }
        public double PourcentageDocuments { get; set; }
    }
}