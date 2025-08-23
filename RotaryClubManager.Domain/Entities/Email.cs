// ==============================================================================
// 📝 MODÈLES DE DONNÉES (DTOs) - DANS LE DOMAIN
// ==============================================================================

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RotaryClubManager.Domain.Entities
{
    /// <summary>
    /// Requête d'envoi d'email
    /// </summary>
    public class EmailRequest
    {
        [Required(ErrorMessage = "Le sujet est obligatoire")]
        [StringLength(200, ErrorMessage = "Le sujet ne peut pas dépasser 200 caractères")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le message est obligatoire")]
        [StringLength(10000, ErrorMessage = "Le message ne peut pas dépasser 10000 caractères")]
        public string Message { get; set; } = string.Empty;

        [Required(ErrorMessage = "Au moins un destinataire est requis")]
        [MinLength(1, ErrorMessage = "Au moins un destinataire est requis")]
        public List<string> Recipients { get; set; } = new();

        public List<EmailAttachment> Attachments { get; set; } = new();

        [JsonPropertyName("isUrgent")]
        public bool IsUrgent { get; set; } = false;

        [JsonPropertyName("sendCopy")]
        public bool SendCopy { get; set; } = true;
    }

    /// <summary>
    /// Pièce jointe d'email
    /// </summary>
    public class EmailAttachment
    {
        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string Base64Content { get; set; } = string.Empty;

        public string ContentType { get; set; } = "application/octet-stream";

        [JsonIgnore]
        public long ContentSize => !string.IsNullOrEmpty(Base64Content)
            ? (long)(Base64Content.Length * 0.75)
            : 0;
    }

    /// <summary>
    /// Réponse d'envoi d'email
    /// </summary>
    public class EmailResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string EmailId { get; set; } = string.Empty;
        public int RecipientsSent { get; set; }
        public int RecipientsTotal { get; set; }
        public DateTime SentAt { get; set; }
    }

    /// <summary>
    /// Réponse d'erreur
    /// </summary>
    public class ErrorResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Élément de l'historique des emails
    /// </summary>
    public class EmailHistoryItem
    {
        public string EmailId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int RecipientCount { get; set; }
        public DateTime SentAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Statut d'un email
    /// </summary>
    public class EmailStatusResponse
    {
        public string EmailId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public int RecipientsTotal { get; set; }
        public int RecipientsDelivered { get; set; }
        public int RecipientsFailed { get; set; }
        public List<string> FailedRecipients { get; set; } = new();
    }
}