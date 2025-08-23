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
    public class EvenementImageController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EvenementImageController> _logger;

        // Types MIME autorisés pour les images
        private static readonly string[] AllowedImageTypes =
        {
            "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp", "image/bmp"
        };

        public EvenementImageController(ApplicationDbContext context, ILogger<EvenementImageController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Méthode utilitaire pour configurer les headers de réponse
        private void ConfigureResponseHeaders()
        {
            try
            {
                // Seulement pour les réponses avec contenu (pas 204)
                if (Response.StatusCode != 204)
                {
                    Response.Headers.Remove("Transfer-Encoding");
                    Response.Headers.Add("Transfer-Encoding", "identity");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Impossible de configurer les headers de réponse");
            }
        }

        // GET: api/EvenementImage
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetEvenementImages(
            [FromQuery] Guid? evenementId = null,
            [FromQuery] string? description = null,
            [FromQuery] DateTime? dateDebut = null,
            [FromQuery] DateTime? dateFin = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12) // Par défaut 12 pour une grille d'images
        {
            try
            {
                ConfigureResponseHeaders();

                var query = _context.EvenementImages
                    .Include(i => i.Evenement)
                    .AsQueryable();

                // Filtres
                if (evenementId.HasValue)
                {
                    query = query.Where(i => i.EvenementId == evenementId.Value);
                }

                if (!string.IsNullOrEmpty(description))
                {
                    query = query.Where(i => i.Description != null && i.Description.Contains(description));
                }

                if (dateDebut.HasValue)
                {
                    query = query.Where(i => i.DateAjout >= dateDebut.Value);
                }

                if (dateFin.HasValue)
                {
                    query = query.Where(i => i.DateAjout <= dateFin.Value);
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var images = await query
                    .OrderByDescending(i => i.DateAjout)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(i => new
                    {
                        i.Id,
                        i.Description,
                        i.DateAjout,
                        i.EvenementId,
                        EvenementLibelle = i.Evenement.Libelle,
                        TailleImage = i.Image.Length,
                        // URL pour afficher l'image
                        ImageUrl = $"/api/EvenementImage/{i.Id}/display"
                    })
                    .ToListAsync();

                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());

                return Ok(images);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des images d'événements");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // GET: api/EvenementImage/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetEvenementImage(Guid id)
        {
            try
            {
                ConfigureResponseHeaders();

                var image = await _context.EvenementImages
                    .Include(i => i.Evenement)
                    .Where(i => i.Id == id)
                    .Select(i => new
                    {
                        i.Id,
                        i.Description,
                        i.DateAjout,
                        i.EvenementId,
                        EvenementLibelle = i.Evenement.Libelle,
                        TailleImage = i.Image.Length,
                        ImageUrl = $"/api/EvenementImage/{i.Id}/display"
                    })
                    .FirstOrDefaultAsync();

                if (image == null)
                {
                    return NotFound(new { message = $"Image avec l'ID {id} non trouvée" });
                }

                return Ok(image);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'image {Id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // GET: api/EvenementImage/5/display
        [HttpGet("{id}/display")]
        [AllowAnonymous] // Permet l'affichage sans authentification pour les images
        public async Task<IActionResult> DisplayImage(Guid id)
        {
            try
            {
                var image = await _context.EvenementImages.FindAsync(id);

                if (image == null)
                {
                    return NotFound();
                }

                // Déterminer le type MIME de l'image
                var contentType = GetImageContentType(image.Image);

                return File(image.Image, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'affichage de l'image {Id}", id);
                return StatusCode(500);
            }
        }

        // GET: api/EvenementImage/5/thumbnail
        [HttpGet("{id}/thumbnail")]
        [AllowAnonymous]
        public async Task<IActionResult> GetThumbnail(Guid id, [FromQuery] int width = 150, [FromQuery] int height = 150)
        {
            try
            {
                var image = await _context.EvenementImages.FindAsync(id);

                if (image == null)
                {
                    return NotFound();
                }

                // Note: Pour une vraie application, vous pourriez vouloir générer et mettre en cache des miniatures
                // Pour l'instant, on retourne l'image originale
                var contentType = GetImageContentType(image.Image);

                return File(image.Image, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération de la miniature {Id}", id);
                return StatusCode(500);
            }
        }

        // GET: api/EvenementImage/5/download
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadImage(Guid id)
        {
            try
            {
                var image = await _context.EvenementImages.FindAsync(id);

                if (image == null)
                {
                    return NotFound(new { message = $"Image avec l'ID {id} non trouvée" });
                }

                var contentType = GetImageContentType(image.Image);
                var extension = GetImageExtension(contentType);
                var fileName = !string.IsNullOrEmpty(image.Description)
                    ? $"{image.Description}{extension}"
                    : $"image_{id}{extension}";

                _logger.LogInformation("Téléchargement de l'image {Id} - {FileName}", id, fileName);

                return File(image.Image, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléchargement de l'image {Id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // POST: api/EvenementImage
        [HttpPost]
        public async Task<ActionResult<object>> CreateEvenementImage([FromForm] CreateEvenementImageRequest request)
        {
            try
            {
                ConfigureResponseHeaders();

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier que l'événement existe
                var evenementExists = await _context.Evenements.AnyAsync(e => e.Id == request.EvenementId);
                if (!evenementExists)
                {
                    return BadRequest(new { message = $"Événement avec l'ID {request.EvenementId} non trouvé" });
                }

                // Validation du fichier image
                if (request.Image == null || request.Image.Length == 0)
                {
                    return BadRequest(new { message = "L'image est requise" });
                }

                // Vérifier le type MIME
                if (!AllowedImageTypes.Contains(request.Image.ContentType.ToLower()))
                {
                    return BadRequest(new { message = $"Type d'image non autorisé. Types acceptés: {string.Join(", ", AllowedImageTypes)}" });
                }

                // Lire le contenu du fichier
                byte[] imageBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await request.Image.CopyToAsync(memoryStream);
                    imageBytes = memoryStream.ToArray();
                }

                var evenementImage = new EvenementImage
                {
                    Id = Guid.NewGuid(),
                    Image = imageBytes,
                    Description = request.Description,
                    DateAjout = DateTime.UtcNow,
                    EvenementId = request.EvenementId
                };

                _context.EvenementImages.Add(evenementImage);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Image créée avec l'ID {Id} pour l'événement {EvenementId}",
                    evenementImage.Id, request.EvenementId);

                // Retourner une projection simple
                var result = new
                {
                    evenementImage.Id,
                    evenementImage.Description,
                    evenementImage.DateAjout,
                    evenementImage.EvenementId,
                    TailleImage = imageBytes.Length,
                    ImageUrl = $"/api/EvenementImage/{evenementImage.Id}/display"
                };

                return CreatedAtAction(nameof(GetEvenementImage),
                    new { id = evenementImage.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'image");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // POST: api/EvenementImage/multiple
        [HttpPost("multiple")]
        public async Task<ActionResult<IEnumerable<object>>> CreateMultipleImages([FromForm] CreateMultipleImagesRequest request)
        {
            try
            {
                ConfigureResponseHeaders();

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier que l'événement existe
                var evenementExists = await _context.Evenements.AnyAsync(e => e.Id == request.EvenementId);
                if (!evenementExists)
                {
                    return BadRequest(new { message = $"Événement avec l'ID {request.EvenementId} non trouvé" });
                }

                if (request.Images == null || !request.Images.Any())
                {
                    return BadRequest(new { message = "Au moins une image est requise" });
                }

                var createdImages = new List<object>();
                var skippedFiles = new List<string>();

                foreach (var imageFile in request.Images)
                {
                    // Validation de chaque fichier
                    if (!AllowedImageTypes.Contains(imageFile.ContentType.ToLower()))
                    {
                        skippedFiles.Add($"{imageFile.FileName} (type non autorisé)");
                        continue; // Skip les fichiers non valides
                    }

                    // Lire le contenu
                    byte[] imageBytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        await imageFile.CopyToAsync(memoryStream);
                        imageBytes = memoryStream.ToArray();
                    }

                    var evenementImage = new EvenementImage
                    {
                        Id = Guid.NewGuid(),
                        Image = imageBytes,
                        Description = imageFile.FileName,
                        DateAjout = DateTime.UtcNow,
                        EvenementId = request.EvenementId
                    };

                    _context.EvenementImages.Add(evenementImage);
                    createdImages.Add(new
                    {
                        evenementImage.Id,
                        evenementImage.Description,
                        evenementImage.DateAjout,
                        TailleImage = imageBytes.Length,
                        ImageUrl = $"/api/EvenementImage/{evenementImage.Id}/display"
                    });
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("{Count} images créées pour l'événement {EvenementId}",
                    createdImages.Count, request.EvenementId);

                var result = new
                {
                    ImagesCreees = createdImages,
                    NombreCreees = createdImages.Count,
                    FichiersIgnores = skippedFiles
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création des images multiples");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // PUT: api/EvenementImage/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvenementImage(Guid id, [FromBody] UpdateEvenementImageRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var image = await _context.EvenementImages.FindAsync(id);
                if (image == null)
                {
                    return NotFound(new { message = $"Image avec l'ID {id} non trouvée" });
                }

                // Mise à jour uniquement de la description
                image.Description = request.Description;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Image {Id} mise à jour", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'image {Id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // DELETE: api/EvenementImage/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvenementImage(Guid id)
        {
            try
            {
                var image = await _context.EvenementImages.FindAsync(id);
                if (image == null)
                {
                    return NotFound(new { message = $"Image avec l'ID {id} non trouvée" });
                }

                _context.EvenementImages.Remove(image);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Image {Id} supprimée", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'image {Id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // GET: api/EvenementImage/evenement/5
        [HttpGet("evenement/{evenementId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetImagesByEvenement(Guid evenementId)
        {
            try
            {
                ConfigureResponseHeaders();

                var evenementExists = await _context.Evenements.AnyAsync(e => e.Id == evenementId);
                if (!evenementExists)
                {
                    return NotFound(new { message = $"Événement avec l'ID {evenementId} non trouvé" });
                }

                var images = await _context.EvenementImages
                    .Where(i => i.EvenementId == evenementId)
                    .OrderByDescending(i => i.DateAjout)
                    .Select(i => new
                    {
                        i.Id,
                        i.Description,
                        i.DateAjout,
                        TailleImage = i.Image.Length,
                        ImageUrl = $"/api/EvenementImage/{i.Id}/display",
                        ThumbnailUrl = $"/api/EvenementImage/{i.Id}/thumbnail"
                    })
                    .ToListAsync();

                return Ok(images);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des images pour l'événement {EvenementId}", evenementId);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // GET: api/EvenementImage/evenement/5/gallery
        [HttpGet("evenement/{evenementId}/gallery")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> GetImageGallery(Guid evenementId)
        {
            try
            {
                ConfigureResponseHeaders();

                var evenement = await _context.Evenements.FindAsync(evenementId);
                if (evenement == null)
                {
                    return NotFound(new { message = $"Événement avec l'ID {evenementId} non trouvé" });
                }

                var images = await _context.EvenementImages
                    .Where(i => i.EvenementId == evenementId)
                    .OrderByDescending(i => i.DateAjout)
                    .Select(i => new
                    {
                        i.Id,
                        i.Description,
                        i.DateAjout,
                        ImageUrl = $"/api/EvenementImage/{i.Id}/display",
                        ThumbnailUrl = $"/api/EvenementImage/{i.Id}/thumbnail"
                    })
                    .ToListAsync();

                var gallery = new
                {
                    Evenement = new
                    {
                        evenement.Id,
                        evenement.Libelle,
                        evenement.Date,
                        evenement.Lieu
                    },
                    Images = images,
                    NombreImages = images.Count
                };

                return Ok(gallery);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la galerie pour l'événement {EvenementId}", evenementId);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // Méthodes utilitaires
        private static string GetImageContentType(byte[] imageBytes)
        {
            // Détection basique du type d'image via les headers
            if (imageBytes.Length >= 2)
            {
                // JPEG
                if (imageBytes[0] == 0xFF && imageBytes[1] == 0xD8)
                    return "image/jpeg";

                // PNG
                if (imageBytes.Length >= 8 && imageBytes[0] == 0x89 && imageBytes[1] == 0x50 &&
                    imageBytes[2] == 0x4E && imageBytes[3] == 0x47)
                    return "image/png";

                // GIF
                if (imageBytes.Length >= 6 && imageBytes[0] == 0x47 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46)
                    return "image/gif";

                // BMP
                if (imageBytes[0] == 0x42 && imageBytes[1] == 0x4D)
                    return "image/bmp";
            }

            return "image/jpeg"; // Par défaut
        }

        private static string GetImageExtension(string contentType)
        {
            return contentType.ToLower() switch
            {
                "image/jpeg" => ".jpg",
                "image/jpg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                _ => ".jpg"
            };
        }
    }

    // DTOs pour les requêtes
    public class CreateEvenementImageRequest
    {
        [Required]
        public Guid EvenementId { get; set; }

        public string? Description { get; set; }

        [Required]
        public IFormFile Image { get; set; } = null!;
    }

    public class CreateMultipleImagesRequest
    {
        [Required]
        public Guid EvenementId { get; set; }

        [Required]
        public IFormFileCollection Images { get; set; } = null!;
    }

    public class UpdateEvenementImageRequest
    {
        public string? Description { get; set; }
    }
}