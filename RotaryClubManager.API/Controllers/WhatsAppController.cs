using Microsoft.AspNetCore.Mvc;
using RotaryClubManager.Infrastructure.Services;

namespace RotaryClubManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WhatsAppController : ControllerBase
    {
        private readonly TwilioService _twilioService;

        public WhatsAppController(TwilioService twilioService)
        {
            _twilioService = twilioService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] WhatsAppMessageRequest request)
        {
            if (string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest("Numéro de téléphone et message requis");
            }

            var (success, messageSid, error) = await _twilioService.SendWhatsAppMessage(
                request.PhoneNumber,
                request.Message
            );

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    messageSid,
                    message = "Message WhatsApp envoyé avec succès"
                });
            }

            return BadRequest(new
            {
                success = false,
                error,
                message = "Erreur lors de l'envoi"
            });
        }
    }

    public class WhatsAppMessageRequest
    {
        public string PhoneNumber { get; set; }
        public string Message { get; set; }
    }
}
