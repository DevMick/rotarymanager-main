using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RotaryClubManager.Infrastructure.Services;
using System.Text.Json;

namespace RotaryClubManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MetaWhatsAppController : ControllerBase
    {
        private readonly MetaWhatsAppService _metaWhatsAppService;
        private readonly ILogger<MetaWhatsAppController> _logger;
        private readonly IConfiguration _configuration;

        public MetaWhatsAppController(
            MetaWhatsAppService metaWhatsAppService,
            ILogger<MetaWhatsAppController> logger,
            IConfiguration configuration)
        {
            _metaWhatsAppService = metaWhatsAppService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Envoie un message texte via Meta WhatsApp Business API
        /// </summary>
        [HttpPost("send")]
        [EnableRateLimiting("WhatsAppPolicy")]
        public async Task<IActionResult> SendMessage([FromBody] MetaWhatsAppRequest request)
        {
            if (string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Numéro de téléphone et message requis",
                    error = "MISSING_REQUIRED_FIELDS"
                });
            }

            var (success, messageId, error) = await _metaWhatsAppService.SendTextMessage(
                request.PhoneNumber,
                request.Message
            );

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    messageId,
                    message = "Message WhatsApp Business envoyé avec succès",
                    timestamp = DateTime.UtcNow,
                    to = request.PhoneNumber,
                    provider = "Meta WhatsApp Business API"
                });
            }

            return BadRequest(new
            {
                success = false,
                error,
                message = "Erreur lors de l'envoi via Meta WhatsApp Business"
            });
        }

        /// <summary>
        /// Envoie une notification de réunion avec boutons interactifs
        /// </summary>
        [HttpPost("send-meeting-notification")]
        [EnableRateLimiting("WhatsAppPolicy")]
        public async Task<IActionResult> SendMeetingNotification([FromBody] MeetingNotificationRequest request)
        {
            if (string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.MemberName))
            {
                return BadRequest("Numéro de téléphone et nom du membre requis");
            }

            var (success, messageId, error) = await _metaWhatsAppService.SendMeetingNotification(
                request.PhoneNumber,
                request.MemberName,
                request.MeetingDate,
                request.Location,
                request.Agenda
            );

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    messageId,
                    message = "Notification de réunion envoyée avec succès",
                    meetingDate = request.MeetingDate,
                    memberName = request.MemberName,
                    location = request.Location
                });
            }

            return BadRequest(new { success = false, error });
        }

        /// <summary>
        /// Envoie une notification d'événement avec boutons
        /// </summary>
        [HttpPost("send-event-notification")]
        [EnableRateLimiting("WhatsAppPolicy")]
        public async Task<IActionResult> SendEventNotification([FromBody] EventNotificationRequest request)
        {
            var (success, messageId, error) = await _metaWhatsAppService.SendEventNotification(
                request.PhoneNumber,
                request.MemberName,
                request.EventName,
                request.EventDate,
                request.Location,
                request.Price
            );

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    messageId,
                    message = "Notification d'événement envoyée avec succès",
                    eventName = request.EventName,
                    eventDate = request.EventDate
                });
            }

            return BadRequest(new { success = false, error });
        }

        /// <summary>
        /// Diffusion vers plusieurs membres du club
        /// </summary>
        [HttpPost("broadcast")]
        [EnableRateLimiting("WhatsAppPolicy")]
        public async Task<IActionResult> BroadcastMessage([FromBody] BroadcastRequest request)
        {
            if (request.PhoneNumbers == null || !request.PhoneNumbers.Any())
            {
                return BadRequest("Liste de numéros de téléphone requise");
            }

            if (string.IsNullOrEmpty(request.Message))
            {
                return BadRequest("Message requis");
            }

            var results = await _metaWhatsAppService.BroadcastMessage(
                request.PhoneNumbers,
                request.Message,
                request.DelayBetweenMessages ?? 1000
            );

            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);

            return Ok(new
            {
                message = "Diffusion terminée",
                totalSent = successCount,
                totalFailed = failureCount,
                successRate = $"{(double)successCount / results.Count * 100:F1}%",
                results = results.Select(r => new
                {
                    phoneNumber = r.PhoneNumber,
                    success = r.Success,
                    messageId = r.MessageId,
                    error = r.Error,
                    timestamp = r.Timestamp
                })
            });
        }

        /// <summary>
        /// Diffusion de notifications de réunion personnalisées
        /// </summary>
        [HttpPost("broadcast-meeting")]
        [EnableRateLimiting("WhatsAppPolicy")]
        public async Task<IActionResult> BroadcastMeetingNotifications([FromBody] BroadcastMeetingRequest request)
        {
            if (request.Members == null || !request.Members.Any())
            {
                return BadRequest("Liste des membres requise");
            }

            var results = await _metaWhatsAppService.BroadcastMeetingNotifications(
                request.Members,
                request.MeetingDate,
                request.Location,
                request.Agenda,
                request.DelayBetweenMessages ?? 2000
            );

            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);

            return Ok(new
            {
                message = "Notifications de réunion envoyées",
                meetingDate = request.MeetingDate,
                location = request.Location,
                totalSent = successCount,
                totalFailed = failureCount,
                members = results.Select(r => new
                {
                    name = r.MemberName,
                    phoneNumber = r.PhoneNumber,
                    success = r.Success,
                    messageId = r.MessageId,
                    error = r.Error
                })
            });
        }

        /// <summary>
        /// Envoie un message avec template pré-approuvé
        /// </summary>
        [HttpPost("send-template")]
        [EnableRateLimiting("WhatsAppPolicy")]
        public async Task<IActionResult> SendTemplateMessage([FromBody] TemplateMessageRequest request)
        {
            var (success, messageId, error) = await _metaWhatsAppService.SendTemplateMessage(
                request.PhoneNumber,
                request.TemplateName,
                request.LanguageCode ?? "fr",
                request.Parameters ?? Array.Empty<string>()
            );

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    messageId,
                    message = "Message template envoyé avec succès",
                    templateName = request.TemplateName,
                    languageCode = request.LanguageCode
                });
            }

            return BadRequest(new { success = false, error });
        }

        /// <summary>
        /// Webhook pour recevoir les messages entrants et les interactions
        /// </summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> ReceiveWebhook([FromBody] JsonElement payload)
        {
            try
            {
                _logger.LogInformation("Webhook reçu: {Payload}", payload.ToString());

                // Traitement des messages entrants
                if (payload.TryGetProperty("entry", out var entry))
                {
                    foreach (var entryItem in entry.EnumerateArray())
                    {
                        if (entryItem.TryGetProperty("changes", out var changes))
                        {
                            foreach (var change in changes.EnumerateArray())
                            {
                                if (change.TryGetProperty("value", out var value))
                                {
                                    // Messages entrants
                                    if (value.TryGetProperty("messages", out var messages))
                                    {
                                        foreach (var message in messages.EnumerateArray())
                                        {
                                            await ProcessIncomingMessage(message);
                                        }
                                    }

                                    // Statuts des messages
                                    if (value.TryGetProperty("statuses", out var statuses))
                                    {
                                        foreach (var status in statuses.EnumerateArray())
                                        {
                                            await ProcessMessageStatus(status);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement du webhook");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Vérification du webhook (requis par Meta)
        /// </summary>
        [HttpGet("webhook")]
        public IActionResult VerifyWebhook(
            [FromQuery(Name = "hub.mode")] string mode,
            [FromQuery(Name = "hub.challenge")] string challenge,
            [FromQuery(Name = "hub.verify_token")] string verifyToken)
        {
            var configuredToken = _configuration["Meta:WebhookVerifyToken"];

            if (mode == "subscribe" && verifyToken == configuredToken)
            {
                _logger.LogInformation("Webhook vérifié avec succès");
                return Ok(challenge);
            }

            _logger.LogWarning("Échec de vérification du webhook. Mode: {Mode}, Token reçu: {Token}", mode, verifyToken);
            return Unauthorized();
        }

        /// <summary>
        /// Test rapide avec le numéro de test Meta
        /// </summary>
        [HttpGet("test")]
        public async Task<IActionResult> TestMetaWhatsApp()
        {
            var testNumber = _configuration["Meta:TestNumber"] ?? "+15550572810";

            var (success, messageId, error) = await _metaWhatsAppService.SendTextMessage(
                testNumber,
                $"🧪 Test Meta WhatsApp Business API - {DateTime.Now:HH:mm:ss}"
            );

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    messageId,
                    message = "Test Meta WhatsApp réussi ✅",
                    testNumber,
                    timestamp = DateTime.UtcNow,
                    provider = "Meta WhatsApp Business API"
                });
            }

            return BadRequest(new
            {
                success = false,
                error,
                message = "Test Meta WhatsApp échoué ❌"
            });
        }

        /// <summary>
        /// Vérification de la configuration Meta
        /// </summary>
        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            try
            {
                var config = new
                {
                    AppId = !string.IsNullOrEmpty(_configuration["Meta:AppId"]) ?
                        $"{_configuration["Meta:AppId"]?[..10]}..." : "Non configuré",
                    PhoneNumberId = !string.IsNullOrEmpty(_configuration["Meta:PhoneNumberId"]) ?
                        $"{_configuration["Meta:PhoneNumberId"]?[..10]}..." : "Non configuré",
                    AccessToken = !string.IsNullOrEmpty(_configuration["Meta:AccessToken"]) ?
                        "Configuré (masqué)" : "Non configuré",
                    TestNumber = _configuration["Meta:TestNumber"],
                    ApiVersion = _configuration["Meta:ApiVersion"],
                    WebhookUrl = _configuration["Meta:WebhookUrl"],
                    IsConfigured = !string.IsNullOrEmpty(_configuration["Meta:PhoneNumberId"]) &&
                                  !string.IsNullOrEmpty(_configuration["Meta:AccessToken"])
                };

                return Ok(new
                {
                    success = true,
                    message = "Configuration Meta WhatsApp Business",
                    config
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur de configuration",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Vérification du statut d'un message
        /// </summary>
        [HttpGet("status/{messageId}")]
        public async Task<IActionResult> GetMessageStatus(string messageId)
        {
            var (success, status, error) = await _metaWhatsAppService.GetMessageStatus(messageId);

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    messageId,
                    status,
                    timestamp = DateTime.UtcNow
                });
            }

            return BadRequest(new { success = false, error });
        }

        #region Méthodes privées

        private async Task ProcessIncomingMessage(JsonElement message)
        {
            try
            {
                var from = message.GetProperty("from").GetString();
                var messageId = message.GetProperty("id").GetString();
                var timestamp = message.GetProperty("timestamp").GetString();

                string messageBody = null;
                string messageType = null;

                if (message.TryGetProperty("type", out var typeElement))
                {
                    messageType = typeElement.GetString();
                }

                if (message.TryGetProperty("text", out var textElement) &&
                    textElement.TryGetProperty("body", out var bodyElement))
                {
                    messageBody = bodyElement.GetString();
                }

                _logger.LogInformation("Message reçu de {From}: {Body} (Type: {Type}, ID: {MessageId})",
                    from, messageBody, messageType, messageId);

                // Traitement automatique des réponses
                if (messageType == "text" && !string.IsNullOrEmpty(messageBody))
                {
                    var response = GetAutoResponse(messageBody);
                    if (!string.IsNullOrEmpty(response))
                    {
                        await _metaWhatsAppService.SendTextMessage(from, response);
                    }
                }

                // TODO: Sauvegarder en base de données
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement du message entrant");
            }
        }

        private async Task ProcessMessageStatus(JsonElement status)
        {
            try
            {
                var messageId = status.GetProperty("id").GetString();
                var statusValue = status.GetProperty("status").GetString();
                var timestamp = status.GetProperty("timestamp").GetString();

                string recipientId = null;
                if (status.TryGetProperty("recipient_id", out var recipientElement))
                {
                    recipientId = recipientElement.GetString();
                }

                _logger.LogInformation("Status du message {MessageId}: {Status} pour {Recipient} à {Timestamp}",
                    messageId, statusValue, recipientId, timestamp);

                // TODO: Mettre à jour le statut en base de données
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement du statut");
            }
        }

        private string GetAutoResponse(string incomingMessage)
        {
            var message = incomingMessage.ToUpper().Trim();

            return message switch
            {
                "HELLO" or "SALUT" or "BONJOUR" =>
                    "👋 Bonjour ! Bienvenue au Rotary Club. Comment puis-je vous aider ?",

                "INFO" or "INFOS" =>
                    "ℹ️ Prochaine réunion : Vendredi 20h\n📍 Lieu : Hôtel Central\n📋 Plus d'infos sur notre site",

                "REUNION" or "MEETING" =>
                    "📅 Réunions tous les vendredis 20h\n📍 Hôtel Central, Salle Emeraude\n🤝 Venez nombreux !",

                "CONFIRME" or "OUI" or "YES" =>
                    "✅ Parfait ! Votre présence est confirmée. À bientôt !",

                "NON" or "NO" =>
                    "❌ Dommage ! Votre absence est notée. À la prochaine fois !",

                "EVENTS" or "EVENEMENTS" =>
                    "🎉 Événements à venir :\n• 15/09 - Dîner de gala\n• 22/09 - Action humanitaire\n• 30/09 - Assemblée générale",

                "HELP" or "AIDE" =>
                    "🆘 Commandes disponibles :\n• INFO - Infos réunion\n• EVENTS - Événements\n• REUNION - Horaires\n• HELP - Cette aide",

                "STOP" or "ARRET" =>
                    "✋ Vous avez été désabonné des notifications. Pour vous réabonner, contactez l'administration.",

                _ => null // Pas de réponse automatique pour les autres messages
            };
        }

        #endregion
    }

    #region Modèles de requête

    public class MetaWhatsAppRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class MeetingNotificationRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public DateTime MeetingDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Agenda { get; set; } = string.Empty;
    }

    public class EventNotificationRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal? Price { get; set; }
    }

    public class BroadcastRequest
    {
        public List<string> PhoneNumbers { get; set; } = new();
        public string Message { get; set; } = string.Empty;
        public int? DelayBetweenMessages { get; set; } = 1000;
    }

    public class BroadcastMeetingRequest
    {
        public List<MemberNotification> Members { get; set; } = new();
        public DateTime MeetingDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Agenda { get; set; } = string.Empty;
        public int? DelayBetweenMessages { get; set; } = 2000;
    }

    public class TemplateMessageRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = "fr";
        public string[] Parameters { get; set; } = Array.Empty<string>();
    }

    #endregion
}