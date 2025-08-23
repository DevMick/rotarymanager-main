using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RotaryClubManager.Infrastructure.Services
{
    public class MetaWhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MetaWhatsAppService> _logger;
        private readonly string _phoneNumberId;
        private readonly string _accessToken;
        private readonly string _apiVersion;

        public MetaWhatsAppService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<MetaWhatsAppService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            _phoneNumberId = _configuration["Meta:PhoneNumberId"] ?? throw new ArgumentNullException("Meta:PhoneNumberId");
            _accessToken = _configuration["Meta:AccessToken"] ?? throw new ArgumentNullException("Meta:AccessToken");
            _apiVersion = _configuration["Meta:ApiVersion"] ?? "v18.0";

            // Configuration du HttpClient
            _httpClient.BaseAddress = new Uri($"https://graph.facebook.com/{_apiVersion}/");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
        }

        /// <summary>
        /// Envoie un message texte simple
        /// </summary>
        public async Task<(bool Success, string MessageId, string Error)> SendTextMessage(
            string toPhoneNumber,
            string message)
        {
            try
            {
                var payload = new
                {
                    messaging_product = "whatsapp",
                    to = CleanPhoneNumber(toPhoneNumber),
                    type = "text",
                    text = new { body = message }
                };

                var response = await SendMessageRequest(payload);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi du message texte vers {PhoneNumber}", toPhoneNumber);
                return (false, null, ex.Message);
            }
        }

        /// <summary>
        /// Envoie un message avec template (pour notifications officielles)
        /// </summary>
        public async Task<(bool Success, string MessageId, string Error)> SendTemplateMessage(
            string toPhoneNumber,
            string templateName,
            string languageCode = "fr",
            params string[] parameters)
        {
            try
            {
                var components = new List<object>();

                if (parameters?.Length > 0)
                {
                    components.Add(new
                    {
                        type = "body",
                        parameters = parameters.Select(p => new { type = "text", text = p }).ToArray()
                    });
                }

                var payload = new
                {
                    messaging_product = "whatsapp",
                    to = CleanPhoneNumber(toPhoneNumber),
                    type = "template",
                    template = new
                    {
                        name = templateName,
                        language = new { code = languageCode },
                        components = components.ToArray()
                    }
                };

                var response = await SendMessageRequest(payload);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi du template {Template} vers {PhoneNumber}",
                    templateName, toPhoneNumber);
                return (false, null, ex.Message);
            }
        }

        /// <summary>
        /// Envoie un message avec boutons interactifs
        /// </summary>
        public async Task<(bool Success, string MessageId, string Error)> SendInteractiveMessage(
            string toPhoneNumber,
            string bodyText,
            List<WhatsAppButton> buttons)
        {
            try
            {
                var payload = new
                {
                    messaging_product = "whatsapp",
                    to = CleanPhoneNumber(toPhoneNumber),
                    type = "interactive",
                    interactive = new
                    {
                        type = "button",
                        body = new { text = bodyText },
                        action = new
                        {
                            buttons = buttons.Select(b => new
                            {
                                type = "reply",
                                reply = new
                                {
                                    id = b.Id,
                                    title = b.Title
                                }
                            }).ToArray()
                        }
                    }
                };

                var response = await SendMessageRequest(payload);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi du message interactif vers {PhoneNumber}", toPhoneNumber);
                return (false, null, ex.Message);
            }
        }

        /// <summary>
        /// Envoie une notification de réunion Rotary avec boutons
        /// </summary>
        public async Task<(bool Success, string MessageId, string Error)> SendMeetingNotification(
            string toPhoneNumber,
            string memberName,
            DateTime meetingDate,
            string location,
            string agenda)
        {
            var message = $"🌟 Bonjour {memberName},\n\n" +
                         $"📅 Prochaine réunion Rotary :\n" +
                         $"🗓️ Date : {meetingDate:dddd dd/MM/yyyy à HH:mm}\n" +
                         $"📍 Lieu : {location}\n" +
                         $"📋 Ordre du jour : {agenda}\n\n" +
                         $"Confirmez-vous votre présence ?";

            var buttons = new List<WhatsAppButton>
            {
                new("confirm_yes", "✅ Je confirme"),
                new("confirm_no", "❌ Je ne peux pas"),
                new("more_info", "ℹ️ Plus d'infos")
            };

            return await SendInteractiveMessage(toPhoneNumber, message, buttons);
        }

        /// <summary>
        /// Envoie une notification d'événement avec boutons
        /// </summary>
        public async Task<(bool Success, string MessageId, string Error)> SendEventNotification(
            string toPhoneNumber,
            string memberName,
            string eventName,
            DateTime eventDate,
            string location,
            decimal? price = null)
        {
            var priceText = price.HasValue ? $"💰 Prix : {price:C}\n" : "";

            var message = $"🎉 Bonjour {memberName},\n\n" +
                         $"Nouvel événement Rotary :\n" +
                         $"🎪 {eventName}\n" +
                         $"📅 {eventDate:dddd dd/MM/yyyy à HH:mm}\n" +
                         $"📍 {location}\n" +
                         $"{priceText}\n" +
                         $"Souhaitez-vous participer ?";

            var buttons = new List<WhatsAppButton>
            {
                new("event_yes", "✅ Je participe"),
                new("event_no", "❌ Pas dispo"),
                new("event_info", "📋 Détails")
            };

            return await SendInteractiveMessage(toPhoneNumber, message, buttons);
        }

        /// <summary>
        /// Diffusion vers plusieurs membres
        /// </summary>
        public async Task<List<MessageResult>> BroadcastMessage(
            List<string> phoneNumbers,
            string message,
            int delayBetweenMessages = 1000)
        {
            var results = new List<MessageResult>();

            foreach (var phoneNumber in phoneNumbers)
            {
                var (success, messageId, error) = await SendTextMessage(phoneNumber, message);

                results.Add(new MessageResult
                {
                    PhoneNumber = phoneNumber,
                    Success = success,
                    MessageId = messageId,
                    Error = error,
                    Timestamp = DateTime.UtcNow
                });

                // Délai entre les messages pour éviter les limites de taux
                if (delayBetweenMessages > 0)
                {
                    await Task.Delay(delayBetweenMessages);
                }
            }

            _logger.LogInformation("Diffusion terminée : {Sent}/{Total} messages envoyés",
                results.Count(r => r.Success), results.Count);

            return results;
        }

        /// <summary>
        /// Diffusion de notifications de réunion personnalisées
        /// </summary>
        public async Task<List<MessageResult>> BroadcastMeetingNotifications(
            List<MemberNotification> members,
            DateTime meetingDate,
            string location,
            string agenda,
            int delayBetweenMessages = 2000)
        {
            var results = new List<MessageResult>();

            foreach (var member in members)
            {
                var (success, messageId, error) = await SendMeetingNotification(
                    member.PhoneNumber,
                    member.Name,
                    meetingDate,
                    location,
                    agenda
                );

                results.Add(new MessageResult
                {
                    PhoneNumber = member.PhoneNumber,
                    Success = success,
                    MessageId = messageId,
                    Error = error,
                    Timestamp = DateTime.UtcNow,
                    MemberName = member.Name
                });

                if (delayBetweenMessages > 0)
                {
                    await Task.Delay(delayBetweenMessages);
                }
            }

            _logger.LogInformation("Notifications de réunion envoyées : {Sent}/{Total}",
                results.Count(r => r.Success), results.Count);

            return results;
        }

        /// <summary>
        /// Vérifie le statut d'un message
        /// </summary>
        public async Task<(bool Success, string Status, string Error)> GetMessageStatus(string messageId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{messageId}");
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var document = JsonDocument.Parse(content);
                    var status = document.RootElement.GetProperty("status").GetString();
                    return (true, status, null);
                }
                else
                {
                    return (false, null, $"HTTP {response.StatusCode}: {content}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification du statut du message {MessageId}", messageId);
                return (false, null, ex.Message);
            }
        }

        private async Task<(bool Success, string MessageId, string Error)> SendMessageRequest(object payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_phoneNumberId}/messages", content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<WhatsAppApiResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });
                    var messageId = result?.Messages?.FirstOrDefault()?.Id;

                    _logger.LogInformation("Message WhatsApp envoyé avec succès. ID: {MessageId}", messageId);
                    return (true, messageId, null);
                }
                else
                {
                    _logger.LogError("Erreur API WhatsApp: {StatusCode} - {Content}",
                        response.StatusCode, responseContent);
                    return (false, null, $"HTTP {response.StatusCode}: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception lors de l'envoi du message WhatsApp");
                return (false, null, ex.Message);
            }
        }

        private static string CleanPhoneNumber(string phoneNumber)
        {
            // Supprime les espaces, tirets et garde seulement les chiffres et le +
            var cleaned = phoneNumber.Trim().Replace(" ", "").Replace("-", "");

            // Assure-toi que le numéro commence par +
            if (!cleaned.StartsWith("+"))
            {
                cleaned = "+" + cleaned;
            }

            return cleaned;
        }
    }

    // Classes de support
    public class WhatsAppButton
    {
        public string Id { get; set; }
        public string Title { get; set; }

        public WhatsAppButton(string id, string title)
        {
            Id = id;
            Title = title;
        }
    }

    public class MessageResult
    {
        public string PhoneNumber { get; set; }
        public bool Success { get; set; }
        public string MessageId { get; set; }
        public string Error { get; set; }
        public DateTime Timestamp { get; set; }
        public string MemberName { get; set; }
    }

    public class MemberNotification
    {
        public string PhoneNumber { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
    }

    public class WhatsAppApiResponse
    {
        public WhatsAppMessage[] Messages { get; set; }
    }

    public class WhatsAppMessage
    {
        public string Id { get; set; }
    }
}