using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Domain.Identity;
using RotaryClubManager.Infrastructure.Data;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ProfileController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: api/profile
        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound("Utilisateur non trouvé");
                }

                // Récupérer les rôles de l'utilisateur
                var roles = await _userManager.GetRolesAsync(user);

                // Récupérer le club principal (première relation active) via UserClubs
                var primaryUserClub = await _context.UserClubs
                    .Include(uc => uc.Club)
                    .Where(uc => uc.UserId == userId)
                    .OrderBy(uc => uc.JoinedDate)
                    .FirstOrDefaultAsync();

                // Récupérer tous les clubs de l'utilisateur
                var userClubs = await _context.UserClubs
                    .Include(uc => uc.Club)
                    .Where(uc => uc.UserId == userId)
                    .Select(uc => new
                    {
                        uc.Club.Id,
                        uc.Club.Name,
                        uc.JoinedDate,
                        IsPrimary = uc.Id == primaryUserClub.Id
                    })
                    .ToListAsync();

                // Récupérer les commissions de l'utilisateur
                var commissions = await GetUserCommissions(userId);

                var profile = new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.PhoneNumber,
                    user.ProfilePictureUrl,
                    user.JoinedDate,
                    user.IsActive,
                    PrimaryClub = primaryUserClub?.Club != null ? new
                    {
                        primaryUserClub.Club.Id,
                        primaryUserClub.Club.Name,
                        JoinedDate = primaryUserClub.JoinedDate
                    } : null,
                    Clubs = userClubs,
                    Roles = roles.ToList(),
                    Commissions = commissions
                };

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du profil utilisateur");
                return StatusCode(500, "Une erreur est survenue lors de la récupération du profil");
            }
        }

        // PUT: api/profile
        [HttpPut]
        public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound("Utilisateur non trouvé");
                }

                // Valider les données
                if (string.IsNullOrWhiteSpace(request.FirstName))
                {
                    return BadRequest("Le prénom est requis");
                }

                if (string.IsNullOrWhiteSpace(request.LastName))
                {
                    return BadRequest("Le nom est requis");
                }

                // Vérifier l'unicité de l'email si modifié
                if (!string.Equals(request.Email, user.Email, StringComparison.OrdinalIgnoreCase))
                {
                    var existingUser = await _userManager.FindByEmailAsync(request.Email);
                    if (existingUser != null)
                    {
                        return BadRequest("Cet email est déjà utilisé par un autre utilisateur");
                    }
                }

                // Mettre à jour les informations
                user.FirstName = request.FirstName.Trim();
                user.LastName = request.LastName.Trim();
                user.Email = request.Email.Trim().ToLower();
                user.UserName = request.Email.Trim().ToLower();
                user.PhoneNumber = request.PhoneNumber?.Trim();
                user.ProfilePictureUrl = request.ProfilePictureUrl?.Trim();

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest($"Erreur lors de la mise à jour : {errors}");
                }

                _logger.LogInformation("Profil utilisateur {UserId} mis à jour avec succès", userId);
                return Ok("Profil mis à jour avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du profil utilisateur");
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour du profil");
            }
        }

        // PUT: api/profile/password
        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound("Utilisateur non trouvé");
                }

                var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest($"Erreur lors du changement de mot de passe : {errors}");
                }

                _logger.LogInformation("Mot de passe changé avec succès pour l'utilisateur {UserId}", userId);
                return Ok("Mot de passe changé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du changement de mot de passe");
                return StatusCode(500, "Une erreur est survenue lors du changement de mot de passe");
            }
        }

        // PUT: api/profile/club
        [HttpPut("club")]
        public async Task<IActionResult> UpdatePrimaryClub(UpdatePrimaryClubRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound("Utilisateur non trouvé");
                }

                // Vérifier que le club existe
                if (!request.ClubId.HasValue)
                {
                    return BadRequest("L'identifiant du club est requis");
                }

                var club = await _context.Clubs.FindAsync(request.ClubId.Value);
                if (club == null)
                {
                    return BadRequest("Club non trouvé");
                }

                // Vérifier que l'utilisateur est déjà membre de ce club
                var userClub = await _context.UserClubs
                    .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.ClubId == request.ClubId.Value);

                if (userClub == null)
                {
                    // Si l'utilisateur n'est pas encore membre, créer la relation
                    userClub = new UserClub
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        ClubId = request.ClubId.Value,
                        JoinedDate = DateTime.UtcNow,
                        
                    };

                    _context.UserClubs.Add(userClub);
                }
              

                // Optionnel : Marquer les autres clubs comme non-primaires si vous voulez maintenir ce concept
                // Ou vous pouvez simplement laisser cette logique côté client pour déterminer le club principal

                await _context.SaveChangesAsync();

                _logger.LogInformation("Club principal mis à jour pour l'utilisateur {UserId} : {ClubId}",
                    userId, request.ClubId);

                return Ok("Club principal mis à jour avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du club principal");
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour du club principal");
            }
        }

        // GET: api/profile/commissions
        [HttpGet("commissions")]
        public async Task<IActionResult> GetUserCommissions()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var commissions = await GetUserCommissions(userId);
                return Ok(commissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des commissions de l'utilisateur");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des commissions");
            }
        }

        // GET: api/profile/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetUserStats()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return NotFound("Utilisateur non trouvé");
                }

                // Compter les commissions actives
                var activeCommissions = await _context.MembresCommission
                    .CountAsync(mc => mc.MembreId == userId && mc.EstActif);

                // Compter les commissions où l'utilisateur est responsable
                var responsableCommissions = await _context.MembresCommission
                    .CountAsync(mc => mc.MembreId == userId && mc.EstActif && mc.EstResponsable);

                // Durée d'adhésion
                var membershipDuration = DateTime.UtcNow - user.JoinedDate;

                var stats = new
                {
                    CommissionsActives = activeCommissions,
                    CommissionsResponsable = responsableCommissions,
                    AnneesDadhesion = Math.Round(membershipDuration.TotalDays / 365.25, 1),
                    DateAdhesion = user.JoinedDate,
                    EstActif = user.IsActive
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques utilisateur");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
            }
        }

        // Méthode privée pour récupérer les commissions d'un utilisateur
        private async Task<List<object>> GetUserCommissions(string userId)
        {
            var commissions = await _context.MembresCommission
                .Include(mc => mc.Commission)
                .Include(mc => mc.Mandat)
                    .ThenInclude(m => m.Club)
                .Where(mc => mc.MembreId == userId && mc.EstActif)
                .Select(mc => new
                {
                    Id = mc.Id,
                    Commission = new
                    {
                        mc.Commission.Id,
                        mc.Commission.Nom,
                        mc.Commission.Description
                    },
                    Club = new
                    {
                        mc.Mandat.Club.Id,
                        mc.Mandat.Club.Name,
                    },
                    mc.EstResponsable,
                    mc.DateNomination,
                    Mandat = new
                    {
                        mc.Mandat.Id,
                        mc.Mandat.Annee,
                        mc.Mandat.EstActuel
                    }
                })
                .ToListAsync();

            return commissions.Cast<object>().ToList();
        }
    }

    // DTOs pour les requêtes
    public class UpdateProfileRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class UpdatePrimaryClubRequest
    {
        public Guid? ClubId { get; set; }
    }
}