using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RotaryClubManager.Application.DTOs.Formation;
using RotaryClubManager.Application.Services.Formation;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace RotaryClubManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FormationController : ControllerBase
    {
        private readonly IFormationService _formationService;

        public FormationController(IFormationService formationService)
        {
            _formationService = formationService;
        }

        // ===== GESTION DES DOCUMENTS DE FORMATION =====

        /// <summary>
        /// Upload un document de formation pour un club
        /// </summary>
        [HttpPost("clubs/{clubId}/documents")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Admin,President")]
        public async Task<ActionResult<DocumentFormationDto>> UploadDocument(
            Guid clubId,
            IFormFile file,
            [FromForm] string titre,
            [FromForm] string description,
            [FromForm] int type = 0,
            [FromForm] bool estActif = true)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                Console.WriteLine($"=== UPLOAD DOCUMENT - DEBUT ===");
                Console.WriteLine($"ClubId: {clubId}");
                Console.WriteLine($"Fichier: {file?.FileName}, Taille: {file?.Length} bytes");
                Console.WriteLine($"Titre: {titre}");
                Console.WriteLine($"Description: {description?.Substring(0, Math.Min(100, description?.Length ?? 0))}...");

                // Validation du fichier
                if (file == null || file.Length == 0)
                    return BadRequest("Aucun fichier fourni");

                if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Seuls les fichiers PDF sont acceptés");

                // Validation des données
                if (string.IsNullOrWhiteSpace(titre))
                    return BadRequest("Le titre est requis");

                if (string.IsNullOrWhiteSpace(description))
                    return BadRequest("La description est requise");

                // Récupération de l'utilisateur connecté
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    // Pour les tests sans authentification, utiliser un userId par défaut
                    userId = "fed93b49-c76d-4e50-b796-06a88da28e6d";
                }

                Console.WriteLine($"UserId: {userId}");

                // Création du DTO à partir des paramètres reçus
                var createDto = new CreateDocumentFormationDto
                {
                    Titre = titre.Trim(),
                    Description = description.Trim(),
                    Type = (Domain.Entities.Formation.TypeDocumentFormation)type,
                    EstActif = estActif
                };

                // Création du DTO pour le fichier
                var uploadFileDto = new UploadFileDto
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    Length = file.Length,
                    FileStream = file.OpenReadStream()
                };

                Console.WriteLine("Appel du service FormationService...");

                // Appel au service pour enregistrer le document
                var document = await _formationService.UploadDocumentAsync(clubId, userId, uploadFileDto, createDto);

                Console.WriteLine($"Document créé avec ID: {document.Id}");
                Console.WriteLine($"NombreChunks initial: {document.NombreChunks}");

                // Attendre un peu pour que le traitement en arrière-plan se termine
                Console.WriteLine("Attente du traitement en arrière-plan (5 secondes)...");
                await Task.Delay(5000);

                // Re-récupérer le document pour voir les chunks mis à jour
                var updatedDocument = await _formationService.GetDocumentAsync(document.Id, clubId);
                if (updatedDocument != null)
                {
                    Console.WriteLine($"NombreChunks après traitement: {updatedDocument.NombreChunks}");
                    document = updatedDocument;
                }

                var processingTime = DateTime.UtcNow - startTime;
                Console.WriteLine($"=== UPLOAD TERMINÉ en {processingTime.TotalSeconds:F2} secondes ===");

                return CreatedAtAction(
                    nameof(GetDocument),
                    new { clubId, documentId = document.Id },
                    document);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"ERREUR VALIDATION: {ex.Message}");
                return BadRequest($"Erreur de validation: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERREUR INTERNE: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, $"Erreur interne: {ex.Message}");
            }
        }

        /// <summary>
        /// Récupère un document de formation par ID
        /// </summary>
        [HttpGet("clubs/{clubId}/documents/{documentId}")]
        public async Task<ActionResult<DocumentFormationDto>> GetDocument(Guid clubId, Guid documentId)
        {
            try
            {
                var document = await _formationService.GetDocumentAsync(documentId, clubId);
                if (document == null)
                    return NotFound("Document non trouvé");

                return Ok(document);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur interne: {ex.Message}");
            }
        }

        /// <summary>
        /// Récupère tous les documents de formation d'un club
        /// </summary>
        [HttpGet("clubs/{clubId}/documents")]
        public async Task<ActionResult<List<DocumentFormationDto>>> GetDocumentsByClub(Guid clubId)
        {
            try
            {
                var documents = await _formationService.GetDocumentsByClubAsync(clubId);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur interne: {ex.Message}");
            }
        }

        /// <summary>
        /// Met à jour un document de formation
        /// </summary>
        [HttpPut("clubs/{clubId}/documents/{documentId}")]
        [Authorize(Roles = "Admin,President")]
        public async Task<ActionResult<DocumentFormationDto>> UpdateDocument(
            Guid clubId,
            Guid documentId,
            [FromBody] UpdateDocumentFormationDto updateDto)
        {
            try
            {
                var document = await _formationService.UpdateDocumentAsync(documentId, clubId, updateDto);
                if (document == null)
                    return NotFound("Document non trouvé");

                return Ok(document);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur interne: {ex.Message}");
            }
        }

        /// <summary>
        /// Supprime un document de formation
        /// </summary>
        [HttpDelete("clubs/{clubId}/documents/{documentId}")]
        [Authorize(Roles = "Admin,President")]
        public async Task<ActionResult> DeleteDocument(Guid clubId, Guid documentId)
        {
            try
            {
                var success = await _formationService.DeleteDocumentAsync(documentId, clubId);
                if (!success)
                    return NotFound("Document non trouvé");

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur interne: {ex.Message}");
            }
        }

        // ===== GESTION DES SESSIONS DE FORMATION =====

        /// <summary>
        /// Démarre une nouvelle session de formation
        /// </summary>
        [HttpPost("sessions")]
        public async Task<ActionResult<SessionFormationDto>> StartSession([FromBody] CreateSessionFormationDto createDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var session = await _formationService.StartSessionAsync(userId, createDto);
                return CreatedAtAction(nameof(GetSession), new { sessionId = session.Id }, session);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur interne: {ex.Message}");
            }
        }

        /// <summary>
        /// Récupère une session de formation par ID
        /// </summary>
        [HttpGet("sessions/{sessionId}")]
        public async Task<ActionResult<SessionFormationDto>> GetSession(Guid sessionId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var session = await _formationService.GetSessionAsync(sessionId, userId);
                if (session == null)
                    return NotFound("Session non trouvée");

                return Ok(session);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur interne: {ex.Message}");
            }
        }

        /// <summary>
        /// Récupère toutes les sessions de formation de l'utilisateur connecté
        /// </summary>
        [HttpGet("sessions")]
        public async Task<ActionResult<List<SessionFormationDto>>> GetUserSessions()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var sessions = await _formationService.GetSessionsByUserAsync(userId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur interne: {ex.Message}");
            }
        }

        /// <summary>
        /// Récupère toutes les sessions de formation d'un club (Admin/President uniquement)
        /// </summary>
        [HttpGet("clubs/{clubId}/sessions")]
        [Authorize(Roles = "Admin,President")]
        public async Task<ActionResult<List<SessionFormationDto>>> GetClubSessions(Guid clubId)
        {
            try
            {
                var sessions = await _formationService.GetSessionsByClubAsync(clubId);
                return Ok(sessions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur interne: {ex.Message}");
            }
        }

        // ===== GESTION DES QUESTIONS ET RÉPONSES =====

        /// <summary>
        /// Récupère les questions pour une session de formation
        /// </summary>
        [HttpGet("sessions/{sessionId}/questions")]
        public async Task<ActionResult<List<QuestionFormationDto>>> GetSessionQuestions(Guid sessionId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var questions = await _formationService.GetQuestionsForSessionAsync(sessionId, userId);
                return Ok(questions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur interne: {ex.Message}");
            }
        }

        /// <summary>
        /// Soumet une réponse à une question
        /// </summary>
        [HttpPost("sessions/{sessionId}/responses")]
        public async Task<ActionResult<ResultatReponseDto>> SubmitReponse(
            Guid sessionId,
            [FromBody] ReponseUtilisateurDto reponseDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var resultat = await _formationService.SubmitReponseAsync(sessionId, userId, reponseDto);
                return Ok(resultat);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur interne: {ex.Message}");
            }
        }

        // ===== GESTION DES BADGES ET PROGRESSION =====

        /// <summary>
        /// Récupère les badges de l'utilisateur connecté
        /// </summary>
        [HttpGet("badges")]
        public async Task<ActionResult<List<BadgeFormationDto>>> GetUserBadges()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var badges = await _formationService.GetBadgesByUserAsync(userId);
                return Ok(badges);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur interne: {ex.Message}");
            }
        }

        /// <summary>
        /// Récupère la progression de formation de l'utilisateur connecté
        /// </summary>
        [HttpGet("progression")]
        public async Task<ActionResult<ProgressionFormationDto>> GetUserProgression()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var progression = await _formationService.GetProgressionByUserAsync(userId);
                return Ok(progression);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur interne: {ex.Message}");
            }
        }

        /// <summary>
        /// Récupère la progression de formation de tous les membres d'un club (Admin/President uniquement)
        /// </summary>
        [HttpGet("clubs/{clubId}/progression")]
        [Authorize(Roles = "Admin,President")]
        public async Task<ActionResult<List<ProgressionFormationDto>>> GetClubProgression(Guid clubId)
        {
            try
            {
                var progression = await _formationService.GetProgressionByClubAsync(clubId);
                return Ok(progression);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur interne: {ex.Message}");
            }
        }

        // ===== RECHERCHE SÉMANTIQUE =====

        /// <summary>
        /// Recherche sémantique dans les documents de formation d'un club
        /// </summary>
        [HttpGet("clubs/{clubId}/search")]
        public async Task<ActionResult<List<DocumentFormationDto>>> SearchDocuments(Guid clubId, [FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return BadRequest("Le paramètre de recherche est requis");

                var documents = await _formationService.SearchDocumentsAsync(clubId, query);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur interne: {ex.Message}");
            }
        }
    }
}