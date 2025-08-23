using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/clubs/{clubId}/mandats/{mandatId}/rubriques")]
    [ApiController]
    [Authorize]
    public class RubriqueBudgetController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RubriqueBudgetController> _logger;

        public RubriqueBudgetController(
            ApplicationDbContext context,
            ILogger<RubriqueBudgetController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/clubs/{clubId}/mandats/{mandatId}/rubriques
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RubriqueBudgetDto>>> GetRubriques(
            Guid clubId,
            Guid mandatId,
            [FromQuery] Guid? sousCategoryId = null,
            [FromQuery] string? recherche = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
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
                    return StatusCode(403, "Accès non autorisé à ce club");
                }

                var query = _context.RubriquesBudget
                    .Include(r => r.SousCategoryBudget)
                        .ThenInclude(sc => sc.CategoryBudget)
                            .ThenInclude(c => c.TypeBudget)
                    .Include(r => r.Mandat)
                    .Include(r => r.Club)
                    .Where(r => r.ClubId == clubId && r.MandatId == mandatId);

                // Filtres optionnels
                if (sousCategoryId.HasValue)
                {
                    query = query.Where(r => r.SousCategoryBudgetId == sousCategoryId.Value);
                }

                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(r => r.Libelle.ToLower().Contains(termeLower));
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var totalPages = Math.Ceiling((double)totalItems / pageSize);

                var rubriques = await query
                    .OrderBy(r => r.SousCategoryBudget.CategoryBudget.TypeBudget.Libelle)
                    .ThenBy(r => r.SousCategoryBudget.CategoryBudget.Libelle)
                    .ThenBy(r => r.SousCategoryBudget.Libelle)
                    .ThenBy(r => r.Libelle)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new RubriqueBudgetDto
                    {
                        Id = r.Id,
                        Libelle = r.Libelle,
                        PrixUnitaire = r.PrixUnitaire,
                        Quantite = r.Quantite,
                        MontantTotal = r.PrixUnitaire * r.Quantite, // Calcul correct
                        SousCategoryBudgetId = r.SousCategoryBudgetId,
                        SousCategoryLibelle = r.SousCategoryBudget.Libelle,
                        CategoryBudgetLibelle = r.SousCategoryBudget.CategoryBudget.Libelle,
                        TypeBudgetLibelle = r.SousCategoryBudget.CategoryBudget.TypeBudget.Libelle,
                        MandatId = r.MandatId,
                        MandatAnnee = r.Mandat.Annee,
                        ClubId = r.ClubId,
                        ClubNom = r.Club.Name,
                        NombreRealisations = r.Realisations.Count(),
                        MontantRealise = r.MontantRealise,
                        EcartBudgetRealise = r.MontantRealise - (r.PrixUnitaire * r.Quantite), // Calcul correct
                        PourcentageRealisation = (r.PrixUnitaire * r.Quantite) > 0
                            ? Math.Round((double)(r.MontantRealise / (r.PrixUnitaire * r.Quantite)) * 100, 2)
                            : 0
                    })
                    .ToListAsync();

                // Headers de pagination
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());

                return Ok(rubriques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des rubriques du club {ClubId} pour le mandat {MandatId}", clubId, mandatId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des rubriques");
            }
        }

        // GET: api/clubs/{clubId}/mandats/{mandatId}/rubriques/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<RubriqueBudgetDetailDto>> GetRubrique(
            Guid clubId,
            Guid mandatId,
            Guid id)
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

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la rubrique est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return StatusCode(403, "Accès non autorisé à ce club");
                }

                var rubrique = await _context.RubriquesBudget
                    .Include(r => r.SousCategoryBudget)
                        .ThenInclude(sc => sc.CategoryBudget)
                            .ThenInclude(c => c.TypeBudget)
                    .Include(r => r.Mandat)
                    .Include(r => r.Club)
                    .Include(r => r.Realisations)
                    .Where(r => r.Id == id && r.ClubId == clubId && r.MandatId == mandatId)
                    .Select(r => new RubriqueBudgetDetailDto
                    {
                        Id = r.Id,
                        Libelle = r.Libelle,
                        PrixUnitaire = r.PrixUnitaire,
                        Quantite = r.Quantite,
                        MontantTotal = r.PrixUnitaire * r.Quantite, // Calcul correct
                        SousCategoryBudgetId = r.SousCategoryBudgetId,
                        SousCategoryLibelle = r.SousCategoryBudget.Libelle,
                        CategoryBudgetLibelle = r.SousCategoryBudget.CategoryBudget.Libelle,
                        TypeBudgetLibelle = r.SousCategoryBudget.CategoryBudget.TypeBudget.Libelle,
                        MandatId = r.MandatId,
                        MandatAnnee = r.Mandat.Annee,
                        ClubId = r.ClubId,
                        ClubNom = r.Club.Name,
                        NombreRealisations = r.Realisations.Count(),
                        MontantRealise = r.MontantRealise,
                        EcartBudgetRealise = r.MontantRealise - (r.PrixUnitaire * r.Quantite), // Calcul correct
                        PourcentageRealisation = (r.PrixUnitaire * r.Quantite) > 0
                            ? Math.Round((double)(r.MontantRealise / (r.PrixUnitaire * r.Quantite)) * 100, 2)
                            : 0,
                        Realisations = r.Realisations.Select(real => new RubriqueBudgetRealiseResumeDto
                        {
                            Id = real.Id,
                            Date = real.Date,
                            Montant = real.Montant,
                            Commentaires = real.Commentaires
                        }).OrderByDescending(real => real.Date).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (rubrique == null)
                {
                    return NotFound($"Rubrique avec l'ID {id} non trouvée pour le club {clubId} et le mandat {mandatId}");
                }

                return Ok(rubrique);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la rubrique {RubriqueId} du club {ClubId}", id, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de la rubrique");
            }
        }

        // POST: api/clubs/{clubId}/mandats/{mandatId}/rubriques
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary,Treasurer")]
        public async Task<ActionResult<RubriqueBudgetDto>> CreateRubrique(
            Guid clubId,
            Guid mandatId,
            [FromBody] CreateRubriqueBudgetRequest request)
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

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return StatusCode(403, "Vous n'avez pas l'autorisation de gérer ce club");
                }

                // Vérifier que le club existe
                var club = await _context.Clubs.FindAsync(clubId);
                if (club == null)
                {
                    return NotFound("Club non trouvé");
                }

                // Vérifier que le mandat existe et appartient au club
                var mandat = await _context.Mandats
                    .FirstOrDefaultAsync(m => m.Id == mandatId && m.ClubId == clubId);
                if (mandat == null)
                {
                    return NotFound("Mandat non trouvé pour ce club");
                }

                // Vérifier que la sous-catégorie existe et appartient au club
                var sousCategory = await _context.SousCategoriesBudget
                    .Include(sc => sc.CategoryBudget)
                        .ThenInclude(c => c.TypeBudget)
                    .FirstOrDefaultAsync(sc => sc.Id == request.SousCategoryBudgetId && sc.ClubId == clubId);
                if (sousCategory == null)
                {
                    return NotFound("Sous-catégorie non trouvée pour ce club");
                }

                // Vérifier l'unicité du libellé dans le club/mandat/sous-catégorie
                var existingRubrique = await _context.RubriquesBudget
                    .AnyAsync(r => r.ClubId == clubId &&
                                 r.MandatId == mandatId &&
                                 r.SousCategoryBudgetId == request.SousCategoryBudgetId &&
                                 r.Libelle.ToLower() == request.Libelle.ToLower());

                if (existingRubrique)
                {
                    return BadRequest($"Une rubrique avec le libellé '{request.Libelle}' existe déjà pour cette sous-catégorie dans ce mandat");
                }

                var rubrique = new RubriqueBudget
                {
                    Id = Guid.NewGuid(),
                    Libelle = request.Libelle,
                    PrixUnitaire = request.PrixUnitaire,
                    Quantite = request.Quantite,
                    MontantRealise = request.MontantRealise ?? 0m, // Valeur par défaut 0 si non spécifiée
                    SousCategoryBudgetId = request.SousCategoryBudgetId,
                    MandatId = mandatId,
                    ClubId = clubId
                };

                _context.RubriquesBudget.Add(rubrique);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Rubrique '{Libelle}' créée pour le club {ClubId} et le mandat {MandatId} avec l'ID {Id}",
                    rubrique.Libelle, clubId, mandatId, rubrique.Id);

                var result = new RubriqueBudgetDto
                {
                    Id = rubrique.Id,
                    Libelle = rubrique.Libelle,
                    PrixUnitaire = rubrique.PrixUnitaire,
                    Quantite = rubrique.Quantite,
                    MontantTotal = rubrique.PrixUnitaire * rubrique.Quantite, // Calcul correct
                    SousCategoryBudgetId = rubrique.SousCategoryBudgetId,
                    SousCategoryLibelle = sousCategory.Libelle,
                    CategoryBudgetLibelle = sousCategory.CategoryBudget.Libelle,
                    TypeBudgetLibelle = sousCategory.CategoryBudget.TypeBudget.Libelle,
                    MandatId = rubrique.MandatId,
                    MandatAnnee = mandat.Annee,
                    ClubId = rubrique.ClubId,
                    ClubNom = club.Name,
                    NombreRealisations = 0,
                    MontantRealise = rubrique.MontantRealise,
                    EcartBudgetRealise = rubrique.MontantRealise - (rubrique.PrixUnitaire * rubrique.Quantite), // Calcul correct
                    PourcentageRealisation = (rubrique.PrixUnitaire * rubrique.Quantite) > 0
                        ? Math.Round((double)(rubrique.MontantRealise / (rubrique.PrixUnitaire * rubrique.Quantite)) * 100, 2)
                        : 0
                };

                return CreatedAtAction(nameof(GetRubrique),
                    new { clubId, mandatId, id = rubrique.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la rubrique pour le club {ClubId}", clubId);
                return StatusCode(500, "Une erreur est survenue lors de la création de la rubrique");
            }
        }

        // PUT: api/clubs/{clubId}/mandats/{mandatId}/rubriques/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary,Treasurer")]
        public async Task<IActionResult> UpdateRubrique(
            Guid clubId,
            Guid mandatId,
            Guid id,
            [FromBody] UpdateRubriqueBudgetRequest request)
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

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la rubrique est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return StatusCode(403, "Vous n'avez pas l'autorisation de gérer ce club");
                }

                var rubrique = await _context.RubriquesBudget
                    .FirstOrDefaultAsync(r => r.Id == id &&
                                            r.ClubId == clubId &&
                                            r.MandatId == mandatId);

                if (rubrique == null)
                {
                    return NotFound($"Rubrique avec l'ID {id} non trouvée pour le club {clubId} et le mandat {mandatId}");
                }

                // Vérifier l'unicité du libellé si modifié
                if (!string.IsNullOrEmpty(request.Libelle) &&
                    request.Libelle.ToLower() != rubrique.Libelle.ToLower())
                {
                    var existingRubrique = await _context.RubriquesBudget
                        .AnyAsync(r => r.Id != id &&
                                     r.ClubId == clubId &&
                                     r.MandatId == mandatId &&
                                     r.SousCategoryBudgetId == (request.SousCategoryBudgetId ?? rubrique.SousCategoryBudgetId) &&
                                     r.Libelle.ToLower() == request.Libelle.ToLower());

                    if (existingRubrique)
                    {
                        return BadRequest($"Une rubrique avec le libellé '{request.Libelle}' existe déjà pour cette sous-catégorie dans ce mandat");
                    }
                }

                // Vérifier que la nouvelle sous-catégorie existe si spécifiée
                if (request.SousCategoryBudgetId.HasValue && request.SousCategoryBudgetId != rubrique.SousCategoryBudgetId)
                {
                    var sousCategoryExists = await _context.SousCategoriesBudget
                        .AnyAsync(sc => sc.Id == request.SousCategoryBudgetId.Value && sc.ClubId == clubId);
                    if (!sousCategoryExists)
                    {
                        return BadRequest("Sous-catégorie non trouvée pour ce club");
                    }
                }

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.Libelle))
                    rubrique.Libelle = request.Libelle;

                if (request.PrixUnitaire.HasValue)
                    rubrique.PrixUnitaire = request.PrixUnitaire.Value;

                if (request.Quantite.HasValue)
                    rubrique.Quantite = request.Quantite.Value;

                if (request.MontantRealise.HasValue)
                    rubrique.MontantRealise = request.MontantRealise.Value;

                if (request.SousCategoryBudgetId.HasValue)
                    rubrique.SousCategoryBudgetId = request.SousCategoryBudgetId.Value;

                _context.Entry(rubrique).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Rubrique {Id} mise à jour dans le club {ClubId}", id, clubId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la rubrique {RubriqueId} du club {ClubId}", id, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de la rubrique");
            }
        }

        // DELETE: api/clubs/{clubId}/mandats/{mandatId}/rubriques/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> DeleteRubrique(
            Guid clubId,
            Guid mandatId,
            Guid id)
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

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la rubrique est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return StatusCode(403, "Vous n'avez pas l'autorisation de gérer ce club");
                }

                var rubrique = await _context.RubriquesBudget
                    .Include(r => r.Realisations)
                    .FirstOrDefaultAsync(r => r.Id == id &&
                                            r.ClubId == clubId &&
                                            r.MandatId == mandatId);

                if (rubrique == null)
                {
                    return NotFound($"Rubrique avec l'ID {id} non trouvée pour le club {clubId} et le mandat {mandatId}");
                }

                // Vérifier s'il y a des réalisations associées
                if (rubrique.Realisations.Any())
                {
                    return BadRequest($"Impossible de supprimer la rubrique '{rubrique.Libelle}' car elle contient des réalisations.");
                }

                _context.RubriquesBudget.Remove(rubrique);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Rubrique '{Libelle}' supprimée du club {ClubId}", rubrique.Libelle, clubId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la rubrique {RubriqueId} du club {ClubId}", id, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de la rubrique");
            }
        }

        // GET: api/clubs/{clubId}/mandats/{mandatId}/rubriques/{id}/realisations
        [HttpGet("{id:guid}/realisations")]
        public async Task<ActionResult<IEnumerable<RubriqueBudgetRealiseResumeDto>>> GetRealisationsRubrique(
            Guid clubId,
            Guid mandatId,
            Guid id)
        {
            try
            {
                // Validation et autorisations
                if (!await CanAccessClub(clubId))
                {
                    return StatusCode(403, "Accès non autorisé à ce club");
                }

                // Vérifier que la rubrique existe
                var rubriqueExists = await _context.RubriquesBudget
                    .AnyAsync(r => r.Id == id && r.ClubId == clubId && r.MandatId == mandatId);

                if (!rubriqueExists)
                {
                    return NotFound($"Rubrique avec l'ID {id} non trouvée");
                }

                // Récupérer les réalisations
                var realisations = await _context.RubriquesBudgetRealisees
                    .Where(r => r.RubriqueBudgetId == id)
                    .OrderByDescending(r => r.Date)
                    .Select(r => new RubriqueBudgetRealiseResumeDto
                    {
                        Id = r.Id,
                        Date = r.Date,
                        Montant = r.Montant,
                        Commentaires = r.Commentaires
                    })
                    .ToListAsync();

                return Ok(realisations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réalisations de la rubrique {RubriqueId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des réalisations");
            }
        }

        // GET: api/clubs/{clubId}/mandats/{mandatId}/rubriques/statistiques
        [HttpGet("statistiques")]
        public async Task<ActionResult<RubriqueBudgetStatistiquesDto>> GetStatistiques(
            Guid clubId,
            Guid mandatId)
        {
            try
            {
                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return StatusCode(403, "Accès non autorisé à ce club");
                }

                var statistiques = await _context.RubriquesBudget
                    .Include(r => r.SousCategoryBudget)
                        .ThenInclude(sc => sc.CategoryBudget)
                            .ThenInclude(c => c.TypeBudget)
                    .Where(r => r.ClubId == clubId && r.MandatId == mandatId)
                    .GroupBy(r => r.SousCategoryBudget.CategoryBudget.TypeBudget.Libelle)
                    .Select(g => new RubriqueBudgetStatistiqueParTypeDto
                    {
                        TypeBudgetLibelle = g.Key,
                        NombreRubriques = g.Count(),
                        MontantTotalBudget = g.Sum(r => r.PrixUnitaire * r.Quantite), // Calcul correct
                        MontantTotalRealise = g.Sum(r => r.MontantRealise)
                    })
                    .ToListAsync();

                var totaux = await _context.RubriquesBudget
                    .Where(r => r.ClubId == clubId && r.MandatId == mandatId)
                    .Select(r => new
                    {
                        MontantBudget = r.PrixUnitaire * r.Quantite, // Calcul correct
                        MontantRealise = r.MontantRealise
                    })
                    .ToListAsync();

                var result = new RubriqueBudgetStatistiquesDto
                {
                    ClubId = clubId,
                    MandatId = mandatId,
                    NombreTotalRubriques = totaux.Count,
                    MontantTotalBudget = totaux.Sum(t => t.MontantBudget),
                    MontantTotalRealise = totaux.Sum(t => t.MontantRealise),
                    EcartTotalBudgetRealise = totaux.Sum(t => t.MontantRealise) - totaux.Sum(t => t.MontantBudget),
                    StatistiquesParType = statistiques.Select(s => new RubriqueBudgetStatistiqueParTypeDto
                    {
                        TypeBudgetLibelle = s.TypeBudgetLibelle,
                        NombreRubriques = s.NombreRubriques,
                        MontantTotalBudget = s.MontantTotalBudget,
                        MontantTotalRealise = s.MontantTotalRealise,
                        EcartBudgetRealise = s.MontantTotalRealise - s.MontantTotalBudget
                    }).OrderBy(s => s.TypeBudgetLibelle).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques des rubriques du club {ClubId}", clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
            }
        }

        // Méthodes d'aide
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

            return User.IsInRole("President") || User.IsInRole("Secretary") || User.IsInRole("Treasurer");
        }
    }

    // DTOs pour les rubriques de budget
    public class RubriqueBudgetDto
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public decimal PrixUnitaire { get; set; }
        public int Quantite { get; set; }
        public decimal MontantTotal { get; set; }
        public Guid SousCategoryBudgetId { get; set; }
        public string SousCategoryLibelle { get; set; } = string.Empty;
        public string CategoryBudgetLibelle { get; set; } = string.Empty;
        public string TypeBudgetLibelle { get; set; } = string.Empty;
        public Guid MandatId { get; set; }
        public int MandatAnnee { get; set; }
        public Guid ClubId { get; set; }
        public string ClubNom { get; set; } = string.Empty;
        public int NombreRealisations { get; set; }
        public decimal MontantRealise { get; set; }
        public decimal EcartBudgetRealise { get; set; }
        public double PourcentageRealisation { get; set; } // Nouveau champ ajouté
    }

    public class RubriqueBudgetDetailDto : RubriqueBudgetDto
    {
        public List<RubriqueBudgetRealiseResumeDto> Realisations { get; set; } = new List<RubriqueBudgetRealiseResumeDto>();
    }

    public class RubriqueBudgetRealiseResumeDto
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Montant { get; set; }
        public string? Commentaires { get; set; }
    }

    public class CreateRubriqueBudgetRequest
    {
        [Required(ErrorMessage = "Le libellé est obligatoire")]
        [MaxLength(200, ErrorMessage = "Le libellé ne peut pas dépasser 200 caractères")]
        public string Libelle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prix unitaire est obligatoire")]
        [Range(0, double.MaxValue, ErrorMessage = "Le prix unitaire doit être positif")]
        public decimal PrixUnitaire { get; set; }

        [Required(ErrorMessage = "La quantité est obligatoire")]
        [Range(1, int.MaxValue, ErrorMessage = "La quantité doit être supérieure à 0")]
        public int Quantite { get; set; } = 1;

        [Range(0, double.MaxValue, ErrorMessage = "Le montant réalisé doit être positif ou nul")]
        public decimal? MontantRealise { get; set; }

        [Required(ErrorMessage = "La sous-catégorie est obligatoire")]
        public Guid SousCategoryBudgetId { get; set; }
    }

    public class UpdateRubriqueBudgetRequest
    {
        [MaxLength(200, ErrorMessage = "Le libellé ne peut pas dépasser 200 caractères")]
        public string? Libelle { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Le prix unitaire doit être positif")]
        public decimal? PrixUnitaire { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La quantité doit être supérieure à 0")]
        public int? Quantite { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Le montant réalisé doit être positif ou nul")]
        public decimal? MontantRealise { get; set; }

        public Guid? SousCategoryBudgetId { get; set; }
    }

    public class RubriqueBudgetStatistiquesDto
    {
        public Guid ClubId { get; set; }
        public Guid MandatId { get; set; }
        public int NombreTotalRubriques { get; set; }
        public decimal MontantTotalBudget { get; set; }
        public decimal MontantTotalRealise { get; set; }
        public decimal EcartTotalBudgetRealise { get; set; }
        public List<RubriqueBudgetStatistiqueParTypeDto> StatistiquesParType { get; set; } = new List<RubriqueBudgetStatistiqueParTypeDto>();
    }

    public class RubriqueBudgetStatistiqueParTypeDto
    {
        public string TypeBudgetLibelle { get; set; } = string.Empty;
        public int NombreRubriques { get; set; }
        public decimal MontantTotalBudget { get; set; }
        public decimal MontantTotalRealise { get; set; }
        public decimal EcartBudgetRealise { get; set; }
    }
}