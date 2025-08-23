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
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

// Alias pour éviter le conflit de noms
using WordDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using static RotaryClubManager.API.Controllers.CalendrierEmailController;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/clubs/{clubId}/reunions")]
    [ApiController]
    [Authorize]
    public class ReunionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantService _tenantService;
        private readonly ILogger<ReunionController> _logger;

        public ReunionController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ITenantService tenantService,
            ILogger<ReunionController> logger)
        {
            _context = context;
            _userManager = userManager;
            _tenantService = tenantService;
            _logger = logger;
        }

        [HttpGet("calendrier/{mois:int}")]
        public async Task<IActionResult> GetCalendrier(Guid clubId, int mois)
        {
            try
            {
                if (clubId == Guid.Empty)
                    return BadRequest("L'identifiant du club est invalide.");

                if (!await CanAccessClub(clubId))
                    return Forbid("Accès non autorisé à ce club.");

                _tenantService.SetCurrentTenantId(clubId);

                // 1. Récupérer les réunions du mois
                var reunions = await _context.Reunions
                    .Where(r => r.ClubId == clubId && r.Date.Month == mois)
                    .Include(r => r.TypeReunion)
                    .Select(r => new ItemCalendrierDto
                    {
                        Libelle = r.TypeReunion.Libelle,
                        Date = r.DateTimeComplete // Utilisation de la propriété calculée
                    })
                    .ToListAsync();

                // 2. Récupérer les événements du mois
                var evenements = await _context.Evenements
                    .Where(e => e.ClubId == clubId && e.Date.Month == mois)
                    .Select(e => new ItemCalendrierDto
                    {
                        Libelle = e.Libelle,
                        Date = e.Date
                    })
                    .ToListAsync();

                // 3. Récupérer les anniversaires des membres du club pour le mois
                // Note: L'entité 'UserClub' est nécessaire pour filtrer les membres par club.
                var anniversaires = await _context.UserClubs
                    .Where(uc => uc.ClubId == clubId && uc.User.DateAnniversaire != default && uc.User.DateAnniversaire.Month == mois)
                    .Select(uc => new ItemCalendrierDto
                    {
                        Libelle = $"Anniversaire de {uc.User.FirstName} {uc.User.LastName}",
                        Date = new DateTime(DateTime.Now.Year, uc.User.DateAnniversaire.Month, uc.User.DateAnniversaire.Day)
                    })
                    .ToListAsync();

                // 4. Agréger les listes et les trier par date
                var calendrier = reunions
                    .Concat(evenements)
                    .Concat(anniversaires)
                    .OrderBy(item => item.Date)
                    .ToList();

                return Ok(calendrier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du calendrier pour le club {ClubId} et le mois {Mois}", clubId, mois);
                return StatusCode(500, "Une erreur est survenue.");
            }
        }

        // Nouvel endpoint pour le calendrier accessible directement via /api/clubs/{clubId}/calendrier/{mois}
        [HttpGet("/api/clubs/{clubId}/calendrier/{mois:int}")]
        public async Task<IActionResult> GetCalendrierDirect(Guid clubId, int mois)
        {
            try
            {
                if (clubId == Guid.Empty)
                    return BadRequest("L'identifiant du club est invalide.");

                if (!await CanAccessClub(clubId))
                    return Forbid("Accès non autorisé à ce club.");

                _tenantService.SetCurrentTenantId(clubId);

                // 1. Récupérer les réunions du mois
                var reunions = await _context.Reunions
                    .Where(r => r.ClubId == clubId && r.Date.Month == mois)
                    .Include(r => r.TypeReunion)
                    .Select(r => new ItemCalendrierDto
                    {
                        Libelle = r.TypeReunion.Libelle,
                        Date = r.DateTimeComplete // Utilisation de la propriété calculée
                    })
                    .ToListAsync();

                // 2. Récupérer les événements du mois
                var evenements = await _context.Evenements
                    .Where(e => e.ClubId == clubId && e.Date.Month == mois)
                    .Select(e => new ItemCalendrierDto
                    {
                        Libelle = e.Libelle,
                        Date = e.Date
                    })
                    .ToListAsync();

                // 3. Récupérer les anniversaires des membres du club pour le mois
                var anniversaires = await _context.UserClubs
                    .Where(uc => uc.ClubId == clubId && uc.User.DateAnniversaire != default && uc.User.DateAnniversaire.Month == mois)
                    .Select(uc => new ItemCalendrierDto
                    {
                        Libelle = $"Anniversaire de {uc.User.FirstName} {uc.User.LastName}",
                        Date = new DateTime(DateTime.Now.Year, uc.User.DateAnniversaire.Month, uc.User.DateAnniversaire.Day)
                    })
                    .ToListAsync();

                // 4. Agréger les listes et les trier par date
                var calendrier = reunions
                    .Concat(evenements)
                    .Concat(anniversaires)
                    .OrderBy(item => item.Date)
                    .ToList();

                return Ok(calendrier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du calendrier pour le club {ClubId} et le mois {Mois}", clubId, mois);
                return StatusCode(500, "Une erreur est survenue.");
            }
        }


        // GET: api/clubs/{clubId}/reunions
        [HttpGet]
        public async Task<IActionResult> GetReunions(
            Guid clubId,
            [FromQuery] Guid? typeReunionId = null,
            [FromQuery] DateTime? dateDebut = null,
            [FromQuery] DateTime? dateFin = null)
        {
            try
            {
                if (clubId == Guid.Empty)
                    return BadRequest("L'identifiant du club est invalide");

                if (!await CanAccessClub(clubId))
                    return Forbid("Accès non autorisé à ce club");

                _tenantService.SetCurrentTenantId(clubId);

                var query = _context.Reunions
                    .Include(r => r.TypeReunion)
                    .Include(r => r.Club)
                    .Where(r => r.ClubId == clubId);

                // Filtres optionnels
                if (typeReunionId.HasValue)
                {
                    query = query.Where(r => r.TypeReunionId == typeReunionId.Value);
                }

                if (dateDebut.HasValue)
                {
                    query = query.Where(r => r.Date >= dateDebut.Value.Date);
                }

                if (dateFin.HasValue)
                {
                    query = query.Where(r => r.Date <= dateFin.Value.Date);
                }

                var reunions = await query
                    .OrderByDescending(r => r.Date)
                    .ThenByDescending(r => r.Heure)
                    .Select(r => new ReunionDto
                    {
                        Id = r.Id,
                        Date = r.Date,
                        Heure = r.Heure,
                        ClubId = r.ClubId,
                        ClubNom = r.Club.Name,
                        TypeReunionId = r.TypeReunionId,
                        TypeReunionLibelle = r.TypeReunion.Libelle,
                        NombreOrdresDuJour = r.OrdresDuJour.Count(),
                        NombrePresences = r.ListesPresence.Count(),
                        NombreInvites = r.Invites.Count(),
                        NombreDocuments = r.Documents.Count()
                    })
                    .ToListAsync();

                return Ok(reunions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réunions pour le club {ClubId}", clubId);
                return StatusCode(500, "Une erreur est survenue");
            }
        }

        // GET: api/clubs/{clubId}/reunions/{reunionId}
        [HttpGet("{reunionId:guid}")]
        public async Task<IActionResult> GetReunion(Guid clubId, Guid reunionId)
        {
            try
            {
                if (clubId == Guid.Empty)
                    return BadRequest("L'identifiant du club est invalide");

                if (reunionId == Guid.Empty)
                    return BadRequest("L'identifiant de la réunion est invalide");

                if (!await CanAccessClub(clubId))
                    return Forbid("Accès non autorisé à ce club");

                _tenantService.SetCurrentTenantId(clubId);

                var reunion = await _context.Reunions
                    .Include(r => r.TypeReunion)
                    .Include(r => r.Club)
                    .Include(r => r.OrdresDuJour)
                    .Include(r => r.ListesPresence)
                        .ThenInclude(lp => lp.Membre)
                    .Include(r => r.Invites)
                    .Include(r => r.Documents)
                    .FirstOrDefaultAsync(r => r.Id == reunionId && r.ClubId == clubId);

                if (reunion == null)
                    return NotFound($"Réunion avec l'ID {reunionId} non trouvée dans le club {clubId}");

                var response = new ReunionDetailDto
                {
                    Id = reunion.Id,
                    Date = reunion.Date,
                    Heure = reunion.Heure,
                    ClubId = reunion.ClubId,
                    ClubNom = reunion.Club.Name,
                    TypeReunionId = reunion.TypeReunionId,
                    TypeReunionLibelle = reunion.TypeReunion.Libelle,
                    Divers = "", // Sera ajouté plus tard si nécessaire
                    OrdresDuJour = reunion.OrdresDuJour.Select(odj => new OrdreDuJourDto
                    {
                        Id = odj.Id,
                        Description = odj.Description
                    }).ToList(),
                    Presences = reunion.ListesPresence.Select(lp => new PresenceDto
                    {
                        Id = lp.Id,
                        MembreId = lp.MembreId,
                        NomCompletMembre = $"{lp.Membre.FirstName} {lp.Membre.LastName}",
                        EmailMembre = lp.Membre.Email
                    }).ToList(),
                    Invites = reunion.Invites.Select(inv => new InviteDto
                    {
                        Id = inv.Id,
                        Nom = inv.Nom,
                        Prenom = inv.Prenom,
                        Email = inv.Email,
                        Telephone = inv.Telephone,
                        Organisation = inv.Organisation
                    }).ToList(),
                    Documents = reunion.Documents.Select(doc => new DocumentDto
                    {
                        Id = doc.Id,
                        Libelle = doc.Libelle,
                        TailleEnOctets = doc.Document?.Length ?? 0
                    }).ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la réunion {ReunionId} du club {ClubId}", reunionId, clubId);
                return StatusCode(500, "Une erreur est survenue");
            }
        }

        // POST: api/clubs/{clubId}/reunions
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> CreerReunion(Guid clubId, [FromBody] CreerReunionRequest request)
        {
            try
            {
                if (clubId == Guid.Empty)
                    return BadRequest("L'identifiant du club est invalide");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (!await CanManageClub(clubId))
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");

                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que le club existe
                var club = await _context.Clubs.FindAsync(clubId);
                if (club == null)
                    return NotFound("Club non trouvé");

                // Vérifier que le type de réunion existe
                var typeReunion = await _context.TypesReunion
                    .FirstOrDefaultAsync(tr => tr.Id == request.TypeReunionId);

                if (typeReunion == null)
                    return BadRequest("Type de réunion non trouvé");

                // Vérifier s'il y a déjà une réunion à la même date, heure et club
                var reunionExistante = await _context.Reunions
                    .AnyAsync(r => r.ClubId == clubId &&
                                 r.Date == request.Date.Date &&
                                 r.Heure == request.Heure &&
                                 r.TypeReunionId == request.TypeReunionId);

                if (reunionExistante)
                    return BadRequest($"Une réunion de type '{typeReunion.Libelle}' existe déjà le {request.Date:dd/MM/yyyy} à {request.Heure:hh\\:mm} dans ce club");

                // Créer la nouvelle réunion
                var reunion = new Reunion
                {
                    Id = Guid.NewGuid(),
                    Date = request.Date.Date,
                    Heure = request.Heure,
                    ClubId = clubId,
                    TypeReunionId = request.TypeReunionId
                };

                _context.Reunions.Add(reunion);
                await _context.SaveChangesAsync();

                var response = new ReunionDto
                {
                    Id = reunion.Id,
                    Date = reunion.Date,
                    Heure = reunion.Heure,
                    ClubId = reunion.ClubId,
                    ClubNom = club.Name,
                    TypeReunionId = reunion.TypeReunionId,
                    TypeReunionLibelle = typeReunion.Libelle,
                    NombreOrdresDuJour = 0,
                    NombrePresences = 0,
                    NombreInvites = 0,
                    NombreDocuments = 0
                };

                _logger.LogInformation("Réunion créée : {TypeReunion} du {Date} à {Heure} pour le club {ClubId} (ID: {ReunionId})",
                    typeReunion.Libelle, request.Date.ToString("dd/MM/yyyy"), request.Heure.ToString(@"hh\:mm"), clubId, reunion.Id);

                return CreatedAtAction(nameof(GetReunion), new { clubId, reunionId = reunion.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la réunion pour le club {ClubId}", clubId);
                return StatusCode(500, "Une erreur est survenue lors de la création de la réunion");
            }
        }

        // POST: api/clubs/{clubId}/reunions/complete
        [HttpPost("complete")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> CreerReunionComplete(Guid clubId, [FromBody] CreerReunionCompleteRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (clubId == Guid.Empty)
                    return BadRequest("L'identifiant du club est invalide");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (!await CanManageClub(clubId))
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");

                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que le club existe
                var club = await _context.Clubs.FindAsync(clubId);
                if (club == null)
                    return NotFound("Club non trouvé");

                // Vérifier que le type de réunion existe
                var typeReunion = await _context.TypesReunion
                    .FirstOrDefaultAsync(tr => tr.Id == request.TypeReunionId);

                if (typeReunion == null)
                    return BadRequest("Type de réunion non trouvé");

                // Vérifier s'il y a déjà une réunion à la même date, heure et club
                var reunionExistante = await _context.Reunions
                    .AnyAsync(r => r.ClubId == clubId &&
                                 r.Date == request.Date.Date &&
                                 r.Heure == request.Heure &&
                                 r.TypeReunionId == request.TypeReunionId);

                if (reunionExistante)
                    return BadRequest($"Une réunion de type '{typeReunion.Libelle}' existe déjà le {request.Date:dd/MM/yyyy} à {request.Heure:hh\\:mm} dans ce club");

                // Créer la nouvelle réunion
                var reunion = new Reunion
                {
                    Id = Guid.NewGuid(),
                    Date = request.Date.Date,
                    Heure = request.Heure,
                    ClubId = clubId,
                    TypeReunionId = request.TypeReunionId
                };

                _context.Reunions.Add(reunion);
                await _context.SaveChangesAsync();

                // Créer les ordres du jour si fournis
                var ordresDuJourCrees = new List<OrdreDuJour>();

                if (request.OrdresDuJour != null && request.OrdresDuJour.Any())
                {
                    var descriptionsValides = request.OrdresDuJour
                        .Where(desc => !string.IsNullOrWhiteSpace(desc))
                        .Select(desc => desc.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    foreach (var description in descriptionsValides)
                    {
                        var ordreDuJour = new OrdreDuJour
                        {
                            Id = Guid.NewGuid(),
                            Description = description,
                            ReunionId = reunion.Id
                        };

                        _context.OrdresDuJour.Add(ordreDuJour);
                        ordresDuJourCrees.Add(ordreDuJour);
                    }

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                var response = new ReunionCompleteDto
                {
                    Id = reunion.Id,
                    Date = reunion.Date,
                    Heure = reunion.Heure,
                    ClubId = reunion.ClubId,
                    ClubNom = club.Name,
                    TypeReunionId = reunion.TypeReunionId,
                    TypeReunionLibelle = typeReunion.Libelle,
                    OrdresDuJour = ordresDuJourCrees.Select(odj => new OrdreDuJourDto
                    {
                        Id = odj.Id,
                        Description = odj.Description
                    }).ToList(),
                    NombreOrdresDuJour = ordresDuJourCrees.Count,
                    NombrePresences = 0,
                    NombreInvites = 0,
                    NombreDocuments = 0
                };

                _logger.LogInformation("Réunion complète créée : {TypeReunion} du {Date} à {Heure} pour le club {ClubId} avec {NombreOrdres} ordres du jour (ID: {ReunionId})",
                    typeReunion.Libelle, request.Date.ToString("dd/MM/yyyy"), request.Heure.ToString(@"hh\:mm"),
                    clubId, ordresDuJourCrees.Count, reunion.Id);

                return CreatedAtAction(nameof(GetReunion), new { clubId, reunionId = reunion.Id }, response);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erreur lors de la création de la réunion complète pour le club {ClubId}", clubId);
                return StatusCode(500, "Une erreur est survenue lors de la création de la réunion complète");
            }
        }

        // PUT: api/clubs/{clubId}/reunions/{reunionId}
        [HttpPut("{reunionId:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> ModifierReunion(Guid clubId, Guid reunionId, [FromBody] ModifierReunionRequest request)
        {
            try
            {
                if (clubId == Guid.Empty)
                    return BadRequest("L'identifiant du club est invalide");

                if (reunionId == Guid.Empty)
                    return BadRequest("L'identifiant de la réunion est invalide");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (!await CanManageClub(clubId))
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");

                _tenantService.SetCurrentTenantId(clubId);

                var reunion = await _context.Reunions
                    .Include(r => r.TypeReunion)
                    .Include(r => r.Club)
                    .FirstOrDefaultAsync(r => r.Id == reunionId && r.ClubId == clubId);

                if (reunion == null)
                    return NotFound($"Réunion avec l'ID {reunionId} non trouvée dans le club {clubId}");

                // Vérifier que le nouveau type de réunion existe (si changé)
                if (request.TypeReunionId != reunion.TypeReunionId)
                {
                    var nouveauType = await _context.TypesReunion
                        .FirstOrDefaultAsync(tr => tr.Id == request.TypeReunionId);

                    if (nouveauType == null)
                        return BadRequest("Le nouveau type de réunion n'existe pas");
                }

                // Vérifier les conflits de date/heure/type dans le club (si changement)
                if (request.Date.Date != reunion.Date ||
                    request.Heure != reunion.Heure ||
                    request.TypeReunionId != reunion.TypeReunionId)
                {
                    var conflitDate = await _context.Reunions
                        .AnyAsync(r => r.ClubId == clubId &&
                                      r.Date == request.Date.Date &&
                                      r.Heure == request.Heure &&
                                      r.TypeReunionId == request.TypeReunionId &&
                                      r.Id != reunionId);

                    if (conflitDate)
                        return BadRequest($"Une autre réunion de ce type existe déjà le {request.Date:dd/MM/yyyy} à {request.Heure:hh\\:mm} dans ce club");
                }

                // Mettre à jour les propriétés
                reunion.Date = request.Date.Date;
                reunion.Heure = request.Heure;
                reunion.TypeReunionId = request.TypeReunionId;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Réunion modifiée (ID: {ReunionId}) dans le club {ClubId} : {Date} à {Heure}",
                    reunionId, clubId, request.Date.ToString("dd/MM/yyyy"), request.Heure.ToString(@"hh\:mm"));

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification de la réunion {ReunionId} du club {ClubId}", reunionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la modification de la réunion");
            }
        }

        // PUT: api/clubs/{clubId}/reunions/{reunionId}/complete
        [HttpPut("{reunionId:guid}/complete")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> ModifierReunionComplete(Guid clubId, Guid reunionId, [FromBody] ModifierReunionCompleteRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (clubId == Guid.Empty)
                    return BadRequest("L'identifiant du club est invalide");

                if (reunionId == Guid.Empty)
                    return BadRequest("L'identifiant de la réunion est invalide");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (!await CanManageClub(clubId))
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");

                _tenantService.SetCurrentTenantId(clubId);

                var reunion = await _context.Reunions
                    .Include(r => r.TypeReunion)
                    .Include(r => r.Club)
                    .Include(r => r.OrdresDuJour)
                    .FirstOrDefaultAsync(r => r.Id == reunionId && r.ClubId == clubId);

                if (reunion == null)
                    return NotFound($"Réunion avec l'ID {reunionId} non trouvée dans le club {clubId}");

                // Vérifier que le nouveau type de réunion existe (si changé)
                if (request.TypeReunionId != reunion.TypeReunionId)
                {
                    var nouveauType = await _context.TypesReunion
                        .FirstOrDefaultAsync(tr => tr.Id == request.TypeReunionId);

                    if (nouveauType == null)
                        return BadRequest("Le nouveau type de réunion n'existe pas");
                }

                // Vérifier les conflits de date/heure/type dans le club (si changement)
                if (request.Date.Date != reunion.Date ||
                    request.Heure != reunion.Heure ||
                    request.TypeReunionId != reunion.TypeReunionId)
                {
                    var conflitDate = await _context.Reunions
                        .AnyAsync(r => r.ClubId == clubId &&
                                      r.Date == request.Date.Date &&
                                      r.Heure == request.Heure &&
                                      r.TypeReunionId == request.TypeReunionId &&
                                      r.Id != reunionId);

                    if (conflitDate)
                        return BadRequest($"Une autre réunion de ce type existe déjà le {request.Date:dd/MM/yyyy} à {request.Heure:hh\\:mm} dans ce club");
                }

                // Mettre à jour les propriétés de la réunion
                reunion.Date = request.Date.Date;
                reunion.Heure = request.Heure;
                reunion.TypeReunionId = request.TypeReunionId;

                // Gestion des ordres du jour selon la stratégie choisie
                if (request.RemplacerOrdresDuJour)
                {
                    _context.OrdresDuJour.RemoveRange(reunion.OrdresDuJour);
                }

                // Ajouter les nouveaux ordres du jour
                if (request.OrdresDuJour != null && request.OrdresDuJour.Any())
                {
                    var descriptionsValides = request.OrdresDuJour
                        .Where(desc => !string.IsNullOrWhiteSpace(desc))
                        .Select(desc => desc.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (!request.RemplacerOrdresDuJour)
                    {
                        var descriptionsExistantes = reunion.OrdresDuJour
                            .Select(odj => odj.Description.ToLower().Trim())
                            .ToHashSet();

                        descriptionsValides = descriptionsValides
                            .Where(desc => !descriptionsExistantes.Contains(desc.ToLower()))
                            .ToList();
                    }

                    foreach (var description in descriptionsValides)
                    {
                        var ordreDuJour = new OrdreDuJour
                        {
                            Id = Guid.NewGuid(),
                            Description = description,
                            ReunionId = reunion.Id
                        };

                        _context.OrdresDuJour.Add(ordreDuJour);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Réunion complète modifiée (ID: {ReunionId}) dans le club {ClubId} : {Date} à {Heure}",
                    reunionId, clubId, request.Date.ToString("dd/MM/yyyy"), request.Heure.ToString(@"hh\:mm"));

                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erreur lors de la modification de la réunion complète {ReunionId} du club {ClubId}", reunionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la modification de la réunion complète");
            }
        }

        // DELETE: api/clubs/{clubId}/reunions/{reunionId}
        [HttpDelete("{reunionId:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> SupprimerReunion(Guid clubId, Guid reunionId)
        {
            try
            {
                if (clubId == Guid.Empty)
                    return BadRequest("L'identifiant du club est invalide");

                if (reunionId == Guid.Empty)
                    return BadRequest("L'identifiant de la réunion est invalide");

                if (!await CanManageClub(clubId))
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");

                _tenantService.SetCurrentTenantId(clubId);

                var reunion = await _context.Reunions
                    .Include(r => r.TypeReunion)
                    .Include(r => r.Club)
                    .FirstOrDefaultAsync(r => r.Id == reunionId && r.ClubId == clubId);

                if (reunion == null)
                    return NotFound($"Réunion avec l'ID {reunionId} non trouvée dans le club {clubId}");

                _context.Reunions.Remove(reunion);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Réunion supprimée : {TypeReunion} du {Date} à {Heure} du club {ClubNom} (ID: {ReunionId})",
                    reunion.TypeReunion.Libelle, reunion.Date.ToString("dd/MM/yyyy"), reunion.Heure.ToString(@"hh\:mm"), reunion.Club.Name, reunionId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la réunion {ReunionId} du club {ClubId}", reunionId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de la réunion");
            }
        }

        // GET: api/clubs/{clubId}/reunions/prochaines
        [HttpGet("prochaines")]
        public async Task<IActionResult> GetProchainesReunions(Guid clubId, [FromQuery] int nombre = 5)
        {
            try
            {
                if (!await CanAccessClub(clubId))
                    return Forbid("Accès non autorisé à ce club");

                var dateActuelle = DateTime.Today;
                var heureActuelle = DateTime.Now.TimeOfDay;

                var prochaines = await _context.Reunions
                    .Include(r => r.TypeReunion)
                    .Include(r => r.Club)
                    .Where(r => r.ClubId == clubId &&
                              (r.Date > dateActuelle ||
                               (r.Date == dateActuelle && r.Heure > heureActuelle)))
                    .OrderBy(r => r.Date)
                    .ThenBy(r => r.Heure)
                    .Take(nombre)
                    .Select(r => new ReunionDto
                    {
                        Id = r.Id,
                        Date = r.Date,
                        Heure = r.Heure,
                        ClubId = r.ClubId,
                        ClubNom = r.Club.Name,
                        TypeReunionId = r.TypeReunionId,
                        TypeReunionLibelle = r.TypeReunion.Libelle,
                        NombreOrdresDuJour = r.OrdresDuJour.Count(),
                        NombrePresences = r.ListesPresence.Count(),
                        NombreInvites = r.Invites.Count(),
                        NombreDocuments = r.Documents.Count()
                    })
                    .ToListAsync();

                return Ok(prochaines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des prochaines réunions du club {ClubId}", clubId);
                return StatusCode(500, "Une erreur est survenue");
            }
        }

        // POST: api/clubs/{clubId}/reunions/{reunionId}/compte-rendu
        [HttpPost("{reunionId:guid}/compte-rendu")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> GenererCompteRendu(Guid clubId, Guid reunionId, [FromBody] CompteRenduRequest request)
        {
            try
            {
                if (clubId == Guid.Empty)
                    return BadRequest("L'identifiant du club est invalide");

                if (reunionId == Guid.Empty)
                    return BadRequest("L'identifiant de la réunion est invalide");

                if (!await CanAccessClub(clubId))
                    return Forbid("Accès non autorisé à ce club");

                _tenantService.SetCurrentTenantId(clubId);

                // Vérifier que la réunion existe
                var reunion = await _context.Reunions
                    .Include(r => r.TypeReunion)
                    .Include(r => r.Club)
                    .FirstOrDefaultAsync(r => r.Id == reunionId && r.ClubId == clubId);

                if (reunion == null)
                    return NotFound($"Réunion avec l'ID {reunionId} non trouvée dans le club {clubId}");

                // Chemin vers le modèle de document
                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ModeComptesRendusReunion.docx");

                // Vérifier que le modèle existe
                if (!System.IO.File.Exists(templatePath))
                {
                    _logger.LogError("Le modèle de document est introuvable : {TemplatePath}", templatePath);
                    return StatusCode(500, "Le modèle de document est introuvable");
                }

                // Lire le modèle et créer une copie en mémoire
                byte[] templateBytes = await System.IO.File.ReadAllBytesAsync(templatePath);

                using (var memStream = new MemoryStream())
                {
                    // Copier le modèle dans le stream de travail
                    memStream.Write(templateBytes, 0, templateBytes.Length);
                    memStream.Position = 0;

                    // Ouvrir le document à partir du modèle
                    using (var document = WordprocessingDocument.Open(memStream, true))
                    {
                        var mainPart = document.MainDocumentPart;
                        if (mainPart?.Document?.Body == null)
                        {
                            _logger.LogError("Le modèle de document est invalide ou corrompu");
                            return StatusCode(500, "Le modèle de document est invalide");
                        }

                        var body = mainPart.Document.Body;

                        // Vider le contenu existant du modèle (si nécessaire)
                        // Commentez cette ligne si vous voulez conserver le contenu du modèle
                        // body.RemoveAllChildren();

                        // Ajouter les styles s'ils n'existent pas déjà
                        if (mainPart.StyleDefinitionsPart == null)
                        {
                            AddDocumentStyles(mainPart);
                        }

                        // En-tête avec nom du club
                        var clubHeader = body.AppendChild(new Paragraph(
                            new Run(new Text(reunion.Club.Name))
                        ));
                        ApplyParagraphStyle(clubHeader, "ClubName");

                        // Titre principal
                        var title = body.AppendChild(new Paragraph(
                            new Run(new Text("COMPTE-RENDU DE RÉUNION"))
                        ));
                        ApplyParagraphStyle(title, "MainTitle");

                        // Sous-titre avec la date et le type de réunion
                        var subtitle = body.AppendChild(new Paragraph(
                            new Run(new Text($"{reunion.TypeReunion.Libelle} du {reunion.Date:dd/MM/yyyy} à {reunion.Heure:hh\\:mm}"))
                        ));
                        ApplyParagraphStyle(subtitle, "SubTitle");

                        // Espacement
                        body.AppendChild(new Paragraph());

                        // Créer un tableau pour les présences et invités SEULEMENT s'il y en a
                        if ((request.Presences != null && request.Presences.Any()) ||
                            (request.Invites != null && request.Invites.Any()))
                        {
                            var table = CreateParticipantsTable(request.Presences, request.Invites);
                            body.AppendChild(table);

                            // Espacement après le tableau
                            body.AppendChild(new Paragraph());
                            body.AppendChild(new Paragraph());
                        }

                        // Section Déroulé
                        var derouleTitle = body.AppendChild(new Paragraph(
                            new Run(new Text("DÉROULÉ"))
                        ));
                        ApplyParagraphStyle(derouleTitle, "SectionTitle");

                        // Section Ordre du jour
                        if (request.OrdresDuJour != null && request.OrdresDuJour.Any())
                        {
                            foreach (var ordre in request.OrdresDuJour)
                            {
                                // Titre de l'ordre du jour
                                var ordreTitle = body.AppendChild(new Paragraph(
                                    new Run(new Text($"{ordre.Numero}. {ordre.Description}"))
                                ));
                                ApplyParagraphStyle(ordreTitle, "OrderTitle");

                                // Contenu de l'ordre du jour
                                if (!string.IsNullOrEmpty(ordre.Contenu))
                                {
                                    var paragraphes = ordre.Contenu.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                                    foreach (var paragraphe in paragraphes)
                                    {
                                        if (!string.IsNullOrWhiteSpace(paragraphe))
                                        {
                                            var contentPara = body.AppendChild(new Paragraph(
                                                new Run(new Text(paragraphe.Trim()))
                                            ));
                                            ApplyParagraphStyle(contentPara, "Normal");
                                        }
                                    }
                                }
                                else
                                {
                                    var emptyPara = body.AppendChild(new Paragraph(
                                        new Run(new Text("(Point non traité ou sans détail)"))
                                    ));
                                    ApplyParagraphStyle(emptyPara, "Italic");
                                }

                                // Espacement entre les ordres
                                body.AppendChild(new Paragraph());
                            }
                        }
                        else
                        {
                            var noOrderPara = body.AppendChild(new Paragraph(
                                new Run(new Text("Aucun ordre du jour défini."))
                            ));
                            ApplyParagraphStyle(noOrderPara, "Italic");
                            body.AppendChild(new Paragraph());
                        }

                        // Sauvegarder le document
                        mainPart.Document.Save();
                    }

                    // Générer le nom de fichier
                    var fileName = $"compte-rendu-{reunion.TypeReunion.Libelle.Replace(" ", "-")}-{reunion.Date:dd-MM-yyyy}.docx";

                    _logger.LogInformation("Compte-rendu généré à partir du modèle pour la réunion {ReunionId} du club {ClubId}", reunionId, clubId);

                    // Retourner le document
                    return File(
                        memStream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                        fileName
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération du compte-rendu pour la réunion {ReunionId} du club {ClubId}", reunionId, clubId);
                return StatusCode(500, $"Erreur lors de la génération du document : {ex.Message}");
            }
        }

        // Méthode pour créer le tableau des participants
        private Table CreateParticipantsTable(List<CompteRenduPresenceDto> presences, List<CompteRenduInviteDto> invites)
        {
            var table = new Table();

            // Propriétés du tableau
            var tableProperties = new TableProperties(
                new TableBorders(
                    new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                    new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                    new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                    new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                    new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                    new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 }
                ),
                new TableWidth() { Type = TableWidthUnitValues.Pct, Width = "5000" }, // 50% de la page
                new TableJustification() { Val = TableRowAlignmentValues.Center }
            );
            table.AppendChild(tableProperties);

            // Grille des colonnes (50% chacune)
            var tableGrid = new TableGrid(
                new GridColumn { Width = "4250" },
                new GridColumn { Width = "4250" }
            );
            table.AppendChild(tableGrid);

            // En-tête du tableau
            var headerRow = table.AppendChild(new TableRow());

            var headerCell1 = headerRow.AppendChild(new TableCell());
            var headerCellProps1 = new TableCellProperties(
                new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "2500" },
                new Shading() { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "E7E7E7" }
            );
            headerCell1.AppendChild(headerCellProps1);
            var headerPara1 = headerCell1.AppendChild(new Paragraph(
                new Run(new Text("LISTE DES PRÉSENCES"))
            ));
            ApplyParagraphStyle(headerPara1, "TableHeader");

            var headerCell2 = headerRow.AppendChild(new TableCell());
            var headerCellProps2 = new TableCellProperties(
                new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "2500" },
                new Shading() { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "E7E7E7" }
            );
            headerCell2.AppendChild(headerCellProps2);
            var headerPara2 = headerCell2.AppendChild(new Paragraph(
                new Run(new Text("LISTE DES INVITÉS"))
            ));
            ApplyParagraphStyle(headerPara2, "TableHeader");

            // Calculer le nombre de lignes nécessaires
            var maxRows = Math.Max(
                presences?.Count ?? 0,
                invites?.Count ?? 0
            );

            // Si aucune présence ni invité, créer une ligne avec cellules vides
            if (maxRows == 0)
            {
                maxRows = 1;
            }

            // Remplir le tableau
            for (int i = 0; i < maxRows; i++)
            {
                var row = table.AppendChild(new TableRow());

                // Cellule des présences
                var cell1 = row.AppendChild(new TableCell());
                var cellProps1 = new TableCellProperties(
                    new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "2500" }
                );
                cell1.AppendChild(cellProps1);

                if (presences != null && i < presences.Count)
                {
                    var presencePara = cell1.AppendChild(new Paragraph(
                        new Run(new Text($"{presences[i].NomComplet}"))
                    ));
                    ApplyParagraphStyle(presencePara, "TableContent");
                }
                else
                {
                    var emptyPara = cell1.AppendChild(new Paragraph(new Run(new Text(""))));
                    ApplyParagraphStyle(emptyPara, "TableContent");
                }

                // Cellule des invités
                var cell2 = row.AppendChild(new TableCell());
                var cellProps2 = new TableCellProperties(
                    new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "2500" }
                );
                cell2.AppendChild(cellProps2);

                if (invites != null && i < invites.Count)
                {
                    var invite = invites[i];
                    var nomComplet = $"{invite.Prenom} {invite.Nom}".Trim();
                    // Pas d'affichage de l'organisation dans le document Word
                    var invitePara = cell2.AppendChild(new Paragraph(
                        new Run(new Text($"{nomComplet}"))
                    ));
                    ApplyParagraphStyle(invitePara, "TableContent");
                }
                else
                {
                    var emptyPara = cell2.AppendChild(new Paragraph(new Run(new Text(""))));
                    ApplyParagraphStyle(emptyPara, "TableContent");
                }
            }

            return table;
        }

        // Méthode améliorée pour appliquer les styles
        private void ApplyParagraphStyle(Paragraph paragraph, string styleName)
        {
            var paragraphProperties = paragraph.GetFirstChild<ParagraphProperties>();
            if (paragraphProperties == null)
            {
                paragraphProperties = new ParagraphProperties();
                paragraph.PrependChild(paragraphProperties);
            }

            // Appliquer le style défini
            paragraphProperties.ParagraphStyleId = new ParagraphStyleId() { Val = styleName };

            // Ajouter des propriétés de formatage selon le style
            var run = paragraph.GetFirstChild<Run>();
            if (run != null)
            {
                var runProperties = run.GetFirstChild<RunProperties>();
                if (runProperties == null)
                {
                    runProperties = new RunProperties();
                    run.PrependChild(runProperties);
                }

                switch (styleName)
                {
                    case "ClubName":
                        runProperties.AppendChild(new Bold());
                        runProperties.AppendChild(new FontSize() { Val = "24" }); // 12pt
                        paragraphProperties.AppendChild(new Justification() { Val = JustificationValues.Center });
                        paragraphProperties.AppendChild(new SpacingBetweenLines() { After = "240" }); // 12pt après
                        break;

                    case "MainTitle":
                        runProperties.AppendChild(new Bold());
                        runProperties.AppendChild(new FontSize() { Val = "32" }); // 16pt
                        paragraphProperties.AppendChild(new Justification() { Val = JustificationValues.Center });
                        paragraphProperties.AppendChild(new SpacingBetweenLines() { After = "240" }); // 12pt après
                        break;

                    case "SubTitle":
                        runProperties.AppendChild(new Bold());
                        runProperties.AppendChild(new FontSize() { Val = "26" }); // 13pt
                        paragraphProperties.AppendChild(new Justification() { Val = JustificationValues.Center });
                        paragraphProperties.AppendChild(new SpacingBetweenLines() { After = "360" }); // 18pt après
                        break;

                    case "SectionTitle":
                        runProperties.AppendChild(new Bold());
                        runProperties.AppendChild(new FontSize() { Val = "24" }); // 12pt
                        paragraphProperties.AppendChild(new SpacingBetweenLines() { Before = "240", After = "120" }); // 12pt avant, 6pt après
                        break;

                    case "OrderTitle":
                        runProperties.AppendChild(new Bold());
                        runProperties.AppendChild(new FontSize() { Val = "22" }); // 11pt
                        paragraphProperties.AppendChild(new SpacingBetweenLines() { Before = "120", After = "60" }); // 6pt avant, 3pt après
                        break;

                    case "TableHeader":
                        runProperties.AppendChild(new Bold());
                        runProperties.AppendChild(new FontSize() { Val = "20" }); // 10pt
                        paragraphProperties.AppendChild(new Justification() { Val = JustificationValues.Center });
                        break;

                    case "TableContent":
                        runProperties.AppendChild(new FontSize() { Val = "20" }); // 10pt
                        paragraphProperties.AppendChild(new SpacingBetweenLines() { LineRule = LineSpacingRuleValues.Auto, Line = "240" }); // Interligne automatique
                        break;

                    case "Italic":
                        runProperties.AppendChild(new Italic());
                        runProperties.AppendChild(new FontSize() { Val = "20" }); // 10pt
                        runProperties.AppendChild(new Color() { Val = "666666" }); // Gris
                        break;

                    case "Normal":
                    default:
                        runProperties.AppendChild(new FontSize() { Val = "22" }); // 11pt
                        paragraphProperties.AppendChild(new SpacingBetweenLines() { After = "120" }); // 6pt après
                        break;
                }
            }
        }

        // Méthode améliorée pour créer les styles du document
        private void AddDocumentStyles(MainDocumentPart mainPart)
        {
            var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
            var styles = new Styles();

            // Style Normal (par défaut)
            var normalStyle = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "Normal",
                Default = true
            };
            normalStyle.AppendChild(new StyleName() { Val = "Normal" });
            normalStyle.AppendChild(new StyleRunProperties(
                new RunFonts() { Ascii = "Calibri", HighAnsi = "Calibri" },
                new FontSize() { Val = "22" }, // 11pt
                new FontSizeComplexScript() { Val = "22" }
            ));
            styles.AppendChild(normalStyle);

            // Style pour le nom du club
            var clubNameStyle = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "ClubName"
            };
            clubNameStyle.AppendChild(new StyleName() { Val = "Club Name" });
            clubNameStyle.AppendChild(new StyleRunProperties(
                new RunFonts() { Ascii = "Calibri", HighAnsi = "Calibri" },
                new Bold(),
                new FontSize() { Val = "24" }
            ));
            clubNameStyle.AppendChild(new StyleParagraphProperties(
                new Justification() { Val = JustificationValues.Center }
            ));
            styles.AppendChild(clubNameStyle);

            // Style pour le titre principal
            var mainTitleStyle = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "MainTitle"
            };
            mainTitleStyle.AppendChild(new StyleName() { Val = "Main Title" });
            mainTitleStyle.AppendChild(new StyleRunProperties(
                new RunFonts() { Ascii = "Calibri", HighAnsi = "Calibri" },
                new Bold(),
                new FontSize() { Val = "32" }
            ));
            mainTitleStyle.AppendChild(new StyleParagraphProperties(
                new Justification() { Val = JustificationValues.Center }
            ));
            styles.AppendChild(mainTitleStyle);

            // Style pour le sous-titre
            var subTitleStyle = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "SubTitle"
            };
            subTitleStyle.AppendChild(new StyleName() { Val = "Sub Title" });
            subTitleStyle.AppendChild(new StyleRunProperties(
                new RunFonts() { Ascii = "Calibri", HighAnsi = "Calibri" },
                new Bold(),
                new FontSize() { Val = "26" }
            ));
            subTitleStyle.AppendChild(new StyleParagraphProperties(
                new Justification() { Val = JustificationValues.Center }
            ));
            styles.AppendChild(subTitleStyle);

            // Style pour les titres de section
            var sectionTitleStyle = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "SectionTitle"
            };
            sectionTitleStyle.AppendChild(new StyleName() { Val = "Section Title" });
            sectionTitleStyle.AppendChild(new StyleRunProperties(
                new RunFonts() { Ascii = "Calibri", HighAnsi = "Calibri" },
                new Bold(),
                new FontSize() { Val = "24" }
            ));
            styles.AppendChild(sectionTitleStyle);

            // Style pour les ordres du jour
            var orderTitleStyle = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "OrderTitle"
            };
            orderTitleStyle.AppendChild(new StyleName() { Val = "Order Title" });
            orderTitleStyle.AppendChild(new StyleRunProperties(
                new RunFonts() { Ascii = "Calibri", HighAnsi = "Calibri" },
                new Bold(),
                new FontSize() { Val = "22" }
            ));
            styles.AppendChild(orderTitleStyle);

            // Style pour les en-têtes de tableau
            var tableHeaderStyle = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "TableHeader"
            };
            tableHeaderStyle.AppendChild(new StyleName() { Val = "Table Header" });
            tableHeaderStyle.AppendChild(new StyleRunProperties(
                new RunFonts() { Ascii = "Calibri", HighAnsi = "Calibri" },
                new Bold(),
                new FontSize() { Val = "20" }
            ));
            tableHeaderStyle.AppendChild(new StyleParagraphProperties(
                new Justification() { Val = JustificationValues.Center }
            ));
            styles.AppendChild(tableHeaderStyle);

            // Style pour le contenu des tableaux
            var tableContentStyle = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "TableContent"
            };
            tableContentStyle.AppendChild(new StyleName() { Val = "Table Content" });
            tableContentStyle.AppendChild(new StyleRunProperties(
                new RunFonts() { Ascii = "Calibri", HighAnsi = "Calibri" },
                new FontSize() { Val = "20" }
            ));
            styles.AppendChild(tableContentStyle);

            // Style pour le texte en italique
            var italicStyle = new Style()
            {
                Type = StyleValues.Paragraph,
                StyleId = "Italic"
            };
            italicStyle.AppendChild(new StyleName() { Val = "Italic" });
            italicStyle.AppendChild(new StyleRunProperties(
                new RunFonts() { Ascii = "Calibri", HighAnsi = "Calibri" },
                new Italic(),
                new FontSize() { Val = "20" },
                new Color() { Val = "666666" }
            ));
            styles.AppendChild(italicStyle);

            stylesPart.Styles = styles;
        }

        // Méthodes d'aide pour vérifier les autorisations
        private async Task<bool> CanAccessClub(Guid clubId)
        {
            if (User.IsInRole("Admin"))
                return true;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return false;

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

            var userInClub = await _context.UserClubs
                .AnyAsync(uc => uc.UserId == userId && uc.ClubId == clubId);

            if (!userInClub)
                return false;

            return User.IsInRole("President") || User.IsInRole("Secretary");
        }
    }

    // DTOs existants et nouveaux
    public class ReunionDto
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Heure { get; set; }
        public Guid ClubId { get; set; }
        public string ClubNom { get; set; } = string.Empty;
        public Guid TypeReunionId { get; set; }
        public string TypeReunionLibelle { get; set; } = string.Empty;
        public int NombreOrdresDuJour { get; set; }
        public int NombrePresences { get; set; }
        public int NombreInvites { get; set; }
        public int NombreDocuments { get; set; }
    }

    public class ReunionDetailDto : ReunionDto
    {
        public string Divers { get; set; } = string.Empty;
        public List<OrdreDuJourDto> OrdresDuJour { get; set; } = new List<OrdreDuJourDto>();
        public List<PresenceDto> Presences { get; set; } = new List<PresenceDto>();
        public List<InviteDto> Invites { get; set; } = new List<InviteDto>();
        public List<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
    }


    public class PresenceDto
    {
        public Guid Id { get; set; }
        public string MembreId { get; set; } = string.Empty;
        public string NomCompletMembre { get; set; } = string.Empty;
        public string EmailMembre { get; set; } = string.Empty;
    }

    public class InviteDto
    {
        public Guid Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Telephone { get; set; }
        public string? Organisation { get; set; }
    }

    public class DocumentDto
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public int TailleEnOctets { get; set; }
    }

    public class CreerReunionRequest
    {
        [Required(ErrorMessage = "La date est obligatoire")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "L'heure est obligatoire")]
        public TimeSpan Heure { get; set; }

        [Required(ErrorMessage = "Le type de réunion est obligatoire")]
        public Guid TypeReunionId { get; set; }
    }

    public class ModifierReunionRequest
    {
        [Required(ErrorMessage = "La date est obligatoire")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "L'heure est obligatoire")]
        public TimeSpan Heure { get; set; }

        [Required(ErrorMessage = "Le type de réunion est obligatoire")]
        public Guid TypeReunionId { get; set; }
    }

    public class CreerReunionCompleteRequest
    {
        [Required(ErrorMessage = "La date est obligatoire")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "L'heure est obligatoire")]
        public TimeSpan Heure { get; set; }

        [Required(ErrorMessage = "Le type de réunion est obligatoire")]
        public Guid TypeReunionId { get; set; }

        public List<string> OrdresDuJour { get; set; } = new List<string>();
    }

    public class ModifierReunionCompleteRequest
    {
        [Required(ErrorMessage = "La date est obligatoire")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "L'heure est obligatoire")]
        public TimeSpan Heure { get; set; }

        [Required(ErrorMessage = "Le type de réunion est obligatoire")]
        public Guid TypeReunionId { get; set; }

        public List<string> OrdresDuJour { get; set; } = new List<string>();
        public bool RemplacerOrdresDuJour { get; set; } = false;
    }

    public class ReunionCompleteDto
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Heure { get; set; }
        public Guid ClubId { get; set; }
        public string ClubNom { get; set; } = string.Empty;
        public Guid TypeReunionId { get; set; }
        public string TypeReunionLibelle { get; set; } = string.Empty;
        public List<OrdreDuJourDto> OrdresDuJour { get; set; } = new List<OrdreDuJourDto>();
        public int NombreOrdresDuJour { get; set; }
        public int NombrePresences { get; set; }
        public int NombreInvites { get; set; }
        public int NombreDocuments { get; set; }
    }

    public class OrdreDuJourDto
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    // DTOs pour la génération de compte-rendu
    public class CompteRenduRequest
    {
        public List<CompteRenduPresenceDto> Presences { get; set; } = new List<CompteRenduPresenceDto>();
        public List<CompteRenduInviteDto> Invites { get; set; } = new List<CompteRenduInviteDto>();
        public List<CompteRenduOrdreDuJourDto> OrdresDuJour { get; set; } = new List<CompteRenduOrdreDuJourDto>();
        public string? Divers { get; set; }
    }

    public class CompteRenduPresenceDto
    {
        public string NomComplet { get; set; } = string.Empty;
    }

    public class CompteRenduInviteDto
    {
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string? Organisation { get; set; }
    }

    public class CompteRenduOrdreDuJourDto
    {
        public int Numero { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Contenu { get; set; }
    }
}