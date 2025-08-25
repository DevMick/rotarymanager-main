using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Infrastructure.Data;

namespace RotaryClubManager.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HealthController> _logger;

        public HealthController(ApplicationDbContext context, ILogger<HealthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var response = new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    version = "2.0.0",
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
                };

                _logger.LogInformation("Health check passed at {Timestamp}", DateTime.UtcNow);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(500, new { status = "unhealthy", error = ex.Message });
            }
        }

        [HttpGet("detailed")]
        public async Task<IActionResult> GetDetailed()
        {
            var healthChecks = new Dictionary<string, object>();

            try
            {
                healthChecks["application"] = new
                {
                    status = "healthy",
                    uptime = Environment.TickCount64,
                    timestamp = DateTime.UtcNow
                };

                try
                {
                    var canConnect = await _context.Database.CanConnectAsync();
                    healthChecks["database"] = new
                    {
                        status = canConnect ? "healthy" : "unhealthy",
                        provider = _context.Database.ProviderName,
                        timestamp = DateTime.UtcNow
                    };
                }
                catch (Exception dbEx)
                {
                    healthChecks["database"] = new
                    {
                        status = "unhealthy",
                        error = dbEx.Message,
                        timestamp = DateTime.UtcNow
                    };
                }

                var overallStatus = healthChecks.Values
                    .OfType<dynamic>()
                    .All(x => x.status == "healthy") ? "healthy" : "degraded";

                var response = new
                {
                    status = overallStatus,
                    checks = healthChecks,
                    timestamp = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detailed health check failed");
                return StatusCode(500, new
                {
                    status = "unhealthy",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        [HttpGet("ready")]
        public async Task<IActionResult> Ready()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                
                if (!canConnect)
                {
                    return StatusCode(503, new { status = "not ready", reason = "database not accessible" });
                }

                return Ok(new { status = "ready", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Readiness check failed");
                return StatusCode(503, new { status = "not ready", error = ex.Message });
            }
        }

        [HttpGet("live")]
        public IActionResult Live()
        {
            return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
        }
    }
}
