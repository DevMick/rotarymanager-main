using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EvenementDocumentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EvenementDocumentController> _logger;

        public EvenementDocumentController(ApplicationDbContext context, ILogger<EvenementDocumentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/EvenementDocument
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetEvenementDocuments(
            [FromQuery] Guid? evenementId = null,
            [FromQuery] string? libelle = null,
            [FromQuery] DateTime? dateDebut = null,
            [FromQuery] DateTime? dateFin = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.EvenementDocuments
                    .Include(d => d.Evenement)
                    .AsQueryable();

                // Filtres
                if (evenementId.HasValue)
                {
                    query = query.Where(d => d.EvenementId == evenementId.Value);
                }

                if (!string.IsNullOrEmpty(libelle))
                {
                    query = query.Where(d => d.Libelle != null && d.Libelle.Contains(libelle));
                }

                if (dateDebut.HasValue)
                {
                    query = query.Where(d => d.DateAjout >= dateDebut.Value);
                }

                if (dateFin.HasValue)
                {
                    query = query.Where(d => d.DateAjout <= dateFin.Value);
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var documents = await query
                    .OrderByDescending(d => d.DateAjout)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(d => new
                    {
                        d.Id,
                        d.Libelle,
                        d.DateAjout,
                        d.EvenementId,
                        EvenementLibelle = d.Evenement.Libelle,
                        TailleDocument = d.Document.Length
                    })
                    .ToListAsync();

                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());

                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des documents d'événements");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // GET: api/EvenementDocument/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetEvenementDocument(Guid id)
        {
            try
            {
                var document = await _context.EvenementDocuments
                    .Include(d => d.Evenement)
                    .Where(d => d.Id == id)
                    .Select(d => new
                    {
                        d.Id,
                        d.Libelle,
                        d.DateAjout,
                        d.EvenementId,
                        EvenementLibelle = d.Evenement.Libelle,
                        TailleDocument = d.Document.Length
                    })
                    .FirstOrDefaultAsync();

                if (document == null)
                {
                    return NotFound($"Document avec l'ID {id} non trouvé");
                }

                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du document {Id}", id);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // GET: api/EvenementDocument/5/download
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadDocument(Guid id)
        {
            try
            {
                var document = await _context.EvenementDocuments.FindAsync(id);

                if (document == null)
                {
                    return NotFound($"Document avec l'ID {id} non trouvé");
                }

                var fileName = !string.IsNullOrEmpty(document.Libelle)
                    ? $"{document.Libelle}.pdf"
                    : $"document_{id}.pdf";

                _logger.LogInformation("Téléchargement du document {Id} - {FileName}", id, fileName);

                return File(document.Document, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléchargement du document {Id}", id);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // POST: api/EvenementDocument
        [HttpPost]
        public async Task<ActionResult<EvenementDocument>> CreateEvenementDocument([FromForm] CreateEvenementDocumentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier que l'événement existe
                var evenementExists = await _context.Evenements.AnyAsync(e => e.Id == request.EvenementId);
                if (!evenementExists)
                {
                    return BadRequest($"Événement avec l'ID {request.EvenementId} non trouvé");
                }

                // Validation du fichier
                if (request.Document == null || request.Document.Length == 0)
                {
                    return BadRequest("Le document est requis");
                }

                // Limite de taille (ex: 10MB)
                if (request.Document.Length > 10 * 1024 * 1024)
                {
                    return BadRequest("Le document ne peut pas dépasser 10MB");
                }

                // Lire le contenu du fichier
                byte[] documentBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await request.Document.CopyToAsync(memoryStream);
                    documentBytes = memoryStream.ToArray();
                }

                var evenementDocument = new EvenementDocument
                {
                    Id = Guid.NewGuid(),
                    Libelle = request.Libelle ?? request.Document.FileName,
                    Document = documentBytes,
                    DateAjout = DateTime.UtcNow,
                    EvenementId = request.EvenementId
                };

                _context.EvenementDocuments.Add(evenementDocument);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Document créé avec l'ID {Id} pour l'événement {EvenementId}",
                    evenementDocument.Id, request.EvenementId);

                return CreatedAtAction(nameof(GetEvenementDocument),
                    new { id = evenementDocument.Id }, evenementDocument);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du document");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // PUT: api/EvenementDocument/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvenementDocument(Guid id, [FromBody] UpdateEvenementDocumentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var document = await _context.EvenementDocuments.FindAsync(id);
                if (document == null)
                {
                    return NotFound($"Document avec l'ID {id} non trouvé");
                }

                // Mise à jour uniquement du libellé (le document binaire ne change pas via PUT)
                document.Libelle = request.Libelle;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Document {Id} mis à jour", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du document {Id}", id);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // PUT: api/EvenementDocument/5/replace
        [HttpPut("{id}/replace")]
        public async Task<IActionResult> ReplaceDocument(Guid id, [FromForm] ReplaceDocumentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var document = await _context.EvenementDocuments.FindAsync(id);
                if (document == null)
                {
                    return NotFound($"Document avec l'ID {id} non trouvé");
                }

                // Validation du nouveau fichier
                if (request.NouveauDocument == null || request.NouveauDocument.Length == 0)
                {
                    return BadRequest("Le nouveau document est requis");
                }

                if (request.NouveauDocument.Length > 10 * 1024 * 1024)
                {
                    return BadRequest("Le document ne peut pas dépasser 10MB");
                }

                // Lire le contenu du nouveau fichier
                byte[] documentBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await request.NouveauDocument.CopyToAsync(memoryStream);
                    documentBytes = memoryStream.ToArray();
                }

                // Remplacer le document
                document.Document = documentBytes;
                document.DateAjout = DateTime.UtcNow; // Mettre à jour la date de modification

                if (!string.IsNullOrEmpty(request.NouveauLibelle))
                {
                    document.Libelle = request.NouveauLibelle;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Document {Id} remplacé", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du remplacement du document {Id}", id);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // DELETE: api/EvenementDocument/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvenementDocument(Guid id)
        {
            try
            {
                var document = await _context.EvenementDocuments.FindAsync(id);
                if (document == null)
                {
                    return NotFound($"Document avec l'ID {id} non trouvé");
                }

                _context.EvenementDocuments.Remove(document);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Document {Id} supprimé", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du document {Id}", id);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // GET: api/EvenementDocument/evenement/5
        [HttpGet("evenement/{evenementId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetDocumentsByEvenement(Guid evenementId)
        {
            try
            {
                var evenementExists = await _context.Evenements.AnyAsync(e => e.Id == evenementId);
                if (!evenementExists)
                {
                    return NotFound($"Événement avec l'ID {evenementId} non trouvé");
                }

                var documents = await _context.EvenementDocuments
                    .Where(d => d.EvenementId == evenementId)
                    .OrderBy(d => d.Libelle)
                    .Select(d => new
                    {
                        d.Id,
                        d.Libelle,
                        d.DateAjout,
                        TailleDocument = d.Document.Length
                    })
                    .ToListAsync();

                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des documents pour l'événement {EvenementId}", evenementId);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }
    }

    // DTOs pour les requêtes
    public class CreateEvenementDocumentRequest
    {
        [Required]
        public Guid EvenementId { get; set; }

        public string? Libelle { get; set; }

        [Required]
        public IFormFile Document { get; set; } = null!;
    }

    public class UpdateEvenementDocumentRequest
    {
        public string? Libelle { get; set; }
    }

    public class ReplaceDocumentRequest
    {
        [Required]
        public IFormFile NouveauDocument { get; set; } = null!;

        public string? NouveauLibelle { get; set; }
    }
}