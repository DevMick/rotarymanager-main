using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Infrastructure.Data;
using RotaryClubManager.Domain.Entities;
using System.ComponentModel.DataAnnotations;

[ApiController]
[Route("api/[controller]")]
public class ReunionDocumentController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReunionDocumentController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/ReunionDocument
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReunionDocumentDto>>> GetReunionDocuments()
    {
        var documents = await _context.ReunionDocuments
            .Include(d => d.Reunion)
            .Select(d => new ReunionDocumentDto
            {
                Id = d.Id,
                Libelle = d.Libelle,
                ReunionId = d.ReunionId,
                TailleDocument = d.Document.Length,
                // Inclure seulement les données basiques de la réunion pour éviter les références circulaires
                ReunionInfo = new ReunionBasicDto
                {
                    Id = d.Reunion.Id,
                    Date = d.Reunion.Date,
                    Heure = d.Reunion.Heure,
                    DateTimeComplete = d.Reunion.DateTimeComplete
                }
            })
            .OrderBy(d => d.Libelle)
            .ToListAsync();

        return documents;
    }

    // GET: api/ReunionDocument/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ReunionDocumentDto>> GetReunionDocument(Guid id)
    {
        var document = await _context.ReunionDocuments
            .Include(d => d.Reunion)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null)
        {
            return NotFound();
        }

        return new ReunionDocumentDto
        {
            Id = document.Id,
            Libelle = document.Libelle,
            ReunionId = document.ReunionId,
            TailleDocument = document.Document.Length,
            ReunionInfo = new ReunionBasicDto
            {
                Id = document.Reunion.Id,
                Date = document.Reunion.Date,
                Heure = document.Reunion.Heure,
                DateTimeComplete = document.Reunion.DateTimeComplete
            }
        };
    }

    // GET: api/ReunionDocument/reunion/5
    [HttpGet("reunion/{reunionId}")]
    public async Task<ActionResult<IEnumerable<ReunionDocumentDto>>> GetDocumentsByReunion(Guid reunionId)
    {
        // Vérifier si la réunion existe
        var reunionExists = await _context.Reunions.AnyAsync(r => r.Id == reunionId);
        if (!reunionExists)
        {
            return NotFound("La réunion spécifiée n'existe pas.");
        }

        var documents = await _context.ReunionDocuments
            .Include(d => d.Reunion)
            .Where(d => d.ReunionId == reunionId)
            .Select(d => new ReunionDocumentDto
            {
                Id = d.Id,
                Libelle = d.Libelle,
                ReunionId = d.ReunionId,
                TailleDocument = d.Document.Length,
                ReunionInfo = new ReunionBasicDto
                {
                    Id = d.Reunion.Id,
                    Date = d.Reunion.Date,
                    Heure = d.Reunion.Heure,
                    DateTimeComplete = d.Reunion.DateTimeComplete
                }
            })
            .OrderBy(d => d.Libelle)
            .ToListAsync();

        return documents;
    }

    // GET: api/ReunionDocument/5/download
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadDocument(Guid id)
    {
        var document = await _context.ReunionDocuments.FindAsync(id);
        if (document == null)
        {
            return NotFound();
        }

        // Déterminer le type MIME basé sur l'extension du fichier
        var contentType = GetContentType(document.Libelle);

        return File(document.Document, contentType, document.Libelle);
    }

    // POST: api/ReunionDocument/upload
    [HttpPost("upload")]
    public async Task<ActionResult<ReunionDocumentDto>> UploadDocument([FromForm] UploadDocumentDto uploadDto)
    {
        if (uploadDto.File == null || uploadDto.File.Length == 0)
        {
            return BadRequest("Aucun fichier fourni.");
        }

        // Vérifier si la réunion existe
        var reunionExists = await _context.Reunions.AnyAsync(r => r.Id == uploadDto.ReunionId);
        if (!reunionExists)
        {
            return BadRequest("La réunion spécifiée n'existe pas.");
        }

        // Vérifier la taille du fichier (par exemple, max 10MB)
        const int maxFileSize = 10 * 1024 * 1024; // 10MB
        if (uploadDto.File.Length > maxFileSize)
        {
            return BadRequest("Le fichier est trop volumineux. Taille maximale autorisée : 10MB.");
        }

        // Vérifier le type de fichier
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".jpg", ".jpeg", ".png" };
        var fileExtension = Path.GetExtension(uploadDto.File.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            return BadRequest("Type de fichier non autorisé.");
        }

        // Convertir le fichier en byte array
        byte[] documentBytes;
        using (var memoryStream = new MemoryStream())
        {
            await uploadDto.File.CopyToAsync(memoryStream);
            documentBytes = memoryStream.ToArray();
        }

        var libelle = !string.IsNullOrWhiteSpace(uploadDto.Libelle)
            ? uploadDto.Libelle
            : uploadDto.File.FileName;

        var reunionDocument = new ReunionDocument
        {
            Id = Guid.NewGuid(),
            Libelle = libelle,
            ReunionId = uploadDto.ReunionId,
            Document = documentBytes
        };

        _context.ReunionDocuments.Add(reunionDocument);
        await _context.SaveChangesAsync();

        // Récupérer le document avec les données de la réunion
        var documentAvecReunion = await _context.ReunionDocuments
            .Include(d => d.Reunion)
            .FirstOrDefaultAsync(d => d.Id == reunionDocument.Id);

        var result = new ReunionDocumentDto
        {
            Id = documentAvecReunion!.Id,
            Libelle = documentAvecReunion.Libelle,
            ReunionId = documentAvecReunion.ReunionId,
            TailleDocument = documentAvecReunion.Document.Length,
            ReunionInfo = new ReunionBasicDto
            {
                Id = documentAvecReunion.Reunion.Id,
                Date = documentAvecReunion.Reunion.Date,
                Heure = documentAvecReunion.Reunion.Heure,
                DateTimeComplete = documentAvecReunion.Reunion.DateTimeComplete
            }
        };

        return CreatedAtAction(nameof(GetReunionDocument), new { id = reunionDocument.Id }, result);
    }

    // PUT: api/ReunionDocument/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDocument(Guid id, UpdateReunionDocumentDto updateDto)
    {
        var document = await _context.ReunionDocuments.FindAsync(id);
        if (document == null)
        {
            return NotFound();
        }

        // Vérifier si la réunion existe (si changée)
        if (updateDto.ReunionId.HasValue && updateDto.ReunionId != document.ReunionId)
        {
            var reunionExists = await _context.Reunions.AnyAsync(r => r.Id == updateDto.ReunionId.Value);
            if (!reunionExists)
            {
                return BadRequest("La réunion spécifiée n'existe pas.");
            }
        }

        // Mettre à jour les propriétés
        if (!string.IsNullOrWhiteSpace(updateDto.Libelle))
            document.Libelle = updateDto.Libelle;

        if (updateDto.ReunionId.HasValue)
            document.ReunionId = updateDto.ReunionId.Value;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ReunionDocumentExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    // PUT: api/ReunionDocument/5/replace-file
    [HttpPut("{id}/replace-file")]
    public async Task<IActionResult> ReplaceDocumentFile(Guid id, [FromForm] ReplaceFileDto replaceDto)
    {
        var document = await _context.ReunionDocuments.FindAsync(id);
        if (document == null)
        {
            return NotFound();
        }

        if (replaceDto.File == null || replaceDto.File.Length == 0)
        {
            return BadRequest("Aucun fichier fourni.");
        }

        // Vérifier la taille du fichier
        const int maxFileSize = 10 * 1024 * 1024; // 10MB
        if (replaceDto.File.Length > maxFileSize)
        {
            return BadRequest("Le fichier est trop volumineux. Taille maximale autorisée : 10MB.");
        }

        // Vérifier le type de fichier
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".jpg", ".jpeg", ".png" };
        var fileExtension = Path.GetExtension(replaceDto.File.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            return BadRequest("Type de fichier non autorisé.");
        }

        // Convertir le nouveau fichier en byte array
        byte[] documentBytes;
        using (var memoryStream = new MemoryStream())
        {
            await replaceDto.File.CopyToAsync(memoryStream);
            documentBytes = memoryStream.ToArray();
        }

        // Remplacer le document
        document.Document = documentBytes;

        // Optionnellement, mettre à jour le libellé avec le nouveau nom de fichier
        if (!string.IsNullOrWhiteSpace(replaceDto.NouveauLibelle))
        {
            document.Libelle = replaceDto.NouveauLibelle;
        }
        else
        {
            document.Libelle = replaceDto.File.FileName;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/ReunionDocument/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        var document = await _context.ReunionDocuments.FindAsync(id);
        if (document == null)
        {
            return NotFound();
        }

        _context.ReunionDocuments.Remove(document);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/ReunionDocument/statistiques
    [HttpGet("statistiques")]
    public async Task<ActionResult<StatistiquesDocuments>> GetStatistiquesDocuments()
    {
        var documents = await _context.ReunionDocuments.ToListAsync();

        var stats = new StatistiquesDocuments
        {
            NombreTotal = documents.Count,
            TailleTotale = documents.Sum(d => (long)d.Document.Length),
            TailleMoyenne = documents.Any() ? documents.Average(d => d.Document.Length) : 0,
            TailleMax = documents.Any() ? documents.Max(d => d.Document.Length) : 0,
            TailleMin = documents.Any() ? documents.Min(d => d.Document.Length) : 0
        };

        return stats;
    }

    private bool ReunionDocumentExists(Guid id)
    {
        return _context.ReunionDocuments.Any(e => e.Id == id);
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
            ".txt" => "text/plain",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }
}

// DTOs
public class ReunionDocumentDto
{
    public Guid Id { get; set; }
    public string Libelle { get; set; } = string.Empty;
    public Guid ReunionId { get; set; }
    public int TailleDocument { get; set; }
    public ReunionBasicDto? ReunionInfo { get; set; }
}

public class ReunionBasicDto
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan Heure { get; set; }
    public DateTime DateTimeComplete { get; set; }
}

public class UploadDocumentDto
{
    [Required]
    public Guid ReunionId { get; set; }

    [Required]
    public IFormFile File { get; set; } = null!;

    [StringLength(200)]
    public string? Libelle { get; set; }
}

public class UpdateReunionDocumentDto
{
    [StringLength(200)]
    public string? Libelle { get; set; }

    public Guid? ReunionId { get; set; }
}

public class ReplaceFileDto
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [StringLength(200)]
    public string? NouveauLibelle { get; set; }
}

public class StatistiquesDocuments
{
    public int NombreTotal { get; set; }
    public long TailleTotale { get; set; }
    public double TailleMoyenne { get; set; }
    public int TailleMax { get; set; }
    public int TailleMin { get; set; }
}