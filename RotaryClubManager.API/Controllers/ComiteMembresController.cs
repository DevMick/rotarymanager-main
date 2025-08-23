using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Domain.Identity;
using RotaryClubManager.Infrastructure.Data;
using RotaryClubManager.Infrastructure.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/clubs/{clubId}/mandats/{mandatId}/membres")]
    [ApiController]
    [Authorize]
    public class ComiteMembresController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantService _tenantService;
        private readonly ILogger<ComiteMembresController> _logger;

        public ComiteMembresController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ITenantService tenantService,
            ILogger<ComiteMembresController> logger)
        {
            _context = context;
            _userManager = userManager;
            _tenantService = tenantService;
            _logger = logger;
        }

        // GET: api/clubs/{clubId}/mandats/{mandatId}/membres
        // Récupérer tous les membres d'un mandat spécifique
        [HttpGet]
        public async Task<IActionResult> GetMembresMandat(Guid clubId, Guid mandatId)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (mandatId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du mandat est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que le mandat appartient bien au club
                var mandat = await _context.Mandats
                    .FirstOrDefaultAsync(m => m.Id == mandatId && m.ClubId == clubId);

                if (mandat == null)
                {
                    return NotFound($"Mandat avec l'ID {mandatId} non trouvé dans le club {clubId}");
                }

                // Récupérer tous les membres de ce mandat
                var membres = await _context.ComiteMembres
                    .Include(cm => cm.Membre)
                    .Include(cm => cm.Fonction)
                    .Include(cm => cm.Mandat)
                    .Where(cm => cm.MandatId == mandatId)
                    .OrderBy(cm => cm.Membre.LastName)
                    .ThenBy(cm => cm.Membre.FirstName)
                    .Select(cm => new ComiteMembreDetailDto
                    {
                        Id = cm.Id,
                        MembreId = cm.MembreId,
                        NomCompletMembre = $"{cm.Membre.FirstName} {cm.Membre.LastName}",
                        EmailMembre = cm.Membre.Email,
                        IsActiveMembre = cm.Membre.IsActive,
                        FonctionId = cm.FonctionId,
                        NomFonction = cm.Fonction.NomFonction
                    })
                    .ToListAsync();

                var response = new
                {
                    Mandat = new
                    {
                        Id = mandat.Id,
                        Annee = mandat.Annee,
                        Description = mandat.Description,
                        ClubId = mandat.ClubId
                    },
                    Membres = membres,
                    Statistiques = new
                    {
                        TotalMembres = membres.Count,
                        MembresActifs = membres.Count(m => m.IsActiveMembre),
                        MembresInactifs = membres.Count(m => !m.IsActiveMembre),
                        FonctionsDistinctes = membres.Select(m => m.FonctionId).Distinct().Count()
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des membres du mandat {MandatId} du club {ClubId}",
                    mandatId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des membres");
            }
        }

        // GET: api/clubs/{clubId}/mandats/{mandatId}/membres/{comiteMembreId}
        // Récupérer un membre spécifique d'un mandat
        [HttpGet("{comiteMembreId:guid}")]
        public async Task<IActionResult> GetComiteMembre(Guid clubId, Guid mandatId, Guid comiteMembreId)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (mandatId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du mandat est invalide");
                }

                if (comiteMembreId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du membre est invalide");
                }

                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                var comiteMembre = await _context.ComiteMembres
                    .Include(cm => cm.Membre)
                    .Include(cm => cm.Fonction)
                    .Include(cm => cm.Mandat)
                    .FirstOrDefaultAsync(cm => cm.Id == comiteMembreId &&
                                             cm.MandatId == mandatId &&
                                             cm.Mandat.ClubId == clubId);

                if (comiteMembre == null)
                {
                    return NotFound("Membre non trouvé");
                }

                var response = new ComiteMembreDetailDto
                {
                    Id = comiteMembre.Id,
                    MembreId = comiteMembre.MembreId,
                    NomCompletMembre = $"{comiteMembre.Membre.FirstName} {comiteMembre.Membre.LastName}",
                    EmailMembre = comiteMembre.Membre.Email,
                    IsActiveMembre = comiteMembre.Membre.IsActive,
                    FonctionId = comiteMembre.FonctionId,
                    NomFonction = comiteMembre.Fonction.NomFonction
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du membre {ComiteMembreId} du club {ClubId}",
                    comiteMembreId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du membre");
            }
        }

        // POST: api/clubs/{clubId}/mandats/{mandatId}/membres
        // Affecter un membre à un mandat
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> AffecterMembreMandat(
            Guid clubId,
            Guid mandatId,
            [FromBody] AffecterMembreMandatRequest request)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (mandatId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du mandat est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que le mandat appartient bien au club
                var mandat = await _context.Mandats
                    .FirstOrDefaultAsync(m => m.Id == mandatId && m.ClubId == clubId);

                if (mandat == null)
                {
                    return NotFound($"Mandat avec l'ID {mandatId} non trouvé dans le club {clubId}");
                }

                // Vérifier que l'utilisateur existe et est membre du club via UserClubs
                var userClub = await _context.UserClubs
                    .Include(uc => uc.User)
                    .FirstOrDefaultAsync(uc => uc.UserId == request.MembreId &&
                                             uc.ClubId == clubId &&
                                             uc.User.IsActive);

                if (userClub == null)
                {
                    return NotFound("Membre non trouvé dans ce club ou membre inactif");
                }

                // Vérifier que la fonction existe
                var fonction = await _context.Fonctions
                    .FirstOrDefaultAsync(f => f.Id == request.FonctionId);

                if (fonction == null)
                {
                    return NotFound("Fonction non trouvée");
                }

                // Vérifier si le membre n'est pas déjà affecté à ce mandat avec cette fonction
                var existingAffectation = await _context.ComiteMembres
                    .FirstOrDefaultAsync(cm => cm.MembreId == request.MembreId &&
                                             cm.MandatId == mandatId &&
                                             cm.FonctionId == request.FonctionId);

                if (existingAffectation != null)
                {
                    return BadRequest($"Le membre est déjà affecté à ce mandat avec cette fonction");
                }

                // Créer l'affectation
                var comiteMembre = new ComiteMembre
                {
                    Id = Guid.NewGuid(),
                    MembreId = request.MembreId,
                    MandatId = mandatId,
                    FonctionId = request.FonctionId
                };

                _context.ComiteMembres.Add(comiteMembre);
                await _context.SaveChangesAsync();

                // Charger les données pour la réponse
                var comiteMembreCree = await _context.ComiteMembres
                    .Include(cm => cm.Membre)
                    .Include(cm => cm.Fonction)
                    .FirstOrDefaultAsync(cm => cm.Id == comiteMembre.Id);

                var response = new ComiteMembreDetailDto
                {
                    Id = comiteMembreCree!.Id,
                    MembreId = comiteMembreCree.MembreId,
                    NomCompletMembre = $"{comiteMembreCree.Membre.FirstName} {comiteMembreCree.Membre.LastName}",
                    EmailMembre = comiteMembreCree.Membre.Email,
                    IsActiveMembre = comiteMembreCree.Membre.IsActive,
                    FonctionId = comiteMembreCree.FonctionId,
                    NomFonction = comiteMembreCree.Fonction.NomFonction
                };

                _logger.LogInformation(
                    "Membre {MembreId} affecté au mandat {MandatAnnee} du club {ClubId} avec la fonction {FonctionNom}",
                    request.MembreId,
                    mandat.Annee,
                    clubId,
                    fonction.NomFonction
                );

                return CreatedAtAction(
                    nameof(GetComiteMembre),
                    new { clubId, mandatId, comiteMembreId = comiteMembre.Id },
                    response
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'affectation du membre {MembreId} au mandat {MandatId} du club {ClubId}",
                    request.MembreId, mandatId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de l'affectation du membre");
            }
        }

        // PUT: api/clubs/{clubId}/mandats/{mandatId}/membres/{comiteMembreId}
        // Modifier l'affectation d'un membre à un mandat
        [HttpPut("{comiteMembreId:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> ModifierAffectationMembre(
            Guid clubId,
            Guid mandatId,
            Guid comiteMembreId,
            [FromBody] ModifierAffectationMandatRequest request)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (mandatId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du mandat est invalide");
                }

                if (comiteMembreId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du membre est invalide");
                }

                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                var comiteMembre = await _context.ComiteMembres
                    .Include(cm => cm.Mandat)
                    .FirstOrDefaultAsync(cm => cm.Id == comiteMembreId &&
                                             cm.MandatId == mandatId &&
                                             cm.Mandat.ClubId == clubId);

                if (comiteMembre == null)
                {
                    return NotFound("Affectation de membre non trouvée");
                }

                // Vérifier que la nouvelle fonction existe si spécifiée
                if (request.FonctionId.HasValue)
                {
                    var fonction = await _context.Fonctions
                        .FirstOrDefaultAsync(f => f.Id == request.FonctionId.Value);

                    if (fonction == null)
                    {
                        return NotFound("Fonction non trouvée");
                    }

                    // Vérifier qu'il n'y a pas déjà une affectation avec cette fonction pour ce membre et ce mandat
                    var existingWithFunction = await _context.ComiteMembres
                        .FirstOrDefaultAsync(cm => cm.MembreId == comiteMembre.MembreId &&
                                                 cm.MandatId == mandatId &&
                                                 cm.FonctionId == request.FonctionId.Value &&
                                                 cm.Id != comiteMembreId);

                    if (existingWithFunction != null)
                    {
                        return BadRequest("Ce membre a déjà cette fonction dans ce mandat");
                    }

                    comiteMembre.FonctionId = request.FonctionId.Value;
                }

                _context.Entry(comiteMembre).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Affectation {ComiteMembreId} modifiée avec succès", comiteMembreId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification de l'affectation {ComiteMembreId}", comiteMembreId);
                return StatusCode(500, "Une erreur est survenue lors de la modification");
            }
        }

        // DELETE: api/clubs/{clubId}/mandats/{mandatId}/membres/{comiteMembreId}
        // Supprimer l'affectation d'un membre à un mandat
        [HttpDelete("{comiteMembreId}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> SupprimerComiteMembre(
            Guid clubId,
            Guid mandatId,
            string comiteMembreId)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (mandatId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du mandat est invalide");
                }

                if (string.IsNullOrEmpty(comiteMembreId))
                {
                    return BadRequest("L'identifiant est requis");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                ComiteMembre? comiteMembre = null;

                // Essayer d'abord comme ID de ComiteMembre (Guid)
                if (Guid.TryParse(comiteMembreId, out Guid comiteMembreGuid))
                {
                    comiteMembre = await _context.ComiteMembres
                        .Include(cm => cm.Membre)
                        .Include(cm => cm.Fonction)
                        .Include(cm => cm.Mandat)
                        .FirstOrDefaultAsync(cm => cm.Id == comiteMembreGuid &&
                                                 cm.MandatId == mandatId &&
                                                 cm.Mandat.ClubId == clubId);
                }

                // Si pas trouvé, essayer comme MembreId (string)
                if (comiteMembre == null)
                {
                    comiteMembre = await _context.ComiteMembres
                        .Include(cm => cm.Membre)
                        .Include(cm => cm.Fonction)
                        .Include(cm => cm.Mandat)
                        .FirstOrDefaultAsync(cm => cm.MembreId == comiteMembreId &&
                                                 cm.MandatId == mandatId &&
                                                 cm.Mandat.ClubId == clubId);
                }

                if (comiteMembre == null)
                {
                    return NotFound("Affectation de membre au mandat non trouvée");
                }

                // Sauvegarder les informations pour le log avant suppression
                var membreNom = $"{comiteMembre.Membre.FirstName} {comiteMembre.Membre.LastName}";
                var mandatAnnee = comiteMembre.Mandat.Annee;
                var fonctionNom = comiteMembre.Fonction.NomFonction;

                // Supprimer l'affectation
                _context.ComiteMembres.Remove(comiteMembre);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Membre {MembreNom} supprimé du mandat {MandatAnnee} du club {ClubId} (fonction: {FonctionNom})",
                    membreNom,
                    mandatAnnee,
                    clubId,
                    fonctionNom
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors de la suppression du membre {ComiteMembreId} du mandat {MandatId} du club {ClubId}",
                    comiteMembreId, mandatId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression du membre du mandat");
            }
        }

        // GET: api/clubs/{clubId}/mandats/{mandatId}/membres/disponibles
        // Récupérer les membres du club qui ne sont pas encore dans ce mandat
        [HttpGet("disponibles")]
        public async Task<IActionResult> GetMembresDisponibles(Guid clubId, Guid mandatId)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (mandatId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du mandat est invalide");
                }

                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                // Récupérer les IDs des membres déjà affectés à ce mandat
                var membresAfjectes = await _context.ComiteMembres
                    .Where(cm => cm.MandatId == mandatId)
                    .Select(cm => cm.MembreId)
                    .ToListAsync();

                // Récupérer tous les membres actifs du club qui ne sont pas déjà affectés
                var membresDisponibles = await _context.UserClubs
                    .Include(uc => uc.User)
                    .Where(uc => uc.ClubId == clubId
                                &&
                               uc.User.IsActive &&
                               !membresAfjectes.Contains(uc.UserId))
                    .Select(uc => new MembreComiteDisponibleDto
                    {
                        MembreId = uc.UserId,
                        NomComplet = $"{uc.User.FirstName} {uc.User.LastName}",
                        Email = uc.User.Email
                    })
                    .OrderBy(u => u.NomComplet)
                    .ToListAsync();

                return Ok(membresDisponibles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des membres disponibles pour le mandat {MandatId}", mandatId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des membres disponibles");
            }
        }

        // GET: api/clubs/{clubId}/fonctions
        // Récupérer toutes les fonctions disponibles
        [HttpGet("~/api/clubs/{clubId}/fonctions")]
        public async Task<IActionResult> GetFonctions(Guid clubId)
        {
            try
            {
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                var fonctions = await _context.Fonctions
                    .OrderBy(f => f.NomFonction)
                    .Select(f => new ComiteFonctionDto
                    {
                        FonctionId = f.Id,
                        NomFonction = f.NomFonction
                    })
                    .ToListAsync();

                return Ok(fonctions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des fonctions");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des fonctions");
            }
        }

        // Méthodes d'aide pour vérifier les autorisations
        private async Task<bool> CanAccessClub(Guid clubId)
        {
            if (User.IsInRole("Admin"))
                return true;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return false;

            // Vérifier si l'utilisateur a une relation active avec ce club
            var hasAccess = await _context.UserClubs
                .AnyAsync(uc => uc.UserId == userId && uc.ClubId == clubId);

            return hasAccess;
        }

        private async Task<bool> CanManageClub(Guid clubId)
        {
            if (User.IsInRole("Admin"))
                return true;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return false;

            // Vérifier si l'utilisateur appartient au club
            var userInClub = await _context.UserClubs
                .AnyAsync(uc => uc.UserId == userId && uc.ClubId == clubId);

            if (!userInClub)
                return false;

            return User.IsInRole("President") || User.IsInRole("Secretary");
        }
    }

    // DTOs pour les membres de mandat
    public class ComiteMembreDetailDto
    {
        public Guid Id { get; set; }
        public string MembreId { get; set; } = string.Empty;
        public string NomCompletMembre { get; set; } = string.Empty;
        public string EmailMembre { get; set; } = string.Empty;
        public bool IsActiveMembre { get; set; }
        public Guid FonctionId { get; set; }
        public string NomFonction { get; set; } = string.Empty;
    }

    public class AffecterMembreMandatRequest
    {
        [Required]
        public string MembreId { get; set; } = string.Empty;

        [Required]
        public Guid FonctionId { get; set; }
    }

    public class ModifierAffectationMandatRequest
    {
        public Guid? FonctionId { get; set; }
    }

    public class MembreComiteDisponibleDto
    {
        public string MembreId { get; set; } = string.Empty;
        public string NomComplet { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class ComiteFonctionDto
    {
        public Guid FonctionId { get; set; }
        public string NomFonction { get; set; } = string.Empty;
    }
}