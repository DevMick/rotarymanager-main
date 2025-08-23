using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace RotaryClubManager.Infrastructure.Services
{
    public class TwilioService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TwilioService> _logger;

        public TwilioService(IConfiguration configuration, ILogger<TwilioService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // 🔑 Configuration Twilio depuis appsettings.json
            var accountSid = _configuration["Twilio:AccountSid"];
            var authToken = _configuration["Twilio:AuthToken"];

            TwilioClient.Init(accountSid, authToken);
        }

        public async Task<(bool Success, string MessageSid, string Error)> SendWhatsAppMessage(
            string toPhoneNumber,
            string messageBody)
        {
            try
            {
                // 📱 ICI le numéro WhatsApp sandbox est utilisé
                var whatsAppNumber = _configuration["Twilio:WhatsAppNumber"]; // +14155238886

                var message = await MessageResource.CreateAsync(
                    body: messageBody,
                    from: new PhoneNumber($"whatsapp:{whatsAppNumber}"), // whatsapp:+14155238886
                    to: new PhoneNumber($"whatsapp:{toPhoneNumber}")     // whatsapp:+22595031843
                );

                _logger.LogInformation($"Message WhatsApp envoyé. SID: {message.Sid}");

                return (true, message.Sid, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur envoi WhatsApp vers {toPhoneNumber}");
                return (false, null, ex.Message);
            }
        }
    }
}