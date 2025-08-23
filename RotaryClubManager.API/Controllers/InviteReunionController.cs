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
    [Route("api/clubs/{clubId}/reunions/{reunionId}/invites")]
    [ApiController]
    [Authorize]
    public class InviteReunionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantService _tenantService;
        private readonly ILogger<InviteReunionController> _logger;

        public InviteReunionController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ITenantService tenantService,
            ILogger<InviteReunionController> logger)
        {
            _context = context;
            _userManager = userManager;
            _tenantService = tenantService;
            _logger = logger;
        }

        // GET: api/clubs/{clubId}/reunions/{reunionId}/invites
        // Récupérer tous les invités d'une réunion
        [HttpGet]
        public async Task<IActionResult> GetInvitesReunion(Guid clubId, Guid reunionId)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (reunionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la réunion est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que la réunion existe
                var reunion = await _context.Reunions
                    .Include(r => r.TypeReunion)
                    .FirstOrDefaultAsync(r => r.Id == reunionId);

                if (reunion == null)
                {
                    return NotFound("Réunion non trouvée");
                }

                // Récupérer tous les invités de cette réunion
                var invites = await _context.InvitesReunion
                    .Where(inv => inv.ReunionId == reunionId)
                    .OrderBy(inv => inv.Nom)
                    .ThenBy(inv => inv.Prenom)
                    .Select(inv => new InviteReunionDetailDto
                    {
                        Id = inv.Id,
                        Nom = inv.Nom,
                        Prenom = inv.Prenom,
                        NomComplet = $"{inv.Prenom} {inv.Nom}",
                        Email = inv.Email,
                        Telephone = inv.Telephone,
                        Organisation = inv.Organisation,
                        ReunionId = inv.ReunionId
                    })
                    .ToListAsync();

                var response = new
                {
                    Reunion = new
                    {
                        Id = reunion.Id,
                        Date = reunion.Date,
                        TypeReunionLibelle = reunion.TypeReunion.Libelle
                    },
                    Invites = invites,
                    Statistiques = new
                    {
                        TotalInvites = invites.Count,
                        AvecEmail = invites.Count(i => !string.IsNullOrEmpty(i.Email)),
                        AvecTelephone = invites.Count(i => !string.IsNullOrEmpty(i.Telephone)),
                        AvecOrganisation = invites.Count(i => !string.IsNullOrEmpty(i.Organisation)),
                        OrganisationsUniques = invites.Where(i => !string.IsNullOrEmpty(i.Organisation))
                                                   .Select(i => i.Organisation)
                                                   .Distinct()
                                                   .Count()
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des invités de la réunion {ReunionId} du club {ClubId}",
                    reunionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des invités");
            }
        }

        // GET: api/clubs/{clubId}/reunions/{reunionId}/invites/{inviteId}
        // Récupérer un invité spécifique
        [HttpGet("{inviteId:guid}")]
        public async Task<IActionResult> GetInviteReunion(Guid clubId, Guid reunionId, Guid inviteId)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (reunionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la réunion est invalide");
                }

                if (inviteId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'invité est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                // Récupérer l'invité avec les détails de la réunion
                var invite = await _context.InvitesReunion
                    .Include(inv => inv.Reunion)
                        .ThenInclude(r => r.TypeReunion)
                    .FirstOrDefaultAsync(inv => inv.Id == inviteId &&
                                              inv.ReunionId == reunionId);

                if (invite == null)
                {
                    return NotFound("Invité non trouvé");
                }

                var response = new InviteReunionCompletDto
                {
                    Id = invite.Id,
                    Nom = invite.Nom,
                    Prenom = invite.Prenom,
                    NomComplet = $"{invite.Prenom} {invite.Nom}",
                    Email = invite.Email,
                    Telephone = invite.Telephone,
                    Organisation = invite.Organisation,
                    ReunionId = invite.ReunionId,
                    Reunion = new ReunionInfoDto
                    {
                        Id = invite.Reunion.Id,
                        Date = invite.Reunion.Date,
                        TypeReunionLibelle = invite.Reunion.TypeReunion.Libelle
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'invité {InviteId} de la réunion {ReunionId}",
                    inviteId, reunionId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de l'invité");
            }
        }

        // POST: api/clubs/{clubId}/reunions/{reunionId}/invites
        // Ajouter un nouvel invité à une réunion
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> AjouterInvite(
            Guid clubId,
            Guid reunionId,
            [FromBody] AjouterInviteRequest request)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (reunionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la réunion est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                // Définir le tenant actuel
                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que la réunion existe
                var reunion = await _context.Reunions
                    .Include(r => r.TypeReunion)
                    .FirstOrDefaultAsync(r => r.Id == reunionId);

                if (reunion == null)
                {
                    return NotFound("Réunion non trouvée");
                }

                // Vérifier qu'un invité avec le même nom/prénom n'existe pas déjà
                var inviteExistant = await _context.InvitesReunion
                    .AnyAsync(inv => inv.ReunionId == reunionId &&
                                   inv.Nom.ToLower().Trim() == request.Nom.ToLower().Trim() &&
                                   inv.Prenom.ToLower().Trim() == request.Prenom.ToLower().Trim());

                if (inviteExistant)
                {
                    return BadRequest($"Un invité avec le nom '{request.Prenom} {request.Nom}' existe déjà pour cette réunion");
                }

                // Vérifier l'unicité de l'email s'il est fourni
                if (!string.IsNullOrEmpty(request.Email))
                {
                    var emailExistant = await _context.InvitesReunion
                        .AnyAsync(inv => inv.ReunionId == reunionId &&
                                       !string.IsNullOrEmpty(inv.Email) &&
                                       inv.Email.ToLower() == request.Email.ToLower());

                    if (emailExistant)
                    {
                        return BadRequest($"Un invité avec l'email '{request.Email}' existe déjà pour cette réunion");
                    }
                }

                // Créer le nouvel invité
                var invite = new InviteReunion
                {
                    Id = Guid.NewGuid(),
                    Nom = request.Nom.Trim(),
                    Prenom = request.Prenom.Trim(),
                    Email = string.IsNullOrEmpty(request.Email) ? null : request.Email.Trim().ToLower(),
                    Telephone = string.IsNullOrEmpty(request.Telephone) ? null : request.Telephone.Trim(),
                    Organisation = string.IsNullOrEmpty(request.Organisation) ? null : request.Organisation.Trim(),
                    ReunionId = reunionId
                };

                _context.InvitesReunion.Add(invite);
                await _context.SaveChangesAsync();

                var response = new InviteReunionDetailDto
                {
                    Id = invite.Id,
                    Nom = invite.Nom,
                    Prenom = invite.Prenom,
                    NomComplet = $"{invite.Prenom} {invite.Nom}",
                    Email = invite.Email,
                    Telephone = invite.Telephone,
                    Organisation = invite.Organisation,
                    ReunionId = invite.ReunionId
                };

                _logger.LogInformation(
                    "Invité ajouté avec succès : {InviteNom} à la réunion {TypeReunion} du {Date} (Invite ID: {InviteId})",
                    $"{invite.Prenom} {invite.Nom}",
                    reunion.TypeReunion.Libelle,
                    reunion.Date,
                    invite.Id
                );

                return CreatedAtAction(
                    nameof(GetInviteReunion),
                    new { clubId, reunionId, inviteId = invite.Id },
                    response
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout de l'invité à la réunion {ReunionId} du club {ClubId}",
                    reunionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de l'ajout de l'invité");
            }
        }

        // PUT: api/clubs/{clubId}/reunions/{reunionId}/invites/{inviteId}
        // Modifier un invité existant
        [HttpPut("{inviteId:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> ModifierInvite(
            Guid clubId,
            Guid reunionId,
            Guid inviteId,
            [FromBody] ModifierInviteRequest request)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (reunionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la réunion est invalide");
                }

                if (inviteId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'invité est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                // Récupérer l'invité
                var invite = await _context.InvitesReunion
                    .Include(inv => inv.Reunion)
                        .ThenInclude(r => r.TypeReunion)
                    .FirstOrDefaultAsync(inv => inv.Id == inviteId &&
                                              inv.ReunionId == reunionId);

                if (invite == null)
                {
                    return NotFound("Invité non trouvé");
                }

                // Vérifier l'unicité du nom/prénom (si changé)
                if (!string.Equals($"{invite.Prenom} {invite.Nom}", $"{request.Prenom} {request.Nom}", StringComparison.OrdinalIgnoreCase))
                {
                    var nomExistant = await _context.InvitesReunion
                        .AnyAsync(inv => inv.ReunionId == reunionId &&
                                       inv.Nom.ToLower().Trim() == request.Nom.ToLower().Trim() &&
                                       inv.Prenom.ToLower().Trim() == request.Prenom.ToLower().Trim() &&
                                       inv.Id != inviteId);

                    if (nomExistant)
                    {
                        return BadRequest($"Un autre invité avec le nom '{request.Prenom} {request.Nom}' existe déjà pour cette réunion");
                    }
                }

                // Vérifier l'unicité de l'email (si changé et fourni)
                if (!string.IsNullOrEmpty(request.Email) &&
                    !string.Equals(invite.Email, request.Email, StringComparison.OrdinalIgnoreCase))
                {
                    var emailExistant = await _context.InvitesReunion
                        .AnyAsync(inv => inv.ReunionId == reunionId &&
                                       !string.IsNullOrEmpty(inv.Email) &&
                                       inv.Email.ToLower() == request.Email.ToLower() &&
                                       inv.Id != inviteId);

                    if (emailExistant)
                    {
                        return BadRequest($"Un autre invité avec l'email '{request.Email}' existe déjà pour cette réunion");
                    }
                }

                // Mettre à jour les propriétés
                var ancienNom = $"{invite.Prenom} {invite.Nom}";

                invite.Nom = request.Nom.Trim();
                invite.Prenom = request.Prenom.Trim();
                invite.Email = string.IsNullOrEmpty(request.Email) ? null : request.Email.Trim().ToLower();
                invite.Telephone = string.IsNullOrEmpty(request.Telephone) ? null : request.Telephone.Trim();
                invite.Organisation = string.IsNullOrEmpty(request.Organisation) ? null : request.Organisation.Trim();

                _context.Entry(invite).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Invité modifié avec succès (ID: {InviteId}) pour la réunion {TypeReunion} du {Date} : '{AncienNom}' -> '{NouveauNom}'",
                    inviteId,
                    invite.Reunion.TypeReunion.Libelle,
                    invite.Reunion.Date,
                    ancienNom,
                    $"{invite.Prenom} {invite.Nom}"
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification de l'invité {InviteId} de la réunion {ReunionId}",
                    inviteId, reunionId);
                return StatusCode(500, "Une erreur est survenue lors de la modification de l'invité");
            }
        }

        // DELETE: api/clubs/{clubId}/reunions/{reunionId}/invites/{inviteId}
        // Supprimer un invité
        [HttpDelete("{inviteId:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> SupprimerInvite(Guid clubId, Guid reunionId, Guid inviteId)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (reunionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la réunion est invalide");
                }

                if (inviteId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'invité est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                // Récupérer l'invité avec les détails
                var invite = await _context.InvitesReunion
                    .Include(inv => inv.Reunion)
                        .ThenInclude(r => r.TypeReunion)
                    .FirstOrDefaultAsync(inv => inv.Id == inviteId &&
                                              inv.ReunionId == reunionId);

                if (invite == null)
                {
                    return NotFound("Invité non trouvé");
                }

                // Sauvegarder les informations pour le log
                var inviteNom = $"{invite.Prenom} {invite.Nom}";
                var infoReunion = $"{invite.Reunion.TypeReunion.Libelle} du {invite.Reunion.Date:dd/MM/yyyy HH:mm}";

                // Supprimer l'invité
                _context.InvitesReunion.Remove(invite);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Invité supprimé avec succès : {InviteNom} retiré de la réunion {InfoReunion} (Invite ID: {InviteId})",
                    inviteNom,
                    infoReunion,
                    inviteId
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'invité {InviteId} de la réunion {ReunionId}",
                    inviteId, reunionId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de l'invité");
            }
        }

        // POST: api/clubs/{clubId}/reunions/{reunionId}/invites/batch
        // Ajouter plusieurs invités en une seule opération
        [HttpPost("batch")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> AjouterInvitesBatch(
            Guid clubId,
            Guid reunionId,
            [FromBody] AjouterInvitesBatchRequest request)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (reunionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la réunion est invalide");
                }

                if (request.Invites == null || !request.Invites.Any())
                {
                    return BadRequest("Au moins un invité est requis");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que la réunion existe
                var reunion = await _context.Reunions
                    .Include(r => r.TypeReunion)
                    .FirstOrDefaultAsync(r => r.Id == reunionId);

                if (reunion == null)
                {
                    return NotFound("Réunion non trouvée");
                }

                // Récupérer les invités existants pour vérifier les doublons
                var invitesExistants = await _context.InvitesReunion
                    .Where(inv => inv.ReunionId == reunionId)
                    .Select(inv => new {
                        NomComplet = $"{inv.Prenom.ToLower().Trim()} {inv.Nom.ToLower().Trim()}",
                        Email = inv.Email != null ? inv.Email.ToLower() : null
                    })
                    .ToListAsync();

                var invitesTraites = new List<InviteReunion>();
                var invitesIgnores = new List<string>();

                foreach (var inviteRequest in request.Invites)
                {
                    // Nettoyer les données
                    var nomCompletCandidat = $"{inviteRequest.Prenom.ToLower().Trim()} {inviteRequest.Nom.ToLower().Trim()}";
                    var emailCandidat = string.IsNullOrEmpty(inviteRequest.Email) ? null : inviteRequest.Email.ToLower().Trim();

                    // Vérifier les doublons
                    var doublonNom = invitesExistants.Any(ie => ie.NomComplet == nomCompletCandidat);
                    var doublonEmail = !string.IsNullOrEmpty(emailCandidat) &&
                                     invitesExistants.Any(ie => ie.Email == emailCandidat);

                    if (doublonNom || doublonEmail)
                    {
                        invitesIgnores.Add($"{inviteRequest.Prenom} {inviteRequest.Nom}");
                        continue;
                    }

                    // Créer l'invité
                    var invite = new InviteReunion
                    {
                        Id = Guid.NewGuid(),
                        Nom = inviteRequest.Nom.Trim(),
                        Prenom = inviteRequest.Prenom.Trim(),
                        Email = emailCandidat,
                        Telephone = string.IsNullOrEmpty(inviteRequest.Telephone) ? null : inviteRequest.Telephone.Trim(),
                        Organisation = string.IsNullOrEmpty(inviteRequest.Organisation) ? null : inviteRequest.Organisation.Trim(),
                        ReunionId = reunionId
                    };

                    invitesTraites.Add(invite);

                    // Ajouter aux existants pour éviter les doublons dans le lot
                    invitesExistants.Add(new
                    {
                        NomComplet = nomCompletCandidat,
                        Email = emailCandidat
                    });
                }

                // Ajouter tous les nouveaux invités
                _context.InvitesReunion.AddRange(invitesTraites);
                await _context.SaveChangesAsync();

                var response = new
                {
                    InvitesAjoutes = invitesTraites.Select(i => new InviteReunionDetailDto
                    {
                        Id = i.Id,
                        Nom = i.Nom,
                        Prenom = i.Prenom,
                        NomComplet = $"{i.Prenom} {i.Nom}",
                        Email = i.Email,
                        Telephone = i.Telephone,
                        Organisation = i.Organisation,
                        ReunionId = i.ReunionId
                    }).ToList(),
                    Statistiques = new
                    {
                        InvitesDemandesTraitement = request.Invites.Count(),
                        InvitesAjoutes = invitesTraites.Count,
                        InvitesIgnores = invitesIgnores.Count,
                        InvitesIgnoresNoms = invitesIgnores,
                        TotalInvitesReunion = await _context.InvitesReunion.CountAsync(inv => inv.ReunionId == reunionId)
                    }
                };

                _logger.LogInformation(
                    "Ajout en lot de {NombreAjoutes} invités à la réunion {TypeReunion} du {Date} (Réunion ID: {ReunionId})",
                    invitesTraites.Count,
                    reunion.TypeReunion.Libelle,
                    reunion.Date,
                    reunionId
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout en lot des invités à la réunion {ReunionId} du club {ClubId}",
                    reunionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de l'ajout des invités");
            }
        }

        // GET: api/clubs/{clubId}/reunions/{reunionId}/invites/organisations
        // Récupérer la liste des organisations représentées
        [HttpGet("organisations")]
        public async Task<IActionResult> GetOrganisationsInvites(Guid clubId, Guid reunionId)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (reunionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la réunion est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                _tenantService.SetCurrentTenantId(clubId);

                // Récupérer les organisations avec le nombre d'invités par organisation
                var organisations = await _context.InvitesReunion
                    .Where(inv => inv.ReunionId == reunionId && !string.IsNullOrEmpty(inv.Organisation))
                    .GroupBy(inv => inv.Organisation)
                    .Select(g => new
                    {
                        Organisation = g.Key,
                        NombreInvites = g.Count(),
                        Invites = g.Select(inv => new
                        {
                            Id = inv.Id,
                            NomComplet = $"{inv.Prenom} {inv.Nom}",
                            Email = inv.Email,
                            Telephone = inv.Telephone
                        }).ToList()
                    })
                    .OrderByDescending(o => o.NombreInvites)
                    .ThenBy(o => o.Organisation)
                    .ToListAsync();

                var invitesSansOrganisation = await _context.InvitesReunion
                    .Where(inv => inv.ReunionId == reunionId && string.IsNullOrEmpty(inv.Organisation))
                    .CountAsync();

                var response = new
                {
                    Organisations = organisations,
                    Statistiques = new
                    {
                        NombreOrganisations = organisations.Count,
                        TotalInvitesAvecOrganisation = organisations.Sum(o => o.NombreInvites),
                        InvitesSansOrganisation = invitesSansOrganisation,
                        OrganisationLaPlusRepresentee = organisations.FirstOrDefault()?.Organisation
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des organisations pour la réunion {ReunionId}",
                    reunionId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des organisations");
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

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return false;

            return User.IsInRole("President") || User.IsInRole("Secretary");
        }
    }

    // DTOs pour les invités de réunion
    public class InviteReunionDetailDto
    {
        public Guid Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string NomComplet { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Telephone { get; set; }
        public string? Organisation { get; set; }
        public Guid ReunionId { get; set; }
    }

    public class InviteReunionCompletDto : InviteReunionDetailDto
    {
        public ReunionInfoDto Reunion { get; set; } = null!;
    }

    public class ReunionInfoDto
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string TypeReunionLibelle { get; set; } = string.Empty;
    }

    public class AjouterInviteRequest
    {
        [Required(ErrorMessage = "Le nom est obligatoire")]
        [MaxLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        [MinLength(2, ErrorMessage = "Le nom doit contenir au moins 2 caractères")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est obligatoire")]
        [MaxLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
        [MinLength(2, ErrorMessage = "Le prénom doit contenir au moins 2 caractères")]
        public string Prenom { get; set; } = string.Empty;

        [MaxLength(255, ErrorMessage = "L'email ne peut pas dépasser 255 caractères")]
        [EmailAddress(ErrorMessage = "L'email n'est pas dans un format valide")]
        public string? Email { get; set; }

        [MaxLength(20, ErrorMessage = "Le téléphone ne peut pas dépasser 20 caractères")]
        [Phone(ErrorMessage = "Le numéro de téléphone n'est pas dans un format valide")]
        public string? Telephone { get; set; }

        [MaxLength(200, ErrorMessage = "L'organisation ne peut pas dépasser 200 caractères")]
        public string? Organisation { get; set; }
    }

    public class ModifierInviteRequest
    {
        [Required(ErrorMessage = "Le nom est obligatoire")]
        [MaxLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        [MinLength(2, ErrorMessage = "Le nom doit contenir au moins 2 caractères")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est obligatoire")]
        [MaxLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
        [MinLength(2, ErrorMessage = "Le prénom doit contenir au moins 2 caractères")]
        public string Prenom { get; set; } = string.Empty;

        [MaxLength(255, ErrorMessage = "L'email ne peut pas dépasser 255 caractères")]
        [EmailAddress(ErrorMessage = "L'email n'est pas dans un format valide")]
        public string? Email { get; set; }

        [MaxLength(20, ErrorMessage = "Le téléphone ne peut pas dépasser 20 caractères")]
        [Phone(ErrorMessage = "Le numéro de téléphone n'est pas dans un format valide")]
        public string? Telephone { get; set; }

        [MaxLength(200, ErrorMessage = "L'organisation ne peut pas dépasser 200 caractères")]
        public string? Organisation { get; set; }
    }

    public class AjouterInvitesBatchRequest
    {
        [Required(ErrorMessage = "Au moins un invité est requis")]
        public IEnumerable<AjouterInviteRequest> Invites { get; set; } = new List<AjouterInviteRequest>();
    }
}