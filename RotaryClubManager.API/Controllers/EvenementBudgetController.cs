using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EvenementBudgetController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EvenementBudgetController> _logger;

        public EvenementBudgetController(ApplicationDbContext context, ILogger<EvenementBudgetController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/EvenementBudget
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetEvenementBudgets(
            [FromQuery] Guid? evenementId = null,
            [FromQuery] string? libelle = null,
            [FromQuery] decimal? montantMin = null,
            [FromQuery] decimal? montantMax = null,
            [FromQuery] bool? depassementBudget = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.EvenementBudgets
                    .Include(b => b.Evenement)
                    .AsQueryable();

                // Filtres
                if (evenementId.HasValue)
                {
                    query = query.Where(b => b.EvenementId == evenementId.Value);
                }

                if (!string.IsNullOrEmpty(libelle))
                {
                    query = query.Where(b => b.Libelle.Contains(libelle));
                }

                if (montantMin.HasValue)
                {
                    query = query.Where(b => b.MontantBudget >= montantMin.Value);
                }

                if (montantMax.HasValue)
                {
                    query = query.Where(b => b.MontantBudget <= montantMax.Value);
                }

                if (depassementBudget.HasValue)
                {
                    if (depassementBudget.Value)
                    {
                        query = query.Where(b => b.MontantRealise > b.MontantBudget);
                    }
                    else
                    {
                        query = query.Where(b => b.MontantRealise <= b.MontantBudget);
                    }
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var budgets = await query
                    .OrderBy(b => b.Evenement.Date)
                    .ThenBy(b => b.Libelle)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new
                    {
                        b.Id,
                        b.Libelle,
                        b.MontantBudget,
                        b.MontantRealise,
                        b.EvenementId,
                        EvenementLibelle = b.Evenement.Libelle,
                        EvenementDate = b.Evenement.Date,
                        // Calculs automatiques
                        Ecart = b.MontantRealise - b.MontantBudget,
                        PourcentageRealisation = b.MontantBudget > 0 ? (b.MontantRealise / b.MontantBudget) * 100 : 0,
                        EstDepassement = b.MontantRealise > b.MontantBudget,
                        EstSousConsomme = b.MontantRealise < b.MontantBudget * 0.8m // Moins de 80% consommé
                    })
                    .ToListAsync();

                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());

                return Ok(budgets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des budgets d'événements");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // GET: api/EvenementBudget/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetEvenementBudget(Guid id)
        {
            try
            {
                var budget = await _context.EvenementBudgets
                    .Include(b => b.Evenement)
                    .Where(b => b.Id == id)
                    .Select(b => new
                    {
                        b.Id,
                        b.Libelle,
                        b.MontantBudget,
                        b.MontantRealise,
                        b.EvenementId,
                        Evenement = new
                        {
                            b.Evenement.Id,
                            b.Evenement.Libelle,
                            b.Evenement.Date,
                            b.Evenement.Lieu
                        },
                        // Analyses
                        Ecart = b.MontantRealise - b.MontantBudget,
                        PourcentageRealisation = b.MontantBudget > 0 ? (b.MontantRealise / b.MontantBudget) * 100 : 0,
                        EstDepassement = b.MontantRealise > b.MontantBudget,
                        MontantRestant = b.MontantBudget - b.MontantRealise,
                        StatutBudget = b.MontantRealise > b.MontantBudget ? "Dépassement" :
                                     b.MontantRealise >= b.MontantBudget * 0.8m ? "En cours" : "Sous-consommé"
                    })
                    .FirstOrDefaultAsync();

                if (budget == null)
                {
                    return NotFound($"Budget avec l'ID {id} non trouvé");
                }

                return Ok(budget);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du budget {Id}", id);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // POST: api/EvenementBudget
        [HttpPost]
        public async Task<ActionResult<EvenementBudget>> CreateEvenementBudget(CreateEvenementBudgetRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier que l'événement existe
                var evenementExists = await _context.Evenements.AnyAsync(e => e.Id == request.EvenementId);
                if (!evenementExists)
                {
                    return BadRequest($"Événement avec l'ID {request.EvenementId} non trouvé");
                }

                // Vérifier l'unicité du libellé pour cet événement
                var libelleExists = await _context.EvenementBudgets
                    .AnyAsync(b => b.EvenementId == request.EvenementId && b.Libelle == request.Libelle);
                if (libelleExists)
                {
                    return BadRequest($"Un budget avec le libellé '{request.Libelle}' existe déjà pour cet événement");
                }

                var evenementBudget = new EvenementBudget
                {
                    Id = Guid.NewGuid(),
                    Libelle = request.Libelle,
                    MontantBudget = request.MontantBudget,
                    MontantRealise = request.MontantRealise ?? 0,
                    EvenementId = request.EvenementId
                };

                _context.EvenementBudgets.Add(evenementBudget);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Budget créé avec l'ID {Id} pour l'événement {EvenementId}",
                    evenementBudget.Id, request.EvenementId);

                return CreatedAtAction(nameof(GetEvenementBudget),
                    new { id = evenementBudget.Id }, evenementBudget);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du budget");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // PUT: api/EvenementBudget/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvenementBudget(Guid id, UpdateEvenementBudgetRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var budget = await _context.EvenementBudgets.FindAsync(id);
                if (budget == null)
                {
                    return NotFound($"Budget avec l'ID {id} non trouvé");
                }

                // Vérifier l'unicité du libellé si il a changé
                if (budget.Libelle != request.Libelle)
                {
                    var libelleExists = await _context.EvenementBudgets
                        .AnyAsync(b => b.EvenementId == budget.EvenementId &&
                                      b.Libelle == request.Libelle &&
                                      b.Id != id);
                    if (libelleExists)
                    {
                        return BadRequest($"Un budget avec le libellé '{request.Libelle}' existe déjà pour cet événement");
                    }
                }

                // Mise à jour des propriétés
                budget.Libelle = request.Libelle;
                budget.MontantBudget = request.MontantBudget;
                budget.MontantRealise = request.MontantRealise;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Budget {Id} mis à jour", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du budget {Id}", id);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // PATCH: api/EvenementBudget/5/montant-realise
        [HttpPatch("{id}/montant-realise")]
        public async Task<IActionResult> UpdateMontantRealise(Guid id, [FromBody] UpdateMontantRealiseRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var budget = await _context.EvenementBudgets.FindAsync(id);
                if (budget == null)
                {
                    return NotFound($"Budget avec l'ID {id} non trouvé");
                }

                var ancienMontant = budget.MontantRealise;
                budget.MontantRealise = request.MontantRealise;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Montant réalisé du budget {Id} mis à jour de {AncienMontant} à {NouveauMontant}",
                    id, ancienMontant, request.MontantRealise);

                // Retourner les nouveaux calculs
                var result = new
                {
                    Id = budget.Id,
                    MontantRealise = budget.MontantRealise,
                    Ecart = budget.MontantRealise - budget.MontantBudget,
                    PourcentageRealisation = budget.MontantBudget > 0 ? (budget.MontantRealise / budget.MontantBudget) * 100 : 0,
                    EstDepassement = budget.MontantRealise > budget.MontantBudget
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du montant réalisé {Id}", id);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // DELETE: api/EvenementBudget/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvenementBudget(Guid id)
        {
            try
            {
                var budget = await _context.EvenementBudgets.FindAsync(id);
                if (budget == null)
                {
                    return NotFound($"Budget avec l'ID {id} non trouvé");
                }

                _context.EvenementBudgets.Remove(budget);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Budget {Id} supprimé", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du budget {Id}", id);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // GET: api/EvenementBudget/evenement/5
        [HttpGet("evenement/{evenementId}")]
        public async Task<ActionResult<object>> GetBudgetsByEvenement(Guid evenementId)
        {
            try
            {
                var evenement = await _context.Evenements.FindAsync(evenementId);
                if (evenement == null)
                {
                    return NotFound($"Événement avec l'ID {evenementId} non trouvé");
                }

                var budgets = await _context.EvenementBudgets
                    .Where(b => b.EvenementId == evenementId)
                    .OrderBy(b => b.Libelle)
                    .Select(b => new
                    {
                        b.Id,
                        b.Libelle,
                        b.MontantBudget,
                        b.MontantRealise,
                        Ecart = b.MontantRealise - b.MontantBudget,
                        PourcentageRealisation = b.MontantBudget > 0 ? (b.MontantRealise / b.MontantBudget) * 100 : 0,
                        EstDepassement = b.MontantRealise > b.MontantBudget
                    })
                    .ToListAsync();

                // Calculs de synthèse
                var totalBudget = budgets.Sum(b => b.MontantBudget);
                var totalRealise = budgets.Sum(b => b.MontantRealise);
                var nombreDepassements = budgets.Count(b => b.EstDepassement);

                var result = new
                {
                    Evenement = new
                    {
                        evenement.Id,
                        evenement.Libelle,
                        evenement.Date
                    },
                    Budgets = budgets,
                    Synthese = new
                    {
                        NombreBudgets = budgets.Count,
                        TotalBudget = totalBudget,
                        TotalRealise = totalRealise,
                        EcartTotal = totalRealise - totalBudget,
                        PourcentageRealisationGlobal = totalBudget > 0 ? (totalRealise / totalBudget) * 100 : 0,
                        NombreDepassements = nombreDepassements,
                        MontantRestant = totalBudget - totalRealise,
                        StatutGlobal = totalRealise > totalBudget ? "Dépassement" :
                                     totalRealise >= totalBudget * 0.8m ? "En cours" : "Sous-consommé"
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des budgets pour l'événement {EvenementId}", evenementId);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // GET: api/EvenementBudget/evenement/5/synthese
        [HttpGet("evenement/{evenementId}/synthese")]
        public async Task<ActionResult<object>> GetSyntheseBudget(Guid evenementId)
        {
            try
            {
                var evenementExists = await _context.Evenements.AnyAsync(e => e.Id == evenementId);
                if (!evenementExists)
                {
                    return NotFound($"Événement avec l'ID {evenementId} non trouvé");
                }

                var budgets = await _context.EvenementBudgets
                    .Where(b => b.EvenementId == evenementId)
                    .ToListAsync();

                var recettes = await _context.EvenementRecettes
                    .Where(r => r.EvenementId == evenementId)
                    .ToListAsync();

                var totalBudget = budgets.Sum(b => b.MontantBudget);
                var totalRealise = budgets.Sum(b => b.MontantRealise);
                var totalRecettes = recettes.Sum(r => r.Montant);

                var synthese = new
                {
                    Budget = new
                    {
                        TotalBudget = totalBudget,
                        TotalRealise = totalRealise,
                        EcartBudget = totalRealise - totalBudget,
                        PourcentageRealisation = totalBudget > 0 ? (totalRealise / totalBudget) * 100 : 0
                    },
                    Recettes = new
                    {
                        TotalRecettes = totalRecettes
                    },
                    Resultat = new
                    {
                        ResultatNet = totalRecettes - totalRealise,
                        MargeReelle = totalRecettes > 0 ? ((totalRecettes - totalRealise) / totalRecettes) * 100 : 0,
                        EstRentable = totalRecettes >= totalRealise
                    },
                    Details = new
                    {
                        NombreBudgets = budgets.Count,
                        NombreRecettes = recettes.Count,
                        BudgetsEnDepassement = budgets.Count(b => b.MontantRealise > b.MontantBudget),
                        BudgetsSousConsommes = budgets.Count(b => b.MontantRealise < b.MontantBudget * 0.8m)
                    }
                };

                return Ok(synthese);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération de la synthèse budget pour l'événement {EvenementId}", evenementId);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // GET: api/EvenementBudget/alertes
        [HttpGet("alertes")]
        public async Task<ActionResult<object>> GetAlertesBudget()
        {
            try
            {
                var budgets = await _context.EvenementBudgets
                    .Include(b => b.Evenement)
                    .Where(b => b.Evenement.Date >= DateTime.Today.AddDays(-30)) // Événements récents
                    .ToListAsync();

                var alertes = new
                {
                    Depassements = budgets
                        .Where(b => b.MontantRealise > b.MontantBudget)
                        .Select(b => new
                        {
                            b.Id,
                            b.Libelle,
                            EvenementLibelle = b.Evenement.Libelle,
                            EvenementDate = b.Evenement.Date,
                            MontantBudget = b.MontantBudget,
                            MontantRealise = b.MontantRealise,
                            Depassement = b.MontantRealise - b.MontantBudget,
                            PourcentageDepassement = (b.MontantRealise / b.MontantBudget - 1) * 100
                        })
                        .OrderByDescending(b => b.PourcentageDepassement)
                        .ToList(),

                    SousConsommations = budgets
                        .Where(b => b.MontantRealise < b.MontantBudget * 0.5m && b.MontantBudget > 0)
                        .Select(b => new
                        {
                            b.Id,
                            b.Libelle,
                            EvenementLibelle = b.Evenement.Libelle,
                            EvenementDate = b.Evenement.Date,
                            MontantBudget = b.MontantBudget,
                            MontantRealise = b.MontantRealise,
                            PourcentageUtilisation = (b.MontantRealise / b.MontantBudget) * 100
                        })
                        .OrderBy(b => b.PourcentageUtilisation)
                        .ToList()
                };

                return Ok(alertes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération des alertes budget");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }
    }

    // DTOs pour les requêtes
    public class CreateEvenementBudgetRequest
    {
        [Required]
        public Guid EvenementId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Libelle { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant budget doit être supérieur à 0")]
        public decimal MontantBudget { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Le montant réalisé ne peut pas être négatif")]
        public decimal? MontantRealise { get; set; }
    }

    public class UpdateEvenementBudgetRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Libelle { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant budget doit être supérieur à 0")]
        public decimal MontantBudget { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Le montant réalisé ne peut pas être négatif")]
        public decimal MontantRealise { get; set; }
    }

    public class UpdateMontantRealiseRequest
    {
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Le montant réalisé ne peut pas être négatif")]
        public decimal MontantRealise { get; set; }
    }
}