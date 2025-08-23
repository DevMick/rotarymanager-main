// 1. Mise à jour du contrôleur AuthController avec injection du DbContext
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Application.DTOs.Authentication;
using RotaryClubManager.Application.Services.Authentication;
using RotaryClubManager.Domain.Identity;
using RotaryClubManager.Infrastructure.Data;

namespace RotaryClubManager.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context; // Ajout du contexte

    public AuthController(
        IAuthService authService,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context) // Injection du contexte
    {
        _authService = authService;
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context; // Initialisation du contexte
    }

   

    // Vos endpoints existants restent identiques...
    [HttpPost("register-admin")]
    //[Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> RegisterAdmin([FromBody] RegisterAdminRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterAdminAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("register-initial-admin")]
    public async Task<IActionResult> RegisterInitialAdmin([FromBody] RegisterAdminRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var adminExists = await _authService.AdminExistsAsync();
        if (adminExists)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Un administrateur existe déjà. Utilisez l'endpoint register-admin."
            });
        }

        var result = await _authService.RegisterAdminAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.LoginAsync(request);
        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    [Authorize]
    [HttpPost("revoke-token")]
    public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RevokeTokenAsync(request.RefreshToken);
        if (!result)
        {
            return BadRequest(new { Success = false, Message = "Token invalide" });
        }

        return Ok(new { Success = true, Message = "Token révoqué avec succès" });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Success = false, Message = "Utilisateur non trouvé" });
        }

        var clubIdClaim = User.FindFirst("ClubId")?.Value;
        Guid? clubId = null;
        if (!string.IsNullOrEmpty(clubIdClaim) && Guid.TryParse(clubIdClaim, out var parsedClubId))
        {
            clubId = parsedClubId;
        }

        var user = await _authService.GetUserByIdAsync(userId, clubId);
        if (user == null)
        {
            return NotFound(new { Success = false, Message = "Utilisateur non trouvé" });
        }

        return Ok(new { Success = true, User = user, NumeroMembre = user.NumeroMembre });
    }

   

    [HttpGet("club/{clubId}/member/{userId}")]
    [Authorize]
    public async Task<IActionResult> GetClubMember(Guid clubId, string userId)
    {
        try
        {
            // Accès direct aux données
            var userClub = await _context.UserClubs
                .Include(uc => uc.User)
                .Include(uc => uc.Club)
                .FirstOrDefaultAsync(uc => uc.ClubId == clubId && uc.UserId == userId);

            if (userClub?.User == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Membre non trouvé dans ce club"
                });
            }

            // Obtenir les rôles
            var roles = await _userManager.GetRolesAsync(userClub.User);

            var memberData = new
            {
                Id = userClub.User.Id,
                Email = userClub.User.Email,
                FirstName = userClub.User.FirstName,
                LastName = userClub.User.LastName,
                FullName = $"{userClub.User.FirstName} {userClub.User.LastName}",
                PhoneNumber = userClub.User.PhoneNumber,
                ProfilePictureUrl = userClub.User.ProfilePictureUrl,
                NumeroMembre = userClub.User.NumeroMembre,
                DateAnniversaire = userClub.User.DateAnniversaire.ToString("dd/MM/yyyy"),
                // Dates
                UserJoinedDate = userClub.User.JoinedDate,
                ClubJoinedDate = userClub.JoinedDate,
                UserJoinedDateFormatted = userClub.User.JoinedDate.ToString("dd/MM/yyyy"),
                ClubJoinedDateFormatted = userClub.JoinedDate.ToString("dd/MM/yyyy"),
                IsActive = userClub.User.IsActive,
                Roles = roles.ToArray(),
                ClubId = userClub.ClubId,
                ClubName = userClub.Club?.Name,
                UserClubId = userClub.Id,

            };

            return Ok(new
            {
                Success = true,
                Member = memberData
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Erreur lors de la récupération du membre",
                Error = ex.Message
            });
        }
    }

    [HttpGet("user/{userId}/clubs")]
    [Authorize]
    public async Task<IActionResult> GetUserClubs(string userId)
    {
        try
        {
            // Vérifier que l'utilisateur existe
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Utilisateur non trouvé"
                });
            }

            // Obtenir tous les clubs de l'utilisateur
            var userClubs = await _context.UserClubs
                .Include(uc => uc.Club)
                .Where(uc => uc.UserId == userId)
                .OrderBy(uc => uc.JoinedDate)
                .Select(uc => new
                {
                    UserClubId = uc.Id,
                    ClubId = uc.ClubId,
                    ClubName = uc.Club.Name,
                    JoinedDate = uc.JoinedDate,
                    JoinedDateFormatted = uc.JoinedDate.ToString("dd/MM/yyyy"),
                    Club = new
                    {
                        Id = uc.Club.Id,
                        Name = uc.Club.Name,
                        // Ajoutez d'autres propriétés du club si nécessaire
                    }
                })
                .ToListAsync();

            return Ok(new
            {
                Success = true,
                UserId = userId,
                UserInfo = new
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = $"{user.FirstName} {user.LastName}"
                },
                ClubCount = userClubs.Count,
                Clubs = userClubs
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Erreur lors de la récupération des clubs de l'utilisateur",
                Error = ex.Message
            });
        }
    }

    [HttpGet("all-users-with-clubs")]
    [Authorize(Roles = "Admin")] // Restreint aux admins
    public async Task<IActionResult> GetAllUsersWithClubs()
    {
        try
        {
            var usersWithClubs = await _context.Users
                .Include(u => u.UserClubs)
                .ThenInclude(uc => uc.Club)
                .Select(u => new
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    FullName = $"{u.FirstName} {u.LastName}",
                    PhoneNumber = u.PhoneNumber,
                    UserJoinedDate = u.JoinedDate,
                    UserJoinedDateFormatted = u.JoinedDate.ToString("dd/MM/yyyy"),
                    IsActive = u.IsActive,
                    ClubCount = u.UserClubs.Count,
                    Clubs = u.UserClubs.Select(uc => new
                    {
                        UserClubId = uc.Id,
                        ClubId = uc.ClubId,
                        ClubName = uc.Club.Name,
                        ClubJoinedDate = uc.JoinedDate,
                        ClubJoinedDateFormatted = uc.JoinedDate.ToString("dd/MM/yyyy")
                    }).ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                Success = true,
                UserCount = usersWithClubs.Count,
                Users = usersWithClubs
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Erreur lors de la récupération de tous les utilisateurs",
                Error = ex.Message
            });
        }
    }

    [HttpGet("club/{clubId}/stats")]
    [Authorize]
    public async Task<IActionResult> GetClubStats(Guid clubId)
    {
        try
        {
            var club = await _context.Clubs.FindAsync(clubId);
            if (club == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Club non trouvé"
                });
            }

            var memberCount = await _context.UserClubs
                .CountAsync(uc => uc.ClubId == clubId);

            var activeMembers = await _context.UserClubs
                .Include(uc => uc.User)
                .CountAsync(uc => uc.ClubId == clubId && uc.User.IsActive);

            var recentJoins = await _context.UserClubs
                .Include(uc => uc.User)
                .Where(uc => uc.ClubId == clubId)
                .Where(uc => uc.JoinedDate >= DateTime.UtcNow.AddDays(-30))
                .CountAsync();

            var oldestMember = await _context.UserClubs
                .Include(uc => uc.User)
                .Where(uc => uc.ClubId == clubId)
                .OrderBy(uc => uc.JoinedDate)
                .FirstOrDefaultAsync();

            var newestMember = await _context.UserClubs
                .Include(uc => uc.User)
                .Where(uc => uc.ClubId == clubId)
                .OrderByDescending(uc => uc.JoinedDate)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                Success = true,
                ClubId = clubId,
                ClubName = club.Name,
                Stats = new
                {
                    TotalMembers = memberCount,
                    ActiveMembers = activeMembers,
                    InactiveMembers = memberCount - activeMembers,
                    RecentJoins30Days = recentJoins,
                    OldestMember = oldestMember != null ? new
                    {
                        Id = oldestMember.User.Id,
                        Name = $"{oldestMember.User.FirstName} {oldestMember.User.LastName}",
                        JoinedDate = oldestMember.JoinedDate,
                        JoinedDateFormatted = oldestMember.JoinedDate.ToString("dd/MM/yyyy")
                    } : null,
                    NewestMember = newestMember != null ? new
                    {
                        Id = newestMember.User.Id,
                        Name = $"{newestMember.User.FirstName} {newestMember.User.LastName}",
                        JoinedDate = newestMember.JoinedDate,
                        JoinedDateFormatted = newestMember.JoinedDate.ToString("dd/MM/yyyy")
                    } : null
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Erreur lors de la récupération des statistiques du club",
                Error = ex.Message
            });
        }
    }

    // Vos autres endpoints existants...
    [HttpPost("debug-login")]
    public async Task<IActionResult> DebugLogin([FromBody] LoginRequest request)
    {
        var result = await _authService.DebugLoginAsync(request);
        return Ok(result);
    }

    [HttpPost("create-test-user")]
    public async Task<IActionResult> CreateTestUser()
    {
        var result = await _authService.CreateTestUserAsync();
        return Ok(result);
    }

    [HttpGet("password-policy")]
    public IActionResult GetPasswordPolicy()
    {
        return Ok(new
        {
            RequiredLength = "Minimum 6 caractères (par défaut)",
            RequireNonAlphanumeric = "Caractères spéciaux requis",
            RequireDigit = "Chiffres requis",
            RequireLowercase = "Minuscules requises",
            RequireUppercase = "Majuscules requises",
            Message = "Vérifiez votre configuration Identity dans Program.cs/Startup.cs"
        });
    }

    [HttpPost("promote-to-admin/{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> PromoteToAdmin(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("Utilisateur non trouvé");
            }

            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return Ok(new { Message = $"L'utilisateur {user.Email} est déjà Admin" });
            }

            var result = await _userManager.AddToRoleAsync(user, "Admin");

            if (result.Succeeded)
            {
                return Ok(new
                {
                    Message = $"L'utilisateur {user.Email} a été promu Admin avec succès",
                    UserId = userId,
                    Email = user.Email
                });
            }
            else
            {
                return BadRequest(new
                {
                    Message = "Erreur lors de la promotion",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("my-user-info")]
    public async Task<IActionResult> GetMyUserInfo()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Success = false, Message = "Utilisateur non connecté" });
        }

        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { Success = false, Message = "Utilisateur non trouvé" });
        }

        var isAdmin = User.IsInRole("Admin");

        return Ok(new
        {
            Success = true,
            UserId = userId,
            User = user,
            IsAdmin = isAdmin,
            Roles = User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                          .Select(c => c.Value).ToList()
        });
    }

    // NOUVEAUX ENDPOINTS AVEC ACCÈS DIRECT AUX DONNÉES

    [HttpGet("club/{clubId}/members")]
    [Authorize]
    public async Task<IActionResult> GetClubMembers(Guid clubId)
    {
        try
        {
            // Accès direct aux données via le contexte
            var members = await _context.UserClubs
                .Include(uc => uc.User)
                .Include(uc => uc.Club)
                .Where(uc => uc.ClubId == clubId)
                .OrderBy(uc => uc.JoinedDate)
                .ToListAsync();

            var result = new List<object>();

            foreach (var userClub in members)
            {
                if (userClub.User != null)
                {
                    // Obtenir les rôles directement
                    var roles = await _userManager.GetRolesAsync(userClub.User);

                    var memberData = new
                    {
                        Id = userClub.User.Id,
                        Email = userClub.User.Email,
                        FirstName = userClub.User.FirstName,
                        LastName = userClub.User.LastName,
                        FullName = $"{userClub.User.FirstName} {userClub.User.LastName}",
                        PhoneNumber = userClub.User.PhoneNumber,
                        ProfilePictureUrl = userClub.User.ProfilePictureUrl,
                        NumeroMembre = userClub.User.NumeroMembre,
                        DateAnniversaire = userClub.User.DateAnniversaire,

                        // Dates importantes
                        UserJoinedDate = userClub.User.JoinedDate,
                        ClubJoinedDate = userClub.JoinedDate, // ← Date d'adhésion au club
                        UserJoinedDateFormatted = userClub.User.JoinedDate.ToString("dd/MM/yyyy"),
                        ClubJoinedDateFormatted = userClub.JoinedDate.ToString("dd/MM/yyyy"),

                        IsActive = userClub.User.IsActive,
                        Roles = roles.ToArray(),

                        // Informations du club
                        ClubId = userClub.ClubId,
                        ClubName = userClub.Club?.Name,

                        // Informations supplémentaires de la relation
                        UserClubId = userClub.Id
                    };

                    result.Add(memberData);
                }
            }

            return Ok(new
            {
                Success = true,
                ClubId = clubId,
                MemberCount = result.Count,
                Members = result
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Erreur lors de la récupération des membres du club",
                Error = ex.Message
            });
        }
    }
    [HttpDelete("club/{clubId}/member/{userId}")]
    [Authorize(Roles = "Admin")] // Seuls les admins peuvent supprimer des membres
    public async Task<IActionResult> RemoveClubMember(Guid clubId, string userId)
    {
        try
        {
            // Vérifier que le club existe
            var club = await _context.Clubs.FindAsync(clubId);
            if (club == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Club non trouvé"
                });
            }

            // Vérifier que l'utilisateur existe
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Utilisateur non trouvé"
                });
            }

            // Chercher la relation UserClub
            var userClub = await _context.UserClubs
                .FirstOrDefaultAsync(uc => uc.ClubId == clubId && uc.UserId == userId);

            if (userClub == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "L'utilisateur n'est pas membre de ce club"
                });
            }

            // Empêcher la suppression du dernier admin du club (optionnel)
            var isUserAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isUserAdmin)
            {
                var adminCount = await _context.UserClubs
                    .Include(uc => uc.User)
                    .CountAsync(uc => uc.ClubId == clubId &&
                               _context.UserRoles.Any(ur => ur.UserId == uc.UserId &&
                               _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin")));

                if (adminCount <= 1)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Impossible de supprimer le dernier administrateur du club"
                    });
                }
            }

            // Supprimer la relation UserClub
            _context.UserClubs.Remove(userClub);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Success = true,
                Message = $"L'utilisateur {user.FirstName} {user.LastName} a été retiré du club {club.Name}",
                RemovedMember = new
                {
                    UserId = userId,
                    UserName = $"{user.FirstName} {user.LastName}",
                    UserEmail = user.Email,
                    ClubId = clubId,
                    ClubName = club.Name,
                    RemovedDate = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Erreur lors de la suppression du membre",
                Error = ex.Message
            });
        }
    }

    // Endpoint alternatif pour supprimer complètement un utilisateur du système (plus drastique)
    [HttpDelete("user/{userId}")]
    [Authorize(Roles = "SuperAdmin")] // Réservé aux super admins
    public async Task<IActionResult> DeleteUser(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Utilisateur non trouvé"
                });
            }

            // Empêcher la suppression du dernier super admin
            var isSuperAdmin = await _userManager.IsInRoleAsync(user, "SuperAdmin");
            if (isSuperAdmin)
            {
                var superAdminCount = await _userManager.GetUsersInRoleAsync("SuperAdmin");
                if (superAdminCount.Count <= 1)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Impossible de supprimer le dernier Super Administrateur"
                    });
                }
            }

            // Supprimer d'abord toutes les relations UserClub
            var userClubs = await _context.UserClubs
                .Where(uc => uc.UserId == userId)
                .ToListAsync();

            _context.UserClubs.RemoveRange(userClubs);

            // Supprimer l'utilisateur (cela supprimera aussi ses rôles automatiquement)
            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Erreur lors de la suppression de l'utilisateur",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }

            return Ok(new
            {
                Success = true,
                Message = $"L'utilisateur {user.FirstName} {user.LastName} a été supprimé définitivement du système",
                DeletedUser = new
                {
                    UserId = userId,
                    UserName = $"{user.FirstName} {user.LastName}",
                    UserEmail = user.Email,
                    DeletedDate = DateTime.UtcNow,
                    ClubsRemoved = userClubs.Count
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Erreur lors de la suppression de l'utilisateur",
                Error = ex.Message
            });
        }
    }

    // Endpoint pour désactiver un utilisateur au lieu de le supprimer (option plus douce)
    [HttpPatch("user/{userId}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateUser(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Utilisateur non trouvé"
                });
            }

            if (!user.IsActive)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "L'utilisateur est déjà désactivé"
                });
            }

            // Désactiver l'utilisateur
            user.IsActive = false;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Erreur lors de la désactivation",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }

            return Ok(new
            {
                Success = true,
                Message = $"L'utilisateur {user.FirstName} {user.LastName} a été désactivé",
                DeactivatedUser = new
                {
                    UserId = userId,
                    UserName = $"{user.FirstName} {user.LastName}",
                    UserEmail = user.Email,
                    DeactivatedDate = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Erreur lors de la désactivation",
                Error = ex.Message
            });
        }
    }

    // Endpoint pour réactiver un utilisateur
    [HttpPatch("user/{userId}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ActivateUser(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Utilisateur non trouvé"
                });
            }

            if (user.IsActive)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "L'utilisateur est déjà actif"
                });
            }

            // Réactiver l'utilisateur
            user.IsActive = true;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Erreur lors de l'activation",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }

            return Ok(new
            {
                Success = true,
                Message = $"L'utilisateur {user.FirstName} {user.LastName} a été réactivé",
                ActivatedUser = new
                {
                    UserId = userId,
                    UserName = $"{user.FirstName} {user.LastName}",
                    UserEmail = user.Email,
                    ActivatedDate = DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "Erreur lors de l'activation",
                Error = ex.Message
            });
        }
    }
}