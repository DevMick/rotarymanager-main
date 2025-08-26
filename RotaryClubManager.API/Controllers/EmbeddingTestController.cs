using Microsoft.AspNetCore.Mvc;
using RotaryClubManager.Application.Services.Formation;
using RotaryClubManager.Domain.Entities.Formation;
using RotaryClubManager.Application.DTOs.Formation;
using System.Text.Json;

namespace RotaryClubManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmbeddingTestController : ControllerBase
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IFormationRepository _formationRepository;
        private readonly IConfiguration _configuration;

        public EmbeddingTestController(
            IEmbeddingService embeddingService,
            IFormationRepository formationRepository,
            IConfiguration configuration)
        {
            _embeddingService = embeddingService;
            _formationRepository = formationRepository;
            _configuration = configuration;
        }

        /// <summary>
        /// Test complet : Upload PDF + Extraction + Chunking + Embedding
        /// </summary>
        [HttpPost("test-full-process")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> TestFullProcess(
            IFormFile file,
            [FromForm] string titre = "Document de test",
            [FromForm] string description = "Test complet du système d'embedding",
            [FromForm] string clubId = "525418ac-1bf3-4da2-9f4e-1fd13dd03119")
        {
            try
            {
                var startTime = DateTime.UtcNow;
                Console.WriteLine($"=== DEBUT TEST COMPLET ===");
                Console.WriteLine($"Fichier: {file.FileName}, Taille: {file.Length} bytes");

                // 1. Validation du fichier
                if (file == null || file.Length == 0)
                    return BadRequest("Aucun fichier fourni");

                if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Seuls les fichiers PDF sont acceptés");

                var clubGuid = Guid.Parse(clubId);
                var userId = "fed93b49-c76d-4e50-b796-06a88da28e6d"; // User de test

                // 2. Sauvegarde temporaire du fichier
                var uploadPath = _configuration["Formation:Upload:StoragePath"] ?? "uploads/formation/";
                var fileName = $"TEST_{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadPath, fileName);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                Console.WriteLine($"Fichier sauvegardé: {filePath}");

                // 3. Créer le document en base
                var document = new DocumentFormation
                {
                    Id = Guid.NewGuid(),
                    Titre = titre,
                    Description = description,
                    CheminFichier = filePath,
                    DateUpload = DateTime.UtcNow,
                    UploadePar = userId,
                    ClubId = clubGuid,
                    EstActif = true,
                    Type = TypeDocumentFormation.Autre
                };

                await _formationRepository.CreateDocumentAsync(document);
                Console.WriteLine($"Document créé en base: {document.Id}");

                // 4. Test extraction PDF
                Console.WriteLine("=== EXTRACTION PDF ===");
                using var pdfStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var pages = await _embeddingService.ExtractTextFromPdfAsync(pdfStream);
                Console.WriteLine($"Pages extraites: {pages.Count}");

                if (!pages.Any())
                {
                    return BadRequest("Impossible d'extraire le texte du PDF");
                }

                // Afficher un aperçu du contenu
                foreach (var (page, index) in pages.Select((p, i) => (p, i)))
                {
                    var preview = page.Length > 200 ? page.Substring(0, 200) + "..." : page;
                    Console.WriteLine($"Page {index + 1}: {preview}");
                }

                // 5. Test chunking
                Console.WriteLine("=== CHUNKING ===");
                var allChunks = new List<ChunkDocument>();
                var chunkIndex = 0;

                foreach (var (pageText, pageIndex) in pages.Select((p, i) => (p, i)))
                {
                    var textChunks = await _embeddingService.ChunkTextAsync(pageText, 800);
                    Console.WriteLine($"Page {pageIndex + 1}: {textChunks.Count} chunks créés");

                    foreach (var chunkText in textChunks)
                    {
                        if (string.IsNullOrWhiteSpace(chunkText))
                            continue;

                        Console.WriteLine($"Chunk {chunkIndex}: {chunkText.Length} caractères");

                        // 6. Test embedding
                        var embedding = await _embeddingService.GenerateEmbeddingAsync(chunkText);
                        Console.WriteLine($"Embedding généré: {embedding.Length} dimensions");

                        var chunk = new ChunkDocument
                        {
                            Id = Guid.NewGuid(),
                            DocumentFormationId = document.Id,
                            Contenu = chunkText,
                            IndexChunk = chunkIndex++,
                            Embedding = embedding,
                            Metadata = JsonSerializer.Serialize(new
                            {
                                page = pageIndex + 1,
                                length = chunkText.Length,
                                created = DateTime.UtcNow,
                                test = true
                            })
                        };

                        allChunks.Add(chunk);
                    }
                }

                // 7. Sauvegarde des chunks
                Console.WriteLine($"=== SAUVEGARDE DE {allChunks.Count} CHUNKS ===");
                if (allChunks.Any())
                {
                    await _formationRepository.CreateChunksAsync(allChunks);
                    Console.WriteLine("Chunks sauvegardés avec succès");
                }

                // 8. Vérification finale
                var savedChunks = await _formationRepository.GetChunksByDocumentAsync(document.Id);
                Console.WriteLine($"Vérification: {savedChunks.Count} chunks trouvés en base");

                var processingTime = DateTime.UtcNow - startTime;

                return Ok(new
                {
                    Success = true,
                    DocumentId = document.Id,
                    FileName = file.FileName,
                    FileSize = file.Length,
                    PagesExtracted = pages.Count,
                    ChunksCreated = allChunks.Count,
                    ChunksSaved = savedChunks.Count,
                    ProcessingTimeMs = (int)processingTime.TotalMilliseconds,
                    FilePath = filePath,
                    ChunkDetails = savedChunks.Select(c => new
                    {
                        c.Id,
                        c.IndexChunk,
                        ContentLength = c.Contenu.Length,
                        HasEmbedding = c.Embedding != null && c.Embedding.Any(),
                        EmbeddingSize = c.Embedding?.Length ?? 0,
                        ContentPreview = c.Contenu.Length > 100 ? c.Contenu.Substring(0, 100) + "..." : c.Contenu
                    }).ToList(),
                    Message = "Test complet réussi !"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERREUR TEST COMPLET: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                return StatusCode(500, new
                {
                    Success = false,
                    Message = $"Erreur: {ex.Message}",
                    StackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Test de traitement d'un document PDF existant
        /// </summary>
        [HttpPost("process-existing-document/{documentId}")]
        public async Task<IActionResult> ProcessExistingDocument(Guid documentId)
        {
            try
            {
                Console.WriteLine($"=== TRAITEMENT DOCUMENT EXISTANT {documentId} ===");

                var document = await _formationRepository.GetDocumentByIdAsync(documentId);
                if (document == null)
                {
                    return NotFound($"Document {documentId} non trouvé");
                }

                Console.WriteLine($"Document trouvé: {document.Titre}");
                Console.WriteLine($"Chemin fichier: {document.CheminFichier}");

                if (!System.IO.File.Exists(document.CheminFichier))
                {
                    return BadRequest($"Fichier physique non trouvé: {document.CheminFichier}");
                }

                var result = await _embeddingService.ProcessDocumentChunksAsync(documentId);

                if (result)
                {
                    var chunks = await _formationRepository.GetChunksByDocumentAsync(documentId);
                    Console.WriteLine($"Traitement terminé: {chunks.Count} chunks créés");

                    return Ok(new
                    {
                        Success = true,
                        DocumentId = documentId,
                        DocumentTitle = document.Titre,
                        ChunksCreated = chunks.Count,
                        Chunks = chunks.Select(c => new
                        {
                            c.Id,
                            c.IndexChunk,
                            ContentLength = c.Contenu.Length,
                            HasEmbedding = c.Embedding != null && c.Embedding.Any(),
                            ContentPreview = c.Contenu.Length > 150 ? c.Contenu.Substring(0, 150) + "..." : c.Contenu
                        }).ToList(),
                        Message = "Document traité avec succès"
                    });
                }
                else
                {
                    return BadRequest("Échec du traitement du document");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERREUR TRAITEMENT: {ex.Message}");
                return StatusCode(500, $"Erreur: {ex.Message}");
            }
        }

        /// <summary>
        /// Voir le statut d'un document et ses chunks
        /// </summary>
        [HttpGet("document/{documentId}/status")]
        public async Task<IActionResult> GetDocumentStatus(Guid documentId)
        {
            try
            {
                var document = await _formationRepository.GetDocumentByIdAsync(documentId);
                if (document == null)
                {
                    return NotFound($"Document {documentId} non trouvé");
                }

                var chunks = await _formationRepository.GetChunksByDocumentAsync(documentId);

                return Ok(new
                {
                    Document = new
                    {
                        document.Id,
                        document.Titre,
                        document.Description,
                        document.CheminFichier,
                        FileExists = System.IO.File.Exists(document.CheminFichier),
                        document.DateUpload,
                        document.EstActif
                    },
                    ChunksSummary = new
                    {
                        TotalChunks = chunks.Count,
                        WithEmbeddings = chunks.Count(c => c.Embedding != null && c.Embedding.Any()),
                        WithoutEmbeddings = chunks.Count(c => c.Embedding == null || !c.Embedding.Any()),
                        TotalContentLength = chunks.Sum(c => c.Contenu.Length),
                        AverageChunkSize = chunks.Any() ? chunks.Average(c => c.Contenu.Length) : 0
                    },
                    Chunks = chunks.Select(c => new
                    {
                        c.Id,
                        c.IndexChunk,
                        ContentLength = c.Contenu.Length,
                        HasEmbedding = c.Embedding != null && c.Embedding.Any(),
                        EmbeddingDimensions = c.Embedding?.Length ?? 0,
                        ContentPreview = c.Contenu.Length > 200 ? c.Contenu.Substring(0, 200) + "..." : c.Contenu,
                        c.Metadata
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur: {ex.Message}");
            }
        }

        /// <summary>
        /// Test simple d'extraction PDF
        /// </summary>
        [HttpPost("test-pdf-extraction")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> TestPdfExtraction(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("Aucun fichier fourni");

                Console.WriteLine($"Test extraction PDF: {file.FileName}");

                using var stream = file.OpenReadStream();
                var pages = await _embeddingService.ExtractTextFromPdfAsync(stream);

                return Ok(new
                {
                    FileName = file.FileName,
                    FileSize = file.Length,
                    PagesExtracted = pages.Count,
                    Pages = pages.Select((page, index) => new
                    {
                        PageNumber = index + 1,
                        Length = page.Length,
                        Preview = page.Length > 300 ? page.Substring(0, 300) + "..." : page
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur extraction PDF: {ex.Message}");
            }
        }
    }
}