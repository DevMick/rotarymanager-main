using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/fonctions/{fonctionId}/echeances")]
    [ApiController]
    [Authorize]
    public class FonctionEcheancesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FonctionEcheancesController> _logger;

        public FonctionEcheancesController(
            ApplicationDbContext context,
            ILogger<FonctionEcheancesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/fonctions/{fonctionId}/echeances
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FonctionEcheanceDto>>> GetEcheances(
            Guid fonctionId,
            [FromQuery] FrequenceType? frequence = null,
            [FromQuery] DateTime? dateDebut = null,
            [FromQuery] DateTime? dateFin = null,
            [FromQuery] bool? actives = null)
        {
            try
            {
                // Validation des paramètres
                if (fonctionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la fonction est invalide");
                }

                // Vérifier que la fonction existe
                var fonction = await _context.Fonctions.FindAsync(fonctionId);
                if (fonction == null)
                {
                    return NotFound($"Fonction avec l'ID {fonctionId} introuvable");
                }

                var query = _context.FonctionEcheances
                    .Include(fe => fe.Fonction)
                    .Where(fe => fe.FonctionId == fonctionId);

                // Filtres optionnels
                if (frequence.HasValue)
                {
                    query = query.Where(fe => fe.Frequence == frequence.Value);
                }

                if (dateDebut.HasValue)
                {
                    query = query.Where(fe => fe.DateButoir >= dateDebut.Value);
                }

                if (dateFin.HasValue)
                {
                    query = query.Where(fe => fe.DateButoir <= dateFin.Value);
                }

                if (actives.HasValue && actives.Value)
                {
                    // Considérer comme actives les échéances futures ou récurrentes
                    var dateActuelle = DateTime.UtcNow;
                    query = query.Where(fe => fe.DateButoir > dateActuelle || fe.Frequence != FrequenceType.Unique);
                }

                var echeances = await query
                    .OrderBy(fe => fe.DateButoir)
                    .ThenBy(fe => fe.Libelle)
                    .Select(fe => new FonctionEcheanceDto
                    {
                        Id = fe.Id,
                        Libelle = fe.Libelle,
                        DateButoir = fe.DateButoir,
                        Frequence = fe.Frequence,
                        FrequenceLibelle = fe.Frequence.ToString(),
                        FonctionId = fe.FonctionId,
                        FonctionNom = fe.Fonction.NomFonction,
                        EstEchue = fe.DateButoir < DateTime.UtcNow && fe.Frequence == FrequenceType.Unique,
                        EstRecurrente = fe.Frequence != FrequenceType.Unique
                    })
                    .ToListAsync();

                return Ok(echeances);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des échéances de la fonction {FonctionId}", fonctionId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des échéances");
            }
        }

        // GET: api/fonctions/{fonctionId}/echeances/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<FonctionEcheanceDto>> GetEcheance(Guid fonctionId, Guid id)
        {
            try
            {
                // Validation des paramètres
                if (fonctionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la fonction est invalide");
                }

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'échéance est invalide");
                }

                var echeance = await _context.FonctionEcheances
                    .Include(fe => fe.Fonction)
                    .Where(fe => fe.Id == id && fe.FonctionId == fonctionId)
                    .Select(fe => new FonctionEcheanceDto
                    {
                        Id = fe.Id,
                        Libelle = fe.Libelle,
                        DateButoir = fe.DateButoir,
                        Frequence = fe.Frequence,
                        FrequenceLibelle = fe.Frequence.ToString(),
                        FonctionId = fe.FonctionId,
                        FonctionNom = fe.Fonction.NomFonction,
                        EstEchue = fe.DateButoir < DateTime.UtcNow && fe.Frequence == FrequenceType.Unique,
                        EstRecurrente = fe.Frequence != FrequenceType.Unique
                    })
                    .FirstOrDefaultAsync();

                if (echeance == null)
                {
                    return NotFound($"Échéance avec l'ID {id} non trouvée pour la fonction {fonctionId}");
                }

                return Ok(echeance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'échéance {Id} de la fonction {FonctionId}", id, fonctionId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de l'échéance");
            }
        }

        // POST: api/fonctions/{fonctionId}/echeances
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<ActionResult<FonctionEcheanceDto>> CreateEcheance(
            Guid fonctionId,
            [FromBody] CreateFonctionEcheanceRequest request)
        {
            try
            {
                // Validation des paramètres
                if (fonctionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la fonction est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier que la fonction existe
                var fonction = await _context.Fonctions.FindAsync(fonctionId);
                if (fonction == null)
                {
                    return NotFound($"Fonction avec l'ID {fonctionId} introuvable");
                }

                // Vérifier l'unicité du libellé dans la fonction
                var existingEcheance = await _context.FonctionEcheances
                    .AnyAsync(fe => fe.FonctionId == fonctionId &&
                                   fe.Libelle.ToLower() == request.Libelle.ToLower());

                if (existingEcheance)
                {
                    return BadRequest($"Une échéance avec le libellé '{request.Libelle}' existe déjà pour cette fonction");
                }

                var echeance = new FonctionEcheances
                {
                    Id = Guid.NewGuid(),
                    Libelle = request.Libelle,
                    DateButoir = request.DateButoir,
                    Frequence = request.Frequence,
                    FonctionId = fonctionId
                };

                _context.FonctionEcheances.Add(echeance);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Échéance '{Libelle}' créée pour la fonction {FonctionId} avec l'ID {Id}",
                    echeance.Libelle, fonctionId, echeance.Id);

                var result = new FonctionEcheanceDto
                {
                    Id = echeance.Id,
                    Libelle = echeance.Libelle,
                    DateButoir = echeance.DateButoir,
                    Frequence = echeance.Frequence,
                    FrequenceLibelle = echeance.Frequence.ToString(),
                    FonctionId = echeance.FonctionId,
                    FonctionNom = fonction.NomFonction,
                    EstEchue = echeance.DateButoir < DateTime.UtcNow && echeance.Frequence == FrequenceType.Unique,
                    EstRecurrente = echeance.Frequence != FrequenceType.Unique
                };

                return CreatedAtAction(nameof(GetEcheance),
                    new { fonctionId, id = echeance.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'échéance pour la fonction {FonctionId}", fonctionId);
                return StatusCode(500, "Une erreur est survenue lors de la création de l'échéance");
            }
        }

        // PUT: api/fonctions/{fonctionId}/echeances/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> UpdateEcheance(
            Guid fonctionId,
            Guid id,
            [FromBody] UpdateFonctionEcheanceRequest request)
        {
            try
            {
                // Validation des paramètres
                if (fonctionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la fonction est invalide");
                }

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'échéance est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var echeance = await _context.FonctionEcheances
                    .FirstOrDefaultAsync(fe => fe.Id == id && fe.FonctionId == fonctionId);

                if (echeance == null)
                {
                    return NotFound($"Échéance avec l'ID {id} non trouvée pour la fonction {fonctionId}");
                }

                // Vérifier l'unicité du libellé si modifié
                if (!string.IsNullOrEmpty(request.Libelle) &&
                    request.Libelle.ToLower() != echeance.Libelle.ToLower())
                {
                    var existingEcheance = await _context.FonctionEcheances
                        .AnyAsync(fe => fe.Id != id &&
                                       fe.FonctionId == fonctionId &&
                                       fe.Libelle.ToLower() == request.Libelle.ToLower());

                    if (existingEcheance)
                    {
                        return BadRequest($"Une échéance avec le libellé '{request.Libelle}' existe déjà pour cette fonction");
                    }
                }

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.Libelle))
                    echeance.Libelle = request.Libelle;

                if (request.DateButoir.HasValue)
                    echeance.DateButoir = request.DateButoir.Value;

                if (request.Frequence.HasValue)
                    echeance.Frequence = request.Frequence.Value;

                _context.Entry(echeance).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Échéance {Id} mise à jour avec succès", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'échéance {Id}", id);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de l'échéance");
            }
        }

        // DELETE: api/fonctions/{fonctionId}/echeances/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> DeleteEcheance(Guid fonctionId, Guid id)
        {
            try
            {
                // Validation des paramètres
                if (fonctionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la fonction est invalide");
                }

                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'échéance est invalide");
                }

                var echeance = await _context.FonctionEcheances
                    .FirstOrDefaultAsync(fe => fe.Id == id && fe.FonctionId == fonctionId);

                if (echeance == null)
                {
                    return NotFound($"Échéance avec l'ID {id} non trouvée pour la fonction {fonctionId}");
                }

                _context.FonctionEcheances.Remove(echeance);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Échéance {Id} supprimée de la fonction {FonctionId}", id, fonctionId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'échéance {Id}", id);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de l'échéance");
            }
        }

        // GET: api/fonctions/{fonctionId}/echeances/prochaines
        [HttpGet("prochaines")]
        public async Task<ActionResult<IEnumerable<FonctionEcheanceDto>>> GetProchainesEcheances(
            Guid fonctionId,
            [FromQuery] int jours = 30)
        {
            try
            {
                // Validation des paramètres
                if (fonctionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la fonction est invalide");
                }

                var dateLimite = DateTime.UtcNow.AddDays(jours);

                var prochaines = await _context.FonctionEcheances
                    .Include(fe => fe.Fonction)
                    .Where(fe => fe.FonctionId == fonctionId && fe.DateButoir <= dateLimite)
                    .OrderBy(fe => fe.DateButoir)
                    .Select(fe => new FonctionEcheanceDto
                    {
                        Id = fe.Id,
                        Libelle = fe.Libelle,
                        DateButoir = fe.DateButoir,
                        Frequence = fe.Frequence,
                        FrequenceLibelle = fe.Frequence.ToString(),
                        FonctionId = fe.FonctionId,
                        FonctionNom = fe.Fonction.NomFonction,
                        EstEchue = fe.DateButoir < DateTime.UtcNow && fe.Frequence == FrequenceType.Unique,
                        EstRecurrente = fe.Frequence != FrequenceType.Unique
                    })
                    .ToListAsync();

                return Ok(prochaines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des prochaines échéances de la fonction {FonctionId}", fonctionId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des prochaines échéances");
            }
        }

        // GET: api/fonctions/{fonctionId}/echeances/frequences
        [HttpGet("frequences")]
        public ActionResult<IEnumerable<FrequenceInfo>> GetFrequences()
        {
            var frequences = Enum.GetValues<FrequenceType>()
                .Select(f => new FrequenceInfo
                {
                    Valeur = f,
                    Libelle = f.ToString(),
                    Description = GetFrequenceDescription(f)
                })
                .ToList();

            return Ok(frequences);
        }

        private static string GetFrequenceDescription(FrequenceType frequence)
        {
            return frequence switch
            {
                FrequenceType.Unique => "Une seule fois à la date spécifiée",
                FrequenceType.Quotidienne => "Tous les jours",
                FrequenceType.Hebdomadaire => "Toutes les semaines",
                FrequenceType.Mensuelle => "Tous les mois",
                FrequenceType.Trimestrielle => "Tous les 3 mois",
                FrequenceType.Semestrielle => "Tous les 6 mois",
                FrequenceType.Annuelle => "Tous les ans",
                FrequenceType.ParMandat => "À chaque mandat",
                _ => frequence.ToString()
            };
        }
    }

    // DTOs pour les échéances de fonction
    public class FonctionEcheanceDto
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public DateTime DateButoir { get; set; }
        public FrequenceType Frequence { get; set; }
        public string FrequenceLibelle { get; set; } = string.Empty;
        public Guid FonctionId { get; set; }
        public string FonctionNom { get; set; } = string.Empty;
        public bool EstEchue { get; set; }
        public bool EstRecurrente { get; set; }
    }

    public class CreateFonctionEcheanceRequest
    {
        [Required(ErrorMessage = "Le libellé est obligatoire")]
        [MaxLength(200, ErrorMessage = "Le libellé ne peut pas dépasser 200 caractères")]
        public string Libelle { get; set; } = string.Empty;

        [Required(ErrorMessage = "La date butoir est obligatoire")]
        public DateTime DateButoir { get; set; }

        [Required(ErrorMessage = "La fréquence est obligatoire")]
        public FrequenceType Frequence { get; set; } = FrequenceType.Unique;
    }

    public class UpdateFonctionEcheanceRequest
    {
        [MaxLength(200, ErrorMessage = "Le libellé ne peut pas dépasser 200 caractères")]
        public string? Libelle { get; set; }

        public DateTime? DateButoir { get; set; }

        public FrequenceType? Frequence { get; set; }
    }

    public class FrequenceInfo
    {
        public FrequenceType Valeur { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}