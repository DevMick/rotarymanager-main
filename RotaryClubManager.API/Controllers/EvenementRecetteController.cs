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
    public class EvenementRecetteController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EvenementRecetteController> _logger;

        public EvenementRecetteController(ApplicationDbContext context, ILogger<EvenementRecetteController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/EvenementRecette
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetEvenementRecettes(
            [FromQuery] Guid? evenementId = null,
            [FromQuery] string? libelle = null,
            [FromQuery] decimal? montantMin = null,
            [FromQuery] decimal? montantMax = null,
            [FromQuery] DateTime? dateEvenementDebut = null,
            [FromQuery] DateTime? dateEvenementFin = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.EvenementRecettes
                    .Include(r => r.Evenement)
                    .AsQueryable();

                // Filtres
                if (evenementId.HasValue)
                {
                    query = query.Where(r => r.EvenementId == evenementId.Value);
                }

                if (!string.IsNullOrEmpty(libelle))
                {
                    query = query.Where(r => r.Libelle.Contains(libelle));
                }

                if (montantMin.HasValue)
                {
                    query = query.Where(r => r.Montant >= montantMin.Value);
                }

                if (montantMax.HasValue)
                {
                    query = query.Where(r => r.Montant <= montantMax.Value);
                }

                if (dateEvenementDebut.HasValue)
                {
                    query = query.Where(r => r.Evenement.Date >= dateEvenementDebut.Value);
                }

                if (dateEvenementFin.HasValue)
                {
                    query = query.Where(r => r.Evenement.Date <= dateEvenementFin.Value);
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var recettes = await query
                    .OrderByDescending(r => r.Evenement.Date)
                    .ThenByDescending(r => r.Montant)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        r.Id,
                        r.Libelle,
                        r.Montant,
                        r.EvenementId,
                        EvenementLibelle = r.Evenement.Libelle,
                        EvenementDate = r.Evenement.Date,
                        EvenementLieu = r.Evenement.Lieu,
                        EvenementEstInterne = r.Evenement.EstInterne
                    })
                    .ToListAsync();

                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());

                return Ok(recettes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des recettes d'événements");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // GET: api/EvenementRecette/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetEvenementRecette(Guid id)
        {
            try
            {
                var recette = await _context.EvenementRecettes
                    .Include(r => r.Evenement)
                    .Where(r => r.Id == id)
                    .Select(r => new
                    {
                        r.Id,
                        r.Libelle,
                        r.Montant,
                        r.EvenementId,
                        Evenement = new
                        {
                            r.Evenement.Id,
                            r.Evenement.Libelle,
                            r.Evenement.Date,
                            r.Evenement.Lieu,
                            r.Evenement.EstInterne
                        }
                    })
                    .FirstOrDefaultAsync();

                if (recette == null)
                {
                    return NotFound($"Recette avec l'ID {id} non trouvée");
                }

                return Ok(recette);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la recette {Id}", id);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // POST: api/EvenementRecette
        [HttpPost]
        public async Task<ActionResult<EvenementRecette>> CreateEvenementRecette(CreateEvenementRecetteRequest request)
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
                var libelleExists = await _context.EvenementRecettes
                    .AnyAsync(r => r.EvenementId == request.EvenementId && r.Libelle == request.Libelle);
                if (libelleExists)
                {
                    return BadRequest($"Une recette avec le libellé '{request.Libelle}' existe déjà pour cet événement");
                }

                var evenementRecette = new EvenementRecette
                {
                    Id = Guid.NewGuid(),
                    Libelle = request.Libelle,
                    Montant = request.Montant,
                    EvenementId = request.EvenementId
                };

                _context.EvenementRecettes.Add(evenementRecette);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Recette créée avec l'ID {Id} pour l'événement {EvenementId}",
                    evenementRecette.Id, request.EvenementId);

                return CreatedAtAction(nameof(GetEvenementRecette),
                    new { id = evenementRecette.Id }, evenementRecette);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la recette");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // POST: api/EvenementRecette/multiple
        [HttpPost("multiple")]
        public async Task<ActionResult<IEnumerable<object>>> CreateMultipleRecettes(CreateMultipleRecettesRequest request)
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

                if (request.Recettes == null || !request.Recettes.Any())
                {
                    return BadRequest("Au moins une recette est requise");
                }

                var recettesExistantes = await _context.EvenementRecettes
                    .Where(r => r.EvenementId == request.EvenementId)
                    .Select(r => r.Libelle.ToLower())
                    .ToListAsync();

                var recettesCreees = new List<object>();

                foreach (var recetteDto in request.Recettes)
                {
                    // Validation de chaque recette
                    if (string.IsNullOrWhiteSpace(recetteDto.Libelle) || recetteDto.Montant <= 0)
                    {
                        continue; // Skip les recettes invalides
                    }

                    // Éviter les doublons
                    if (recettesExistantes.Contains(recetteDto.Libelle.ToLower()))
                    {
                        continue;
                    }

                    var evenementRecette = new EvenementRecette
                    {
                        Id = Guid.NewGuid(),
                        Libelle = recetteDto.Libelle,
                        Montant = recetteDto.Montant,
                        EvenementId = request.EvenementId
                    };

                    _context.EvenementRecettes.Add(evenementRecette);
                    recettesCreees.Add(new
                    {
                        evenementRecette.Id,
                        evenementRecette.Libelle,
                        evenementRecette.Montant
                    });

                    recettesExistantes.Add(recetteDto.Libelle.ToLower());
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("{Count} recettes créées pour l'événement {EvenementId}",
                    recettesCreees.Count, request.EvenementId);

                return Ok(recettesCreees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création des recettes multiples");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // PUT: api/EvenementRecette/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvenementRecette(Guid id, UpdateEvenementRecetteRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var recette = await _context.EvenementRecettes.FindAsync(id);
                if (recette == null)
                {
                    return NotFound($"Recette avec l'ID {id} non trouvée");
                }

                // Vérifier l'unicité du libellé si il a changé
                if (recette.Libelle != request.Libelle)
                {
                    var libelleExists = await _context.EvenementRecettes
                        .AnyAsync(r => r.EvenementId == recette.EvenementId &&
                                      r.Libelle == request.Libelle &&
                                      r.Id != id);
                    if (libelleExists)
                    {
                        return BadRequest($"Une recette avec le libellé '{request.Libelle}' existe déjà pour cet événement");
                    }
                }

                // Mise à jour des propriétés
                recette.Libelle = request.Libelle;
                recette.Montant = request.Montant;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Recette {Id} mise à jour", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la recette {Id}", id);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // DELETE: api/EvenementRecette/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvenementRecette(Guid id)
        {
            try
            {
                var recette = await _context.EvenementRecettes.FindAsync(id);
                if (recette == null)
                {
                    return NotFound($"Recette avec l'ID {id} non trouvée");
                }

                _context.EvenementRecettes.Remove(recette);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Recette {Id} supprimée", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la recette {Id}", id);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // GET: api/EvenementRecette/evenement/5
        [HttpGet("evenement/{evenementId}")]
        public async Task<ActionResult<object>> GetRecettesByEvenement(Guid evenementId)
        {
            try
            {
                var evenement = await _context.Evenements.FindAsync(evenementId);
                if (evenement == null)
                {
                    return NotFound($"Événement avec l'ID {evenementId} non trouvé");
                }

                var recettes = await _context.EvenementRecettes
                    .Where(r => r.EvenementId == evenementId)
                    .OrderByDescending(r => r.Montant)
                    .Select(r => new
                    {
                        r.Id,
                        r.Libelle,
                        r.Montant
                    })
                    .ToListAsync();

                // Calculs de synthèse
                var totalRecettes = recettes.Sum(r => r.Montant);
                var recetteMoyenne = recettes.Any() ? recettes.Average(r => r.Montant) : 0;
                var recetteMax = recettes.Any() ? recettes.Max(r => r.Montant) : 0;

                var result = new
                {
                    Evenement = new
                    {
                        evenement.Id,
                        evenement.Libelle,
                        evenement.Date,
                        evenement.EstInterne
                    },
                    Recettes = recettes,
                    Synthese = new
                    {
                        NombreRecettes = recettes.Count,
                        TotalRecettes = totalRecettes,
                        RecetteMoyenne = recetteMoyenne,
                        RecetteMaximale = recetteMax,
                        RecetteMinimale = recettes.Any() ? recettes.Min(r => r.Montant) : 0
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des recettes pour l'événement {EvenementId}", evenementId);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // GET: api/EvenementRecette/statistiques
        [HttpGet("statistiques")]
        public async Task<ActionResult<object>> GetStatistiquesRecettes(
            [FromQuery] int? annee = null,
            [FromQuery] bool? estInterne = null,
            [FromQuery] DateTime? dateDebut = null,
            [FromQuery] DateTime? dateFin = null)
        {
            try
            {
                var query = _context.EvenementRecettes
                    .Include(r => r.Evenement)
                    .AsQueryable();

                // Filtres
                if (annee.HasValue)
                {
                    query = query.Where(r => r.Evenement.Date.Year == annee.Value);
                }

                if (estInterne.HasValue)
                {
                    query = query.Where(r => r.Evenement.EstInterne == estInterne.Value);
                }

                if (dateDebut.HasValue)
                {
                    query = query.Where(r => r.Evenement.Date >= dateDebut.Value);
                }

                if (dateFin.HasValue)
                {
                    query = query.Where(r => r.Evenement.Date <= dateFin.Value);
                }

                var recettes = await query.ToListAsync();

                var stats = new
                {
                    Global = new
                    {
                        NombreRecettes = recettes.Count,
                        TotalRecettes = recettes.Sum(r => r.Montant),
                        RecetteMoyenne = recettes.Any() ? recettes.Average(r => r.Montant) : 0,
                        RecetteMaximale = recettes.Any() ? recettes.Max(r => r.Montant) : 0,
                        RecetteMinimale = recettes.Any() ? recettes.Min(r => r.Montant) : 0
                    },
                    ParTypeEvenement = new
                    {
                        Internes = new
                        {
                            Nombre = recettes.Count(r => r.Evenement.EstInterne),
                            Total = recettes.Where(r => r.Evenement.EstInterne).Sum(r => r.Montant),
                            Moyenne = recettes.Where(r => r.Evenement.EstInterne).Any() ?
                                     recettes.Where(r => r.Evenement.EstInterne).Average(r => r.Montant) : 0
                        },
                        Externes = new
                        {
                            Nombre = recettes.Count(r => !r.Evenement.EstInterne),
                            Total = recettes.Where(r => !r.Evenement.EstInterne).Sum(r => r.Montant),
                            Moyenne = recettes.Where(r => !r.Evenement.EstInterne).Any() ?
                                     recettes.Where(r => !r.Evenement.EstInterne).Average(r => r.Montant) : 0
                        }
                    },
                    ParMois = recettes
                        .GroupBy(r => new { r.Evenement.Date.Year, r.Evenement.Date.Month })
                        .Select(g => new
                        {
                            Annee = g.Key.Year,
                            Mois = g.Key.Month,
                            NombreRecettes = g.Count(),
                            TotalRecettes = g.Sum(r => r.Montant),
                            RecetteMoyenne = g.Average(r => r.Montant)
                        })
                        .OrderBy(x => x.Annee)
                        .ThenBy(x => x.Mois)
                        .ToList(),
                    TopRecettes = recettes
                        .OrderByDescending(r => r.Montant)
                        .Take(10)
                        .Select(r => new
                        {
                            r.Libelle,
                            r.Montant,
                            EvenementLibelle = r.Evenement.Libelle,
                            EvenementDate = r.Evenement.Date
                        })
                        .ToList()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération des statistiques des recettes");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // GET: api/EvenementRecette/top-recettes
        [HttpGet("top-recettes")]
        public async Task<ActionResult<IEnumerable<object>>> GetTopRecettes(
            [FromQuery] int limit = 10,
            [FromQuery] DateTime? dateDebut = null,
            [FromQuery] DateTime? dateFin = null)
        {
            try
            {
                var query = _context.EvenementRecettes
                    .Include(r => r.Evenement)
                    .AsQueryable();

                if (dateDebut.HasValue)
                {
                    query = query.Where(r => r.Evenement.Date >= dateDebut.Value);
                }

                if (dateFin.HasValue)
                {
                    query = query.Where(r => r.Evenement.Date <= dateFin.Value);
                }

                var topRecettes = await query
                    .OrderByDescending(r => r.Montant)
                    .Take(limit)
                    .Select(r => new
                    {
                        r.Id,
                        r.Libelle,
                        r.Montant,
                        Evenement = new
                        {
                            r.Evenement.Id,
                            r.Evenement.Libelle,
                            r.Evenement.Date,
                            r.Evenement.Lieu,
                            r.Evenement.EstInterne
                        }
                    })
                    .ToListAsync();

                return Ok(topRecettes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du top des recettes");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        // GET: api/EvenementRecette/comparaison-budget/{evenementId}
        [HttpGet("comparaison-budget/{evenementId}")]
        public async Task<ActionResult<object>> GetComparaisonBudget(Guid evenementId)
        {
            try
            {
                var evenement = await _context.Evenements.FindAsync(evenementId);
                if (evenement == null)
                {
                    return NotFound($"Événement avec l'ID {evenementId} non trouvé");
                }

                var recettes = await _context.EvenementRecettes
                    .Where(r => r.EvenementId == evenementId)
                    .ToListAsync();

                var budgets = await _context.EvenementBudgets
                    .Where(b => b.EvenementId == evenementId)
                    .ToListAsync();

                var totalRecettes = recettes.Sum(r => r.Montant);
                var totalBudget = budgets.Sum(b => b.MontantBudget);
                var totalRealise = budgets.Sum(b => b.MontantRealise);

                var comparaison = new
                {
                    Evenement = new
                    {
                        evenement.Id,
                        evenement.Libelle,
                        evenement.Date
                    },
                    Recettes = new
                    {
                        Details = recettes.Select(r => new { r.Libelle, r.Montant }),
                        Total = totalRecettes,
                        Nombre = recettes.Count
                    },
                    Depenses = new
                    {
                        BudgetPrevu = totalBudget,
                        DepenseReelle = totalRealise,
                        EcartBudget = totalRealise - totalBudget
                    },
                    Resultat = new
                    {
                        BeneficePrevu = totalRecettes - totalBudget,
                        BeneficeReel = totalRecettes - totalRealise,
                        MargeReelle = totalRecettes > 0 ? ((totalRecettes - totalRealise) / totalRecettes) * 100 : 0,
                        EstRentable = totalRecettes >= totalRealise,
                        TauxCouverture = totalBudget > 0 ? (totalRecettes / totalBudget) * 100 : 0
                    }
                };

                return Ok(comparaison);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la comparaison budget/recettes pour l'événement {EvenementId}", evenementId);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }
    }

    // DTOs pour les requêtes
    public class CreateEvenementRecetteRequest
    {
        [Required]
        public Guid EvenementId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Libelle { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        public decimal Montant { get; set; }
    }

    public class CreateMultipleRecettesRequest
    {
        [Required]
        public Guid EvenementId { get; set; }

        [Required]
        public List<RecetteDto> Recettes { get; set; } = new();
    }

    public class RecetteDto
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Libelle { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        public decimal Montant { get; set; }
    }

    public class UpdateEvenementRecetteRequest
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Libelle { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        public decimal Montant { get; set; }
    }
}