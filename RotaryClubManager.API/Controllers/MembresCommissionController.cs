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
    [Route("api/clubs/{clubId}/commissions/{commissionId}/membres")]
    [ApiController]
    [Authorize]
    public class MembresCommissionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantService _tenantService;
        private readonly ILogger<MembresCommissionController> _logger;

        public MembresCommissionController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ITenantService tenantService,
            ILogger<MembresCommissionController> logger)
        {
            _context = context;
            _userManager = userManager;
            _tenantService = tenantService;
            _logger = logger;
        }

        // MÉTHODE HELPER : Récupérer le mandat actuel (année la plus grande)
        private async Task<Mandat?> GetMandatActuelAsync(Guid clubId)
        {
            return await _context.Mandats
                .Where(m => m.ClubId == clubId)
                .OrderByDescending(m => m.Annee)
                .FirstOrDefaultAsync();
        }

        // GET: api/clubs/{clubId}/commissions/{commissionId}/membres
        // Récupérer tous les membres d'une commission spécifique
        [HttpGet]
        public async Task<IActionResult> GetMembresCommission(Guid clubId, [FromRoute] string commissionId)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (string.IsNullOrEmpty(commissionId) || commissionId.ToLower() == "undefined")
                {
                    return BadRequest("L'identifiant de la commission est requis");
                }

                // Tenter de convertir l'ID en Guid
                if (!Guid.TryParse(commissionId, out Guid commissionGuid))
                {
                    return BadRequest("L'identifiant de la commission n'est pas dans un format valide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que la commission existe
                var commission = await _context.Commissions
                    .FirstOrDefaultAsync(c => c.Id == commissionGuid);

                if (commission == null)
                {
                    return NotFound($"Commission avec l'ID {commissionGuid} non trouvée");
                }

                // Récupérer tous les membres de cette commission pour ce club
                var membres = await _context.MembresCommission
                    .Include(mc => mc.Membre)
                    .Include(mc => mc.Mandat)
                    .Include(mc => mc.Commission)
                    .Where(mc => mc.CommissionId == commissionGuid && mc.Mandat.ClubId == clubId)
                    .OrderBy(mc => mc.Membre.LastName)
                    .ThenBy(mc => mc.Membre.FirstName)
                    .Select(mc => new MembreCommissionDetailDto
                    {
                        Id = mc.Id,
                        MembreId = mc.MembreId,
                        NomCompletMembre = $"{mc.Membre.FirstName} {mc.Membre.LastName}",
                        EmailMembre = mc.Membre.Email,
                        EstResponsable = mc.EstResponsable,
                        EstActif = mc.EstActif,
                        DateNomination = mc.DateNomination,
                        DateDemission = mc.DateDemission,
                        Commentaires = mc.Commentaires,
                        MandatId = mc.MandatId,
                        MandatAnnee = mc.Mandat.Annee,
                        MandatDescription = mc.Mandat.Description
                    })
                    .ToListAsync();

                var response = new
                {
                    Commission = new
                    {
                        Id = commission.Id,
                        Nom = commission.Nom,
                        Description = commission.Description
                    },
                    Club = new
                    {
                        Id = clubId
                    },
                    Membres = membres,
                    Statistiques = new
                    {
                        TotalMembres = membres.Count,
                        MembresActifs = membres.Count(m => m.EstActif),
                        Responsables = membres.Count(m => m.EstResponsable && m.EstActif),
                        MembresInactifs = membres.Count(m => !m.EstActif)
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des membres de la commission {CommissionId} du club {ClubId}",
                    commissionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des membres");
            }
        }

        // GET: api/clubs/{clubId}/commissions/{commissionId}/membres/{membreCommissionId}
        // Récupérer un membre spécifique d'une commission
        [HttpGet("{membreCommissionId:guid}")]
        public async Task<IActionResult> GetMembreCommission(Guid clubId, [FromRoute] string commissionId, Guid membreCommissionId)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (string.IsNullOrEmpty(commissionId) || commissionId.ToLower() == "undefined")
                {
                    return BadRequest("L'identifiant de la commission est requis");
                }

                // Tenter de convertir l'ID en Guid
                if (!Guid.TryParse(commissionId, out Guid commissionGuid))
                {
                    return BadRequest("L'identifiant de la commission n'est pas dans un format valide");
                }

                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                var membreCommission = await _context.MembresCommission
                    .Include(mc => mc.Membre)
                    .Include(mc => mc.Mandat)
                    .Include(mc => mc.Commission)
                    .FirstOrDefaultAsync(mc => mc.Id == membreCommissionId &&
                                             mc.CommissionId == commissionGuid &&
                                             mc.Mandat.ClubId == clubId);

                if (membreCommission == null)
                {
                    return NotFound("Membre de commission non trouvé");
                }

                var response = new MembreCommissionDetailDto
                {
                    Id = membreCommission.Id,
                    MembreId = membreCommission.MembreId,
                    NomCompletMembre = $"{membreCommission.Membre.FirstName} {membreCommission.Membre.LastName}",
                    EmailMembre = membreCommission.Membre.Email,
                    EstResponsable = membreCommission.EstResponsable,
                    EstActif = membreCommission.EstActif,
                    DateNomination = membreCommission.DateNomination,
                    DateDemission = membreCommission.DateDemission,
                    Commentaires = membreCommission.Commentaires,
                    MandatId = membreCommission.MandatId,
                    MandatAnnee = membreCommission.Mandat.Annee,
                    MandatDescription = membreCommission.Mandat.Description
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du membre de commission {MembreCommissionId} du club {ClubId}",
                    membreCommissionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du membre");
            }
        }

        // POST: api/clubs/{clubId}/commissions/{commissionId}/membres
        // Affecter un membre à une commission
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> AffecterMembreCommission(
            Guid clubId,
            [FromRoute] string commissionId,
            [FromBody] AffecterMembreCommissionRequest request)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (string.IsNullOrEmpty(commissionId) || commissionId.ToLower() == "undefined")
                {
                    return BadRequest("L'identifiant de la commission est requis");
                }

                // Tenter de convertir l'ID en Guid
                if (!Guid.TryParse(commissionId, out Guid commissionGuid))
                {
                    return BadRequest("L'identifiant de la commission n'est pas dans un format valide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que la commission existe
                var commission = await _context.Commissions
                    .FirstOrDefaultAsync(c => c.Id == commissionGuid);

                if (commission == null)
                {
                    return NotFound($"Commission avec l'ID {commissionGuid} non trouvée");
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

                var user = userClub.User;

                // Vérifier que le mandat existe et appartient au club
                var mandat = await _context.Mandats
                    .FirstOrDefaultAsync(m => m.Id == request.MandatId && m.ClubId == clubId);

                if (mandat == null)
                {
                    return NotFound("Mandat non trouvé pour ce club");
                }

                // Vérifier si le membre n'est pas déjà affecté à cette commission pour ce mandat
                var existingAffectation = await _context.MembresCommission
                    .FirstOrDefaultAsync(mc => mc.MembreId == request.MembreId &&
                                             mc.CommissionId == commissionGuid &&
                                             mc.MandatId == request.MandatId &&
                                             mc.EstActif);

                if (existingAffectation != null)
                {
                    return BadRequest($"Le membre est déjà affecté à cette commission pour le mandat {mandat.Annee}");
                }

                // Vérifier les contraintes de responsabilité
                if (request.EstResponsable)
                {
                    // Vérifier s'il y a déjà un responsable actif pour cette commission et ce mandat
                    var existingResponsable = await _context.MembresCommission
                        .AnyAsync(mc => mc.CommissionId == commissionGuid &&
                                      mc.MandatId == request.MandatId &&
                                      mc.EstResponsable &&
                                      mc.EstActif);

                    if (existingResponsable)
                    {
                        return BadRequest("Il y a déjà un responsable pour cette commission dans ce mandat. " +
                                        "Veuillez d'abord retirer le responsable actuel ou ne pas définir ce membre comme responsable.");
                    }
                }

                // Créer l'affectation
                var membreCommission = new MembreCommission
                {
                    Id = Guid.NewGuid(),
                    MembreId = request.MembreId,
                    CommissionId = commissionGuid,
                    MandatId = request.MandatId,
                    EstResponsable = request.EstResponsable,
                    DateNomination = request.DateNomination?.ToUniversalTime() ?? DateTime.UtcNow,
                    EstActif = true,
                    Commentaires = request.Commentaires
                };

                _context.MembresCommission.Add(membreCommission);
                await _context.SaveChangesAsync();

                // Charger les données pour la réponse
                var membreCommissionCree = await _context.MembresCommission
                    .Include(mc => mc.Membre)
                    .Include(mc => mc.Mandat)
                    .FirstOrDefaultAsync(mc => mc.Id == membreCommission.Id);

                var response = new MembreCommissionDetailDto
                {
                    Id = membreCommissionCree!.Id,
                    MembreId = membreCommissionCree.MembreId,
                    NomCompletMembre = $"{membreCommissionCree.Membre.FirstName} {membreCommissionCree.Membre.LastName}",
                    EmailMembre = membreCommissionCree.Membre.Email,
                    EstResponsable = membreCommissionCree.EstResponsable,
                    EstActif = membreCommissionCree.EstActif,
                    DateNomination = membreCommissionCree.DateNomination,
                    DateDemission = membreCommissionCree.DateDemission,
                    Commentaires = membreCommissionCree.Commentaires,
                    MandatId = membreCommissionCree.MandatId,
                    MandatAnnee = membreCommissionCree.Mandat.Annee,
                    MandatDescription = membreCommissionCree.Mandat.Description
                };

                _logger.LogInformation(
                    "Membre {MembreId} affecté à la commission {CommissionNom} du club {ClubId} pour le mandat {MandatAnnee} {Role}",
                    request.MembreId,
                    commission.Nom,
                    clubId,
                    mandat.Annee,
                    request.EstResponsable ? "en tant que responsable" : "en tant que membre"
                );

                return CreatedAtAction(
                    nameof(GetMembreCommission),
                    new { clubId, commissionId, membreCommissionId = membreCommission.Id },
                    response
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'affectation du membre {MembreId} à la commission {CommissionId} du club {ClubId}",
                    request.MembreId, commissionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de l'affectation du membre");
            }
        }

        // PUT: api/clubs/{clubId}/commissions/{commissionId}/membres/{membreCommissionId}
        // Modifier l'affectation d'un membre à une commission
        [HttpPut("{membreCommissionId:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> ModifierAffectationMembre(
            Guid clubId,
            [FromRoute] string commissionId,
            Guid membreCommissionId,
            [FromBody] ModifierAffectationMembreRequest request)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (string.IsNullOrEmpty(commissionId) || commissionId.ToLower() == "undefined")
                {
                    return BadRequest("L'identifiant de la commission est requis");
                }

                // Tenter de convertir l'ID en Guid
                if (!Guid.TryParse(commissionId, out Guid commissionGuid))
                {
                    return BadRequest("L'identifiant de la commission n'est pas dans un format valide");
                }

                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                var membreCommission = await _context.MembresCommission
                    .Include(mc => mc.Mandat)
                    .FirstOrDefaultAsync(mc => mc.Id == membreCommissionId &&
                                             mc.CommissionId == commissionGuid &&
                                             mc.Mandat.ClubId == clubId);

                if (membreCommission == null)
                {
                    return NotFound("Affectation de membre non trouvée");
                }

                // Vérifier les contraintes si on veut définir comme responsable
                if (request.EstResponsable.HasValue && request.EstResponsable.Value && !membreCommission.EstResponsable)
                {
                    var existingResponsable = await _context.MembresCommission
                        .AnyAsync(mc => mc.CommissionId == commissionGuid &&
                                      mc.MandatId == membreCommission.MandatId &&
                                      mc.EstResponsable &&
                                      mc.EstActif &&
                                      mc.Id != membreCommissionId);

                    if (existingResponsable)
                    {
                        return BadRequest("Il y a déjà un responsable pour cette commission dans ce mandat");
                    }
                }

                // Mettre à jour les propriétés
                if (request.EstResponsable.HasValue)
                    membreCommission.EstResponsable = request.EstResponsable.Value;

                if (request.EstActif.HasValue)
                {
                    membreCommission.EstActif = request.EstActif.Value;

                    // Si on désactive, définir la date de démission
                    if (!request.EstActif.Value && membreCommission.DateDemission == null)
                    {
                        membreCommission.DateDemission = DateTime.UtcNow;
                    }
                    // Si on réactive, supprimer la date de démission
                    else if (request.EstActif.Value)
                    {
                        membreCommission.DateDemission = null;
                    }
                }

                if (request.Commentaires != null)
                    membreCommission.Commentaires = request.Commentaires;

                _context.Entry(membreCommission).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Affectation {MembreCommissionId} modifiée avec succès", membreCommissionId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification de l'affectation {MembreCommissionId}", membreCommissionId);
                return StatusCode(500, "Une erreur est survenue lors de la modification");
            }
        }

        // GET: api/clubs/{clubId}/commissions/{commissionId}/membres/disponibles
        // Récupérer les membres du club qui ne sont pas encore dans cette commission
        [HttpGet("disponibles")]
        public async Task<IActionResult> GetMembresDisponibles(Guid clubId, [FromRoute] string commissionId, [FromQuery] Guid? mandatId = null)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }
                if (string.IsNullOrEmpty(commissionId) || commissionId.ToLower() == "undefined")
                {
                    return BadRequest("L'identifiant de la commission est requis");
                }
                // Tenter de convertir l'ID en Guid
                if (!Guid.TryParse(commissionId, out Guid commissionGuid))
                {
                    return BadRequest("L'identifiant de la commission n'est pas dans un format valide");
                }
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }
                _tenantService.SetCurrentTenantId(clubId);

                // Si aucun mandat spécifié, prendre le mandat actuel (année la plus grande)
                if (!mandatId.HasValue)
                {
                    var mandatActuel = await GetMandatActuelAsync(clubId);
                    if (mandatActuel == null)
                    {
                        return BadRequest("Aucun mandat trouvé pour ce club");
                    }
                    mandatId = mandatActuel.Id;
                }

                // Récupérer les IDs des membres déjà affectés à cette commission pour ce mandat
                var membresAfjectes = await _context.MembresCommission
                    .Where(mc => mc.CommissionId == commissionGuid &&
                               mc.MandatId == mandatId &&
                               mc.EstActif)
                    .Select(mc => mc.MembreId)
                    .ToListAsync();

                // Récupérer tous les membres actifs du club qui ne sont pas déjà affectés via UserClubs
                var membresDisponibles = await _context.UserClubs
                    .Include(uc => uc.User)
                    .Where(uc => uc.ClubId == clubId &&
                               uc.User.IsActive &&
                               !membresAfjectes.Contains(uc.UserId))
                    .Select(uc => new MembreDisponibleDto
                    {
                        Id = uc.UserId,
                        NomComplet = $"{uc.User.FirstName} {uc.User.LastName}",
                        Email = uc.User.Email
                    })
                    .OrderBy(u => u.NomComplet)
                    .ToListAsync();

                return Ok(membresDisponibles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des membres disponibles pour la commission {CommissionId}", commissionId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des membres disponibles");
            }
        }

        // GET: api/clubs/{clubId}/membres
        // Récupérer tous les membres d'un club spécifique
        [HttpGet("~/api/clubs/{clubId}/membres")]
        public async Task<IActionResult> GetMembresClub(Guid clubId, [FromQuery] bool? includeInactive = false)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que le club existe
                var club = await _context.Clubs.FirstOrDefaultAsync(c => c.Id == clubId);
                if (club == null)
                {
                    return NotFound($"Club avec l'ID {clubId} non trouvé");
                }

                // Récupérer tous les membres du club via UserClubs
                var queryMembers = _context.UserClubs
                    .Include(uc => uc.User)
                    .Where(uc => uc.ClubId == clubId);

                var membres = await queryMembers
                    .OrderBy(uc => uc.User.LastName)
                    .ThenBy(uc => uc.User.FirstName)
                    .Select(uc => new MembreClubDto
                    {
                        Id = uc.User.Id,
                        NomComplet = $"{uc.User.FirstName} {uc.User.LastName}",
                        FirstName = uc.User.FirstName,
                        LastName = uc.User.LastName,
                        Email = uc.User.Email,
                        PhoneNumber = uc.User.PhoneNumber,
                        IsActive = uc.User.IsActive,
                    })
                    .ToListAsync();

                // Récupérer les rôles pour chaque membre
                var membresAvecRoles = new List<MembreClubDetailDto>();
                foreach (var membre in membres)
                {
                    var user = await _userManager.FindByIdAsync(membre.Id);
                    var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();

                    membresAvecRoles.Add(new MembreClubDetailDto
                    {
                        Id = membre.Id,
                        NomComplet = membre.NomComplet,
                        FirstName = membre.FirstName,
                        LastName = membre.LastName,
                        Email = membre.Email,
                        PhoneNumber = membre.PhoneNumber,
                        IsActive = membre.IsActive,
                        Roles = roles.ToList()
                    });
                }

                var response = new
                {
                    Club = new
                    {
                        Id = club.Id,
                        Name = club.Name,
                    },
                    Membres = membresAvecRoles,
                    Statistiques = new
                    {
                        TotalMembres = membresAvecRoles.Count,
                        MembresActifs = membresAvecRoles.Count(m => m.IsActive),
                        MembresInactifs = membresAvecRoles.Count(m => !m.IsActive),
                        Administrateurs = membresAvecRoles.Count(m => m.Roles.Contains("Admin")),
                        Presidents = membresAvecRoles.Count(m => m.Roles.Contains("President")),
                        Secretaires = membresAvecRoles.Count(m => m.Roles.Contains("Secretary"))
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des membres du club {ClubId}", clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des membres");
            }
        }

        // GET: api/clubs/{clubId}/membres/{membreId}
        // Récupérer un membre spécifique d'un club
        [HttpGet("~/api/clubs/{clubId}/membres/{membreId}")]
        public async Task<IActionResult> GetMembreClub(Guid clubId, string membreId)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }
                if (string.IsNullOrEmpty(membreId))
                {
                    return BadRequest("L'identifiant du membre est requis");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                // Récupérer le membre via UserClubs
                var userClub = await _context.UserClubs
                    .Include(uc => uc.User)
                    .FirstOrDefaultAsync(uc => uc.UserId == membreId && uc.ClubId == clubId);

                if (userClub == null)
                {
                    return NotFound("Membre non trouvé dans ce club");
                }

                var user = userClub.User;

                // Récupérer les rôles du membre
                var applicationUser = await _userManager.FindByIdAsync(membreId);
                var roles = applicationUser != null ? await _userManager.GetRolesAsync(applicationUser) : new List<string>();

                // Récupérer les commissions du membre pour le mandat actuel
                var mandatActuel = await GetMandatActuelAsync(clubId);

                var commissionsActuelles = new List<CommissionMembreDto>();
                if (mandatActuel != null)
                {
                    commissionsActuelles = await _context.MembresCommission
                        .Include(mc => mc.Commission)
                        .Where(mc => mc.MembreId == membreId &&
                                   mc.MandatId == mandatActuel.Id &&
                                   mc.EstActif)
                        .Select(mc => new CommissionMembreDto
                        {
                            CommissionId = mc.CommissionId,
                            NomCommission = mc.Commission.Nom,
                            EstResponsable = mc.EstResponsable,
                            DateNomination = mc.DateNomination
                        })
                        .ToListAsync();
                }

                var response = new MembreClubCompletDto
                {
                    Id = user.Id,
                    NomComplet = $"{user.FirstName} {user.LastName}",
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.IsActive,
                    Roles = roles.ToList(),
                    CommissionsActuelles = commissionsActuelles
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du membre {MembreId} du club {ClubId}", membreId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du membre");
            }
        }

        // DELETE: api/clubs/{clubId}/commissions/{commissionId}/membres/by-membre/{membreId}
        // Supprimer l'affectation d'un membre à une commission en utilisant le MembreId
        [HttpDelete("by-membre/{membreId}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> SupprimerMembreCommissionByMembreId(
            Guid clubId,
            [FromRoute] string commissionId,
            [FromRoute] string membreId,
            [FromQuery] Guid? mandatId = null)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (string.IsNullOrEmpty(commissionId) || commissionId.ToLower() == "undefined")
                {
                    return BadRequest("L'identifiant de la commission est requis");
                }

                if (string.IsNullOrEmpty(membreId))
                {
                    return BadRequest("L'identifiant du membre est requis");
                }

                // Tenter de convertir l'ID en Guid
                if (!Guid.TryParse(commissionId, out Guid commissionGuid))
                {
                    return BadRequest("L'identifiant de la commission n'est pas dans un format valide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                // Si aucun mandat spécifié, prendre le mandat actuel (année la plus grande)
                if (!mandatId.HasValue)
                {
                    var mandatActuel = await GetMandatActuelAsync(clubId);

                    if (mandatActuel != null)
                    {
                        mandatId = mandatActuel.Id;
                    }
                }

                // Construire la requête de base
                var query = _context.MembresCommission
                    .Include(mc => mc.Membre)
                    .Include(mc => mc.Commission)
                    .Include(mc => mc.Mandat)
                    .Where(mc => mc.MembreId == membreId &&
                                mc.CommissionId == commissionGuid &&
                                mc.Mandat.ClubId == clubId);

                // Ajouter le filtre sur le mandat si spécifié
                if (mandatId.HasValue)
                {
                    query = query.Where(mc => mc.MandatId == mandatId.Value);
                }

                // Récupérer l'affectation (prendre la première si plusieurs)
                var membreCommission = await query.FirstOrDefaultAsync();

                if (membreCommission == null)
                {
                    var errorMessage = mandatId.HasValue
                        ? $"Aucune affectation trouvée pour le membre {membreId} dans cette commission pour le mandat spécifié"
                        : $"Aucune affectation trouvée pour le membre {membreId} dans cette commission";

                    return NotFound(errorMessage);
                }

                // Sauvegarder les informations pour le log avant suppression
                var membreNom = $"{membreCommission.Membre.FirstName} {membreCommission.Membre.LastName}";
                var commissionNom = membreCommission.Commission.Nom;
                var mandatAnnee = membreCommission.Mandat.Annee;
                var etaitResponsable = membreCommission.EstResponsable;

                // Supprimer l'affectation
                _context.MembresCommission.Remove(membreCommission);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Membre {MembreNom} (ID: {MembreId}) supprimé de la commission {CommissionNom} du club {ClubId} pour le mandat {MandatAnnee} {Role}",
                    membreNom,
                    membreId,
                    commissionNom,
                    clubId,
                    mandatAnnee,
                    etaitResponsable ? "(était responsable)" : ""
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors de la suppression du membre {MembreId} de la commission {CommissionId} du club {ClubId}",
                    membreId, commissionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression du membre de la commission");
            }
        }

        // DELETE: api/clubs/{clubId}/commissions/{commissionId}/membres/{membreCommissionId}
        // Supprimer l'affectation d'un membre à une commission (version flexible)
        [HttpDelete("{membreCommissionId}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> SupprimerMembreCommissionFlexible(
            Guid clubId,
            [FromRoute] string commissionId,
            [FromRoute] string membreCommissionId,
            [FromQuery] Guid? mandatId = null)
        {
            try
            {
                // Validation des paramètres de base
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (string.IsNullOrEmpty(commissionId) || commissionId.ToLower() == "undefined")
                {
                    return BadRequest("L'identifiant de la commission est requis");
                }

                if (string.IsNullOrEmpty(membreCommissionId))
                {
                    return BadRequest("L'identifiant est requis");
                }

                // Tenter de convertir l'ID de commission en Guid
                if (!Guid.TryParse(commissionId, out Guid commissionGuid))
                {
                    return BadRequest("L'identifiant de la commission n'est pas dans un format valide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                MembreCommission? membreCommission = null;

                // Essayer d'abord comme ID de MembresCommission (Guid)
                if (Guid.TryParse(membreCommissionId, out Guid membreCommissionGuid))
                {
                    membreCommission = await _context.MembresCommission
                        .Include(mc => mc.Membre)
                        .Include(mc => mc.Commission)
                        .Include(mc => mc.Mandat)
                        .FirstOrDefaultAsync(mc => mc.Id == membreCommissionGuid &&
                                                 mc.CommissionId == commissionGuid &&
                                                 mc.Mandat.ClubId == clubId);
                }

                // Si pas trouvé, essayer comme MembreId (string)
                if (membreCommission == null)
                {
                    var query = _context.MembresCommission
                        .Include(mc => mc.Membre)
                        .Include(mc => mc.Commission)
                        .Include(mc => mc.Mandat)
                        .Where(mc => mc.MembreId == membreCommissionId &&
                                    mc.CommissionId == commissionGuid &&
                                    mc.Mandat.ClubId == clubId);

                    // Si un mandat est spécifié, l'utiliser
                    if (mandatId.HasValue)
                    {
                        query = query.Where(mc => mc.MandatId == mandatId.Value);
                    }
                    else
                    {
                        // Sinon, prendre l'affectation active ou la plus récente
                        query = query.OrderByDescending(mc => mc.EstActif)
                                   .ThenByDescending(mc => mc.DateNomination);
                    }

                    membreCommission = await query.FirstOrDefaultAsync();
                }

                if (membreCommission == null)
                {
                    return NotFound("Affectation de membre à la commission non trouvée");
                }

                // Sauvegarder les informations pour le log avant suppression
                var membreNom = $"{membreCommission.Membre.FirstName} {membreCommission.Membre.LastName}";
                var commissionNom = membreCommission.Commission.Nom;
                var mandatAnnee = membreCommission.Mandat.Annee;
                var etaitResponsable = membreCommission.EstResponsable;

                // Supprimer l'affectation
                _context.MembresCommission.Remove(membreCommission);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Membre {MembreNom} supprimé de la commission {CommissionNom} du club {ClubId} pour le mandat {MandatAnnee} {Role}",
                    membreNom,
                    commissionNom,
                    clubId,
                    mandatAnnee,
                    etaitResponsable ? "(était responsable)" : ""
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors de la suppression du membre {MembreCommissionId} de la commission {CommissionId} du club {ClubId}",
                    membreCommissionId, commissionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression du membre de la commission");
            }
        }

        // GET: api/clubs/{clubId}/membres/fonctions-commissions
        // Récupérer pour chaque membre du club sa fonction et ses commissions pour le mandat actuel
        [HttpGet("~/api/clubs/{clubId}/membres/fonctions-commissions")]
        public async Task<IActionResult> GetMembresFonctionsCommissions(Guid clubId)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que le club existe
                var club = await _context.Clubs.FirstOrDefaultAsync(c => c.Id == clubId);
                if (club == null)
                {
                    return NotFound($"Club avec l'ID {clubId} non trouvé");
                }

                // Récupérer le mandat actuel (année la plus grande)
                var mandatActuel = await GetMandatActuelAsync(clubId);

                if (mandatActuel == null)
                {
                    return BadRequest("Aucun mandat trouvé pour ce club");
                }

                // Récupérer tous les membres du club
                var membres = await _context.UserClubs
                    .Include(uc => uc.User)
                    .Where(uc => uc.ClubId == clubId && uc.User.IsActive)
                    .Select(uc => new
                    {
                        MembreId = uc.UserId,
                        NomComplet = $"{uc.User.FirstName} {uc.User.LastName}",
                        FirstName = uc.User.FirstName,
                        LastName = uc.User.LastName,
                        Email = uc.User.Email
                    })
                    .ToListAsync();

                var result = new List<MembreFonctionCommissionDto>();

                foreach (var membre in membres)
                {
                    // Récupérer la fonction du membre dans le mandat actuel
                    var fonctionMembre = await _context.ComiteMembres
                        .Include(cm => cm.Fonction)
                        .Where(cm => cm.MembreId == membre.MembreId && cm.MandatId == mandatActuel.Id)
                        .Select(cm => new
                        {
                            FonctionId = cm.FonctionId,
                            NomFonction = cm.Fonction.NomFonction
                        })
                        .FirstOrDefaultAsync();

                    // Récupérer les commissions du membre pour le mandat actuel
                    var commissionsActives = await _context.MembresCommission
                        .Include(mc => mc.Commission)
                        .Where(mc => mc.MembreId == membre.MembreId &&
                                   mc.MandatId == mandatActuel.Id &&
                                   mc.EstActif)
                        .Select(mc => new CommissionMembreInfoDto
                        {
                            CommissionId = mc.CommissionId,
                            NomCommission = mc.Commission.Nom,
                            EstResponsable = mc.EstResponsable,
                            DateNomination = mc.DateNomination
                        })
                        .ToListAsync();

                    var membreInfo = new MembreFonctionCommissionDto
                    {
                        MembreId = membre.MembreId,
                        NomCompletMembre = membre.NomComplet,
                        FirstName = membre.FirstName,
                        LastName = membre.LastName,
                        Email = membre.Email,
                        MandatActuel = new MandatInfoDto
                        {
                            MandatId = mandatActuel.Id,
                            Annee = mandatActuel.Annee,
                            Description = mandatActuel.Description
                        },
                        Fonction = fonctionMembre != null ? new FonctionMembreInfoDto
                        {
                            FonctionId = fonctionMembre.FonctionId,
                            NomFonction = fonctionMembre.NomFonction
                        } : null,
                        Commissions = commissionsActives
                    };

                    result.Add(membreInfo);
                }

                // Trier par nom de famille puis prénom
                result = result.OrderBy(r => r.LastName).ThenBy(r => r.FirstName).ToList();

                var response = new
                {
                    Club = new
                    {
                        Id = club.Id,
                        Name = club.Name
                    },
                    MandatActuel = new
                    {
                        Id = mandatActuel.Id,
                        Annee = mandatActuel.Annee,
                        Description = mandatActuel.Description
                    },
                    Membres = result,
                    Statistiques = new
                    {
                        TotalMembres = result.Count,
                        MembresAvecFonction = result.Count(m => m.Fonction != null),
                        MembresSansFonction = result.Count(m => m.Fonction == null),
                        MembresAvecCommissions = result.Count(m => m.Commissions.Any()),
                        MembresSansCommission = result.Count(m => !m.Commissions.Any()),
                        TotalCommissionsAffectees = result.SelectMany(m => m.Commissions).Count(),
                        ResponsablesCommissions = result.SelectMany(m => m.Commissions).Count(c => c.EstResponsable)
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des fonctions et commissions des membres du club {ClubId}", clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des informations");
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

        // Ajoutez cette méthode temporaire dans votre contrôleur
        [HttpPost("debug-detailed")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> DebugDetailedAffecterMembreCommission(
            Guid clubId,
            [FromRoute] string commissionId,
            [FromBody] AffecterMembreCommissionRequest request)
        {
            var debugInfo = new List<string>();

            try
            {
                debugInfo.Add($"1. Début - Club: {clubId}, Commission: {commissionId}, Membre: {request.MembreId}");

                // Validation de base
                if (!Guid.TryParse(commissionId, out Guid commissionGuid))
                {
                    return BadRequest(new { Error = "CommissionId invalide", Debug = debugInfo });
                }
                debugInfo.Add($"2. CommissionId parsé: {commissionGuid}");

                // Vérifications d'autorisation
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Pas d'autorisation pour gérer ce club");
                }
                debugInfo.Add("3. Autorisation validée");

                _tenantService.SetCurrentTenantId(clubId);
                debugInfo.Add("4. Tenant défini");

                // Vérifier la commission
                var commission = await _context.Commissions
                    .FirstOrDefaultAsync(c => c.Id == commissionGuid);

                if (commission == null)
                {
                    debugInfo.Add($"5. ERREUR: Commission {commissionGuid} non trouvée");
                    return NotFound(new { Error = "Commission non trouvée", Debug = debugInfo });
                }
                debugInfo.Add($"5. Commission trouvée: {commission.Nom}");

                // Vérifier le membre dans UserClubs
                var userClub = await _context.UserClubs
                    .Include(uc => uc.User)
                    .FirstOrDefaultAsync(uc => uc.UserId == request.MembreId && uc.ClubId == clubId);

                if (userClub == null)
                {
                    debugInfo.Add($"6. ERREUR: Membre {request.MembreId} non trouvé dans le club {clubId}");

                    // Vérifier si le membre existe dans AspNetUsers
                    var userExists = await _context.Users.AnyAsync(u => u.Id == request.MembreId);
                    debugInfo.Add($"6.1. Membre existe dans AspNetUsers: {userExists}");

                    // Vérifier s'il y a une relation UserClub pour ce membre
                    var userClubExists = await _context.UserClubs.AnyAsync(uc => uc.UserId == request.MembreId);
                    debugInfo.Add($"6.2. Membre a des relations UserClub: {userClubExists}");

                    return NotFound(new { Error = "Membre non trouvé dans ce club", Debug = debugInfo });
                }
                debugInfo.Add($"6. Membre trouvé: {userClub.User.FirstName} {userClub.User.LastName}");

                // Vérifier le mandat
                var mandat = await _context.Mandats
                    .FirstOrDefaultAsync(m => m.Id == request.MandatId && m.ClubId == clubId);

                if (mandat == null)
                {
                    debugInfo.Add($"7. ERREUR: Mandat {request.MandatId} non trouvé dans le club {clubId}");
                    return NotFound(new { Error = "Mandat non trouvé", Debug = debugInfo });
                }
                debugInfo.Add($"7. Mandat trouvé: {mandat.Annee}");

                // Vérifier les affectations existantes
                var existingCount = await _context.MembresCommission
                    .CountAsync(mc => mc.MembreId == request.MembreId &&
                                    mc.CommissionId == commissionGuid &&
                                    mc.MandatId == request.MandatId);
                debugInfo.Add($"8. Affectations existantes: {existingCount}");

                if (existingCount > 0)
                {
                    return BadRequest(new { Error = "Affectation déjà existante", Debug = debugInfo });
                }

                // Vérifier la contrainte de responsabilité
                if (request.EstResponsable)
                {
                    var existingResponsable = await _context.MembresCommission
                        .AnyAsync(mc => mc.CommissionId == commissionGuid &&
                                      mc.MandatId == request.MandatId &&
                                      mc.EstResponsable &&
                                      mc.EstActif);

                    debugInfo.Add($"9. Responsable existant: {existingResponsable}");

                    if (existingResponsable)
                    {
                        return BadRequest(new { Error = "Un responsable existe déjà", Debug = debugInfo });
                    }
                }

                // Créer l'entité
                var membreCommission = new MembreCommission
                {
                    Id = Guid.NewGuid(),
                    MembreId = request.MembreId,
                    CommissionId = commissionGuid,
                    MandatId = request.MandatId,
                    EstResponsable = request.EstResponsable,
                    DateNomination = request.DateNomination?.ToUniversalTime() ?? DateTime.UtcNow,
                    EstActif = true,
                    Commentaires = request.Commentaires
                };
                debugInfo.Add($"10. Entité créée avec ID: {membreCommission.Id}");

                // Tentative d'ajout
                _context.MembresCommission.Add(membreCommission);
                debugInfo.Add("11. Entité ajoutée au contexte");

                // Tentative de sauvegarde
                await _context.SaveChangesAsync();
                debugInfo.Add("12. Sauvegarde réussie!");

                return Ok(new
                {
                    Success = true,
                    Message = "Affectation créée avec succès",
                    AffectationId = membreCommission.Id,
                    Debug = debugInfo
                });
            }
            catch (Exception ex)
            {
                debugInfo.Add($"ERREUR: {ex.Message}");
                debugInfo.Add($"Type: {ex.GetType().Name}");

                if (ex.InnerException != null)
                {
                    debugInfo.Add($"Inner Exception: {ex.InnerException.Message}");
                }

                _logger.LogError(ex, "Erreur détaillée lors de l'affectation avec debug info: {@DebugInfo}", debugInfo);

                return StatusCode(500, new
                {
                    Error = ex.Message,
                    Type = ex.GetType().Name,
                    InnerException = ex.InnerException?.Message,
                    Debug = debugInfo
                });
            }
        }

        // Ajoutez cette méthode temporaire dans votre MembresCommissionController

        [HttpGet("debug-ef-model")]
        [AllowAnonymous]
        public IActionResult DebugEFModel()
        {
            try
            {
                var entityType = _context.Model.FindEntityType(typeof(MembreCommission));

                var properties = entityType?.GetProperties().Select(p => new
                {
                    Name = p.Name,
                    Type = p.ClrType.Name,
                    IsNullable = p.IsNullable,
                    IsForeignKey = p.IsForeignKey(),
                    IsKey = p.IsKey()
                }).ToList();

                var foreignKeys = entityType?.GetForeignKeys().Select(fk => new
                {
                    Properties = string.Join(", ", fk.Properties.Select(p => p.Name)),
                    PrincipalEntityType = fk.PrincipalEntityType.ClrType.Name,
                    PrincipalKey = string.Join(", ", fk.PrincipalKey.Properties.Select(p => p.Name))
                }).ToList();

                return Ok(new
                {
                    EntityName = entityType?.ClrType.Name,
                    Properties = properties,
                    ForeignKeys = foreignKeys,
                    ModelWasBuilt = _context.Model != null
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }
    }

    // DTOs pour les membres de commissions
    public class MembreCommissionDetailDto
    {
        public Guid Id { get; set; }
        public string MembreId { get; set; } = string.Empty;
        public string NomCompletMembre { get; set; } = string.Empty;
        public string EmailMembre { get; set; } = string.Empty;
        public bool EstResponsable { get; set; }
        public bool EstActif { get; set; }
        public DateTime DateNomination { get; set; }
        public DateTime? DateDemission { get; set; }
        public string? Commentaires { get; set; }
        public Guid MandatId { get; set; }
        public int MandatAnnee { get; set; }
        public string? MandatDescription { get; set; }
    }

    public class AffecterMembreCommissionRequest
    {
        [Required]
        public string MembreId { get; set; } = string.Empty;

        [Required]
        public Guid MandatId { get; set; }

        public bool EstResponsable { get; set; } = false;

        [DataType(DataType.DateTime)]
        public DateTime? DateNomination { get; set; }

        [MaxLength(500)]
        public string? Commentaires { get; set; }
    }

    public class ModifierAffectationMembreRequest
    {
        public bool? EstResponsable { get; set; }
        public bool? EstActif { get; set; }

        [MaxLength(500)]
        public string? Commentaires { get; set; }
    }

    public class MembreDisponibleDto
    {
        public string Id { get; set; } = string.Empty;
        public string NomComplet { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Department { get; set; }
    }

    // DTOs pour les membres du club
    public class MembreClubDto
    {
        public string Id { get; set; } = string.Empty;
        public string NomComplet { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Department { get; set; }
        public bool IsActive { get; set; }
    }

    public class MembreClubDetailDto : MembreClubDto
    {
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class MembreClubCompletDto : MembreClubDetailDto
    {
        public List<CommissionMembreDto> CommissionsActuelles { get; set; } = new List<CommissionMembreDto>();
    }

    public class CommissionMembreDto
    {
        public Guid CommissionId { get; set; }
        public string NomCommission { get; set; } = string.Empty;
        public bool EstResponsable { get; set; }
        public DateTime DateNomination { get; set; }
    }

    // DTOs pour la réponse des fonctions et commissions
    public class MembreFonctionCommissionDto
    {
        public string MembreId { get; set; } = string.Empty;
        public string NomCompletMembre { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public MandatInfoDto MandatActuel { get; set; } = new();
        public FonctionMembreInfoDto? Fonction { get; set; }
        public List<CommissionMembreInfoDto> Commissions { get; set; } = new();
    }

    public class MandatInfoDto
    {
        public Guid MandatId { get; set; }
        public int Annee { get; set; }
        public string? Description { get; set; }
    }

    public class FonctionMembreInfoDto
    {
        public Guid FonctionId { get; set; }
        public string NomFonction { get; set; } = string.Empty;
    }

    public class CommissionMembreInfoDto
    {
        public Guid CommissionId { get; set; }
        public string NomCommission { get; set; } = string.Empty;
        public bool EstResponsable { get; set; }
        public DateTime DateNomination { get; set; }
    }
}