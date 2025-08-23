using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using RotaryClubManager.Application.Services;
using RotaryClubManager.Domain.Entities;
using System.Linq;

namespace RotaryClubManager.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        // En production, utilisez une vraie base de données
        private static readonly List<EmailHistoryItem> _emailHistory = new();
        private static readonly Dictionary<string, EmailStatusResponse> _emailStatuses = new();

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Méthode existante avec template professionnel
        /// </summary>
        public async Task<EmailSendResult> SendEmailAsync(EmailRequest request)
        {
            return await SendEmailInternalAsync(request, useTemplate: true);
        }

        /// <summary>
        /// Nouvelle méthode pour envoyer des emails sans template
        /// </summary>
        public async Task<EmailSendResult> SendSimpleEmailAsync(EmailRequest request)
        {
            return await SendEmailInternalAsync(request, useTemplate: false);
        }

        /// <summary>
        /// Méthode interne pour l'envoi d'emails avec ou sans template
        /// </summary>
        private async Task<EmailSendResult> SendEmailInternalAsync(EmailRequest request, bool useTemplate)
        {
            var emailId = Guid.NewGuid().ToString();
            var sentCount = 0;

            try
            {
                _logger.LogDebug("Début de l'envoi d'email - ID: {EmailId}, Template: {UseTemplate}", emailId, useTemplate);

                // Configuration SMTP pour Hostinger
                using var client = new MailKit.Net.Smtp.SmtpClient();

                var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.hostinger.com";
                var smtpPort = _configuration.GetValue<int>("Email:SmtpPort", 465);
                var smtpUser = _configuration["Email:SmtpUser"];
                var smtpPass = _configuration["Email:SmtpPassword"];
                var fromEmail = _configuration["Email:FromEmail"];
                var fromName = _configuration["Email:FromName"] ?? "Rotary Club Manager";
                var enableSsl = _configuration.GetValue<bool>("Email:EnableSsl", true);

                _logger.LogDebug("Configuration SMTP - Host: {Host}, Port: {Port}, User: {User}",
                    smtpHost, smtpPort, smtpUser);

                // Configuration SSL personnalisée pour Hostinger (comme dans AppNotifier)
                client.ServerCertificateValidationCallback = (sender, certificate, chain, errors) =>
                {
                    // Ignorer les erreurs de révocation spécifiquement
                    if (errors == System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors)
                    {
                        foreach (var chainStatus in chain.ChainStatus)
                        {
                            // Ignorer uniquement les erreurs de révocation
                            if (chainStatus.Status == System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.RevocationStatusUnknown ||
                                chainStatus.Status == System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.OfflineRevocation)
                            {
                                continue;
                            }
                            // Rejeter les autres erreurs
                            return false;
                        }
                        return true;
                    }
                    // Accepter si pas d'erreurs ou seulement erreurs de révocation
                    return errors == System.Net.Security.SslPolicyErrors.None;
                };

                // Connexion SSL pour Hostinger
                if (smtpPort == 465)
                {
                    await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.SslOnConnect);
                }
                else
                {
                    await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                }

                _logger.LogDebug("Connexion SMTP établie");

                // Authentification
                await client.AuthenticateAsync(smtpUser, smtpPass);
                _logger.LogDebug("Authentification SMTP réussie");

                // Création du message
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.Subject = request.Subject;

                // Message ID pour le tracking
                message.MessageId = $"{emailId}@{fromEmail.Split('@')[1]}";

                // Ajout des destinataires
                foreach (var recipient in request.Recipients)
                {
                    message.To.Add(new MailboxAddress("", recipient));
                }

                // Choisir le template en fonction du paramètre
                var htmlBody = useTemplate
                    ? CreateProfessionalHtmlEmailTemplate(request.Message, fromName, request.Subject)
                    : request.Message; // Utiliser directement le message HTML fourni

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody,
                    TextBody = ExtractTextFromHtml(request.Message) // Extraire le texte du HTML
                };

                // Ajout des pièces jointes
                if (request.Attachments?.Any() == true)
                {
                    _logger.LogDebug("Ajout de {Count} pièces jointes", request.Attachments.Count);

                    foreach (var attachment in request.Attachments)
                    {
                        try
                        {
                            var bytes = Convert.FromBase64String(attachment.Base64Content);
                            bodyBuilder.Attachments.Add(attachment.FileName, bytes,
                                ContentType.Parse(attachment.ContentType));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Erreur lors de l'ajout de la pièce jointe: {FileName}",
                                attachment.FileName);
                        }
                    }
                }

                message.Body = bodyBuilder.ToMessageBody();

                // Envoi de l'email
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                sentCount = request.Recipients.Count;
                _logger.LogInformation("Email envoyé avec succès - ID: {EmailId}, Destinataires: {Count}",
                    emailId, sentCount);

                // Enregistrement dans l'historique
                _emailHistory.Add(new EmailHistoryItem
                {
                    EmailId = emailId,
                    Subject = request.Subject,
                    RecipientCount = sentCount,
                    SentAt = DateTime.UtcNow,
                    Status = "Envoyé",
                    SenderName = fromName
                });

                _emailStatuses[emailId] = new EmailStatusResponse
                {
                    EmailId = emailId,
                    Subject = request.Subject,
                    Status = "Delivered",
                    SentAt = DateTime.UtcNow,
                    RecipientsTotal = request.Recipients.Count,
                    RecipientsDelivered = sentCount,
                    RecipientsFailed = 0
                };

                return new EmailSendResult
                {
                    Success = true,
                    EmailId = emailId,
                    RecipientsSent = sentCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi d'email - ID: {EmailId}", emailId);

                // Enregistrement de l'échec
                _emailStatuses[emailId] = new EmailStatusResponse
                {
                    EmailId = emailId,
                    Subject = request.Subject ?? "Sujet non défini",
                    Status = "Failed",
                    SentAt = DateTime.UtcNow,
                    RecipientsTotal = request.Recipients?.Count ?? 0,
                    RecipientsDelivered = 0,
                    RecipientsFailed = request.Recipients?.Count ?? 0,
                    FailedRecipients = request.Recipients?.ToList() ?? new List<string>()
                };

                return new EmailSendResult
                {
                    Success = false,
                    EmailId = emailId,
                    RecipientsSent = sentCount,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Template HTML professionnel bien formaté (méthode existante)
        /// </summary>
        private string CreateProfessionalHtmlEmailTemplate(string message, string fromName, string subject)
        {
            var currentYear = DateTime.Now.Year;

            return $@"
<body style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333333; line-height: 1.5;"">
    <div style=""background-color: #1f4788; padding: 15px; text-align: center; border-radius: 5px 5px 0 0;"">
        <h2 style=""color: white; margin: 0;"">⚙️ Rotary Club Manager</h2>
        <p style=""color: white; margin: 8px 0 0 0; opacity: 0.9; font-size: 14px;"">Service Above Self</p>
    </div>
    
    <div style=""background-color: #f8f9fa; border: 1px solid #dee2e6; border-top: none; padding: 20px; border-radius: 0 0 5px 5px;"">
        <p style=""margin-bottom: 15px; font-weight: bold; color: #1f4788;"">Message du système :</p>
        
        <div style=""background-color: #ffffff; border: 1px solid #e0e0e0; border-radius: 6px; padding: 20px; margin-bottom: 20px;"">
            <div style=""font-size: 15px; line-height: 1.6; color: #333333;"">
                {message.Replace("\n", "<br>").Replace("\r", "")}
            </div>
        </div>
        
        <div style=""background-color: #e7f3ff; border: 1px solid #b3d7ff; border-radius: 5px; padding: 15px; margin: 20px 0;"">
            <p style=""margin: 0; color: #0056b3; font-size: 14px;"">
                📧 Cet email a été envoyé automatiquement par le système Rotary Club Manager.
            </p>
        </div>
        
        <div style=""margin-top: 30px; padding-top: 15px; border-top: 1px solid #dee2e6; color: #6c757d; font-size: 0.9em;"">
            <p>Pour toute question concernant ce message, veuillez contacter l'administration de votre club.</p>
            <p style=""margin-bottom: 0;"">
                <i>Cordialement,<br>Équipe {fromName}</i>
            </p>
        </div>
    </div>
    
    <div style=""text-align: center; margin-top: 15px; font-size: 0.8em; color: #6c757d;"">
        <p>© {currentYear} {fromName} - Service Above Self</p>
        <p>Ceci est un message automatique, merci de ne pas y répondre.</p>
    </div>
</body>";
        }

        /// <summary>
        /// Extrait le texte simple d'un contenu HTML
        /// </summary>
        private string ExtractTextFromHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            // Remplacement basique des balises HTML par des retours à la ligne
            var text = html
                .Replace("<br>", "\n")
                .Replace("<br/>", "\n")
                .Replace("</p>", "\n")
                .Replace("</div>", "\n")
                .Replace("</h1>", "\n")
                .Replace("</h2>", "\n")
                .Replace("</h3>", "\n");

            // Suppression de toutes les balises HTML
            text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]*>", "");

            // Décodage des entités HTML basiques
            text = text
                .Replace("&nbsp;", " ")
                .Replace("&amp;", "&")
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"");

            // Nettoyage des espaces multiples et retours à la ligne
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\n\s*\n", "\n\n");

            return text.Trim();
        }

        public async Task<List<EmailHistoryItem>> GetEmailHistoryAsync(int page, int pageSize)
        {
            await Task.Delay(1); // Simulation async

            return _emailHistory
                .OrderByDescending(x => x.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public async Task<EmailStatusResponse?> GetEmailStatusAsync(string emailId)
        {
            await Task.Delay(1); // Simulation async

            _emailStatuses.TryGetValue(emailId, out var status);
            return status;
        }
    }
}