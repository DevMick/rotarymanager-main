using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/clubs/{clubId}/documents")]
    [ApiController]
    [Authorize]
    public class DocumentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(
            ApplicationDbContext context,
            ILogger<DocumentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/clubs/{clubId}/documents
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DocumentListeDto>>> GetDocuments(
            Guid clubId,
            [FromQuery] Guid? categorieId = null,
            [FromQuery] Guid? typeDocumentId = null,
            [FromQuery] string? recherche = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
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

                var query = _context.Documents
                    .Include(d => d.Categorie)
                    .Include(d => d.TypeDocument)
                    .Include(d => d.Club)
                    .Where(d => d.ClubId == clubId);

                // Filtres optionnels
                if (categorieId.HasValue)
                {
                    query = query.Where(d => d.CategorieId == categorieId.Value);
                }

                if (typeDocumentId.HasValue)
                {
                    query = query.Where(d => d.TypeDocumentId == typeDocumentId.Value);
                }

                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(d => d.Nom.ToLower().Contains(termeLower) ||
                                           (d.Description != null && d.Description.ToLower().Contains(termeLower)));
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var totalPages = Math.Ceiling((double)totalItems / pageSize);

                var documents = await query
                    .OrderBy(d => d.Nom)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(d => new DocumentListeDto
                    {
                        Id = d.Id,
                        Nom = d.Nom,
                        Description = d.Description,
                        TailleFichier = d.Fichier.Length,
                        CategorieId = d.CategorieId,
                        CategorieLibelle = d.Categorie.Libelle,
                        TypeDocumentId = d.TypeDocumentId,
                        TypeDocumentLibelle = d.TypeDocument.Libelle,
                        ClubId = d.ClubId,
                        ClubNom = d.Club.Name
                    })
                    .ToListAsync();

                // Headers de pagination
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());

                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des documents du club {ClubId}", clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des documents");
            }
        }

        // GET: api/clubs/{clubId}/documents/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<DocumentDetailDto>> GetDocument(Guid clubId, Guid id)
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
                    return BadRequest("L'identifiant du document est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                var document = await _context.Documents
                    .Include(d => d.Categorie)
                    .Include(d => d.TypeDocument)
                    .Include(d => d.Club)
                    .Where(d => d.Id == id && d.ClubId == clubId)
                    .Select(d => new DocumentDetailDto
                    {
                        Id = d.Id,
                        Nom = d.Nom,
                        Description = d.Description,
                        TailleFichier = d.Fichier.Length,
                        CategorieId = d.CategorieId,
                        CategorieLibelle = d.Categorie.Libelle,
                        TypeDocumentId = d.TypeDocumentId,
                        TypeDocumentLibelle = d.TypeDocument.Libelle,
                        ClubId = d.ClubId,
                        ClubNom = d.Club.Name
                    })
                    .FirstOrDefaultAsync();

                if (document == null)
                {
                    return NotFound($"Document avec l'ID {id} non trouvé dans le club {clubId}");
                }

                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du document {DocumentId} du club {ClubId}", id, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du document");
            }
        }

        // POST: api/clubs/{clubId}/documents
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary,Treasurer")]
        public async Task<ActionResult<DocumentDetailDto>> CreateDocument(Guid clubId, [FromForm] CreateDocumentRequest request)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
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

                // Vérifier que la catégorie existe
                var categorie = await _context.Categories.FindAsync(request.CategorieId);
                if (categorie == null)
                {
                    return BadRequest("Catégorie non trouvée");
                }

                // Vérifier que le type de document existe
                var typeDocument = await _context.TypesDocument.FindAsync(request.TypeDocumentId);
                if (typeDocument == null)
                {
                    return BadRequest("Type de document non trouvé");
                }

                // Vérifier l'unicité du nom dans le club/catégorie/type
                var existingDocument = await _context.Documents
                    .AnyAsync(d => d.ClubId == clubId &&
                                 d.CategorieId == request.CategorieId &&
                                 d.TypeDocumentId == request.TypeDocumentId &&
                                 d.Nom.ToLower() == request.Nom.ToLower());

                if (existingDocument)
                {
                    return BadRequest($"Un document avec le nom '{request.Nom}' existe déjà dans cette catégorie et ce type pour ce club");
                }

                // Valider et lire le fichier
                if (request.Fichier == null || request.Fichier.Length == 0)
                {
                    return BadRequest("Le fichier est obligatoire");
                }

                // Limite de taille (par exemple 50MB)
                const long maxFileSize = 50 * 1024 * 1024;
                if (request.Fichier.Length > maxFileSize)
                {
                    return BadRequest($"Le fichier est trop volumineux. Taille maximale autorisée : {maxFileSize / (1024 * 1024)}MB");
                }

                byte[] fichierBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await request.Fichier.CopyToAsync(memoryStream);
                    fichierBytes = memoryStream.ToArray();
                }

                var document = new Document
                {
                    Id = Guid.NewGuid(),
                    Nom = request.Nom,
                    Description = request.Description,
                    Fichier = fichierBytes,
                    CategorieId = request.CategorieId,
                    TypeDocumentId = request.TypeDocumentId,
                    ClubId = clubId
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Document '{Nom}' créé pour le club {ClubId} avec l'ID {Id}",
                    document.Nom, clubId, document.Id);

                var result = new DocumentDetailDto
                {
                    Id = document.Id,
                    Nom = document.Nom,
                    Description = document.Description,
                    TailleFichier = document.Fichier.Length,
                    CategorieId = document.CategorieId,
                    CategorieLibelle = categorie.Libelle,
                    TypeDocumentId = document.TypeDocumentId,
                    TypeDocumentLibelle = typeDocument.Libelle,
                    ClubId = document.ClubId,
                    ClubNom = club.Name
                };

                return CreatedAtAction(nameof(GetDocument), new { clubId, id = document.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du document pour le club {ClubId}", clubId);
                return StatusCode(500, "Une erreur est survenue lors de la création du document");
            }
        }

        // PUT: api/clubs/{clubId}/documents/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary,Treasurer")]
        public async Task<IActionResult> UpdateDocument(Guid clubId, Guid id, [FromBody] UpdateDocumentRequest request)
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
                    return BadRequest("L'identifiant du document est invalide");
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

                var document = await _context.Documents
                    .FirstOrDefaultAsync(d => d.Id == id && d.ClubId == clubId);

                if (document == null)
                {
                    return NotFound($"Document avec l'ID {id} non trouvé dans le club {clubId}");
                }

                // Vérifier l'unicité du nom si modifié
                if (!string.IsNullOrEmpty(request.Nom) &&
                    request.Nom.ToLower() != document.Nom.ToLower())
                {
                    var existingDocument = await _context.Documents
                        .AnyAsync(d => d.Id != id &&
                                     d.ClubId == clubId &&
                                     d.CategorieId == (request.CategorieId ?? document.CategorieId) &&
                                     d.TypeDocumentId == (request.TypeDocumentId ?? document.TypeDocumentId) &&
                                     d.Nom.ToLower() == request.Nom.ToLower());

                    if (existingDocument)
                    {
                        return BadRequest($"Un document avec le nom '{request.Nom}' existe déjà dans cette catégorie et ce type pour ce club");
                    }
                }

                // Vérifier que la nouvelle catégorie existe si spécifiée
                if (request.CategorieId.HasValue && request.CategorieId != document.CategorieId)
                {
                    var categorieExists = await _context.Categories.AnyAsync(c => c.Id == request.CategorieId.Value);
                    if (!categorieExists)
                    {
                        return BadRequest("Catégorie non trouvée");
                    }
                }

                // Vérifier que le nouveau type de document existe si spécifié
                if (request.TypeDocumentId.HasValue && request.TypeDocumentId != document.TypeDocumentId)
                {
                    var typeDocumentExists = await _context.TypesDocument.AnyAsync(t => t.Id == request.TypeDocumentId.Value);
                    if (!typeDocumentExists)
                    {
                        return BadRequest("Type de document non trouvé");
                    }
                }

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.Nom))
                    document.Nom = request.Nom;

                if (request.Description != null)
                    document.Description = request.Description;

                if (request.CategorieId.HasValue)
                    document.CategorieId = request.CategorieId.Value;

                if (request.TypeDocumentId.HasValue)
                    document.TypeDocumentId = request.TypeDocumentId.Value;

                _context.Entry(document).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Document {Id} mis à jour dans le club {ClubId}", id, clubId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du document {DocumentId} du club {ClubId}", id, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour du document");
            }
        }

        // DELETE: api/clubs/{clubId}/documents/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> DeleteDocument(Guid clubId, Guid id)
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
                    return BadRequest("L'identifiant du document est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                var document = await _context.Documents
                    .FirstOrDefaultAsync(d => d.Id == id && d.ClubId == clubId);

                if (document == null)
                {
                    return NotFound($"Document avec l'ID {id} non trouvé dans le club {clubId}");
                }

                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Document '{Nom}' supprimé du club {ClubId}", document.Nom, clubId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du document {DocumentId} du club {ClubId}", id, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression du document");
            }
        }

        // GET: api/clubs/{clubId}/documents/{id}/telecharger
        [HttpGet("{id:guid}/telecharger")]
        public async Task<IActionResult> TelechargerDocument(Guid clubId, Guid id)
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
                    return BadRequest("L'identifiant du document est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                var document = await _context.Documents
                    .FirstOrDefaultAsync(d => d.Id == id && d.ClubId == clubId);

                if (document == null)
                {
                    return NotFound($"Document avec l'ID {id} non trouvé dans le club {clubId}");
                }

                // Déterminer le type MIME basé sur l'extension du fichier
                var contentType = GetContentType(document.Nom);

                return File(document.Fichier, contentType, document.Nom);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléchargement du document {DocumentId} du club {ClubId}", id, clubId);
                return StatusCode(500, "Une erreur est survenue lors du téléchargement du document");
            }
        }

        // GET: api/clubs/{clubId}/documents/statistiques
        [HttpGet("statistiques")]
        public async Task<ActionResult<DocumentStatistiquesDto>> GetStatistiques(Guid clubId)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                var statistiques = await _context.Documents
                    .Include(d => d.Categorie)
                    .Include(d => d.TypeDocument)
                    .Where(d => d.ClubId == clubId)
                    .GroupBy(d => new { d.CategorieId, d.Categorie.Libelle })
                    .Select(g => new DocumentStatistiqueDetailDto
                    {
                        CategorieId = g.Key.CategorieId,
                        CategorieLibelle = g.Key.Libelle,
                        NombreDocuments = g.Count(),
                        TailleTotale = g.Sum(d => (long)d.Fichier.Length)
                    })
                    .ToListAsync();

                var result = new DocumentStatistiquesDto
                {
                    ClubId = clubId,
                    NombreTotalDocuments = statistiques.Sum(s => s.NombreDocuments),
                    TailleTotaleDocuments = statistiques.Sum(s => s.TailleTotale),
                    CategoriesDetails = statistiques.OrderByDescending(s => s.NombreDocuments).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques des documents du club {ClubId}", clubId);
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

        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".txt" => "text/plain",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                _ => "application/octet-stream"
            };
        }
    }

    // DTOs pour les documents
    public class DocumentListeDto
    {
        public Guid Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string? Description { get; set; }
        public long TailleFichier { get; set; }
        public Guid CategorieId { get; set; }
        public string CategorieLibelle { get; set; } = string.Empty;
        public Guid TypeDocumentId { get; set; }
        public string TypeDocumentLibelle { get; set; } = string.Empty;
        public Guid ClubId { get; set; }
        public string ClubNom { get; set; } = string.Empty;
    }

    public class DocumentDetailDto : DocumentListeDto
    {
        // Hérite de toutes les propriétés de DocumentListeDto
    }

    public class CreateDocumentRequest
    {
        [Required(ErrorMessage = "Le nom est obligatoire")]
        [MaxLength(200, ErrorMessage = "Le nom ne peut pas dépasser 200 caractères")]
        public string Nom { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "La description ne peut pas dépasser 1000 caractères")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Le fichier est obligatoire")]
        public IFormFile Fichier { get; set; } = null!;

        [Required(ErrorMessage = "La catégorie est obligatoire")]
        public Guid CategorieId { get; set; }

        [Required(ErrorMessage = "Le type de document est obligatoire")]
        public Guid TypeDocumentId { get; set; }
    }

    public class UpdateDocumentRequest
    {
        [MaxLength(200, ErrorMessage = "Le nom ne peut pas dépasser 200 caractères")]
        public string? Nom { get; set; }

        [MaxLength(1000, ErrorMessage = "La description ne peut pas dépasser 1000 caractères")]
        public string? Description { get; set; }

        public Guid? CategorieId { get; set; }

        public Guid? TypeDocumentId { get; set; }
    }

    public class DocumentStatistiquesDto
    {
        public Guid ClubId { get; set; }
        public int NombreTotalDocuments { get; set; }
        public long TailleTotaleDocuments { get; set; }
        public List<DocumentStatistiqueDetailDto> CategoriesDetails { get; set; } = new List<DocumentStatistiqueDetailDto>();
    }

    public class DocumentStatistiqueDetailDto
    {
        public Guid CategorieId { get; set; }
        public string CategorieLibelle { get; set; } = string.Empty;
        public int NombreDocuments { get; set; }
        public long TailleTotale { get; set; }
    }
}