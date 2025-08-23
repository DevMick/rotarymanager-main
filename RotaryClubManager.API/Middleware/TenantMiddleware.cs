using RotaryClubManager.Infrastructure.Data;
using RotaryClubManager.Infrastructure.Services;
using System.IdentityModel.Tokens.Jwt;

namespace RotaryClubManager.API.Middleware
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITenantService tenantService, ApplicationDbContext dbContext)
        {
            // 1. Essayer de résoudre le tenant à partir du token JWT
            Guid? tenantId = GetTenantFromToken(context);

            // 2. Si pas de tenant dans le token, essayer de le résoudre depuis les headers
            if (tenantId == null || tenantId == Guid.Empty)
            {
                tenantId = GetTenantFromHeader(context);
            }

            // 3. Si le tenant est valide, le configurer pour cette requête
            if (tenantId.HasValue && tenantId != Guid.Empty)
            {
                // Vérifier que le tenant existe
                var clubExists = await dbContext.Clubs.FindAsync(tenantId.Value);
                if (clubExists != null)
                {
                    tenantService.SetCurrentTenantId(tenantId.Value);
                }
            }

            await _next(context);
        }

        private Guid? GetTenantFromToken(HttpContext context)
        {
            // Récupérer le token d'authentification
            if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
                return null;

            var bearerToken = authHeader.ToString();
            if (string.IsNullOrEmpty(bearerToken) || !bearerToken.StartsWith("Bearer "))
                return null;

            var token = bearerToken.Substring("Bearer ".Length);
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(token))
                return null;

            try
            {
                var jwtToken = handler.ReadJwtToken(token);
                var clubIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "ClubId");

                if (clubIdClaim != null && Guid.TryParse(clubIdClaim.Value, out var clubId))
                    return clubId;
            }
            catch
            {
                // En cas d'erreur de lecture du token, on continue
                return null;
            }

            return null;
        }

        private Guid? GetTenantFromHeader(HttpContext context)
        {
            // Essayer de résoudre le tenant depuis un header personnalisé
            if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantHeader))
            {
                if (Guid.TryParse(tenantHeader, out var clubId))
                    return clubId;
            }

            return null;
        }
    }

    // Extension method pour ajouter le middleware
    public static class TenantMiddlewareExtensions
    {
        public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TenantMiddleware>();
        }
    }
}
