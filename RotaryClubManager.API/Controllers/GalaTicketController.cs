using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/gala-tickets")]
    [ApiController]
    [Authorize]
    public class GalaTicketController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GalaTicketController> _logger;

        public GalaTicketController(
            ApplicationDbContext context,
            ILogger<GalaTicketController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/gala-tickets/gala/{galaId}
        [HttpGet("gala/{galaId:guid}")]
        public async Task<ActionResult<IEnumerable<GalaTicketInfoDto>>> GetTicketsByGala(
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

                var query = _context.GalaTickets
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

                var tickets = await query
                    .OrderBy(t => t.Membre != null ? t.Membre.LastName : t.Externe)
                    .ThenBy(t => t.Membre != null ? t.Membre.FirstName : "")
                    .Select(t => new GalaTicketInfoDto
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

                return Ok(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des tickets du gala {GalaId}", galaId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des tickets");
            }
        }

        // GET: api/gala-tickets/membre/{membreId}
        [HttpGet("membre/{membreId}")]
        public async Task<ActionResult<IEnumerable<GalaTicketInfoDto>>> GetTicketsByMembre(string membreId)
        {
            try
            {
                // Vérifier que le membre existe
                var membreExists = await _context.Users.AnyAsync(u => u.Id == membreId);
                if (!membreExists)
                {
                    return NotFound($"Membre avec l'ID {membreId} introuvable");
                }

                var tickets = await _context.GalaTickets
                    .Include(t => t.Gala)
                    .Include(t => t.Membre)
                    .Where(t => t.MembreId == membreId)
                    .OrderByDescending(t => t.Gala.Date)
                    .Select(t => new GalaTicketInfoDto
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

                return Ok(tickets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des tickets du membre {MembreId}", membreId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des tickets");
            }
        }

        // GET: api/gala-tickets/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GalaTicketDetailInfoDto>> GetTicket(Guid id)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant du ticket est invalide");
                }

                var ticket = await _context.GalaTickets
                    .Include(t => t.Gala)
                    .Include(t => t.Membre)
                    .Where(t => t.Id == id)
                    .Select(t => new GalaTicketDetailInfoDto
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

                if (ticket == null)
                {
                    return NotFound($"Ticket avec l'ID {id} introuvable");
                }

                return Ok(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du ticket {TicketId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du ticket");
            }
        }

        // POST: api/gala-tickets
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary,Treasurer")]
        public async Task<ActionResult<GalaTicketInfoDto>> CreateTicket([FromBody] CreateGalaTicketRequest request)
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

                    // Vérifier qu'il n'existe pas déjà un ticket pour ce membre et ce gala
                    var existingTicket = await _context.GalaTickets
                        .AnyAsync(t => t.GalaId == request.GalaId && t.MembreId == request.MembreId);

                    if (existingTicket)
                    {
                        return BadRequest("Un ticket existe déjà pour ce membre dans ce gala. Utilisez l'endpoint de modification pour changer la quantité.");
                    }
                }

                var ticket = new GalaTicket
                {
                    Id = Guid.NewGuid(),
                    GalaId = request.GalaId,
                    MembreId = !string.IsNullOrEmpty(request.MembreId) ? request.MembreId : null,
                    Externe = !string.IsNullOrEmpty(request.Externe) ? request.Externe : null,
                    Quantite = request.Quantite
                };

                _context.GalaTickets.Add(ticket);
                await _context.SaveChangesAsync();

                var participantInfo = !string.IsNullOrEmpty(request.MembreId)
                    ? $"Membre {request.MembreId}"
                    : $"Externe '{request.Externe}'";

                _logger.LogInformation("Ticket créé avec l'ID {Id} - {ParticipantInfo} → Gala {GalaId}, Quantité: {Quantite}",
                    ticket.Id, participantInfo, request.GalaId, request.Quantite);

                // Récupérer le ticket avec les données complètes
                var result = await _context.GalaTickets
                    .Include(t => t.Membre)
                    .Where(t => t.Id == ticket.Id)
                    .Select(t => new GalaTicketInfoDto
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

                return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du ticket");
                return StatusCode(500, "Une erreur est survenue lors de la création du ticket");
            }
        }

        // PUT: api/gala-tickets/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary,Treasurer")]
        public async Task<IActionResult> UpdateTicket(Guid id, [FromBody] UpdateGalaTicketRequest request)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant du ticket est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var ticket = await _context.GalaTickets.FindAsync(id);
                if (ticket == null)
                {
                    return NotFound($"Ticket avec l'ID {id} introuvable");
                }

                // Mettre à jour les champs si fournis
                if (request.Quantite.HasValue)
                    ticket.Quantite = request.Quantite.Value;

                if (request.Externe != null)
                    ticket.Externe = !string.IsNullOrEmpty(request.Externe) ? request.Externe : null;

                _context.Entry(ticket).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Ticket {Id} mis à jour avec succès - Nouvelle quantité: {Quantite}, Externe: {Externe}",
                    id, ticket.Quantite, ticket.Externe);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du ticket {TicketId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour du ticket");
            }
        }

        // DELETE: api/gala-tickets/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary,Treasurer")]
        public async Task<IActionResult> DeleteTicket(Guid id)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant du ticket est invalide");
                }

                var ticket = await _context.GalaTickets
                    .Include(t => t.Membre)
                    .Include(t => t.Gala)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (ticket == null)
                {
                    return NotFound($"Ticket avec l'ID {id} introuvable");
                }

                _context.GalaTickets.Remove(ticket);
                await _context.SaveChangesAsync();

                var participantInfo = ticket.Membre != null
                    ? $"{ticket.Membre.FirstName} {ticket.Membre.LastName}"
                    : ticket.Externe;

                _logger.LogInformation("Ticket supprimé - Participant '{ParticipantInfo}' retiré du gala '{GalaLibelle}' (Quantité: {Quantite})",
                    participantInfo, ticket.Gala.Libelle, ticket.Quantite);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du ticket {TicketId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la suppression du ticket");
            }
        }

        // GET: api/gala-tickets/gala/{galaId}/statistiques
        [HttpGet("gala/{galaId:guid}/statistiques")]
        public async Task<ActionResult<GalaTicketStatistiquesDto>> GetStatistiquesTicketsByGala(Guid galaId)
        {
            try
            {
                // Vérifier que le gala existe
                var gala = await _context.Galas.FindAsync(galaId);
                if (gala == null)
                {
                    return NotFound($"Gala avec l'ID {galaId} introuvable");
                }

                var statistiques = await _context.GalaTickets
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

                var totalTicketsDisponibles = gala.NombreSouchesTickets * gala.QuantiteParSoucheTickets;
                var totalTicketsVendus = statistiques.Sum(s => s.Quantite);
                var nombreParticipants = statistiques.Count;

                var result = new GalaTicketStatistiquesDto
                {
                    GalaId = galaId,
                    TotalTicketsDisponibles = totalTicketsDisponibles,
                    TotalTicketsVendus = totalTicketsVendus,
                    TotalTicketsRestants = totalTicketsDisponibles - totalTicketsVendus,
                    PourcentageVendu = totalTicketsDisponibles > 0
                        ? Math.Round((double)totalTicketsVendus / totalTicketsDisponibles * 100, 2)
                        : 0,
                    NombreParticipants = nombreParticipants,
                    QuantiteMoyenneParParticipant = nombreParticipants > 0
                        ? Math.Round((double)totalTicketsVendus / nombreParticipants, 2)
                        : 0,
                    VentesParParticipant = statistiques.Select(s => new GalaTicketVenteParParticipantDto
                    {
                        MembreId = s.MembreId,
                        Externe = s.Externe,
                        ParticipantNom = s.ParticipantNom ?? "Inconnu",
                        Quantite = s.Quantite,
                        PourcentageDuTotal = totalTicketsVendus > 0
                            ? Math.Round((double)s.Quantite / totalTicketsVendus * 100, 2)
                            : 0
                    }).OrderByDescending(v => v.Quantite).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques des tickets du gala {GalaId}", galaId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
            }
        }

        // GET: api/gala-tickets/gala/{galaId}/top-vendeurs
        [HttpGet("gala/{galaId:guid}/top-vendeurs")]
        public async Task<ActionResult<IEnumerable<GalaTicketVenteParParticipantDto>>> GetTopVendeurs(
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

                var topVendeurs = await _context.GalaTickets
                    .Include(t => t.Membre)
                    .Where(t => t.GalaId == galaId)
                    .OrderByDescending(t => t.Quantite)
                    .Take(limit)
                    .Select(t => new GalaTicketVenteParParticipantDto
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
                _logger.LogError(ex, "Erreur lors de la récupération du top vendeurs du gala {GalaId}", galaId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du top vendeurs");
            }
        }

        // POST: api/gala-tickets/bulk-create
        [HttpPost("bulk-create")]
        [Authorize(Roles = "Admin,President,Secretary,Treasurer")]
        public async Task<ActionResult<BulkTicketResultDto>> CreateBulkTickets([FromBody] BulkCreateTicketsRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = new BulkTicketResultDto();
                var ticketsToAdd = new List<GalaTicket>();

                foreach (var ticketRequest in request.Tickets)
                {
                    try
                    {
                        // Vérifier que le gala existe
                        var galaExists = await _context.Galas.AnyAsync(g => g.Id == ticketRequest.GalaId);
                        if (!galaExists)
                        {
                            result.Erreurs.Add($"Gala avec l'ID {ticketRequest.GalaId} introuvable");
                            continue;
                        }

                        // Vérifier qu'au moins un des deux champs est renseigné
                        if (string.IsNullOrEmpty(ticketRequest.MembreId) && string.IsNullOrEmpty(ticketRequest.Externe))
                        {
                            result.Erreurs.Add("Soit l'ID du membre soit le nom externe doit être renseigné");
                            continue;
                        }

                        // Si un membre est spécifié, vérifier qu'il existe et qu'il n'a pas déjà de ticket
                        if (!string.IsNullOrEmpty(ticketRequest.MembreId))
                        {
                            var membreExists = await _context.Users.AnyAsync(u => u.Id == ticketRequest.MembreId);
                            if (!membreExists)
                            {
                                result.Erreurs.Add($"Membre avec l'ID {ticketRequest.MembreId} introuvable");
                                continue;
                            }

                            var existingTicket = await _context.GalaTickets
                                .AnyAsync(t => t.GalaId == ticketRequest.GalaId && t.MembreId == ticketRequest.MembreId);

                            if (existingTicket)
                            {
                                result.Erreurs.Add($"Un ticket existe déjà pour le membre {ticketRequest.MembreId} dans le gala {ticketRequest.GalaId}");
                                continue;
                            }
                        }

                        var ticket = new GalaTicket
                        {
                            Id = Guid.NewGuid(),
                            GalaId = ticketRequest.GalaId,
                            MembreId = !string.IsNullOrEmpty(ticketRequest.MembreId) ? ticketRequest.MembreId : null,
                            Externe = !string.IsNullOrEmpty(ticketRequest.Externe) ? ticketRequest.Externe : null,
                            Quantite = ticketRequest.Quantite
                        };

                        ticketsToAdd.Add(ticket);
                        result.TicketsCreees++;
                    }
                    catch (Exception ex)
                    {
                        result.Erreurs.Add($"Erreur lors du traitement du ticket : {ex.Message}");
                    }
                }

                if (ticketsToAdd.Any())
                {
                    _context.GalaTickets.AddRange(ticketsToAdd);
                    await _context.SaveChangesAsync();
                }

                result.NombreTotal = request.Tickets.Count;

                _logger.LogInformation("Création en masse de tickets terminée : {NombreCreees}/{NombreTotal} tickets créés",
                    result.TicketsCreees, result.NombreTotal);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création en masse des tickets");
                return StatusCode(500, "Une erreur est survenue lors de la création en masse des tickets");
            }
        }
    }

    // DTOs mis à jour pour les tickets
    public class GalaTicketInfoDto
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

    public class GalaTicketDetailInfoDto : GalaTicketInfoDto
    {
        public string GalaLieu { get; set; } = string.Empty;
    }

    public class CreateGalaTicketRequest
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

    public class UpdateGalaTicketRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "La quantité doit être supérieure à 0")]
        public int? Quantite { get; set; }

        [StringLength(250, ErrorMessage = "Le nom externe ne peut pas dépasser 250 caractères")]
        public string? Externe { get; set; }
    }

    public class BulkCreateTicketsRequest
    {
        [Required(ErrorMessage = "La liste des tickets est obligatoire")]
        [MinLength(1, ErrorMessage = "Au moins un ticket est requis")]
        public List<CreateGalaTicketRequest> Tickets { get; set; } = new List<CreateGalaTicketRequest>();
    }

    public class BulkTicketResultDto
    {
        public int NombreTotal { get; set; }
        public int TicketsCreees { get; set; }
        public List<string> Erreurs { get; set; } = new List<string>();
    }

    public class GalaTicketStatistiquesDto
    {
        public Guid GalaId { get; set; }
        public int TotalTicketsDisponibles { get; set; }
        public int TotalTicketsVendus { get; set; }
        public int TotalTicketsRestants { get; set; }
        public double PourcentageVendu { get; set; }
        public int NombreParticipants { get; set; }
        public double QuantiteMoyenneParParticipant { get; set; }
        public List<GalaTicketVenteParParticipantDto> VentesParParticipant { get; set; } = new List<GalaTicketVenteParParticipantDto>();
    }

    public class GalaTicketVenteParParticipantDto
    {
        public string? MembreId { get; set; }
        public string? Externe { get; set; }
        public string ParticipantNom { get; set; } = string.Empty;
        public int Quantite { get; set; }
        public double PourcentageDuTotal { get; set; }
    }
}