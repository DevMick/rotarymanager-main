// ==============================================================================
// 📧 CONTRÔLEUR EMAIL COMPLET POUR ROTARY CLUB - CORRIGÉ
// ==============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RotaryClubManager.Application.Services;
using RotaryClubManager.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Text.Json.Serialization;

namespace RotaryClubManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [EnableRateLimiting("EmailPolicy")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailController> _logger;
        private readonly IConfiguration _configuration;

        public EmailController(
            IEmailService emailService,
            ILogger<EmailController> logger,
            IConfiguration configuration)
        {
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Envoie un email aux membres du club avec template professionnel
        /// </summary>
        /// <param name="request">Données de l'email</param>
        /// <returns>Résultat de l'envoi</returns>
        [HttpPost("send")]
        [ProducesResponseType(typeof(EmailResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 429)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
        {
            try
            {
                _logger.LogInformation("Tentative d'envoi d'email avec template - Destinataires: {Count}", request.Recipients?.Count ?? 0);

                // Validation de la requête
                var validationResult = await ValidateEmailRequest(request);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Validation échouée: {Errors}", string.Join(", ", validationResult.Errors));
                    return BadRequest(new ErrorResponse
                    {
                        Success = false,
                        Message = "Données invalides",
                        Errors = validationResult.Errors.ToArray()
                    });
                }

                // Envoi de l'email avec template professionnel
                var result = await _emailService.SendEmailAsync(request);

                if (result.Success)
                {
                    _logger.LogInformation("Email envoyé avec succès - ID: {EmailId}, Destinataires: {Count}",
                        result.EmailId, result.RecipientsSent);

                    return Ok(new EmailResponse
                    {
                        Success = true,
                        Message = "Email envoyé avec succès",
                        EmailId = result.EmailId,
                        RecipientsSent = result.RecipientsSent,
                        RecipientsTotal = request.Recipients.Count,
                        SentAt = DateTime.UtcNow
                    });
                }
                else
                {
                    _logger.LogError("Échec de l'envoi d'email: {Message}", result.ErrorMessage);
                    return StatusCode(500, new ErrorResponse
                    {
                        Success = false,
                        Message = "Erreur lors de l'envoi de l'email",
                        Errors = new[] { result.ErrorMessage }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de l'envoi d'email");
                return StatusCode(500, new ErrorResponse
                {
                    Success = false,
                    Message = "Erreur interne du serveur",
                    Errors = new[] { "Une erreur inattendue s'est produite" }
                });
            }
        }

        /// <summary>
        /// Envoie un email simple sans template professionnel
        /// </summary>
        /// <param name="request">Données de l'email</param>
        /// <returns>Résultat de l'envoi</returns>
        [HttpPost("send-simple")]
        [ProducesResponseType(typeof(EmailResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 401)]
        [ProducesResponseType(typeof(ErrorResponse), 429)]
        [ProducesResponseType(typeof(ErrorResponse), 500)]
        public async Task<IActionResult> SendSimpleEmail([FromBody] EmailRequest request)
        {
            try
            {
                _logger.LogInformation("Tentative d'envoi d'email simple - Destinataires: {Count}", request.Recipients?.Count ?? 0);

                // Validation de la requête
                var validationResult = await ValidateEmailRequest(request);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Validation échouée: {Errors}", string.Join(", ", validationResult.Errors));
                    return BadRequest(new ErrorResponse
                    {
                        Success = false,
                        Message = "Données invalides",
                        Errors = validationResult.Errors.ToArray()
                    });
                }

                // Envoi de l'email sans template professionnel
                var result = await _emailService.SendSimpleEmailAsync(request);

                if (result.Success)
                {
                    _logger.LogInformation("Email simple envoyé avec succès - ID: {EmailId}, Destinataires: {Count}",
                        result.EmailId, result.RecipientsSent);

                    return Ok(new EmailResponse
                    {
                        Success = true,
                        Message = "Email simple envoyé avec succès",
                        EmailId = result.EmailId,
                        RecipientsSent = result.RecipientsSent,
                        RecipientsTotal = request.Recipients.Count,
                        SentAt = DateTime.UtcNow
                    });
                }
                else
                {
                    _logger.LogError("Échec de l'envoi d'email simple: {Message}", result.ErrorMessage);
                    return StatusCode(500, new ErrorResponse
                    {
                        Success = false,
                        Message = "Erreur lors de l'envoi de l'email simple",
                        Errors = new[] { result.ErrorMessage }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de l'envoi d'email simple");
                return StatusCode(500, new ErrorResponse
                {
                    Success = false,
                    Message = "Erreur interne du serveur",
                    Errors = new[] { "Une erreur inattendue s'est produite" }
                });
            }
        }

        /// <summary>
        /// Obtient l'historique des emails envoyés
        /// </summary>
        [HttpGet("history")]
        [ProducesResponseType(typeof(List<EmailHistoryItem>), 200)]
        public async Task<IActionResult> GetEmailHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var history = await _emailService.GetEmailHistoryAsync(page, pageSize);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'historique");
                return StatusCode(500, new ErrorResponse
                {
                    Success = false,
                    Message = "Erreur lors de la récupération de l'historique"
                });
            }
        }

        /// <summary>
        /// Obtient le statut d'un email envoyé
        /// </summary>
        [HttpGet("status/{emailId}")]
        [ProducesResponseType(typeof(EmailStatusResponse), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> GetEmailStatus(string emailId)
        {
            try
            {
                var status = await _emailService.GetEmailStatusAsync(emailId);
                if (status == null)
                {
                    return NotFound(new ErrorResponse
                    {
                        Success = false,
                        Message = "Email non trouvé"
                    });
                }

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du statut");
                return StatusCode(500, new ErrorResponse
                {
                    Success = false,
                    Message = "Erreur lors de la récupération du statut"
                });
            }
        }

        private async Task<EmailValidationResult> ValidateEmailRequest(EmailRequest request)
        {
            var errors = new List<string>();

            // Validation des champs obligatoires
            if (string.IsNullOrWhiteSpace(request.Subject))
                errors.Add("Le sujet est obligatoire");

            if (string.IsNullOrWhiteSpace(request.Message))
                errors.Add("Le message est obligatoire");

            if (request.Recipients == null || !request.Recipients.Any())
                errors.Add("Au moins un destinataire est requis");

            // Validation des emails
            if (request.Recipients != null)
            {
                var invalidEmails = request.Recipients
                    .Where(email => !IsValidEmail(email))
                    .ToList();

                if (invalidEmails.Any())
                    errors.Add($"Emails invalides: {string.Join(", ", invalidEmails)}");
            }

            // Limites de l'application
            var maxRecipients = _configuration.GetValue<int>("Email:MaxRecipients", 100);
            if (request.Recipients?.Count > maxRecipients)
                errors.Add($"Maximum {maxRecipients} destinataires autorisés");

            var maxMessageLength = _configuration.GetValue<int>("Email:MaxMessageLength", 10000);
            if (request.Message?.Length > maxMessageLength)
                errors.Add($"Message trop long (max {maxMessageLength} caractères)");

            var maxSubjectLength = _configuration.GetValue<int>("Email:MaxSubjectLength", 200);
            if (request.Subject?.Length > maxSubjectLength)
                errors.Add($"Sujet trop long (max {maxSubjectLength} caractères)");

            // Validation des pièces jointes
            if (request.Attachments?.Any() == true)
            {
                var maxAttachments = _configuration.GetValue<int>("Email:MaxAttachments", 5);
                if (request.Attachments.Count > maxAttachments)
                    errors.Add($"Maximum {maxAttachments} pièces jointes autorisées");

                var maxAttachmentSize = _configuration.GetValue<long>("Email:MaxAttachmentSizeMB", 10) * 1024 * 1024;
                foreach (var attachment in request.Attachments)
                {
                    if (attachment.ContentSize > maxAttachmentSize)
                        errors.Add($"Pièce jointe '{attachment.FileName}' trop volumineuse");
                }
            }

            await Task.CompletedTask; // Pour éviter l'avertissement async

            return new EmailValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Résultat de validation d'email - Nom modifié pour éviter le conflit
    /// </summary>
    public class EmailValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}