using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using RotaryClubManager.Application.DTOs.Authentication;
using RotaryClubManager.Application.Services.Authentication;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Domain.Identity;
using RotaryClubManager.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace RotaryClubManager.Infrastructure.Services.Authentication
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogger<AuthService> logger,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Vérifier si l'utilisateur existe déjà
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Un utilisateur avec cet email existe déjà.",
                        Errors = new[] { "Email déjà utilisé." }
                    };
                }

                // VALIDATION : Vérifier que le ClubId n'est pas vide (Guid.Empty)
                if (request.ClubId == Guid.Empty)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Un club valide doit être spécifié lors de l'enregistrement.",
                        Errors = new[] { "ClubId invalide." }
                    };
                }

                // Vérifier si le club existe
                var club = await _context.Clubs.FindAsync(request.ClubId);
                if (club == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Le club spécifié n'existe pas.",
                        Errors = new[] { "ClubId invalide." }
                    };
                }

                // Créer l'utilisateur
                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    JoinedDate = DateTime.SpecifyKind(request.JoinedDate, DateTimeKind.Utc),
                    DateAnniversaire = DateTime.SpecifyKind(request.DateAnniversaire, DateTimeKind.Utc),
                    IsActive = true,
                    EmailConfirmed = true,
                    NumeroMembre = request.NumeroMembre ?? string.Empty
                };

                var result = await _userManager.CreateAsync(user, request.Password);

                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Erreur lors de la création de l'utilisateur.",
                        Errors = result.Errors.Select(e => e.Description).ToArray()
                    };
                }

                // Ajouter le rôle Membre par défaut
                if (!await _roleManager.RoleExistsAsync("Member"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Member"));
                }

                var addRoleResult = await _userManager.AddToRoleAsync(user, "Member");
                if (!addRoleResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Erreur lors de l'attribution du rôle.",
                        Errors = addRoleResult.Errors.Select(e => e.Description).ToArray()
                    };
                }

                // CRÉER LA RELATION USERCLUB (OBLIGATOIRE)
                var userClub = new UserClub
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    ClubId = request.ClubId,
                    JoinedDate = DateTime.SpecifyKind(request.JoinedDate, DateTimeKind.Utc)
                };

                _context.UserClubs.Add(userClub);

                // IMPORTANT : Sauvegarder tous les changements
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Générer les tokens avec le ClubId
                var tokens = await GenerateTokensAsync(user, request.ClubId);

                _logger.LogInformation("Utilisateur {Email} enregistré avec succès dans le club {ClubId}",
                    request.Email, request.ClubId);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Utilisateur créé avec succès.",
                    UserId = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Token = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken,
                    Expiration = tokens.Expiration,
                    ClubId = request.ClubId,
                    ClubName = club.Name,
                    NumeroMembre = user.NumeroMembre
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erreur lors de l'enregistrement de l'utilisateur {Email}", request.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Une erreur est survenue lors de l'enregistrement.",
                    Errors = new[] { ex.Message, ex.InnerException?.Message }.Where(e => !string.IsNullOrEmpty(e)).ToArray()
                };
            }
        }

        public async Task<AuthResponseDto> RegisterAdminAsync(RegisterAdminRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Vérifier si l'utilisateur existe déjà
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Un utilisateur avec cet email existe déjà.",
                        Errors = new[] { "Email déjà utilisé." }
                    };
                }

                // Vérifier si le club existe (si ClubId est spécifié)
                Club? club = null;
                if (request.ClubId.HasValue && request.ClubId.Value != Guid.Empty)
                {
                    club = await _context.Clubs.FindAsync(request.ClubId.Value);
                    if (club == null)
                    {
                        return new AuthResponseDto
                        {
                            Success = false,
                            Message = "Le club spécifié n'existe pas.",
                            Errors = new[] { "ClubId invalide." }
                        };
                    }
                }

                // Créer l'administrateur
                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    JoinedDate = DateTime.SpecifyKind(request.JoinedDate, DateTimeKind.Utc),
                    DateAnniversaire = DateTime.SpecifyKind(request.DateAnniversaire, DateTimeKind.Utc),
                    IsActive = true,
                    EmailConfirmed = true,
                    NumeroMembre = request.NumeroMembre ?? string.Empty
                    // SUPPRIMÉ : Department et Position car ils n'existent pas dans le modèle
                };

                var result = await _userManager.CreateAsync(user, request.Password);

                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Erreur lors de la création de l'administrateur.",
                        Errors = result.Errors.Select(e => e.Description).ToArray()
                    };
                }

                // Créer le rôle Admin s'il n'existe pas
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    var roleResult = await _roleManager.CreateAsync(new IdentityRole("Admin"));
                    if (!roleResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return new AuthResponseDto
                        {
                            Success = false,
                            Message = "Erreur lors de la création du rôle Admin.",
                            Errors = roleResult.Errors.Select(e => e.Description).ToArray()
                        };
                    }
                }

                // Ajouter l'utilisateur au rôle Admin
                var addRoleResult = await _userManager.AddToRoleAsync(user, "Admin");
                if (!addRoleResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Erreur lors de l'attribution du rôle Admin.",
                        Errors = addRoleResult.Errors.Select(e => e.Description).ToArray()
                    };
                }

                // Si un club est spécifié, créer une relation UserClub
                if (request.ClubId.HasValue && request.ClubId.Value != Guid.Empty)
                {
                    var userClub = new UserClub
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        ClubId = request.ClubId.Value,
                        JoinedDate = DateTime.SpecifyKind(request.JoinedDate, DateTimeKind.Utc)
                    };

                    _context.UserClubs.Add(userClub);
                }

                // Sauvegarder TOUS les changements en une seule fois
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Générer les tokens
                var tokens = await GenerateTokensAsync(user, request.ClubId);

                _logger.LogInformation("Administrateur {Email} créé avec succès", request.Email);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Administrateur créé avec succès.",
                    UserId = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Token = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken,
                    Expiration = tokens.Expiration,
                    ClubId = request.ClubId,
                    ClubName = club?.Name,
                    NumeroMembre = user.NumeroMembre
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erreur lors de la création de l'administrateur {Email}", request.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Une erreur est survenue lors de la création de l'administrateur.",
                    Errors = new[] { ex.Message, ex.InnerException?.Message }.Where(e => !string.IsNullOrEmpty(e)).ToArray()
                };
            }
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Email, mot de passe ou club incorrect."
                    };
                }

                if (!user.IsActive)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Ce compte est désactivé. Veuillez contacter l'administrateur."
                    };
                }

                // VALIDATION : Vérifier que le ClubId n'est pas vide (Guid.Empty)
                if (request.ClubId == Guid.Empty)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Un club valide doit être spécifié pour la connexion."
                    };
                }

                // VALIDATION CRUCIALE : Vérifier que l'utilisateur appartient au club spécifié
                var userClub = await _context.UserClubs
                    .Include(uc => uc.Club)
                    .FirstOrDefaultAsync(uc => uc.UserId == user.Id && uc.ClubId == request.ClubId);

                if (userClub == null)
                {
                    _logger.LogWarning("Tentative de connexion refusée: Utilisateur {Email} n'appartient pas au club {ClubId}",
                        request.Email, request.ClubId);

                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Email, mot de passe ou club incorrect."
                    };
                }

                // Vérifier le mot de passe
                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    // Générer les tokens avec le ClubId
                    var tokens = await GenerateTokensAsync(user, request.ClubId);

                    // Obtenir les rôles de l'utilisateur
                    var roles = await _userManager.GetRolesAsync(user);

                    _logger.LogInformation("Utilisateur {Email} connecté avec succès au club {ClubName} ({ClubId})",
                        request.Email, userClub.Club?.Name, request.ClubId);

                    return new AuthResponseDto
                    {
                        Success = true,
                        Message = "Connexion réussie.",
                        UserId = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Token = tokens.AccessToken,
                        RefreshToken = tokens.RefreshToken,
                        Expiration = tokens.Expiration,
                        Roles = roles.ToArray(),
                        ClubId = request.ClubId,
                        ClubName = userClub.Club?.Name,
                        NumeroMembre = user.NumeroMembre,
                        DateAnniversaire = user.DateAnniversaire

                    };
                }
                else if (result.IsLockedOut)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Compte verrouillé suite à plusieurs tentatives de connexion. Veuillez réessayer plus tard."
                    };
                }
                else
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Email, mot de passe ou club incorrect."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la connexion de l'utilisateur {Email}", request.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Une erreur est survenue lors de la connexion.",
                    Errors = new[] { ex.Message }
                };
            }
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "La fonctionnalité de refresh token n'est pas encore implémentée."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du rafraîchissement du token");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Une erreur est survenue lors du rafraîchissement du token.",
                    Errors = new[] { ex.Message }
                };
            }
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            try
            {
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la révocation du token");
                return false;
            }
        }

        public async Task<UserDto> GetUserByIdAsync(string userId, Guid? clubId = null)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return null;
                }

                // Obtenir les rôles de l'utilisateur
                var roles = await _userManager.GetRolesAsync(user);

                // Obtenir les informations du club principal (première relation UserClub active)
                var primaryUserClub = await _context.UserClubs
                    .Include(uc => uc.Club)
                    .Where(uc => uc.UserId == userId)
                    .OrderBy(uc => uc.JoinedDate)
                    .FirstOrDefaultAsync();

                return new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    JoinedDate = user.JoinedDate,
                    IsActive = user.IsActive,
                    PrimaryClubId = primaryUserClub?.ClubId,
                    PrimaryClubName = primaryUserClub?.Club?.Name,
                    Roles = roles.ToArray(),
                    NumeroMembre = user.NumeroMembre,
                    DateAnniversaire = user.DateAnniversaire
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'utilisateur {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> AdminExistsAsync()
        {
            try
            {
                // Vérifier si le rôle Admin existe
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    return false;
                }

                // Vérifier s'il y a des utilisateurs dans ce rôle
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                return adminUsers != null && adminUsers.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification de l'existence d'un administrateur");
                return false;
            }
        }

        public async Task<AuthResponseDto> DebugLoginAsync(LoginRequest request)
        {
            try
            {
                var authResult = await LoginAsync(request);

                // Ajouter des informations de débogage
                if (!authResult.Success)
                {
                    var user = await _userManager.FindByEmailAsync(request.Email);
                    if (user != null)
                    {
                        authResult.DebugInfo = new Dictionary<string, string>
                        {
                            { "UserExists", "true" },
                            { "UserId", user.Id },
                            { "EmailConfirmed", user.EmailConfirmed.ToString() },
                            { "IsActive", user.IsActive.ToString() },
                            { "LockoutEnabled", user.LockoutEnabled.ToString() },
                            { "LockoutEnd", user.LockoutEnd?.ToString() ?? "null" }
                        };
                    }
                    else
                    {
                        authResult.DebugInfo = new Dictionary<string, string>
                        {
                            { "UserExists", "false" }
                        };
                    }
                }

                return authResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du debug login pour {Email}", request.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Erreur de débogage de connexion.",
                    Errors = new[] { ex.Message },
                    DebugInfo = new Dictionary<string, string>
                    {
                        { "Exception", ex.GetType().Name },
                        { "Message", ex.Message },
                        { "StackTrace", ex.StackTrace }
                    }
                };
            }
        }

        public async Task<AuthResponseDto> CreateTestUserAsync()
        {
            try
            {
                // Obtenir le premier club disponible
                var firstClub = await _context.Clubs.FirstOrDefaultAsync();
                if (firstClub == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Aucun club disponible pour créer un utilisateur de test."
                    };
                }

                // Générer un email unique
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var testEmail = $"test.user{timestamp}@example.com";

                // Créer un utilisateur de test
                var registerRequest = new RegisterRequest
                {
                    Email = testEmail,
                    Password = "Test123!",
                    FirstName = "Test",
                    LastName = "User",
                    PhoneNumber = "+1234567890",
                    ClubId = firstClub.Id,
                    NumeroMembre = $"TEST{timestamp}",
                    JoinedDate = DateTime.UtcNow // Déjà en UTC
                };

                // Utiliser la méthode d'enregistrement existante
                var result = await RegisterAsync(registerRequest);

                // Ajouter des informations supplémentaires
                if (result.Success)
                {
                    result.Message = $"Utilisateur de test créé avec succès: {testEmail} / Test123!";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création d'un utilisateur de test");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Erreur lors de la création de l'utilisateur de test.",
                    Errors = new[] { ex.Message }
                };
            }
        }

        public async Task<List<ClubMemberDto>> GetClubMembersAsync(Guid clubId)
        {
            try
            {
                var members = await _context.UserClubs
                    .Include(uc => uc.User)
                    .Include(uc => uc.Club)
                    .Where(uc => uc.ClubId == clubId)
                    .OrderBy(uc => uc.JoinedDate)
                    .ToListAsync();

                var result = new List<ClubMemberDto>();

                foreach (var userClub in members)
                {
                    if (userClub.User != null)
                    {
                        // Obtenir les rôles de l'utilisateur
                        var roles = await _userManager.GetRolesAsync(userClub.User);

                        var memberDto = new ClubMemberDto
                        {
                            Id = userClub.User.Id,
                            Email = userClub.User.Email,
                            FirstName = userClub.User.FirstName,
                            LastName = userClub.User.LastName,
                            PhoneNumber = userClub.User.PhoneNumber,
                            ProfilePictureUrl = userClub.User.ProfilePictureUrl,
                            UserJoinedDate = userClub.User.JoinedDate,
                            ClubJoinedDate = userClub.JoinedDate,
                            IsActive = userClub.User.IsActive,
                            Roles = roles.ToArray(),
                            ClubId = userClub.ClubId,
                            ClubName = userClub.Club?.Name,
                            NumeroMembre = userClub.User.NumeroMembre
                        };

                        result.Add(memberDto);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des membres du club {ClubId}", clubId);
                return new List<ClubMemberDto>();
            }
        }

        // Méthodes privées utilitaires
        private async Task<(string AccessToken, string RefreshToken, DateTime Expiration)> GenerateTokensAsync(ApplicationUser user, Guid? clubId = null)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"] ?? "rotaryclubmanagersecretkey123456789012345");
            var issuer = jwtSettings["Issuer"] ?? "RotaryClubManager";
            var audience = jwtSettings["Audience"] ?? "RotaryClubManagerApp";
            var expiration = DateTime.UtcNow.AddHours(1);

            // Obtenir les claims de base de l'utilisateur
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName)
            };

            // Ajouter les rôles aux claims
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Ajouter le ClubId aux claims
            Guid? targetClubId = clubId;

            // Si aucun clubId spécifique, prendre le club principal (premier club rejoint)
            if (!targetClubId.HasValue || targetClubId.Value == Guid.Empty)
            {
                var primaryUserClub = await _context.UserClubs
                    .Where(uc => uc.UserId == user.Id)
                    .OrderBy(uc => uc.JoinedDate)
                    .FirstOrDefaultAsync();

                targetClubId = primaryUserClub?.ClubId;
            }

            // Ajouter le ClubId au token si disponible
            if (targetClubId.HasValue && targetClubId.Value != Guid.Empty)
            {
                claims.Add(new Claim("ClubId", targetClubId.Value.ToString()));

                // Optionnel : ajouter aussi le nom du club
                var club = await _context.Clubs.FindAsync(targetClubId.Value);
                if (club != null)
                {
                    claims.Add(new Claim("ClubName", club.Name));
                }
            }

            // Créer le token JWT
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiration,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(token);

            // Générer un refresh token simple
            var refreshToken = Guid.NewGuid().ToString();

            return (accessToken, refreshToken, expiration);
        }
    }
}