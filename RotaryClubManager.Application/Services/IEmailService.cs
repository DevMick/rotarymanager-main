using RotaryClubManager.Domain.Entities;

namespace RotaryClubManager.Application.Services
{
    public interface IEmailService
    {
        Task<EmailSendResult> SendSimpleEmailAsync(EmailRequest request);
        Task<EmailSendResult> SendEmailAsync(EmailRequest request);
        Task<List<EmailHistoryItem>> GetEmailHistoryAsync(int page, int pageSize);
        Task<EmailStatusResponse?> GetEmailStatusAsync(string emailId);
    }

    public class EmailSendResult
    {
        public bool Success { get; set; }
        public string EmailId { get; set; } = string.Empty;
        public int RecipientsSent { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}