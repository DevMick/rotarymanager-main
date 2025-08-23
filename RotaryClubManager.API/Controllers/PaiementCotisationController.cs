using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;

namespace RotaryClubManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaiementCotisationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaiementCotisationController> _logger;

        public PaiementCotisationController(ApplicationDbContext context, ILogger<PaiementCotisationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Méthode utilitaire pour configurer les headers de réponse
        private void ConfigureResponseHeaders()
        {
            try
            {
                if (Response.StatusCode != 204)
                {
                    Response.Headers.Remove("Transfer-Encoding");
                    Response.Headers.Add("Transfer-Encoding", "identity");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Impossible de configurer les headers de réponse");
            }
        }

        // Méthode utilitaire pour obtenir DateTime local (sans UTC)
        private static DateTime GetLocalDateTime(DateTime? dateTime = null)
        {
            var dt = dateTime ?? DateTime.Now;
            return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
        }

        // GET: api/PaiementCotisation
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetPaiementCotisations(
            [FromQuery] string? membreId = null,
            [FromQuery] Guid? clubId = null,
            [FromQuery] DateTime? dateDebut = null,
            [FromQuery] DateTime? dateFin = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                ConfigureResponseHeaders();

                var query = _context.PaiementCotisations
                    .Include(p => p.Membre)
                    .Include(p => p.Club)
                    .AsQueryable();

                // Filtres
                if (!string.IsNullOrEmpty(membreId))
                {
                    query = query.Where(p => p.MembreId == membreId);
                }

                if (clubId.HasValue)
                {
                    query = query.Where(p => p.ClubId == clubId.Value);
                }

                if (dateDebut.HasValue)
                {
                    query = query.Where(p => p.Date >= dateDebut.Value);
                }

                if (dateFin.HasValue)
                {
                    query = query.Where(p => p.Date <= dateFin.Value);
                }

                // Pagination
                var totalItems = await query.CountAsync();
                var paiements = await query
                    .OrderByDescending(p => p.Date)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.Id,
                        p.Montant,
                        p.Date,
                        p.Commentaires,
                        p.MembreId,
                        p.ClubId,
                        Membre = new
                        {
                            p.Membre.Id,
                            p.Membre.FirstName,
                            p.Membre.LastName,
                            p.Membre.Email
                        },
                        Club = new
                        {
                            p.Club.Id,
                            p.Club.Name,
                        }
                    })
                    .ToListAsync();

                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());

                return Ok(paiements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paiements de cotisations");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // GET: api/PaiementCotisation/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetPaiementCotisation(Guid id)
        {
            try
            {
                ConfigureResponseHeaders();

                var paiementCotisation = await _context.PaiementCotisations
                    .Include(p => p.Membre)
                    .Include(p => p.Club)
                    .Where(p => p.Id == id)
                    .Select(p => new
                    {
                        p.Id,
                        p.Montant,
                        p.Date,
                        p.Commentaires,
                        p.MembreId,
                        p.ClubId,
                        Membre = new
                        {
                            p.Membre.Id,
                            p.Membre.FirstName,
                            p.Membre.LastName,
                            p.Membre.Email
                        },
                        Club = new
                        {
                            p.Club.Id,
                            p.Club.Name,
                        }
                    })
                    .FirstOrDefaultAsync();

                if (paiementCotisation == null)
                {
                    return NotFound(new { message = $"Paiement avec l'ID {id} non trouvé" });
                }

                return Ok(paiementCotisation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du paiement {Id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // GET: api/PaiementCotisation/membre/{membreId}
        [HttpGet("membre/{membreId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetPaiementsByMembre(string membreId)
        {
            try
            {
                ConfigureResponseHeaders();

                var membreExists = await _context.Users.AnyAsync(u => u.Id == membreId);
                if (!membreExists)
                {
                    return NotFound(new { message = "Le membre spécifié n'existe pas" });
                }

                var paiements = await _context.PaiementCotisations
                    .Include(p => p.Membre)
                    .Include(p => p.Club)
                    .Where(p => p.MembreId == membreId)
                    .OrderByDescending(p => p.Date)
                    .Select(p => new
                    {
                        p.Id,
                        p.Montant,
                        p.Date,
                        p.Commentaires,
                        p.MembreId,
                        p.ClubId,
                        Club = new
                        {
                            p.Club.Id,
                            p.Club.Name,
                        }
                    })
                    .ToListAsync();

                return Ok(paiements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paiements pour le membre {MembreId}", membreId);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // GET: api/PaiementCotisation/club/{clubId}
        [HttpGet("club/{clubId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetPaiementsByClub(Guid clubId)
        {
            try
            {
                ConfigureResponseHeaders();

                var clubExists = await _context.Clubs.AnyAsync(c => c.Id == clubId);
                if (!clubExists)
                {
                    return NotFound(new { message = "Le club spécifié n'existe pas" });
                }

                var paiements = await _context.PaiementCotisations
                    .Include(p => p.Membre)
                    .Include(p => p.Club)
                    .Where(p => p.ClubId == clubId)
                    .OrderByDescending(p => p.Date)
                    .Select(p => new
                    {
                        p.Id,
                        p.Montant,
                        p.Date,
                        p.Commentaires,
                        p.MembreId,
                        p.ClubId,
                        Membre = new
                        {
                            p.Membre.Id,
                            p.Membre.FirstName,
                            p.Membre.LastName,
                            p.Membre.Email
                        }
                    })
                    .ToListAsync();

                return Ok(paiements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paiements pour le club {ClubId}", clubId);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // GET: api/PaiementCotisation/periode
        [HttpGet("periode")]
        public async Task<ActionResult<IEnumerable<object>>> GetPaiementsByPeriode(
            [FromQuery] DateTime dateDebut,
            [FromQuery] DateTime dateFin,
            [FromQuery] Guid? clubId = null)
        {
            try
            {
                ConfigureResponseHeaders();

                if (dateDebut > dateFin)
                {
                    return BadRequest(new { message = "La date de début ne peut pas être supérieure à la date de fin" });
                }

                var query = _context.PaiementCotisations
                    .Include(p => p.Membre)
                    .Include(p => p.Club)
                    .Where(p => p.Date >= dateDebut && p.Date <= dateFin);

                if (clubId.HasValue)
                {
                    query = query.Where(p => p.ClubId == clubId.Value);
                }

                var paiements = await query
                    .OrderByDescending(p => p.Date)
                    .Select(p => new
                    {
                        p.Id,
                        p.Montant,
                        p.Date,
                        p.Commentaires,
                        p.MembreId,
                        p.ClubId,
                        Membre = new
                        {
                            p.Membre.FirstName,
                            p.Membre.LastName,
                            p.Membre.Email
                        },
                        Club = new
                        {
                            p.Club.Id,
                            p.Club.Name,
                        }
                    })
                    .ToListAsync();

                return Ok(paiements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paiements par période");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // GET: api/PaiementCotisation/statistiques/membre/{membreId}
        [HttpGet("statistiques/membre/{membreId}")]
        public async Task<ActionResult<object>> GetStatistiquesPaiementMembre(string membreId, [FromQuery] Guid? clubId = null)
        {
            try
            {
                ConfigureResponseHeaders();

                var membreExists = await _context.Users.AnyAsync(u => u.Id == membreId);
                if (!membreExists)
                {
                    return NotFound(new { message = "Le membre spécifié n'existe pas" });
                }

                var query = _context.PaiementCotisations.Where(p => p.MembreId == membreId);

                if (clubId.HasValue)
                {
                    query = query.Where(p => p.ClubId == clubId.Value);
                }

                var paiements = await query.ToListAsync();

                var stats = new
                {
                    MembreId = membreId,
                    ClubId = clubId,
                    NombrePaiements = paiements.Count,
                    MontantTotal = paiements.Sum(p => p.Montant),
                    DernierPaiement = paiements.OrderByDescending(p => p.Date).FirstOrDefault()?.Date,
                    MontantMoyen = paiements.Any() ? paiements.Average(p => p.Montant) : 0
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération des statistiques pour le membre {MembreId}", membreId);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // GET: api/PaiementCotisation/statistiques/club/{clubId}
        [HttpGet("statistiques/club/{clubId}")]
        public async Task<ActionResult<object>> GetStatistiquesClub(Guid clubId)
        {
            try
            {
                ConfigureResponseHeaders();

                var clubExists = await _context.Clubs.AnyAsync(c => c.Id == clubId);
                if (!clubExists)
                {
                    return NotFound(new { message = "Le club spécifié n'existe pas" });
                }

                var paiements = await _context.PaiementCotisations
                    .Where(p => p.ClubId == clubId)
                    .ToListAsync();

                var aujourdhui = GetLocalDateTime().Date;
                var debutMois = new DateTime(aujourdhui.Year, aujourdhui.Month, 1);
                var debutAnnee = new DateTime(aujourdhui.Year, 1, 1);

                var stats = new
                {
                    ClubId = clubId,
                    NombrePaiementsTotal = paiements.Count,
                    MontantTotal = paiements.Sum(p => p.Montant),
                    MontantMoyenParPaiement = paiements.Any() ? paiements.Average(p => p.Montant) : 0,
                    NombrePaiementsCeMois = paiements.Count(p => p.Date >= debutMois),
                    MontantCeMois = paiements.Where(p => p.Date >= debutMois).Sum(p => p.Montant),
                    NombrePaiementsCetteAnnee = paiements.Count(p => p.Date >= debutAnnee),
                    MontantCetteAnnee = paiements.Where(p => p.Date >= debutAnnee).Sum(p => p.Montant),
                    NombreMembresAyantPaye = paiements.Select(p => p.MembreId).Distinct().Count()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération des statistiques pour le club {ClubId}", clubId);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // GET: api/PaiementCotisation/statistiques/globales
        [HttpGet("statistiques/globales")]
        public async Task<ActionResult<object>> GetStatistiquesGlobales()
        {
            try
            {
                ConfigureResponseHeaders();

                var paiements = await _context.PaiementCotisations.ToListAsync();
                var aujourdhui = GetLocalDateTime().Date;
                var debutMois = new DateTime(aujourdhui.Year, aujourdhui.Month, 1);
                var debutAnnee = new DateTime(aujourdhui.Year, 1, 1);

                var stats = new
                {
                    NombrePaiementsTotal = paiements.Count,
                    MontantTotal = paiements.Sum(p => p.Montant),
                    MontantMoyenParPaiement = paiements.Any() ? paiements.Average(p => p.Montant) : 0,
                    NombrePaiementsCeMois = paiements.Count(p => p.Date >= debutMois),
                    MontantCeMois = paiements.Where(p => p.Date >= debutMois).Sum(p => p.Montant),
                    NombrePaiementsCetteAnnee = paiements.Count(p => p.Date >= debutAnnee),
                    MontantCetteAnnee = paiements.Where(p => p.Date >= debutAnnee).Sum(p => p.Montant),
                    NombreClubsAvecPaiements = paiements.Select(p => p.ClubId).Distinct().Count(),
                    NombreMembresAyantPaye = paiements.Select(p => p.MembreId).Distinct().Count()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération des statistiques globales");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // POST: api/PaiementCotisation
        [HttpPost]
        public async Task<ActionResult<object>> PostPaiementCotisation([FromBody] CreatePaiementCotisationDto paiementDto)
        {
            try
            {
                ConfigureResponseHeaders();

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier si le membre existe
                var membreExists = await _context.Users.AnyAsync(u => u.Id == paiementDto.MembreId);
                if (!membreExists)
                {
                    return BadRequest(new { message = "Le membre spécifié n'existe pas" });
                }

                // Vérifier si le club existe
                var clubExists = await _context.Clubs.AnyAsync(c => c.Id == paiementDto.ClubId);
                if (!clubExists)
                {
                    return BadRequest(new { message = "Le club spécifié n'existe pas" });
                }

                var paiement = new PaiementCotisation
                {
                    Id = Guid.NewGuid(),
                    Montant = paiementDto.Montant,
                    Date = GetLocalDateTime(paiementDto.Date),
                    Commentaires = paiementDto.Commentaires,
                    MembreId = paiementDto.MembreId,
                    ClubId = paiementDto.ClubId
                };

                _context.PaiementCotisations.Add(paiement);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Paiement de cotisation créé avec l'ID {Id}", paiement.Id);

                // Récupérer le paiement avec les données du membre et du club
                var paiementAvecDetails = await _context.PaiementCotisations
                    .Include(p => p.Membre)
                    .Include(p => p.Club)
                    .Where(p => p.Id == paiement.Id)
                    .Select(p => new
                    {
                        p.Id,
                        p.Montant,
                        p.Date,
                        p.Commentaires,
                        p.MembreId,
                        p.ClubId,
                        Membre = new
                        {
                            p.Membre.Id,
                            p.Membre.FirstName,
                            p.Membre.LastName,
                            p.Membre.Email
                        },
                        Club = new
                        {
                            p.Club.Id,
                            p.Club.Name,
                        }
                    })
                    .FirstOrDefaultAsync();

                return CreatedAtAction(nameof(GetPaiementCotisation),
                    new { id = paiement.Id }, paiementAvecDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du paiement de cotisation");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // PUT: api/PaiementCotisation/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPaiementCotisation(Guid id, [FromBody] UpdatePaiementCotisationDto paiementDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var paiement = await _context.PaiementCotisations.FindAsync(id);
                if (paiement == null)
                {
                    return NotFound(new { message = $"Paiement avec l'ID {id} non trouvé" });
                }

                // Vérifier si le membre existe (si changé)
                if (!string.IsNullOrEmpty(paiementDto.MembreId) && paiementDto.MembreId != paiement.MembreId)
                {
                    var membreExists = await _context.Users.AnyAsync(u => u.Id == paiementDto.MembreId);
                    if (!membreExists)
                    {
                        return BadRequest(new { message = "Le membre spécifié n'existe pas" });
                    }
                }

                // Vérifier si le club existe (si changé)
                if (paiementDto.ClubId.HasValue && paiementDto.ClubId != paiement.ClubId)
                {
                    var clubExists = await _context.Clubs.AnyAsync(c => c.Id == paiementDto.ClubId.Value);
                    if (!clubExists)
                    {
                        return BadRequest(new { message = "Le club spécifié n'existe pas" });
                    }
                }

                // Mettre à jour les propriétés
                if (paiementDto.Montant.HasValue)
                    paiement.Montant = paiementDto.Montant.Value;

                if (paiementDto.Date.HasValue)
                    paiement.Date = GetLocalDateTime(paiementDto.Date.Value);

                if (paiementDto.Commentaires != null)
                    paiement.Commentaires = paiementDto.Commentaires;

                if (!string.IsNullOrEmpty(paiementDto.MembreId))
                    paiement.MembreId = paiementDto.MembreId;

                if (paiementDto.ClubId.HasValue)
                    paiement.ClubId = paiementDto.ClubId.Value;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Paiement de cotisation {Id} mis à jour", id);

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PaiementCotisationExists(id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du paiement {Id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // DELETE: api/PaiementCotisation/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePaiementCotisation(Guid id)
        {
            try
            {
                var paiement = await _context.PaiementCotisations.FindAsync(id);
                if (paiement == null)
                {
                    return NotFound(new { message = $"Paiement avec l'ID {id} non trouvé" });
                }

                _context.PaiementCotisations.Remove(paiement);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Paiement de cotisation {Id} supprimé", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du paiement {Id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        private bool PaiementCotisationExists(Guid id)
        {
            return _context.PaiementCotisations.Any(e => e.Id == id);
        }
    }

    // DTOs mis à jour
    public class CreatePaiementCotisationDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        public int Montant { get; set; }

        public DateTime? Date { get; set; }

        public string? Commentaires { get; set; }

        [Required]
        public string MembreId { get; set; } = string.Empty;

        [Required]
        public Guid ClubId { get; set; }
    }

    public class UpdatePaiementCotisationDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        public int? Montant { get; set; }

        public DateTime? Date { get; set; }

        public string? Commentaires { get; set; }

        public string? MembreId { get; set; }

        public Guid? ClubId { get; set; }
    }
}