using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Infrastructure.Data;

namespace RotaryClubManager.API.Controllers
{
    /// <summary>
    /// Health check controller for monitoring application status
    /// Required for Render.com deployment health checks
    /// </summary>
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

        /// <summary>
        /// Basic health check endpoint
        /// Returns 200 OK if the application is running
        /// </summary>
        /// <returns>Health status</returns>
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var response = new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    version = "1.0.0",
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

        /// <summary>
        /// Detailed health check with database connectivity
        /// </summary>
        /// <returns>Detailed health status</returns>
        [HttpGet("detailed")]
        public async Task<IActionResult> GetDetailed()
        {
            var healthChecks = new Dictionary<string, object>();

            try
            {
                // Check application status
                healthChecks["application"] = new
                {
                    status = "healthy",
                    uptime = Environment.TickCount64,
                    timestamp = DateTime.UtcNow
                };

                // Check database connectivity
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

                // Check environment variables
                var requiredEnvVars = new[]
                {
                    "ASPNETCORE_ENVIRONMENT",
                    "ConnectionStrings__DefaultConnection",
                    "JwtSettings__Secret"
                };

                var envVarStatus = new Dictionary<string, bool>();
                foreach (var envVar in requiredEnvVars)
                {
                    var value = Environment.GetEnvironmentVariable(envVar.Replace("__", ":"));
                    envVarStatus[envVar] = !string.IsNullOrEmpty(value);
                }

                healthChecks["environment"] = new
                {
                    status = envVarStatus.All(x => x.Value) ? "healthy" : "warning",
                    variables = envVarStatus,
                    timestamp = DateTime.UtcNow
                };

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

        /// <summary>
        /// Readiness probe - checks if the application is ready to serve requests
        /// </summary>
        /// <returns>Readiness status</returns>
        [HttpGet("ready")]
        public async Task<IActionResult> Ready()
        {
            try
            {
                // Check if database is accessible
                var canConnect = await _context.Database.CanConnectAsync();
                
                if (!canConnect)
                {
                    return StatusCode(503, new { status = "not ready", reason = "database not accessible" });
                }

                // Check if required configuration is present
                var jwtSecret = Environment.GetEnvironmentVariable("JwtSettings:Secret");
                if (string.IsNullOrEmpty(jwtSecret))
                {
                    return StatusCode(503, new { status = "not ready", reason = "jwt configuration missing" });
                }

                return Ok(new { status = "ready", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Readiness check failed");
                return StatusCode(503, new { status = "not ready", error = ex.Message });
            }
        }

        /// <summary>
        /// Liveness probe - checks if the application is alive
        /// </summary>
        /// <returns>Liveness status</returns>
        [HttpGet("live")]
        public IActionResult Live()
        {
            return Ok(new { status = "alive", timestamp = DateTime.UtcNow });
        }
    }
}
