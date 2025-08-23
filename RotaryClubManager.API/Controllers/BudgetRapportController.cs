using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/clubs/{clubId}/mandats/{mandatId}/budget-rapport")]
    [ApiController]
    [Authorize]
    public class BudgetRapportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BudgetRapportController> _logger;

        public BudgetRapportController(
            ApplicationDbContext context,
            ILogger<BudgetRapportController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/clubs/{clubId}/mandats/{mandatId}/budget-rapport
        [HttpGet]
        public async Task<ActionResult<BudgetRapportCompletDto>> GetRapportComplet(
            Guid clubId,
            Guid mandatId,
            [FromQuery] Guid? typeBudgetId = null,
            [FromQuery] Guid? categoryBudgetId = null,
            [FromQuery] Guid? sousCategoryBudgetId = null,
            [FromQuery] string? recherche = null,
            [FromQuery] string? orderBy = "typebudget", // typebudget, categorie, souscategorie, rubrique, montantprevu, montantrealise, ecart, pourcentage
            [FromQuery] string? orderDirection = "asc", // asc, desc
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
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

                // Vérifier que le mandat existe et appartient au club
                var mandatExists = await _context.Mandats
                    .AnyAsync(m => m.Id == mandatId && m.ClubId == clubId);
                if (!mandatExists)
                {
                    return NotFound("Mandat non trouvé pour ce club");
                }

                // Construction de la requête principale
                var query = _context.RubriquesBudget
                    .Include(rb => rb.SousCategoryBudget)
                        .ThenInclude(scb => scb.CategoryBudget)
                            .ThenInclude(cb => cb.TypeBudget)
                    .Include(rb => rb.Mandat)
                    .Include(rb => rb.Club)
                    .Where(rb => rb.ClubId == clubId && rb.MandatId == mandatId);

                // Filtres optionnels
                if (typeBudgetId.HasValue)
                {
                    query = query.Where(rb => rb.SousCategoryBudget.CategoryBudget.TypeBudgetId == typeBudgetId.Value);
                }

                if (categoryBudgetId.HasValue)
                {
                    query = query.Where(rb => rb.SousCategoryBudget.CategoryBudgetId == categoryBudgetId.Value);
                }

                if (sousCategoryBudgetId.HasValue)
                {
                    query = query.Where(rb => rb.SousCategoryBudgetId == sousCategoryBudgetId.Value);
                }

                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(rb => rb.Libelle.ToLower().Contains(termeLower) ||
                                            rb.SousCategoryBudget.Libelle.ToLower().Contains(termeLower) ||
                                            rb.SousCategoryBudget.CategoryBudget.Libelle.ToLower().Contains(termeLower) ||
                                            rb.SousCategoryBudget.CategoryBudget.TypeBudget.Libelle.ToLower().Contains(termeLower));
                }

                // Projection vers le DTO pour éviter les problèmes de performance
                var queryDto = query.Select(rb => new BudgetRapportLigneDto
                {
                    RubriqueId = rb.Id,
                    TypeBudget = rb.SousCategoryBudget.CategoryBudget.TypeBudget.Libelle,
                    Categorie = rb.SousCategoryBudget.CategoryBudget.Libelle,
                    SousCategorie = rb.SousCategoryBudget.Libelle,
                    Rubrique = rb.Libelle,
                    PrixUnitaire = rb.PrixUnitaire,
                    Quantite = rb.Quantite,
                    MontantPrevu = rb.PrixUnitaire * rb.Quantite,
                    MontantRealise = rb.MontantRealise,
                    Ecart = rb.MontantRealise - (rb.PrixUnitaire * rb.Quantite),
                    PourcentageRealisation = (rb.PrixUnitaire * rb.Quantite) > 0
                        ? Math.Round((double)(rb.MontantRealise / (rb.PrixUnitaire * rb.Quantite)) * 100, 2)
                        : 0,
                    MandatId = rb.MandatId,
                    ClubId = rb.ClubId
                });

                // Tri
                queryDto = orderBy?.ToLower() switch
                {
                    "categorie" => orderDirection?.ToLower() == "desc"
                        ? queryDto.OrderByDescending(x => x.Categorie)
                        : queryDto.OrderBy(x => x.Categorie),
                    "souscategorie" => orderDirection?.ToLower() == "desc"
                        ? queryDto.OrderByDescending(x => x.SousCategorie)
                        : queryDto.OrderBy(x => x.SousCategorie),
                    "rubrique" => orderDirection?.ToLower() == "desc"
                        ? queryDto.OrderByDescending(x => x.Rubrique)
                        : queryDto.OrderBy(x => x.Rubrique),
                    "montantprevu" => orderDirection?.ToLower() == "desc"
                        ? queryDto.OrderByDescending(x => x.MontantPrevu)
                        : queryDto.OrderBy(x => x.MontantPrevu),
                    "montantrealise" => orderDirection?.ToLower() == "desc"
                        ? queryDto.OrderByDescending(x => x.MontantRealise)
                        : queryDto.OrderBy(x => x.MontantRealise),
                    "ecart" => orderDirection?.ToLower() == "desc"
                        ? queryDto.OrderByDescending(x => x.Ecart)
                        : queryDto.OrderBy(x => x.Ecart),
                    "pourcentage" => orderDirection?.ToLower() == "desc"
                        ? queryDto.OrderByDescending(x => x.PourcentageRealisation)
                        : queryDto.OrderBy(x => x.PourcentageRealisation),
                    _ => orderDirection?.ToLower() == "desc"
                        ? queryDto.OrderByDescending(x => x.TypeBudget).ThenByDescending(x => x.Categorie).ThenByDescending(x => x.SousCategorie).ThenByDescending(x => x.Rubrique)
                        : queryDto.OrderBy(x => x.TypeBudget).ThenBy(x => x.Categorie).ThenBy(x => x.SousCategorie).ThenBy(x => x.Rubrique)
                };

                // Pagination
                var totalItems = await queryDto.CountAsync();
                var totalPages = Math.Ceiling((double)totalItems / pageSize);

                var items = await queryDto
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Calcul des totaux
                var totauxQuery = _context.RubriquesBudget
                    .Where(rb => rb.ClubId == clubId && rb.MandatId == mandatId);

                // Appliquer les mêmes filtres pour les totaux
                if (typeBudgetId.HasValue)
                {
                    totauxQuery = totauxQuery.Where(rb => rb.SousCategoryBudget.CategoryBudget.TypeBudgetId == typeBudgetId.Value);
                }
                if (categoryBudgetId.HasValue)
                {
                    totauxQuery = totauxQuery.Where(rb => rb.SousCategoryBudget.CategoryBudgetId == categoryBudgetId.Value);
                }
                if (sousCategoryBudgetId.HasValue)
                {
                    totauxQuery = totauxQuery.Where(rb => rb.SousCategoryBudgetId == sousCategoryBudgetId.Value);
                }

                var totaux = await totauxQuery
                    .GroupBy(rb => 1)
                    .Select(g => new
                    {
                        TotalMontantPrevu = g.Sum(rb => rb.PrixUnitaire * rb.Quantite),
                        TotalMontantRealise = g.Sum(rb => rb.MontantRealise),
                        NombreRubriques = g.Count()
                    })
                    .FirstOrDefaultAsync();

                var totauxCalcules = totaux ?? new { TotalMontantPrevu = 0m, TotalMontantRealise = 0m, NombreRubriques = 0 };

                // Statistiques par type de budget
                var statistiquesParType = await _context.RubriquesBudget
                    .Include(rb => rb.SousCategoryBudget.CategoryBudget.TypeBudget)
                    .Where(rb => rb.ClubId == clubId && rb.MandatId == mandatId)
                    .GroupBy(rb => new {
                        TypeBudgetId = rb.SousCategoryBudget.CategoryBudget.TypeBudgetId,
                        TypeBudgetLibelle = rb.SousCategoryBudget.CategoryBudget.TypeBudget.Libelle
                    })
                    .Select(g => new BudgetRapportStatistiqueParTypeDto
                    {
                        TypeBudgetId = g.Key.TypeBudgetId,
                        TypeBudgetLibelle = g.Key.TypeBudgetLibelle,
                        NombreRubriques = g.Count(),
                        MontantTotalPrevu = g.Sum(rb => rb.PrixUnitaire * rb.Quantite),
                        MontantTotalRealise = g.Sum(rb => rb.MontantRealise),
                        EcartTotal = g.Sum(rb => rb.MontantRealise - (rb.PrixUnitaire * rb.Quantite))
                    })
                    .OrderBy(s => s.TypeBudgetLibelle)
                    .ToListAsync();

                var result = new BudgetRapportCompletDto
                {
                    ClubId = clubId,
                    MandatId = mandatId,
                    TotalItems = totalItems,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)totalPages,
                    NombreRubriques = totauxCalcules.NombreRubriques,
                    TotalMontantPrevu = totauxCalcules.TotalMontantPrevu,
                    TotalMontantRealise = totauxCalcules.TotalMontantRealise,
                    TotalEcart = totauxCalcules.TotalMontantRealise - totauxCalcules.TotalMontantPrevu,
                    PourcentageRealisationGlobal = totauxCalcules.TotalMontantPrevu > 0
                        ? Math.Round((double)(totauxCalcules.TotalMontantRealise / totauxCalcules.TotalMontantPrevu) * 100, 2)
                        : 0,
                    Lignes = items,
                    StatistiquesParType = statistiquesParType
                };

                // Headers de pagination
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du rapport de budget pour le club {ClubId} et le mandat {MandatId}", clubId, mandatId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du rapport de budget");
            }
        }

        // GET: api/clubs/{clubId}/mandats/{mandatId}/budget-rapport/export
        [HttpGet("export")]
        public async Task<ActionResult<IEnumerable<BudgetRapportLigneDto>>> ExportRapportComplet(
            Guid clubId,
            Guid mandatId,
            [FromQuery] Guid? typeBudgetId = null,
            [FromQuery] Guid? categoryBudgetId = null,
            [FromQuery] Guid? sousCategoryBudgetId = null,
            [FromQuery] string? recherche = null)
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

                // Construction de la requête principale (même logique que GET principal)
                var query = _context.RubriquesBudget
                    .Include(rb => rb.SousCategoryBudget)
                        .ThenInclude(scb => scb.CategoryBudget)
                            .ThenInclude(cb => cb.TypeBudget)
                    .Where(rb => rb.ClubId == clubId && rb.MandatId == mandatId);

                // Appliquer les filtres
                if (typeBudgetId.HasValue)
                {
                    query = query.Where(rb => rb.SousCategoryBudget.CategoryBudget.TypeBudgetId == typeBudgetId.Value);
                }

                if (categoryBudgetId.HasValue)
                {
                    query = query.Where(rb => rb.SousCategoryBudget.CategoryBudgetId == categoryBudgetId.Value);
                }

                if (sousCategoryBudgetId.HasValue)
                {
                    query = query.Where(rb => rb.SousCategoryBudgetId == sousCategoryBudgetId.Value);
                }

                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(rb => rb.Libelle.ToLower().Contains(termeLower) ||
                                            rb.SousCategoryBudget.Libelle.ToLower().Contains(termeLower) ||
                                            rb.SousCategoryBudget.CategoryBudget.Libelle.ToLower().Contains(termeLower) ||
                                            rb.SousCategoryBudget.CategoryBudget.TypeBudget.Libelle.ToLower().Contains(termeLower));
                }

                var items = await query
                    .OrderBy(rb => rb.SousCategoryBudget.CategoryBudget.TypeBudget.Libelle)
                    .ThenBy(rb => rb.SousCategoryBudget.CategoryBudget.Libelle)
                    .ThenBy(rb => rb.SousCategoryBudget.Libelle)
                    .ThenBy(rb => rb.Libelle)
                    .Select(rb => new BudgetRapportLigneDto
                    {
                        RubriqueId = rb.Id,
                        TypeBudget = rb.SousCategoryBudget.CategoryBudget.TypeBudget.Libelle,
                        Categorie = rb.SousCategoryBudget.CategoryBudget.Libelle,
                        SousCategorie = rb.SousCategoryBudget.Libelle,
                        Rubrique = rb.Libelle,
                        PrixUnitaire = rb.PrixUnitaire,
                        Quantite = rb.Quantite,
                        MontantPrevu = rb.PrixUnitaire * rb.Quantite,
                        MontantRealise = rb.MontantRealise,
                        Ecart = rb.MontantRealise - (rb.PrixUnitaire * rb.Quantite),
                        PourcentageRealisation = (rb.PrixUnitaire * rb.Quantite) > 0
                            ? Math.Round((double)(rb.MontantRealise / (rb.PrixUnitaire * rb.Quantite)) * 100, 2)
                            : 0,
                        MandatId = rb.MandatId,
                        ClubId = rb.ClubId
                    })
                    .ToListAsync();

                // Header pour indiquer que c'est un export
                Response.Headers.Add("Content-Disposition", $"attachment; filename=budget_rapport_{clubId}_{mandatId}_{DateTime.Now:yyyyMMdd}.json");

                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export du rapport de budget pour le club {ClubId} et le mandat {MandatId}", clubId, mandatId);
                return StatusCode(500, "Une erreur est survenue lors de l'export du rapport de budget");
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
    }

    // DTOs pour le rapport de budget
    public class BudgetRapportCompletDto
    {
        public Guid ClubId { get; set; }
        public Guid MandatId { get; set; }
        public int TotalItems { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int NombreRubriques { get; set; }
        public decimal TotalMontantPrevu { get; set; }
        public decimal TotalMontantRealise { get; set; }
        public decimal TotalEcart { get; set; }
        public double PourcentageRealisationGlobal { get; set; }
        public List<BudgetRapportLigneDto> Lignes { get; set; } = new List<BudgetRapportLigneDto>();
        public List<BudgetRapportStatistiqueParTypeDto> StatistiquesParType { get; set; } = new List<BudgetRapportStatistiqueParTypeDto>();
    }

    public class BudgetRapportLigneDto
    {
        public Guid RubriqueId { get; set; }
        public string TypeBudget { get; set; } = string.Empty;
        public string Categorie { get; set; } = string.Empty;
        public string SousCategorie { get; set; } = string.Empty;
        public string Rubrique { get; set; } = string.Empty;
        public decimal PrixUnitaire { get; set; }
        public int Quantite { get; set; }
        public decimal MontantPrevu { get; set; }
        public decimal MontantRealise { get; set; }
        public decimal Ecart { get; set; }
        public double PourcentageRealisation { get; set; }
        public Guid MandatId { get; set; }
        public Guid ClubId { get; set; }
    }

    public class BudgetRapportStatistiqueParTypeDto
    {
        public Guid TypeBudgetId { get; set; }
        public string TypeBudgetLibelle { get; set; } = string.Empty;
        public int NombreRubriques { get; set; }
        public decimal MontantTotalPrevu { get; set; }
        public decimal MontantTotalRealise { get; set; }
        public decimal EcartTotal { get; set; }
        public double PourcentageRealisation => MontantTotalPrevu > 0
            ? Math.Round((double)(MontantTotalRealise / MontantTotalPrevu) * 100, 2)
            : 0;
    }
}