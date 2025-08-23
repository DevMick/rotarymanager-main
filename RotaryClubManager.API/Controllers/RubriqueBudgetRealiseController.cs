using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/clubs/{clubId}/rubriques/{rubriqueId}/realisations")]
    [ApiController]
    [Authorize]
    public class RubriqueBudgetRealiseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RubriqueBudgetRealiseController> _logger;

        public RubriqueBudgetRealiseController(
            ApplicationDbContext context,
            ILogger<RubriqueBudgetRealiseController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/clubs/{clubId}/rubriques/{rubriqueId}/realisations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RubriqueBudgetRealiseDto>>> GetRealisations(
            Guid clubId,
            Guid rubriqueId,
            [FromQuery] DateTime? dateDebut = null,
            [FromQuery] DateTime? dateFin = null,
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

                if (rubriqueId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la rubrique est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                // Vérifier que la rubrique existe et appartient au club
                var rubrique = await _context.RubriquesBudget
                    .Include(r => r.SousCategoryBudget)
                        .ThenInclude(sc => sc.CategoryBudget)
                            .ThenInclude(c => c.TypeBudget)
                    .Include(r => r.Mandat)
                    .Include(r => r.Club)
                    .FirstOrDefaultAsync(r => r.Id == rubriqueId && r.ClubId == clubId);

                if (rubrique == null)
                {
                    return NotFound($"Rubrique avec l'ID {rubriqueId} non trouvée pour le club {clubId}");
                }

                var query = _context.RubriquesBudgetRealisees
                    .Where(r => r.RubriqueBudgetId == rubriqueId);

                // Filtres optionnels par date
                if (dateDebut.HasValue)
                {
                    query = query.Where(r => r.Date >= dateDebut.Value.Date);
                }

                if (dateFin.HasValue)
                {
                    query = query.Where(r => r.Date <= dateFin.Value.Date);
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var totalPages = Math.Ceiling((double)totalItems / pageSize);

                var realisations = await query
                    .OrderByDescending(r => r.Date)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new RubriqueBudgetRealiseDto
                    {
                        Id = r.Id,
                        Date = r.Date,
                        Montant = r.Montant,
                        Commentaires = r.Commentaires,
                        RubriqueBudgetId = r.RubriqueBudgetId,
                        RubriqueLibelle = rubrique.Libelle,
                        SousCategoryLibelle = rubrique.SousCategoryBudget.Libelle,
                        CategoryBudgetLibelle = rubrique.SousCategoryBudget.CategoryBudget.Libelle,
                        TypeBudgetLibelle = rubrique.SousCategoryBudget.CategoryBudget.TypeBudget.Libelle,
                        MandatAnnee = rubrique.Mandat.Annee,
                        ClubNom = rubrique.Club.Name
                    })
                    .ToListAsync();

                // Headers de pagination
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());

                return Ok(realisations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réalisations de la rubrique {RubriqueId} du club {ClubId}", rubriqueId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des réalisations");
            }
        }

        // GET: api/clubs/{clubId}/rubriques/{rubriqueId}/realisations/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<RubriqueBudgetRealiseDetailDto>> GetRealisation(
            Guid clubId,
            Guid rubriqueId,
            Guid id)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (rubriqueId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la rubrique est invalide");
                }

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la réalisation est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                var realisation = await _context.RubriquesBudgetRealisees
                    .Include(r => r.RubriqueBudget)
                        .ThenInclude(rb => rb.SousCategoryBudget)
                            .ThenInclude(sc => sc.CategoryBudget)
                                .ThenInclude(c => c.TypeBudget)
                    .Include(r => r.RubriqueBudget)
                        .ThenInclude(rb => rb.Mandat)
                    .Include(r => r.RubriqueBudget)
                        .ThenInclude(rb => rb.Club)
                    .Where(r => r.Id == id &&
                              r.RubriqueBudgetId == rubriqueId &&
                              r.RubriqueBudget.ClubId == clubId)
                    .Select(r => new RubriqueBudgetRealiseDetailDto
                    {
                        Id = r.Id,
                        Date = r.Date,
                        Montant = r.Montant,
                        Commentaires = r.Commentaires,
                        RubriqueBudgetId = r.RubriqueBudgetId,
                        RubriqueLibelle = r.RubriqueBudget.Libelle,
                        RubriquePrixUnitaire = r.RubriqueBudget.PrixUnitaire,
                        RubriqueQuantite = r.RubriqueBudget.Quantite,
                        RubriqueMontantTotal = r.RubriqueBudget.MontantTotal,
                        SousCategoryLibelle = r.RubriqueBudget.SousCategoryBudget.Libelle,
                        CategoryBudgetLibelle = r.RubriqueBudget.SousCategoryBudget.CategoryBudget.Libelle,
                        TypeBudgetLibelle = r.RubriqueBudget.SousCategoryBudget.CategoryBudget.TypeBudget.Libelle,
                        MandatAnnee = r.RubriqueBudget.Mandat.Annee,
                        ClubNom = r.RubriqueBudget.Club.Name
                    })
                    .FirstOrDefaultAsync();

                if (realisation == null)
                {
                    return NotFound($"Réalisation avec l'ID {id} non trouvée pour la rubrique {rubriqueId} du club {clubId}");
                }

                return Ok(realisation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la réalisation {RealisationId} de la rubrique {RubriqueId}", id, rubriqueId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de la réalisation");
            }
        }

        // POST: api/clubs/{clubId}/rubriques/{rubriqueId}/realisations
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary,Treasurer")]
        public async Task<ActionResult<RubriqueBudgetRealiseDto>> CreateRealisation(
            Guid clubId,
            Guid rubriqueId,
            [FromBody] CreateRubriqueBudgetRealiseRequest request)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (rubriqueId == Guid.Empty)
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
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                // Vérifier que la rubrique existe et appartient au club
                var rubrique = await _context.RubriquesBudget
                    .Include(r => r.SousCategoryBudget)
                        .ThenInclude(sc => sc.CategoryBudget)
                            .ThenInclude(c => c.TypeBudget)
                    .Include(r => r.Mandat)
                    .Include(r => r.Club)
                    .FirstOrDefaultAsync(r => r.Id == rubriqueId && r.ClubId == clubId);

                if (rubrique == null)
                {
                    return NotFound($"Rubrique avec l'ID {rubriqueId} non trouvée pour le club {clubId}");
                }

                // Vérifier que la date est dans la période du mandat
                if (request.Date < rubrique.Mandat.DateDebut || request.Date > rubrique.Mandat.DateFin)
                {
                    return BadRequest($"La date de réalisation doit être comprise entre {rubrique.Mandat.DateDebut:dd/MM/yyyy} et {rubrique.Mandat.DateFin:dd/MM/yyyy} (période du mandat {rubrique.Mandat.Annee})");
                }

                var realisation = new RubriqueBudgetRealise
                {
                    Id = Guid.NewGuid(),
                    Date = request.Date.Date,
                    Montant = request.Montant,
                    Commentaires = request.Commentaires,
                    RubriqueBudgetId = rubriqueId
                };

                _context.RubriquesBudgetRealisees.Add(realisation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Réalisation de {Montant}€ créée pour la rubrique {RubriqueId} du club {ClubId} à la date {Date}",
                    realisation.Montant, rubriqueId, clubId, realisation.Date);

                var result = new RubriqueBudgetRealiseDto
                {
                    Id = realisation.Id,
                    Date = realisation.Date,
                    Montant = realisation.Montant,
                    Commentaires = realisation.Commentaires,
                    RubriqueBudgetId = realisation.RubriqueBudgetId,
                    RubriqueLibelle = rubrique.Libelle,
                    SousCategoryLibelle = rubrique.SousCategoryBudget.Libelle,
                    CategoryBudgetLibelle = rubrique.SousCategoryBudget.CategoryBudget.Libelle,
                    TypeBudgetLibelle = rubrique.SousCategoryBudget.CategoryBudget.TypeBudget.Libelle,
                    MandatAnnee = rubrique.Mandat.Annee,
                    ClubNom = rubrique.Club.Name
                };

                return CreatedAtAction(nameof(GetRealisation),
                    new { clubId, rubriqueId, id = realisation.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la réalisation pour la rubrique {RubriqueId} du club {ClubId}", rubriqueId, clubId);
                return StatusCode(500, "Une erreur est survenue lors de la création de la réalisation");
            }
        }

        // PUT: api/clubs/{clubId}/rubriques/{rubriqueId}/realisations/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary,Treasurer")]
        public async Task<IActionResult> UpdateRealisation(
            Guid clubId,
            Guid rubriqueId,
            Guid id,
            [FromBody] UpdateRubriqueBudgetRealiseRequest request)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (rubriqueId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la rubrique est invalide");
                }

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la réalisation est invalide");
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

                var realisation = await _context.RubriquesBudgetRealisees
                    .Include(r => r.RubriqueBudget)
                        .ThenInclude(rb => rb.Mandat)
                    .FirstOrDefaultAsync(r => r.Id == id &&
                                            r.RubriqueBudgetId == rubriqueId &&
                                            r.RubriqueBudget.ClubId == clubId);

                if (realisation == null)
                {
                    return NotFound($"Réalisation avec l'ID {id} non trouvée pour la rubrique {rubriqueId} du club {clubId}");
                }

                // Vérifier que la nouvelle date est dans la période du mandat si modifiée
                if (request.Date.HasValue)
                {
                    var nouvelleDate = request.Date.Value.Date;
                    if (nouvelleDate < realisation.RubriqueBudget.Mandat.DateDebut ||
                        nouvelleDate > realisation.RubriqueBudget.Mandat.DateFin)
                    {
                        return BadRequest($"La date de réalisation doit être comprise entre {realisation.RubriqueBudget.Mandat.DateDebut:dd/MM/yyyy} et {realisation.RubriqueBudget.Mandat.DateFin:dd/MM/yyyy} (période du mandat {realisation.RubriqueBudget.Mandat.Annee})");
                    }
                }

                // Mettre à jour les propriétés
                if (request.Date.HasValue)
                    realisation.Date = request.Date.Value.Date;

                if (request.Montant.HasValue)
                    realisation.Montant = request.Montant.Value;

                if (request.Commentaires != null)
                    realisation.Commentaires = request.Commentaires;

                _context.Entry(realisation).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Réalisation {Id} mise à jour pour la rubrique {RubriqueId} du club {ClubId}", id, rubriqueId, clubId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la réalisation {RealisationId} de la rubrique {RubriqueId}", id, rubriqueId);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de la réalisation");
            }
        }

        // DELETE: api/clubs/{clubId}/rubriques/{rubriqueId}/realisations/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary,Treasurer")]
        public async Task<IActionResult> DeleteRealisation(
            Guid clubId,
            Guid rubriqueId,
            Guid id)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (rubriqueId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la rubrique est invalide");
                }

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la réalisation est invalide");
                }

                // Vérifier les autorisations
                if (!await CanManageClub(clubId))
                {
                    return Forbid("Vous n'avez pas l'autorisation de gérer ce club");
                }

                var realisation = await _context.RubriquesBudgetRealisees
                    .Include(r => r.RubriqueBudget)
                    .FirstOrDefaultAsync(r => r.Id == id &&
                                            r.RubriqueBudgetId == rubriqueId &&
                                            r.RubriqueBudget.ClubId == clubId);

                if (realisation == null)
                {
                    return NotFound($"Réalisation avec l'ID {id} non trouvée pour la rubrique {rubriqueId} du club {clubId}");
                }

                _context.RubriquesBudgetRealisees.Remove(realisation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Réalisation de {Montant}€ supprimée de la rubrique {RubriqueId} du club {ClubId}",
                    realisation.Montant, rubriqueId, clubId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la réalisation {RealisationId} de la rubrique {RubriqueId}", id, rubriqueId);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de la réalisation");
            }
        }

        // GET: api/clubs/{clubId}/rubriques/{rubriqueId}/realisations/statistiques
        [HttpGet("statistiques")]
        public async Task<ActionResult<RubriqueBudgetRealiseStatistiquesDto>> GetStatistiques(
            Guid clubId,
            Guid rubriqueId,
            [FromQuery] int? annee = null)
        {
            try
            {
                // Validation des paramètres
                if (clubId == Guid.Empty)
                {
                    return BadRequest("L'identifiant du club est invalide");
                }

                if (rubriqueId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la rubrique est invalide");
                }

                // Vérifier les autorisations
                if (!await CanAccessClub(clubId))
                {
                    return Forbid("Accès non autorisé à ce club");
                }

                // Vérifier que la rubrique existe
                var rubrique = await _context.RubriquesBudget
                    .Include(r => r.Mandat)
                    .FirstOrDefaultAsync(r => r.Id == rubriqueId && r.ClubId == clubId);

                if (rubrique == null)
                {
                    return NotFound($"Rubrique avec l'ID {rubriqueId} non trouvée pour le club {clubId}");
                }

                var query = _context.RubriquesBudgetRealisees
                    .Where(r => r.RubriqueBudgetId == rubriqueId);

                // Filtre optionnel par année
                if (annee.HasValue)
                {
                    query = query.Where(r => r.Date.Year == annee.Value);
                }

                var realisations = await query.ToListAsync();

                // Calculs sécurisés avec vérifications nulles
                var montantTotalRealise = realisations.Sum(r => r.Montant);
                var nombreTotalRealisations = realisations.Count;
                var montantMoyenRealisation = nombreTotalRealisations > 0 ? realisations.Average(r => r.Montant) : 0;
                var ecartBudgetRealise = montantTotalRealise - rubrique.MontantTotal;

                // Calcul sécurisé du pourcentage
                double pourcentageRealisation = 0;
                if (rubrique.MontantTotal > 0)
                {
                    pourcentageRealisation = Math.Round((double)(montantTotalRealise / rubrique.MontantTotal) * 100, 2);
                }

                // Statistiques par mois avec calculs sécurisés
                var statistiquesParMois = realisations
                    .GroupBy(r => new { r.Date.Year, r.Date.Month })
                    .Select(g => {
                        var montants = g.Select(r => r.Montant).ToList();
                        return new RubriqueBudgetRealiseStatistiqueMensuelleDto
                        {
                            Annee = g.Key.Year,
                            Mois = g.Key.Month,
                            NombreRealisations = g.Count(),
                            MontantTotal = montants.Sum(),
                            MontantMoyen = montants.Any() ? montants.Average() : 0,
                            MontantMin = montants.Any() ? montants.Min() : 0,
                            MontantMax = montants.Any() ? montants.Max() : 0
                        };
                    })
                    .OrderBy(s => s.Annee)
                    .ThenBy(s => s.Mois)
                    .ToList();

                var result = new RubriqueBudgetRealiseStatistiquesDto
                {
                    ClubId = clubId,
                    RubriqueBudgetId = rubriqueId,
                    MontantBudgete = rubrique.MontantTotal,
                    NombreTotalRealisations = nombreTotalRealisations,
                    MontantTotalRealise = montantTotalRealise,
                    MontantMoyenRealisation = montantMoyenRealisation,
                    EcartBudgetRealise = ecartBudgetRealise,
                    PourcentageRealisation = pourcentageRealisation,
                    StatistiquesParMois = statistiquesParMois
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques de la rubrique {RubriqueId} du club {ClubId}", rubriqueId, clubId);
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

    // DTOs pour les réalisations de budget
    public class RubriqueBudgetRealiseDto
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Montant { get; set; }
        public string? Commentaires { get; set; }
        public Guid RubriqueBudgetId { get; set; }
        public string RubriqueLibelle { get; set; } = string.Empty;
        public string SousCategoryLibelle { get; set; } = string.Empty;
        public string CategoryBudgetLibelle { get; set; } = string.Empty;
        public string TypeBudgetLibelle { get; set; } = string.Empty;
        public int MandatAnnee { get; set; }
        public string ClubNom { get; set; } = string.Empty;
    }

    public class RubriqueBudgetRealiseDetailDto : RubriqueBudgetRealiseDto
    {
        public decimal RubriquePrixUnitaire { get; set; }
        public int RubriqueQuantite { get; set; }
        public decimal RubriqueMontantTotal { get; set; }
    }

    public class CreateRubriqueBudgetRealiseRequest
    {
        [Required(ErrorMessage = "La date est obligatoire")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Le montant est obligatoire")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        public decimal Montant { get; set; }

        [MaxLength(500, ErrorMessage = "Les commentaires ne peuvent pas dépasser 500 caractères")]
        public string? Commentaires { get; set; }
    }

    public class UpdateRubriqueBudgetRealiseRequest
    {
        public DateTime? Date { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        public decimal? Montant { get; set; }

        [MaxLength(500, ErrorMessage = "Les commentaires ne peuvent pas dépasser 500 caractères")]
        public string? Commentaires { get; set; }
    }

    public class RubriqueBudgetRealiseStatistiquesDto
    {
        public Guid ClubId { get; set; }
        public Guid RubriqueBudgetId { get; set; }
        public decimal MontantBudgete { get; set; }
        public int NombreTotalRealisations { get; set; }
        public decimal MontantTotalRealise { get; set; }
        public decimal MontantMoyenRealisation { get; set; }
        public decimal EcartBudgetRealise { get; set; }
        public double PourcentageRealisation { get; set; }
        public List<RubriqueBudgetRealiseStatistiqueMensuelleDto> StatistiquesParMois { get; set; } = new List<RubriqueBudgetRealiseStatistiqueMensuelleDto>();
    }

    public class RubriqueBudgetRealiseStatistiqueMensuelleDto
    {
        public int Annee { get; set; }
        public int Mois { get; set; }
        public int NombreRealisations { get; set; }
        public decimal MontantTotal { get; set; }
        public decimal MontantMoyen { get; set; }
        public decimal MontantMin { get; set; }
        public decimal MontantMax { get; set; }
    }
}