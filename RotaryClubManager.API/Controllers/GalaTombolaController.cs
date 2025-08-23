using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/gala-tombolas")]
    [ApiController]
    [Authorize]
    public class GalaTombolaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GalaTombolaController> _logger;

        public GalaTombolaController(
            ApplicationDbContext context,
            ILogger<GalaTombolaController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/gala-tombolas/gala/{galaId}
        [HttpGet("gala/{galaId:guid}")]
        public async Task<ActionResult<IEnumerable<GalaTombolaInfoDto>>> GetTombolasByGala(
            Guid galaId,
            [FromQuery] string? recherche = null)
        {
            try
            {
                // Vérifier que le gala existe
                var galaExists = await _context.Galas.AnyAsync(g => g.Id == galaId);
                if (!galaExists)
                {
                    return NotFound($"Gala avec l'ID {galaId} introuvable");
                }

                var query = _context.GalaTombolas
                    .Include(t => t.Membre)
                    .Where(t => t.GalaId == galaId);

                // Filtre par terme de recherche
                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(t =>
                        (t.Membre != null && (
                            t.Membre.FirstName.ToLower().Contains(termeLower) ||
                            t.Membre.LastName.ToLower().Contains(termeLower) ||
                            t.Membre.Email.ToLower().Contains(termeLower)
                        )) ||
                        (t.Externe != null && t.Externe.ToLower().Contains(termeLower)));
                }

                var tombolas = await query
                    .OrderBy(t => t.Membre != null ? t.Membre.LastName : t.Externe)
                    .ThenBy(t => t.Membre != null ? t.Membre.FirstName : "")
                    .Select(t => new GalaTombolaInfoDto
                    {
                        Id = t.Id,
                        GalaId = t.GalaId,
                        MembreId = t.MembreId,
                        MembreNom = t.Membre != null ? $"{t.Membre.FirstName} {t.Membre.LastName}" : null,
                        MembreEmail = t.Membre != null ? t.Membre.Email : null,
                        Externe = t.Externe,
                        Quantite = t.Quantite
                    })
                    .ToListAsync();

                return Ok(tombolas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des tombolas du gala {GalaId}", galaId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des tombolas");
            }
        }

        // GET: api/gala-tombolas/membre/{membreId}
        [HttpGet("membre/{membreId}")]
        public async Task<ActionResult<IEnumerable<GalaTombolaInfoDto>>> GetTombolasByMembre(string membreId)
        {
            try
            {
                // Vérifier que le membre existe
                var membreExists = await _context.Users.AnyAsync(u => u.Id == membreId);
                if (!membreExists)
                {
                    return NotFound($"Membre avec l'ID {membreId} introuvable");
                }

                var tombolas = await _context.GalaTombolas
                    .Include(t => t.Gala)
                    .Include(t => t.Membre)
                    .Where(t => t.MembreId == membreId)
                    .OrderByDescending(t => t.Gala.Date)
                    .Select(t => new GalaTombolaInfoDto
                    {
                        Id = t.Id,
                        GalaId = t.GalaId,
                        GalaLibelle = t.Gala.Libelle,
                        GalaDate = t.Gala.Date,
                        MembreId = t.MembreId,
                        MembreNom = t.Membre != null ? $"{t.Membre.FirstName} {t.Membre.LastName}" : null,
                        MembreEmail = t.Membre != null ? t.Membre.Email : null,
                        Externe = t.Externe,
                        Quantite = t.Quantite
                    })
                    .ToListAsync();

                return Ok(tombolas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des tombolas du membre {MembreId}", membreId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des tombolas");
            }
        }

        // GET: api/gala-tombolas/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GalaTombolaDetailInfoDto>> GetTombola(Guid id)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la tombola est invalide");
                }

                var tombola = await _context.GalaTombolas
                    .Include(t => t.Gala)
                    .Include(t => t.Membre)
                    .Where(t => t.Id == id)
                    .Select(t => new GalaTombolaDetailInfoDto
                    {
                        Id = t.Id,
                        GalaId = t.GalaId,
                        GalaLibelle = t.Gala.Libelle,
                        GalaDate = t.Gala.Date,
                        GalaLieu = t.Gala.Lieu,
                        MembreId = t.MembreId,
                        MembreNom = t.Membre != null ? $"{t.Membre.FirstName} {t.Membre.LastName}" : null,
                        MembreEmail = t.Membre != null ? t.Membre.Email : null,
                        Externe = t.Externe,
                        Quantite = t.Quantite
                    })
                    .FirstOrDefaultAsync();

                if (tombola == null)
                {
                    return NotFound($"Tombola avec l'ID {id} introuvable");
                }

                return Ok(tombola);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la tombola {TombolaId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de la tombola");
            }
        }

        // POST: api/gala-tombolas
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary,Treasurer")]
        public async Task<ActionResult<GalaTombolaInfoDto>> CreateTombola([FromBody] CreateGalaTombolaRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier que le gala existe
                var galaExists = await _context.Galas.AnyAsync(g => g.Id == request.GalaId);
                if (!galaExists)
                {
                    return BadRequest($"Gala avec l'ID {request.GalaId} introuvable");
                }

                // Vérifier qu'au moins un des deux champs (MembreId ou Externe) est renseigné
                if (string.IsNullOrEmpty(request.MembreId) && string.IsNullOrEmpty(request.Externe))
                {
                    return BadRequest("Soit l'ID du membre soit le nom externe doit être renseigné");
                }

                // Si un membre est spécifié, vérifier qu'il existe
                if (!string.IsNullOrEmpty(request.MembreId))
                {
                    var membreExists = await _context.Users.AnyAsync(u => u.Id == request.MembreId);
                    if (!membreExists)
                    {
                        return BadRequest($"Membre avec l'ID {request.MembreId} introuvable");
                    }

                    // Vérifier qu'il n'existe pas déjà une tombola pour ce membre et ce gala
                    var existingTombola = await _context.GalaTombolas
                        .AnyAsync(t => t.GalaId == request.GalaId && t.MembreId == request.MembreId);

                    if (existingTombola)
                    {
                        return BadRequest("Une tombola existe déjà pour ce membre dans ce gala. Utilisez l'endpoint de modification pour changer la quantité.");
                    }
                }

                var tombola = new GalaTombola
                {
                    Id = Guid.NewGuid(),
                    GalaId = request.GalaId,
                    MembreId = !string.IsNullOrEmpty(request.MembreId) ? request.MembreId : null,
                    Externe = !string.IsNullOrEmpty(request.Externe) ? request.Externe : null,
                    Quantite = request.Quantite
                };

                _context.GalaTombolas.Add(tombola);
                await _context.SaveChangesAsync();

                var participantInfo = !string.IsNullOrEmpty(request.MembreId)
                    ? $"Membre {request.MembreId}"
                    : $"Externe '{request.Externe}'";

                _logger.LogInformation("Tombola créée avec l'ID {Id} - {ParticipantInfo} → Gala {GalaId}, Quantité: {Quantite}",
                    tombola.Id, participantInfo, request.GalaId, request.Quantite);

                // Récupérer la tombola avec les données complètes
                var result = await _context.GalaTombolas
                    .Include(t => t.Membre)
                    .Where(t => t.Id == tombola.Id)
                    .Select(t => new GalaTombolaInfoDto
                    {
                        Id = t.Id,
                        GalaId = t.GalaId,
                        MembreId = t.MembreId,
                        MembreNom = t.Membre != null ? $"{t.Membre.FirstName} {t.Membre.LastName}" : null,
                        MembreEmail = t.Membre != null ? t.Membre.Email : null,
                        Externe = t.Externe,
                        Quantite = t.Quantite
                    })
                    .FirstAsync();

                return CreatedAtAction(nameof(GetTombola), new { id = tombola.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la tombola");
                return StatusCode(500, "Une erreur est survenue lors de la création de la tombola");
            }
        }

        // PUT: api/gala-tombolas/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary,Treasurer")]
        public async Task<IActionResult> UpdateTombola(Guid id, [FromBody] UpdateGalaTombolaRequest request)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la tombola est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var tombola = await _context.GalaTombolas.FindAsync(id);
                if (tombola == null)
                {
                    return NotFound($"Tombola avec l'ID {id} introuvable");
                }

                // Mettre à jour les champs si fournis
                if (request.Quantite.HasValue)
                    tombola.Quantite = request.Quantite.Value;

                if (request.Externe != null)
                    tombola.Externe = !string.IsNullOrEmpty(request.Externe) ? request.Externe : null;

                _context.Entry(tombola).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tombola {Id} mise à jour avec succès - Nouvelle quantité: {Quantite}, Externe: {Externe}",
                    id, tombola.Quantite, tombola.Externe);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la tombola {TombolaId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de la tombola");
            }
        }

        // DELETE: api/gala-tombolas/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary,Treasurer")]
        public async Task<IActionResult> DeleteTombola(Guid id)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la tombola est invalide");
                }

                var tombola = await _context.GalaTombolas
                    .Include(t => t.Membre)
                    .Include(t => t.Gala)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tombola == null)
                {
                    return NotFound($"Tombola avec l'ID {id} introuvable");
                }

                _context.GalaTombolas.Remove(tombola);
                await _context.SaveChangesAsync();

                var participantInfo = tombola.Membre != null
                    ? $"{tombola.Membre.FirstName} {tombola.Membre.LastName}"
                    : tombola.Externe;

                _logger.LogInformation("Tombola supprimée - Participant '{ParticipantInfo}' retiré du gala '{GalaLibelle}' (Quantité: {Quantite})",
                    participantInfo, tombola.Gala.Libelle, tombola.Quantite);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la tombola {TombolaId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de la tombola");
            }
        }

        // GET: api/gala-tombolas/gala/{galaId}/statistiques
        [HttpGet("gala/{galaId:guid}/statistiques")]
        public async Task<ActionResult<GalaTombolaStatistiquesDto>> GetStatistiquesTombolasByGala(Guid galaId)
        {
            try
            {
                // Vérifier que le gala existe
                var gala = await _context.Galas.FindAsync(galaId);
                if (gala == null)
                {
                    return NotFound($"Gala avec l'ID {galaId} introuvable");
                }

                var statistiques = await _context.GalaTombolas
                    .Include(t => t.Membre)
                    .Where(t => t.GalaId == galaId)
                    .Select(t => new
                    {
                        t.MembreId,
                        t.Externe,
                        ParticipantNom = t.Membre != null ? $"{t.Membre.FirstName} {t.Membre.LastName}" : t.Externe,
                        t.Quantite
                    })
                    .ToListAsync();

                var totalTombolasDisponibles = gala.NombreSouchesTombola * gala.QuantiteParSoucheTombola;
                var totalTombolasVendues = statistiques.Sum(s => s.Quantite);
                var nombreParticipants = statistiques.Count;

                var result = new GalaTombolaStatistiquesDto
                {
                    GalaId = galaId,
                    TotalTombolasDisponibles = totalTombolasDisponibles,
                    TotalTombolasVendues = totalTombolasVendues,
                    TotalTombolasRestantes = totalTombolasDisponibles - totalTombolasVendues,
                    PourcentageVendu = totalTombolasDisponibles > 0
                        ? Math.Round((double)totalTombolasVendues / totalTombolasDisponibles * 100, 2)
                        : 0,
                    NombreParticipants = nombreParticipants,
                    QuantiteMoyenneParParticipant = nombreParticipants > 0
                        ? Math.Round((double)totalTombolasVendues / nombreParticipants, 2)
                        : 0,
                    VentesParParticipant = statistiques.Select(s => new GalaTombolaVenteParParticipantDto
                    {
                        MembreId = s.MembreId,
                        Externe = s.Externe,
                        ParticipantNom = s.ParticipantNom ?? "Inconnu",
                        Quantite = s.Quantite,
                        PourcentageDuTotal = totalTombolasVendues > 0
                            ? Math.Round((double)s.Quantite / totalTombolasVendues * 100, 2)
                            : 0
                    }).OrderByDescending(v => v.Quantite).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques des tombolas du gala {GalaId}", galaId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
            }
        }

        // GET: api/gala-tombolas/gala/{galaId}/top-vendeurs
        [HttpGet("gala/{galaId:guid}/top-vendeurs")]
        public async Task<ActionResult<IEnumerable<GalaTombolaVenteParParticipantDto>>> GetTopVendeurs(
            Guid galaId,
            [FromQuery] int limit = 10)
        {
            try
            {
                // Vérifier que le gala existe
                var galaExists = await _context.Galas.AnyAsync(g => g.Id == galaId);
                if (!galaExists)
                {
                    return NotFound($"Gala avec l'ID {galaId} introuvable");
                }

                var topVendeurs = await _context.GalaTombolas
                    .Include(t => t.Membre)
                    .Where(t => t.GalaId == galaId)
                    .OrderByDescending(t => t.Quantite)
                    .Take(limit)
                    .Select(t => new GalaTombolaVenteParParticipantDto
                    {
                        MembreId = t.MembreId,
                        Externe = t.Externe,
                        ParticipantNom = t.Membre != null ? $"{t.Membre.FirstName} {t.Membre.LastName}" : t.Externe ?? "Inconnu",
                        Quantite = t.Quantite,
                        PourcentageDuTotal = 0 // Sera calculé après si nécessaire
                    })
                    .ToListAsync();

                return Ok(topVendeurs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du top vendeurs de tombolas du gala {GalaId}", galaId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du top vendeurs");
            }
        }

        // POST: api/gala-tombolas/bulk-create
        [HttpPost("bulk-create")]
        [Authorize(Roles = "Admin,President,Secretary,Treasurer")]
        public async Task<ActionResult<BulkTombolaResultDto>> CreateBulkTombolas([FromBody] BulkCreateTombolasRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = new BulkTombolaResultDto();
                var tombolasToAdd = new List<GalaTombola>();

                foreach (var tombolaRequest in request.Tombolas)
                {
                    try
                    {
                        // Vérifier que le gala existe
                        var galaExists = await _context.Galas.AnyAsync(g => g.Id == tombolaRequest.GalaId);
                        if (!galaExists)
                        {
                            result.Erreurs.Add($"Gala avec l'ID {tombolaRequest.GalaId} introuvable");
                            continue;
                        }

                        // Vérifier qu'au moins un des deux champs est renseigné
                        if (string.IsNullOrEmpty(tombolaRequest.MembreId) && string.IsNullOrEmpty(tombolaRequest.Externe))
                        {
                            result.Erreurs.Add("Soit l'ID du membre soit le nom externe doit être renseigné");
                            continue;
                        }

                        // Si un membre est spécifié, vérifier qu'il existe et qu'il n'a pas déjà de tombola
                        if (!string.IsNullOrEmpty(tombolaRequest.MembreId))
                        {
                            var membreExists = await _context.Users.AnyAsync(u => u.Id == tombolaRequest.MembreId);
                            if (!membreExists)
                            {
                                result.Erreurs.Add($"Membre avec l'ID {tombolaRequest.MembreId} introuvable");
                                continue;
                            }

                            var existingTombola = await _context.GalaTombolas
                                .AnyAsync(t => t.GalaId == tombolaRequest.GalaId && t.MembreId == tombolaRequest.MembreId);

                            if (existingTombola)
                            {
                                result.Erreurs.Add($"Une tombola existe déjà pour le membre {tombolaRequest.MembreId} dans le gala {tombolaRequest.GalaId}");
                                continue;
                            }
                        }

                        var tombola = new GalaTombola
                        {
                            Id = Guid.NewGuid(),
                            GalaId = tombolaRequest.GalaId,
                            MembreId = !string.IsNullOrEmpty(tombolaRequest.MembreId) ? tombolaRequest.MembreId : null,
                            Externe = !string.IsNullOrEmpty(tombolaRequest.Externe) ? tombolaRequest.Externe : null,
                            Quantite = tombolaRequest.Quantite
                        };

                        tombolasToAdd.Add(tombola);
                        result.TombolasCreees++;
                    }
                    catch (Exception ex)
                    {
                        result.Erreurs.Add($"Erreur lors du traitement de la tombola : {ex.Message}");
                    }
                }

                if (tombolasToAdd.Any())
                {
                    _context.GalaTombolas.AddRange(tombolasToAdd);
                    await _context.SaveChangesAsync();
                }

                result.NombreTotal = request.Tombolas.Count;

                _logger.LogInformation("Création en masse de tombolas terminée : {NombreCreees}/{NombreTotal} tombolas créées",
                    result.TombolasCreees, result.NombreTotal);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création en masse des tombolas");
                return StatusCode(500, "Une erreur est survenue lors de la création en masse des tombolas");
            }
        }

        // GET: api/gala-tombolas/gala/{galaId}/tirage-gagnants
        [HttpGet("gala/{galaId:guid}/tirage-gagnants")]
        public async Task<ActionResult<IEnumerable<GalaTombolaGagnantDto>>> TirageGagnants(
            Guid galaId,
            [FromQuery] int nombreGagnants = 1)
        {
            try
            {
                // Vérifier que le gala existe
                var galaExists = await _context.Galas.AnyAsync(g => g.Id == galaId);
                if (!galaExists)
                {
                    return NotFound($"Gala avec l'ID {galaId} introuvable");
                }

                if (nombreGagnants <= 0)
                {
                    return BadRequest("Le nombre de gagnants doit être supérieur à 0");
                }

                // Récupérer toutes les tombolas du gala
                var tombolas = await _context.GalaTombolas
                    .Include(t => t.Membre)
                    .Where(t => t.GalaId == galaId)
                    .ToListAsync();

                if (!tombolas.Any())
                {
                    return BadRequest("Aucune tombola trouvée pour ce gala");
                }

                // Créer une liste de "tickets" pour le tirage (chaque quantité = plusieurs chances)
                var ticketsTirage = new List<(GalaTombola tombola, int numeroTicket)>();
                int numeroTicketCourant = 1;

                foreach (var tombola in tombolas)
                {
                    for (int i = 0; i < tombola.Quantite; i++)
                    {
                        ticketsTirage.Add((tombola, numeroTicketCourant));
                        numeroTicketCourant++;
                    }
                }

                // Effectuer le tirage aléatoire
                var random = new Random();
                var gagnants = new List<GalaTombolaGagnantDto>();
                var ticketsUtilises = new HashSet<int>();

                for (int i = 0; i < Math.Min(nombreGagnants, ticketsTirage.Count); i++)
                {
                    int index;
                    do
                    {
                        index = random.Next(ticketsTirage.Count);
                    } while (ticketsUtilises.Contains(index));

                    ticketsUtilises.Add(index);
                    var (tombola, numeroTicket) = ticketsTirage[index];

                    var participantNom = tombola.Membre != null
                        ? $"{tombola.Membre.FirstName} {tombola.Membre.LastName}"
                        : tombola.Externe ?? "Inconnu";

                    var participantEmail = tombola.Membre?.Email ?? "";

                    gagnants.Add(new GalaTombolaGagnantDto
                    {
                        Position = i + 1,
                        MembreId = tombola.MembreId,
                        Externe = tombola.Externe,
                        ParticipantNom = participantNom,
                        ParticipantEmail = participantEmail,
                        NumeroTicketGagnant = numeroTicket,
                        QuantiteTotaleParticipant = tombola.Quantite
                    });
                }

                _logger.LogInformation("Tirage de tombola effectué pour le gala {GalaId} - {NombreGagnants} gagnant(s) tirés",
                    galaId, gagnants.Count);

                return Ok(gagnants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du tirage des gagnants de tombola du gala {GalaId}", galaId);
                return StatusCode(500, "Une erreur est survenue lors du tirage des gagnants");
            }
        }
    }

    // DTOs mis à jour pour les tombolas
    public class GalaTombolaInfoDto
    {
        public Guid Id { get; set; }
        public Guid GalaId { get; set; }
        public string? GalaLibelle { get; set; }
        public DateTime? GalaDate { get; set; }
        public string? MembreId { get; set; }
        public string? MembreNom { get; set; }
        public string? MembreEmail { get; set; }
        public string? Externe { get; set; }
        public int Quantite { get; set; }
    }

    public class GalaTombolaDetailInfoDto : GalaTombolaInfoDto
    {
        public string GalaLieu { get; set; } = string.Empty;
    }

    public class CreateGalaTombolaRequest
    {
        [Required(ErrorMessage = "L'ID du gala est obligatoire")]
        public Guid GalaId { get; set; }

        public string? MembreId { get; set; }

        [StringLength(250, ErrorMessage = "Le nom externe ne peut pas dépasser 250 caractères")]
        public string? Externe { get; set; }

        [Required(ErrorMessage = "La quantité est obligatoire")]
        [Range(1, int.MaxValue, ErrorMessage = "La quantité doit être supérieure à 0")]
        public int Quantite { get; set; }
    }

    public class UpdateGalaTombolaRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "La quantité doit être supérieure à 0")]
        public int? Quantite { get; set; }

        [StringLength(250, ErrorMessage = "Le nom externe ne peut pas dépasser 250 caractères")]
        public string? Externe { get; set; }
    }

    public class BulkCreateTombolasRequest
    {
        [Required(ErrorMessage = "La liste des tombolas est obligatoire")]
        [MinLength(1, ErrorMessage = "Au moins une tombola est requise")]
        public List<CreateGalaTombolaRequest> Tombolas { get; set; } = new List<CreateGalaTombolaRequest>();
    }

    public class BulkTombolaResultDto
    {
        public int NombreTotal { get; set; }
        public int TombolasCreees { get; set; }
        public List<string> Erreurs { get; set; } = new List<string>();
    }

    public class GalaTombolaStatistiquesDto
    {
        public Guid GalaId { get; set; }
        public int TotalTombolasDisponibles { get; set; }
        public int TotalTombolasVendues { get; set; }
        public int TotalTombolasRestantes { get; set; }
        public double PourcentageVendu { get; set; }
        public int NombreParticipants { get; set; }
        public double QuantiteMoyenneParParticipant { get; set; }
        public List<GalaTombolaVenteParParticipantDto> VentesParParticipant { get; set; } = new List<GalaTombolaVenteParParticipantDto>();
    }

    public class GalaTombolaVenteParParticipantDto
    {
        public string? MembreId { get; set; }
        public string? Externe { get; set; }
        public string ParticipantNom { get; set; } = string.Empty;
        public int Quantite { get; set; }
        public double PourcentageDuTotal { get; set; }
    }

    public class GalaTombolaGagnantDto
    {
        public int Position { get; set; }
        public string? MembreId { get; set; }
        public string? Externe { get; set; }
        public string ParticipantNom { get; set; } = string.Empty;
        public string ParticipantEmail { get; set; } = string.Empty;
        public int NumeroTicketGagnant { get; set; }
        public int QuantiteTotaleParticipant { get; set; }
    }
}