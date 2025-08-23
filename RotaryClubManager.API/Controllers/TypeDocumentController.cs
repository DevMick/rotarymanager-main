using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/types-document")]
    [ApiController]
    [Authorize]
    public class TypeDocumentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TypeDocumentController> _logger;

        public TypeDocumentController(
            ApplicationDbContext context,
            ILogger<TypeDocumentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/types-document
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TypeDocumentDto>>> GetTypesDocument([FromQuery] string? recherche = null)
        {
            try
            {
                var query = _context.TypesDocument.AsQueryable();

                // Filtre par terme de recherche
                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(t => t.Libelle.ToLower().Contains(termeLower));
                }

                var typesDocument = await query
                    .OrderBy(t => t.Libelle)
                    .Select(t => new TypeDocumentDto
                    {
                        Id = t.Id,
                        Libelle = t.Libelle,
                        NombreDocuments = t.Documents.Count()
                    })
                    .ToListAsync();

                return Ok(typesDocument);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des types de document");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des types de document");
            }
        }

        // GET: api/types-document/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TypeDocumentDetailDto>> GetTypeDocument(Guid id)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant du type de document est invalide");
                }

                var typeDocument = await _context.TypesDocument
                    .Include(t => t.Documents)
                        .ThenInclude(d => d.Club)
                    .Include(t => t.Documents)
                        .ThenInclude(d => d.Categorie)
                    .Where(t => t.Id == id)
                    .Select(t => new TypeDocumentDetailDto
                    {
                        Id = t.Id,
                        Libelle = t.Libelle,
                        NombreDocuments = t.Documents.Count(),
                        Documents = t.Documents.Select(d => new DocumentTypeDto
                        {
                            Id = d.Id,
                            Nom = d.Nom,
                            Description = d.Description,
                            CategorieLibelle = d.Categorie.Libelle,
                            ClubNom = d.Club.Name,
                            TailleFichier = d.Fichier.Length
                        }).OrderBy(d => d.Nom).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (typeDocument == null)
                {
                    return NotFound($"Type de document avec l'ID {id} introuvable");
                }

                return Ok(typeDocument);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du type de document {TypeDocumentId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du type de document");
            }
        }

        // POST: api/types-document
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<ActionResult<TypeDocumentDto>> CreateTypeDocument([FromBody] CreateTypeDocumentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier l'unicité du libellé
                var existingTypeDocument = await _context.TypesDocument
                    .AnyAsync(t => t.Libelle.ToLower() == request.Libelle.ToLower());

                if (existingTypeDocument)
                {
                    return BadRequest($"Un type de document avec le libellé '{request.Libelle}' existe déjà");
                }

                var typeDocument = new TypeDocument
                {
                    Id = Guid.NewGuid(),
                    Libelle = request.Libelle
                };

                _context.TypesDocument.Add(typeDocument);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Type de document '{Libelle}' créé avec l'ID {Id}",
                    typeDocument.Libelle, typeDocument.Id);

                var result = new TypeDocumentDto
                {
                    Id = typeDocument.Id,
                    Libelle = typeDocument.Libelle,
                    NombreDocuments = 0
                };

                return CreatedAtAction(nameof(GetTypeDocument), new { id = typeDocument.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du type de document");
                return StatusCode(500, "Une erreur est survenue lors de la création du type de document");
            }
        }

        // PUT: api/types-document/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> UpdateTypeDocument(Guid id, [FromBody] UpdateTypeDocumentRequest request)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant du type de document est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var typeDocument = await _context.TypesDocument.FindAsync(id);
                if (typeDocument == null)
                {
                    return NotFound($"Type de document avec l'ID {id} introuvable");
                }

                // Vérifier l'unicité du libellé si modifié
                if (!string.IsNullOrEmpty(request.Libelle) &&
                    request.Libelle.ToLower() != typeDocument.Libelle.ToLower())
                {
                    var existingTypeDocument = await _context.TypesDocument
                        .AnyAsync(t => t.Id != id && t.Libelle.ToLower() == request.Libelle.ToLower());

                    if (existingTypeDocument)
                    {
                        return BadRequest($"Un type de document avec le libellé '{request.Libelle}' existe déjà");
                    }
                }

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.Libelle))
                    typeDocument.Libelle = request.Libelle;

                _context.Entry(typeDocument).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Type de document {Id} mis à jour avec succès", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du type de document {TypeDocumentId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour du type de document");
            }
        }

        // DELETE: api/types-document/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> DeleteTypeDocument(Guid id)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant du type de document est invalide");
                }

                var typeDocument = await _context.TypesDocument
                    .Include(t => t.Documents)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (typeDocument == null)
                {
                    return NotFound($"Type de document avec l'ID {id} introuvable");
                }

                // Vérifier si le type de document est utilisé
                if (typeDocument.Documents.Any())
                {
                    return BadRequest($"Impossible de supprimer le type de document '{typeDocument.Libelle}' car il contient {typeDocument.Documents.Count} document(s). " +
                                    "Veuillez d'abord supprimer ou modifier le type de tous les documents de ce type.");
                }

                _context.TypesDocument.Remove(typeDocument);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Type de document '{Libelle}' supprimé avec l'ID {Id}", typeDocument.Libelle, id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du type de document {TypeDocumentId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la suppression du type de document");
            }
        }

        // GET: api/types-document/{id}/documents
        [HttpGet("{id:guid}/documents")]
        public async Task<ActionResult<IEnumerable<DocumentTypeDto>>> GetDocumentsTypeDocument(
            Guid id,
            [FromQuery] Guid? clubId = null,
            [FromQuery] Guid? categorieId = null)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant du type de document est invalide");
                }

                // Vérifier que le type de document existe
                var typeDocumentExists = await _context.TypesDocument.AnyAsync(t => t.Id == id);
                if (!typeDocumentExists)
                {
                    return NotFound($"Type de document avec l'ID {id} introuvable");
                }

                var query = _context.Documents
                    .Include(d => d.Categorie)
                    .Include(d => d.Club)
                    .Where(d => d.TypeDocumentId == id);

                // Filtres optionnels
                if (clubId.HasValue)
                {
                    query = query.Where(d => d.ClubId == clubId.Value);
                }

                if (categorieId.HasValue)
                {
                    query = query.Where(d => d.CategorieId == categorieId.Value);
                }

                var documents = await query
                    .OrderBy(d => d.Nom)
                    .Select(d => new DocumentTypeDto
                    {
                        Id = d.Id,
                        Nom = d.Nom,
                        Description = d.Description,
                        CategorieLibelle = d.Categorie.Libelle,
                        ClubNom = d.Club.Name,
                        TailleFichier = d.Fichier.Length
                    })
                    .ToListAsync();

                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des documents du type {TypeDocumentId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des documents");
            }
        }

        // GET: api/types-document/statistiques
        [HttpGet("statistiques")]
        public async Task<ActionResult<TypeDocumentStatistiquesDto>> GetStatistiques()
        {
            try
            {
                var statistiques = await _context.TypesDocument
                    .Select(t => new
                    {
                        t.Id,
                        t.Libelle,
                        NombreDocuments = t.Documents.Count(),
                        TailleTotale = t.Documents.Sum(d => (long)d.Fichier.Length)
                    })
                    .ToListAsync();

                var result = new TypeDocumentStatistiquesDto
                {
                    NombreTypesDocument = statistiques.Count,
                    NombreTotalDocuments = statistiques.Sum(s => s.NombreDocuments),
                    TailleTotaleDocuments = statistiques.Sum(s => s.TailleTotale),
                    TypesDetails = statistiques.Select(s => new TypeDocumentStatistiqueDetailDto
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
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques des types de document");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
            }
        }

        // GET: api/types-document/populaires
        [HttpGet("populaires")]
        public async Task<ActionResult<IEnumerable<TypeDocumentDto>>> GetTypesDocumentPopulaires([FromQuery] int limite = 5)
        {
            try
            {
                var typesPopulaires = await _context.TypesDocument
                    .OrderByDescending(t => t.Documents.Count())
                    .Take(limite)
                    .Select(t => new TypeDocumentDto
                    {
                        Id = t.Id,
                        Libelle = t.Libelle,
                        NombreDocuments = t.Documents.Count()
                    })
                    .ToListAsync();

                return Ok(typesPopulaires);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des types de document populaires");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des types de document populaires");
            }
        }
    }

    // DTOs pour les types de document
    public class TypeDocumentDto
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public int NombreDocuments { get; set; }
    }

    public class TypeDocumentDetailDto : TypeDocumentDto
    {
        public List<DocumentTypeDto> Documents { get; set; } = new List<DocumentTypeDto>();
    }

    public class DocumentTypeDto
    {
        public Guid Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CategorieLibelle { get; set; } = string.Empty;
        public string ClubNom { get; set; } = string.Empty;
        public long TailleFichier { get; set; }
    }

    public class CreateTypeDocumentRequest
    {
        [Required(ErrorMessage = "Le libellé est obligatoire")]
        [MaxLength(100, ErrorMessage = "Le libellé ne peut pas dépasser 100 caractères")]
        public string Libelle { get; set; } = string.Empty;
    }

    public class UpdateTypeDocumentRequest
    {
        [MaxLength(100, ErrorMessage = "Le libellé ne peut pas dépasser 100 caractères")]
        public string? Libelle { get; set; }
    }

    public class TypeDocumentStatistiquesDto
    {
        public int NombreTypesDocument { get; set; }
        public int NombreTotalDocuments { get; set; }
        public long TailleTotaleDocuments { get; set; }
        public List<TypeDocumentStatistiqueDetailDto> TypesDetails { get; set; } = new List<TypeDocumentStatistiqueDetailDto>();
    }

    public class TypeDocumentStatistiqueDetailDto
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public int NombreDocuments { get; set; }
        public long TailleTotale { get; set; }
        public double PourcentageDocuments { get; set; }
    }
}