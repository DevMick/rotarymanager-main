using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RotaryClubManager.API.Middleware;
using RotaryClubManager.Application.Services;
using RotaryClubManager.Application.Services.Authentication;
using RotaryClubManager.Application.Validators.Authentication;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Domain.Identity;
using RotaryClubManager.Infrastructure.Data;
using RotaryClubManager.Infrastructure.Services;
using RotaryClubManager.Infrastructure.Services.Authentication;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// CONFIGURATION DES SERVICES META WHATSAPP
// ============================================================================

// HttpClient pour Meta WhatsApp API
builder.Services.AddHttpClient<MetaWhatsAppService>();
builder.Services.AddScoped<MetaWhatsAppService>();

// ============================================================================
// CONFIGURATION DE LA BASE DE DONNEES
// ============================================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("RotaryClubManager.Infrastructure")
    )
);

// ============================================================================
// CONFIGURATION D'IDENTITY
// ============================================================================

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ============================================================================
// CONFIGURATION DU SERVICE EMAIL
// ============================================================================

builder.Services.AddScoped<IEmailService, EmailService>();

// ============================================================================
// CONFIGURATION DU RATE LIMITING
// ============================================================================

builder.Services.AddRateLimiter(options =>
{
    // Rate limiting pour emails
    options.AddFixedWindowLimiter("EmailPolicy", opt =>
    {
        opt.PermitLimit = builder.Configuration.GetValue<int>("RateLimit:Email:PermitLimit", 10);
        opt.Window = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("RateLimit:Email:WindowMinutes", 1));
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = builder.Configuration.GetValue<int>("RateLimit:Email:QueueLimit", 5);
    });

    // Rate limiting pour WhatsApp (Meta) - Plus généreux
    options.AddFixedWindowLimiter("WhatsAppPolicy", opt =>
    {
        opt.PermitLimit = builder.Configuration.GetValue<int>("RateLimit:WhatsApp:PermitLimit", 80);
        opt.Window = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("RateLimit:WhatsApp:WindowMinutes", 1));
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = builder.Configuration.GetValue<int>("RateLimit:WhatsApp:QueueLimit", 10);
    });

    // Rate limiting général pour l'API
    options.AddFixedWindowLimiter("ApiPolicy", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            await context.HttpContext.Response.WriteAsync(
                $"Trop de requêtes. Réessayez dans {retryAfter.TotalSeconds} secondes.",
                cancellationToken: token);
        }
        else
        {
            await context.HttpContext.Response.WriteAsync(
                "Trop de requêtes. Veuillez patienter avant de réessayer.",
                cancellationToken: token);
        }
    };
});

// ============================================================================
// CONFIGURATION DES SERVICES D'AUTHENTIFICATION
// ============================================================================

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITenantService, TenantService>();

// ============================================================================
// CONFIGURATION JWT
// ============================================================================

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"] ?? "rotaryclubmanagersecretkey123456789012345");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "RotaryClubManager",
        ValidAudience = jwtSettings["Audience"] ?? "RotaryClubManagerApp",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// ============================================================================
// CONFIGURATION DE L'AUTORISATION
// ============================================================================

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ClubMember", policy => policy.RequireAuthenticatedUser());
});

// ============================================================================
// CONFIGURATION DES VALIDATIONS
// ============================================================================

builder.Services.AddFluentValidation(fv =>
    fv.RegisterValidatorsFromAssemblyContaining<RegisterRequestValidator>());

// ============================================================================
// CONFIGURATION DES CONTROLLERS
// ============================================================================

builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = true;
    options.JsonSerializerOptions.MaxDepth = 10;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddEndpointsApiExplorer();

// ============================================================================
// CONFIGURATION DE SWAGGER
// ============================================================================

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Rotary Club Manager API",
        Version = "v1",
        Description = "API pour la gestion des clubs Rotary avec Meta WhatsApp Business et Email",
        Contact = new OpenApiContact
        {
            Name = "Support Rotary Club Manager",
            Email = "support@rotaryclub.org"
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    c.OperationFilter<FileUploadOperationFilter>();

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// ============================================================================
// CONFIGURATION CORS POUR EXPO SNACK ET NGROK
// ============================================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    options.AddPolicy("ExpoSnack", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourdomain.com", "https://app.yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ============================================================================
// CONFIGURATION DU LOGGING
// ============================================================================

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddDebug();
}

builder.Logging.AddFilter("RotaryClubManager.Infrastructure.Services.EmailService", LogLevel.Debug);
builder.Logging.AddFilter("RotaryClubManager.Infrastructure.Services.MetaWhatsAppService", LogLevel.Debug);
builder.Logging.AddFilter("RotaryClubManager.Infrastructure.Services.CalendarDataService", LogLevel.Debug);

// ============================================================================
// CONFIGURATION POUR LA PRODUCTION
// ============================================================================

if (builder.Environment.IsProduction())
{
    builder.Services.AddHttpsRedirection(options =>
    {
        options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
        options.HttpsPort = 443;
    });
}

// ============================================================================
// BUILD DE L'APPLICATION
// ============================================================================

var app = builder.Build();

// ============================================================================
// INITIALISATION FORCEE DE LA BASE DE DONNEES
// ============================================================================

try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Vérification de la connexion à la base de données...");

        // Force la connexion et l'initialisation
        var canConnect = await context.Database.CanConnectAsync();
        if (canConnect)
        {
            logger.LogInformation("Base de données connectée avec succès");

            // Optionnel : Assurez-vous que les migrations sont appliquées
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Application des migrations en attente...");
                await context.Database.MigrateAsync();
                logger.LogInformation("Migrations appliquées");
            }
        }
        else
        {
            logger.LogWarning("Impossible de se connecter à la base de données");
        }
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Erreur lors de l'initialisation de la base de données");
}

// ============================================================================
// CONFIGURATION DU PIPELINE HTTP
// ============================================================================

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Rotary Club Manager API v1");
        c.RoutePrefix = string.Empty;
    });
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Désactiver la redirection HTTPS en développement pour éviter les erreurs
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Configuration CORS permissive pour Expo Snack
var corsPolicy = app.Environment.IsDevelopment() ? "ExpoSnack" : "Production";
app.UseCors(corsPolicy);

// Middleware supplémentaire pour CORS si nécessaire
app.Use(async (context, next) =>
{
    if (app.Environment.IsDevelopment())
    {
        context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        context.Response.Headers.Add("Access-Control-Allow-Headers", "*");

        if (context.Request.Method == "OPTIONS")
        {
            context.Response.StatusCode = 200;
            return;
        }
    }

    await next();
});

app.UseRateLimiter();
app.UseTenantMiddleware();
app.UseAuthentication();
app.UseAuthorization();

// Utiliser les fichiers statiques seulement en production
if (app.Environment.IsProduction())
{
    app.UseStaticFiles();
}

app.MapControllers();

// ============================================================================
// ENDPOINTS DE TEST SEULEMENT (Health via HealthController)
// ============================================================================

// Endpoint de test immédiat
app.MapGet("/api/test", () =>
{
    return Results.Ok(new
    {
        Status = "API Ready",
        Timestamp = DateTime.UtcNow,
        Message = "L'API est prête à recevoir des requêtes",
        Version = "2.0.0"
    });
}).WithTags("Test").AllowAnonymous();

// Test Meta WhatsApp rapide
app.MapGet("/api/whatsapp/quick-test", async (MetaWhatsAppService metaService, ILogger<Program> logger) =>
{
    try
    {
        var testNumber = "+15550572810"; // Numéro de test Meta
        var (success, messageId, error) = await metaService.SendTextMessage(
            testNumber,
            $"Test Meta WhatsApp Business - {DateTime.Now:HH:mm:ss}"
        );

        if (success)
        {
            logger.LogInformation("Test Meta WhatsApp réussi. ID: {MessageId}", messageId);
            return Results.Ok(new
            {
                Success = true,
                Message = "Meta WhatsApp Business fonctionne parfaitement",
                MessageId = messageId,
                TestNumber = testNumber,
                Provider = "Meta WhatsApp Business API",
                Timestamp = DateTime.UtcNow
            });
        }
        else
        {
            return Results.BadRequest(new
            {
                Success = false,
                Message = "Erreur Meta WhatsApp",
                Error = error
            });
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erreur lors du test Meta WhatsApp");
        return Results.BadRequest(new
        {
            Success = false,
            Message = "Exception Meta WhatsApp",
            Error = ex.Message
        });
    }
}).WithTags("WhatsApp").AllowAnonymous().RequireRateLimiting("WhatsAppPolicy");

// ============================================================================
// INITIALISATION ET MESSAGES DE DEMARRAGE
// ============================================================================

try
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Rotary Club Manager API v2.0 démarrée (Meta WhatsApp Business + Calendar Email)");
    logger.LogInformation("Service email: {EmailHost}:{EmailPort}",
        builder.Configuration["Email:SmtpHost"], builder.Configuration["Email:SmtpPort"]);
    logger.LogInformation("Meta WhatsApp Business API configuré");
    logger.LogInformation("Service calendrier email configuré");
    logger.LogInformation("Rate limiting: Email {EmailLimit}/min, WhatsApp {WhatsAppLimit}/min",
        builder.Configuration.GetValue<int>("RateLimit:Email:PermitLimit", 10),
        builder.Configuration.GetValue<int>("RateLimit:WhatsApp:PermitLimit", 80));
    logger.LogInformation("API prête à recevoir des requêtes immédiatement");
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Erreur lors de l'initialisation");
    throw;
}

app.Run();

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.Name.Contains("Upload", StringComparison.OrdinalIgnoreCase) ||
            context.MethodInfo.Name.Contains("Picture", StringComparison.OrdinalIgnoreCase))
        {
            var fileParameters = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType == typeof(IFormFile) ||
                           p.ParameterType == typeof(IFormFileCollection))
                .ToList();

            if (fileParameters.Any())
            {
                operation.Parameters = operation.Parameters
                    .Where(p => !fileParameters.Any(fp => fp.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                operation.RequestBody = new OpenApiRequestBody
                {
                    Required = true,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = fileParameters.ToDictionary(
                                    fp => fp.Name ?? "file",
                                    fp => new OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "binary",
                                        Description = $"Fichier à uploader ({fp.Name})"
                                    }),
                                Required = fileParameters.Select(fp => fp.Name ?? "file").ToHashSet()
                            }
                        }
                    }
                };
            }
        }
    }
}