using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/clubs/{clubId}/evenements")]
    [ApiController]
    [Authorize]
    public class EvenementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EvenementController> _logger;

        public EvenementController(ApplicationDbContext context, ILogger<EvenementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/clubs/{clubId}/evenements
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetEvenements(
            Guid clubId,
            [FromQuery] bool? estInterne = null,
            [FromQuery] DateTime? dateDebut = null,
            [FromQuery] DateTime? dateFin = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
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

                var query = _context.Evenements
                    .Include(e => e.Club)
                    .Where(e => e.ClubId == clubId);

                // Filtres
                if (estInterne.HasValue)
                {
                    query = query.Where(e => e.EstInterne == estInterne.Value);
                }

                if (dateDebut.HasValue)
                {
                    query = query.Where(e => e.Date >= dateDebut.Value);
                }

                if (dateFin.HasValue)
                {
                    query = query.Where(e => e.Date <= dateFin.Value);
                }

                // Pagination
                var totalItems = await query.CountAsync();

                // Projection pour éviter les références circulaires et optimiser les performances
                var evenements = await query
                    .OrderByDescending(e => e.Date)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(e => new
                    {
                        e.Id,
                        e.Libelle,
                        e.Date,
                        e.Lieu,
                        e.Description,
                        e.EstInterne,
                        e.ClubId,
                        ClubNom = e.Club.Name,
                        // Compter les éléments liés sans les charger
                        NombreDocuments = e.Documents.Count(),
                        NombreImages = e.Images.Count(),
                        NombreBudgets = e.Budgets.Count(),
                        NombreRecettes = e.Recettes.Count()
                    })
                    .ToListAsync();

                // Headers de pagination
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", Math.Ceiling((double)totalItems / pageSize).ToString());

                return Ok(evenements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des événements du club {ClubId}", clubId);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // GET: api/clubs/{clubId}/evenements/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<object>> GetEvenement(Guid clubId, Guid id)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'événement est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                var evenement = await _context.Evenements
                    .Include(e => e.Club)
                    .Where(e => e.Id == id && e.ClubId == clubId)
                    .Select(e => new
                    {
                        e.Id,
                        e.Libelle,
                        e.Date,
                        e.Lieu,
                        e.Description,
                        e.EstInterne,
                        e.ClubId,
                        ClubNom = e.Club.Name,
                        Documents = e.Documents.Select(d => new
                        {
                            d.Id,
                            d.Libelle,
                            d.DateAjout,
                            TailleDocument = d.Document.Length
                        }).ToList(),
                        Images = e.Images.Select(i => new
                        {
                            i.Id,
                            i.Description,
                            i.DateAjout,
                            TailleImage = i.Image.Length
                        }).ToList(),
                        Budgets = e.Budgets.Select(b => new
                        {
                            b.Id,
                            b.Libelle,
                            b.MontantBudget,
                            b.MontantRealise
                        }).ToList(),
                        Recettes = e.Recettes.Select(r => new
                        {
                            r.Id,
                            r.Libelle,
                            r.Montant
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (evenement == null)
                {
                    return NotFound(new { message = $"Événement avec l'ID {id} non trouvé dans le club {clubId}" });
                }

                return Ok(evenement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'événement {Id} du club {ClubId}", id, clubId);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // POST: api/clubs/{clubId}/evenements
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary,Treasurer")]
        public async Task<ActionResult<object>> CreateEvenement(Guid clubId, [FromBody] CreateEvenementRequest request)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                // Vérifier que le club existe
                var club = await _context.Clubs.FindAsync(clubId);
                if (club == null)
                {
                    return NotFound("Club non trouvé");
                }

                // Vérifier l'unicité (optionnel - même nom, club, et date)
                var existingEvenement = await _context.Evenements
                    .AnyAsync(e => e.ClubId == clubId &&
                                 e.Libelle.ToLower() == request.Libelle.ToLower() &&
                                 e.Date.Date == request.Date.Date);

                if (existingEvenement)
                {
                    return BadRequest($"Un événement avec le nom '{request.Libelle}' existe déjà à cette date dans ce club");
                }

                var evenement = new Evenement
                {
                    Id = Guid.NewGuid(),
                    Libelle = request.Libelle,
                    Date = request.Date,
                    Lieu = request.Lieu,
                    Description = request.Description,
                    EstInterne = request.EstInterne,
                    ClubId = clubId
                };

                _context.Evenements.Add(evenement);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Événement '{EvenementLibelle}' créé pour le club {ClubId} avec l'ID {Id}",
                    evenement.Libelle, clubId, evenement.Id);

                // Retourner une projection simple
                var result = new
                {
                    evenement.Id,
                    evenement.Libelle,
                    evenement.Date,
                    evenement.Lieu,
                    evenement.Description,
                    evenement.EstInterne,
                    evenement.ClubId,
                    ClubNom = club.Name
                };

                return CreatedAtAction(nameof(GetEvenement), new { clubId, id = evenement.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'événement pour le club {ClubId}", clubId);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // PUT: api/clubs/{clubId}/evenements/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary,Treasurer")]
        public async Task<IActionResult> UpdateEvenement(Guid clubId, Guid id, [FromBody] UpdateEvenementRequest request)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'événement est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                var existingEvenement = await _context.Evenements
                    .FirstOrDefaultAsync(e => e.Id == id && e.ClubId == clubId);

                if (existingEvenement == null)
                {
                    return NotFound(new { message = $"Événement avec l'ID {id} non trouvé dans le club {clubId}" });
                }

                // Vérifier l'unicité si le nom ou la date change
                if (request.Libelle.ToLower() != existingEvenement.Libelle.ToLower() ||
                    request.Date.Date != existingEvenement.Date.Date)
                {
                    var conflictingEvenement = await _context.Evenements
                        .AnyAsync(e => e.Id != id &&
                                     e.ClubId == clubId &&
                                     e.Libelle.ToLower() == request.Libelle.ToLower() &&
                                     e.Date.Date == request.Date.Date);

                    if (conflictingEvenement)
                    {
                        return BadRequest($"Un événement avec le nom '{request.Libelle}' existe déjà à cette date dans ce club");
                    }
                }

                // Mise à jour des propriétés
                existingEvenement.Libelle = request.Libelle;
                existingEvenement.Date = request.Date;
                existingEvenement.Lieu = request.Lieu;
                existingEvenement.Description = request.Description;
                existingEvenement.EstInterne = request.EstInterne;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Événement {Id} mis à jour dans le club {ClubId}", id, clubId);

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await EvenementExists(id, clubId))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'événement {Id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // DELETE: api/clubs/{clubId}/evenements/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> DeleteEvenement(Guid clubId, Guid id)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'événement est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                var evenement = await _context.Evenements
                    .FirstOrDefaultAsync(e => e.Id == id && e.ClubId == clubId);

                if (evenement == null)
                {
                    return NotFound(new { message = $"Événement avec l'ID {id} non trouvé dans le club {clubId}" });
                }

                _context.Evenements.Remove(evenement);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Événement {Id} supprimé du club {ClubId}", id, clubId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'événement {Id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // GET: api/clubs/{clubId}/evenements/{id}/documents
        [HttpGet("{id:guid}/documents")]
        public async Task<ActionResult<IEnumerable<object>>> GetEvenementDocuments(Guid clubId, Guid id)
        {
            try
            {
                // Validation et autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                var evenement = await _context.Evenements
                    .FirstOrDefaultAsync(e => e.Id == id && e.ClubId == clubId);

                if (evenement == null)
                {
                    return NotFound(new { message = $"Événement avec l'ID {id} non trouvé dans le club {clubId}" });
                }

                var documents = await _context.EvenementDocuments
                    .Where(d => d.EvenementId == id)
                    .Select(d => new
                    {
                        d.Id,
                        d.Libelle,
                        d.DateAjout,
                        TailleDocument = d.Document.Length
                    })
                    .OrderBy(d => d.Libelle)
                    .ToListAsync();

                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des documents de l'événement {Id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // GET: api/clubs/{clubId}/evenements/{id}/budget
        [HttpGet("{id:guid}/budget")]
        public async Task<ActionResult<object>> GetEvenementBudget(Guid clubId, Guid id)
        {
            try
            {
                // Validation et autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                var evenement = await _context.Evenements
                    .FirstOrDefaultAsync(e => e.Id == id && e.ClubId == clubId);

                if (evenement == null)
                {
                    return NotFound(new { message = $"Événement avec l'ID {id} non trouvé dans le club {clubId}" });
                }

                var budgets = await _context.EvenementBudgets
                    .Where(b => b.EvenementId == id)
                    .Select(b => new
                    {
                        b.Id,
                        b.Libelle,
                        b.MontantBudget,
                        b.MontantRealise
                    })
                    .ToListAsync();

                var recettes = await _context.EvenementRecettes
                    .Where(r => r.EvenementId == id)
                    .Select(r => new
                    {
                        r.Id,
                        r.Libelle,
                        r.Montant
                    })
                    .ToListAsync();

                var totalBudget = budgets.Sum(b => b.MontantBudget);
                var totalRealise = budgets.Sum(b => b.MontantRealise);
                var totalRecettes = recettes.Sum(r => r.Montant);

                var result = new
                {
                    Budgets = budgets,
                    Recettes = recettes,
                    Totaux = new
                    {
                        TotalBudget = totalBudget,
                        TotalRealise = totalRealise,
                        TotalRecettes = totalRecettes,
                        EcartBudget = totalRealise - totalBudget,
                        ResultatNet = totalRecettes - totalRealise
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du budget de l'événement {Id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // GET: api/clubs/{clubId}/evenements/statistiques
        [HttpGet("statistiques")]
        public async Task<ActionResult<object>> GetStatistiques(
            Guid clubId,
            [FromQuery] int? annee = null,
            [FromQuery] bool? estInterne = null)
        {
            try
            {
                // Validation et autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                var query = _context.Evenements
                    .Where(e => e.ClubId == clubId);

                if (annee.HasValue)
                {
                    query = query.Where(e => e.Date.Year == annee.Value);
                }

                if (estInterne.HasValue)
                {
                    query = query.Where(e => e.EstInterne == estInterne.Value);
                }

                // Optimisation avec projection
                var evenementsStats = await query
                    .Select(e => new
                    {
                        e.EstInterne,
                        e.Date.Year,
                        e.Date.Month,
                        BudgetTotal = e.Budgets.Sum(b => b.MontantBudget),
                        DepenseRealisee = e.Budgets.Sum(b => b.MontantRealise),
                        RecetteTotal = e.Recettes.Sum(r => r.Montant)
                    })
                    .ToListAsync();

                var stats = new
                {
                    ClubId = clubId,
                    NombreEvenements = evenementsStats.Count,
                    EvenementsInternes = evenementsStats.Count(e => e.EstInterne),
                    EvenementsExternes = evenementsStats.Count(e => !e.EstInterne),
                    BudgetTotal = evenementsStats.Sum(e => e.BudgetTotal),
                    DepensesRealisees = evenementsStats.Sum(e => e.DepenseRealisee),
                    RecettesTotales = evenementsStats.Sum(e => e.RecetteTotal),
                    ResultatGlobal = evenementsStats.Sum(e => e.RecetteTotal) - evenementsStats.Sum(e => e.DepenseRealisee),
                    EvenementParMois = evenementsStats
                        .GroupBy(e => new { e.Year, e.Month })
                        .Select(g => new
                        {
                            Annee = g.Key.Year,
                            Mois = g.Key.Month,
                            Nombre = g.Count()
                        })
                        .OrderBy(x => x.Annee)
                        .ThenBy(x => x.Mois)
                        .ToList()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques du club {ClubId}", clubId);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // Méthodes d'aide
        private async Task<bool> EvenementExists(Guid id, Guid clubId)
        {
            return await _context.Evenements.AnyAsync(e => e.Id == id && e.ClubId == clubId);
        }

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

            return User.IsInRole("President") || User.IsInRole("Secretary") || User.IsInRole("Treasurer");
        }
    }

    // DTOs mis à jour pour les requêtes
    public class CreateEvenementRequest
    {
        [Required]
        [MaxLength(200)]
        public string Libelle { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }

        [MaxLength(300)]
        public string? Lieu { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool EstInterne { get; set; } = true;
    }

    public class UpdateEvenementRequest
    {
        [Required]
        [MaxLength(200)]
        public string Libelle { get; set; } = string.Empty;

        [Required]
        public DateTime Date { get; set; }

        [MaxLength(300)]
        public string? Lieu { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool EstInterne { get; set; } = true;
    }
}