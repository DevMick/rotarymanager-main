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
    public class TransitionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TransitionsController> _logger;

        public TransitionsController(
            ApplicationDbContext context,
            ILogger<TransitionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Gestion des Mandats

        // POST: api/transitions/mandats/nouveau
        [HttpPost("mandats/nouveau")]
        public async Task<ActionResult<Mandat>> CreerNouveauMandat(CreerMandatDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Vérifier que le club existe
                var club = await _context.Clubs.FindAsync(dto.ClubId);
                if (club == null)
                {
                    return NotFound($"Club avec l'ID {dto.ClubId} introuvable");
                }

                // Vérifier qu'il n'existe pas déjà un mandat pour cette année
                var mandatExistant = await _context.Mandats
                    .FirstOrDefaultAsync(m => m.ClubId == dto.ClubId && m.Annee == dto.Annee);

                if (mandatExistant != null)
                {
                    return BadRequest($"Un mandat existe déjà pour l'année {dto.Annee} dans ce club");
                }

                // Désactiver le mandat actuel s'il existe
                var mandatActuel = await _context.Mandats
                    .FirstOrDefaultAsync(m => m.ClubId == dto.ClubId && m.EstActuel);

                if (mandatActuel != null)
                {
                    mandatActuel.EstActuel = false;
                    _context.Entry(mandatActuel).State = EntityState.Modified;
                }

                // Créer le nouveau mandat
                var nouveauMandat = new Mandat
                {
                    Id = Guid.NewGuid(),
                    Annee = dto.Annee,
                    DateDebut = dto.DateDebut,
                    DateFin = dto.DateFin,
                    EstActuel = dto.EstActuel,
                    Description = dto.Description ?? string.Empty,
                    ClubId = dto.ClubId
                };

                _context.Mandats.Add(nouveauMandat);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Nouveau mandat créé pour l'année {Annee} du club {ClubId}",
                    dto.Annee, dto.ClubId);

                return CreatedAtAction(nameof(ObtenirMandat), new { id = nouveauMandat.Id }, nouveauMandat);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erreur lors de la création du nouveau mandat");
                return StatusCode(500, "Une erreur est survenue lors de la création du mandat");
            }
        }

        // GET: api/transitions/mandats/{id}
        [HttpGet("mandats/{id:guid}")]
        public async Task<ActionResult<Mandat>> ObtenirMandat(Guid id)
        {
            try
            {
                var mandat = await _context.Mandats
                    .Include(m => m.Club)
                    .Include(m => m.MembresCommission)
                        .ThenInclude(mc => mc.Membre)
                  
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (mandat == null)
                {
                    return NotFound($"Mandat avec l'ID {id} introuvable");
                }

                return Ok(mandat);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du mandat {MandatId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du mandat");
            }
        }

        // POST: api/transitions/mandats/{id}/activer
        [HttpPost("mandats/{id:guid}/activer")]
        public async Task<IActionResult> ActiverMandat(Guid id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var mandat = await _context.Mandats.FindAsync(id);
                if (mandat == null)
                {
                    return NotFound($"Mandat avec l'ID {id} introuvable");
                }

                // Désactiver tous les autres mandats du club
                var autresMandats = await _context.Mandats
                    .Where(m => m.ClubId == mandat.ClubId && m.Id != id && m.EstActuel)
                    .ToListAsync();

                foreach (var autreMandt in autresMandats)
                {
                    autreMandt.EstActuel = false;
                    _context.Entry(autreMandt).State = EntityState.Modified;
                }

                // Activer le mandat
                mandat.EstActuel = true;
                _context.Entry(mandat).State = EntityState.Modified;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Mandat {MandatId} activé pour le club {ClubId}", id, mandat.ClubId);

                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erreur lors de l'activation du mandat {MandatId}", id);
                return StatusCode(500, "Une erreur est survenue lors de l'activation du mandat");
            }
        }

        #endregion

        #region Nominations Comité

        // POST: api/transitions/comite/nommer
        [HttpPost("comite/nommer")]
        public async Task<ActionResult<MembreComite>> NommerMembreComite(NommerMembreComiteDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Vérifications d'existence
                var validationResult = await ValiderNominationComite(dto.MembreId, dto.PosteComiteId, dto.MandatId);
                if (validationResult != null)
                {
                    return validationResult;
                }

                // Vérifier si le membre n'est pas déjà nommé pour ce poste dans ce mandat
                var nominationExistante = await _context.MembresCommission
                    .FirstOrDefaultAsync(mc => mc.MembreId == dto.MembreId &&
                                              mc.MandatId == dto.MandatId &&
                                              mc.EstActif);

                if (nominationExistante != null)
                {
                    return BadRequest("Ce membre est déjà nommé à ce poste pour ce mandat");
                }

                // Désactiver l'ancien membre du poste s'il y en a un actif
                var ancienMembrePoste = await _context.MembresCommission
                    .FirstOrDefaultAsync(mc => 
                                              mc.MandatId == dto.MandatId &&
                                              mc.EstActif);

                if (ancienMembrePoste != null)
                {
                    ancienMembrePoste.EstActif = false;
                    ancienMembrePoste.DateDemission = DateTime.UtcNow;
                    _context.Entry(ancienMembrePoste).State = EntityState.Modified;
                }

                // Obtenir le ClubId depuis le poste
                //var poste = await _context.PostesComite.FindAsync(dto.PosteComiteId);

                // Créer la nouvelle nomination
                var nouvelleNomination = new MembreComite
                {
                    Id = Guid.NewGuid(),
                    MembreId = dto.MembreId,
                    PosteComiteId = dto.PosteComiteId,
                    MandatId = dto.MandatId,
                    ClubId = poste!.ClubId,
                    DateNomination = dto.DateNomination ?? DateTime.UtcNow,
                    EstActif = true,
                    Commentaires = dto.Commentaires ?? string.Empty
                };

                _context.MembresCommission.Add(nouvelleNomination);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Récupérer la nomination créée avec ses relations
                var nominationCreee = await _context.MembresCommission
                    .Include(mc => mc.Membre)
                    .Include(mc => mc.PosteComite)
                    .Include(mc => mc.Mandat)
                    .FirstOrDefaultAsync(mc => mc.Id == nouvelleNomination.Id);

                _logger.LogInformation("Membre {MembreId} nommé au poste {PosteId} pour le mandat {MandatId}",
                    dto.MembreId, dto.PosteComiteId, dto.MandatId);

                return CreatedAtAction(nameof(ObtenirNominationComite),
                    new { id = nouvelleNomination.Id }, nominationCreee);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erreur lors de la nomination au comité");
                return StatusCode(500, "Une erreur est survenue lors de la nomination");
            }
        }

        // POST: api/transitions/comite/{id}/demissionner
        [HttpPost("comite/{id:guid}/demissionner")]
        public async Task<IActionResult> DemissionnerMembreComite(Guid id, DemissionDto dto)
        {
            try
            {
                var nomination = await _context.MembresCommission.FindAsync(id);
                if (nomination == null)
                {
                    return NotFound($"Nomination avec l'ID {id} introuvable");
                }

                if (!nomination.EstActif)
                {
                    return BadRequest("Cette nomination n'est plus active");
                }

                nomination.EstActif = false;
                nomination.DateDemission = dto.DateDemission ?? DateTime.UtcNow;
                nomination.Commentaires += string.IsNullOrEmpty(nomination.Commentaires)
                    ? dto.Commentaires ?? string.Empty
                    : $" | {dto.Commentaires}";

                _context.Entry(nomination).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Démission du membre {MembreId} du poste {PosteId} pour le mandat {MandatId}",
                    nomination.MembreId, nomination.PosteComiteId, nomination.MandatId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la démission du comité {NominationId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la démission");
            }
        }

        // GET: api/transitions/comite/{id}
        [HttpGet("comite/{id:guid}")]
        public async Task<ActionResult<MembreComite>> ObtenirNominationComite(Guid id)
        {
            try
            {
                var nomination = await _context.MembresCommission
                    .Include(mc => mc.Membre)
                    .Include(mc => mc.PosteComite)
                    .Include(mc => mc.Mandat)
                    .Include(mc => mc.Club)
                    .FirstOrDefaultAsync(mc => mc.Id == id);

                if (nomination == null)
                {
                    return NotFound($"Nomination avec l'ID {id} introuvable");
                }

                return Ok(nomination);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la nomination {NominationId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération");
            }
        }

        #endregion

        #region Nominations Commission

        // POST: api/transitions/commission/nommer
        [HttpPost("commission/nommer")]
        public async Task<ActionResult<MembreCommission>> NommerMembreCommission(NommerMembreCommissionDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Vérifications d'existence
                var validationResult = await ValiderNominationCommission(dto.MembreId, dto.CommissionId, dto.MandatId);
                if (validationResult != null)
                {
                    return validationResult;
                }

                // Vérifier si le membre n'est pas déjà dans cette commission pour ce mandat
                var nominationExistante = await _context.MembresCommission
                    .FirstOrDefaultAsync(mc => mc.MembreId == dto.MembreId &&
                                              mc.CommissionId == dto.CommissionId &&
                                              mc.MandatId == dto.MandatId &&
                                              mc.EstActif);

                if (nominationExistante != null)
                {
                    return BadRequest("Ce membre fait déjà partie de cette commission pour ce mandat");
                }

                // Si c'est un responsable, désactiver l'ancien responsable
                if (dto.EstResponsable)
                {
                    var ancienResponsable = await _context.MembresCommission
                        .FirstOrDefaultAsync(mc => mc.CommissionId == dto.CommissionId &&
                                                  mc.MandatId == dto.MandatId &&
                                                  mc.EstResponsable &&
                                                  mc.EstActif);

                    if (ancienResponsable != null)
                    {
                        ancienResponsable.EstResponsable = false;
                        _context.Entry(ancienResponsable).State = EntityState.Modified;
                    }
                }

                // Obtenir le ClubId depuis la commission
                var commission = await _context.Commissions.FindAsync(dto.CommissionId);

                // Créer la nouvelle nomination
                var nouvelleNomination = new MembreCommission
                {
                    Id = Guid.NewGuid(),
                    MembreId = dto.MembreId,
                    CommissionId = dto.CommissionId,
                    MandatId = dto.MandatId,
                    ClubId = commission!.ClubId,
                    EstResponsable = dto.EstResponsable,
                    DateNomination = dto.DateNomination ?? DateTime.UtcNow,
                    EstActif = true,
                    Commentaires = dto.Commentaires ?? string.Empty
                };

                _context.MembresCommission.Add(nouvelleNomination);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Récupérer la nomination créée avec ses relations
                var nominationCreee = await _context.MembresCommission
                    .Include(mc => mc.Membre)
                    .Include(mc => mc.Commission)
                    .Include(mc => mc.Mandat)
                    .FirstOrDefaultAsync(mc => mc.Id == nouvelleNomination.Id);

                _logger.LogInformation("Membre {MembreId} nommé à la commission {CommissionId} pour le mandat {MandatId} (Responsable: {EstResponsable})",
                    dto.MembreId, dto.CommissionId, dto.MandatId, dto.EstResponsable);

                return CreatedAtAction(nameof(ObtenirNominationCommission),
                    new { id = nouvelleNomination.Id }, nominationCreee);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erreur lors de la nomination à la commission");
                return StatusCode(500, "Une erreur est survenue lors de la nomination");
            }
        }

        // POST: api/transitions/commission/{id}/demissionner
        [HttpPost("commission/{id:guid}/demissionner")]
        public async Task<IActionResult> DemissionnerMembreCommission(Guid id, DemissionDto dto)
        {
            try
            {
                var nomination = await _context.MembresCommission.FindAsync(id);
                if (nomination == null)
                {
                    return NotFound($"Nomination avec l'ID {id} introuvable");
                }

                if (!nomination.EstActif)
                {
                    return BadRequest("Cette nomination n'est plus active");
                }

                nomination.EstActif = false;
                nomination.DateDemission = dto.DateDemission ?? DateTime.UtcNow;
                nomination.Commentaires += string.IsNullOrEmpty(nomination.Commentaires)
                    ? dto.Commentaires ?? string.Empty
                    : $" | {dto.Commentaires}";

                _context.Entry(nomination).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Démission du membre {MembreId} de la commission {CommissionId} pour le mandat {MandatId}",
                    nomination.MembreId, nomination.CommissionId, nomination.MandatId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la démission de la commission {NominationId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la démission");
            }
        }

        // POST: api/transitions/commission/{id}/responsable
        [HttpPost("commission/{id:guid}/responsable")]
        public async Task<IActionResult> NommerResponsableCommission(Guid id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var nomination = await _context.MembresCommission
                    .Include(mc => mc.Commission)
                    .Include(mc => mc.Mandat)
                    .FirstOrDefaultAsync(mc => mc.Id == id);

                if (nomination == null)
                {
                    return NotFound($"Nomination avec l'ID {id} introuvable");
                }

                if (!nomination.EstActif)
                {
                    return BadRequest("Cette nomination n'est plus active");
                }

                if (nomination.EstResponsable)
                {
                    return BadRequest("Ce membre est déjà responsable de cette commission");
                }

                // Enlever le statut de responsable à l'ancien responsable
                var ancienResponsable = await _context.MembresCommission
                    .FirstOrDefaultAsync(mc => mc.CommissionId == nomination.CommissionId &&
                                              mc.MandatId == nomination.MandatId &&
                                              mc.EstResponsable &&
                                              mc.EstActif);

                if (ancienResponsable != null)
                {
                    ancienResponsable.EstResponsable = false;
                    _context.Entry(ancienResponsable).State = EntityState.Modified;
                }

                // Nommer le nouveau responsable
                nomination.EstResponsable = true;
                _context.Entry(nomination).State = EntityState.Modified;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Membre {MembreId} nommé responsable de la commission {CommissionId} pour le mandat {MandatId}",
                    nomination.MembreId, nomination.CommissionId, nomination.MandatId);

                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erreur lors de la nomination du responsable de commission");
                return StatusCode(500, "Une erreur est survenue lors de la nomination");
            }
        }

        // GET: api/transitions/commission/{id}
        [HttpGet("commission/{id:guid}")]
        public async Task<ActionResult<MembreCommission>> ObtenirNominationCommission(Guid id)
        {
            try
            {
                var nomination = await _context.MembresCommission
                    .Include(mc => mc.Membre)
                    .Include(mc => mc.Commission)
                    .Include(mc => mc.Mandat)
                    .Include(mc => mc.Club)
                    .FirstOrDefaultAsync(mc => mc.Id == id);

                if (nomination == null)
                {
                    return NotFound($"Nomination avec l'ID {id} introuvable");
                }

                return Ok(nomination);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la nomination {NominationId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération");
            }
        }

        #endregion

        #region Méthodes de validation

        private async Task<ActionResult?> ValiderNominationComite(string membreId, Guid posteComiteId, Guid mandatId)
        {
            // Vérifier que le membre existe
            var membre = await _context.Users.FindAsync(membreId);
            if (membre == null)
            {
                return NotFound($"Membre avec l'ID {membreId} introuvable");
            }

            // Vérifier que le poste existe
            var poste = await _context.PostesComite.FindAsync(posteComiteId);
            if (poste == null)
            {
                return NotFound($"Poste avec l'ID {posteComiteId} introuvable");
            }

            // Vérifier que le mandat existe
            var mandat = await _context.Mandats.FindAsync(mandatId);
            if (mandat == null)
            {
                return NotFound($"Mandat avec l'ID {mandatId} introuvable");
            }

            // Vérifier que le poste et le mandat appartiennent au même club
            if (poste.ClubId != mandat.ClubId)
            {
                return BadRequest("Le poste et le mandat n'appartiennent pas au même club");
            }

            return null;
        }

        private async Task<ActionResult?> ValiderNominationCommission(string membreId, Guid commissionId, Guid mandatId)
        {
            // Vérifier que le membre existe
            var membre = await _context.Users.FindAsync(membreId);
            if (membre == null)
            {
                return NotFound($"Membre avec l'ID {membreId} introuvable");
            }

            // Vérifier que la commission existe
            var commission = await _context.Commissions.FindAsync(commissionId);
            if (commission == null)
            {
                return NotFound($"Commission avec l'ID {commissionId} introuvable");
            }

            // Vérifier que le mandat existe
            var mandat = await _context.Mandats.FindAsync(mandatId);
            if (mandat == null)
            {
                return NotFound($"Mandat avec l'ID {mandatId} introuvable");
            }

            // Vérifier que la commission et le mandat appartiennent au même club
            if (commission.ClubId != mandat.ClubId)
            {
                return BadRequest("La commission et le mandat n'appartiennent pas au même club");
            }

            return null;
        }

        #endregion

        #region Requêtes d'information

        // GET: api/transitions/club/{clubId}/mandat-actuel
        [HttpGet("club/{clubId:guid}/mandat-actuel")]
        public async Task<ActionResult<Mandat>> ObtenirMandatActuel(Guid clubId)
        {
            try
            {
                var mandatActuel = await _context.Mandats
                    .Include(m => m.Club)
                    .Include(m => m.MembresCommission.Where(mc => mc.EstActif))
                        .ThenInclude(mc => mc.Membre)
                    .Include(m => m.MembresCommission.Where(mc => mc.EstActif))
                        .ThenInclude(mc => mc.PosteComite)
                    .Include(m => m.MembresCommission.Where(mc => mc.EstActif))
                        .ThenInclude(mc => mc.Membre)
                    .Include(m => m.MembresCommission.Where(mc => mc.EstActif))
                        .ThenInclude(mc => mc.Commission)
                    .FirstOrDefaultAsync(m => m.ClubId == clubId && m.EstActuel);

                if (mandatActuel == null)
                {
                    return NotFound($"Aucun mandat actuel trouvé pour le club {clubId}");
                }

                return Ok(mandatActuel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du mandat actuel du club {ClubId}", clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du mandat actuel");
            }
        }

        // GET: api/transitions/club/{clubId}/historique-mandats
        [HttpGet("club/{clubId:guid}/historique-mandats")]
        public async Task<ActionResult<IEnumerable<Mandat>>> ObtenirHistoriqueMandats(Guid clubId)
        {
            try
            {
                var mandats = await _context.Mandats
                    .Include(m => m.Club)
                    .Where(m => m.ClubId == clubId)
                    .OrderByDescending(m => m.Annee)
                    .ToListAsync();

                return Ok(mandats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'historique des mandats du club {ClubId}", clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de l'historique");
            }
        }

        #endregion
    }

    #region DTOs

    public class CreerMandatDto
    {
        [Required]
        public int Annee { get; set; }

        [Required]
        public DateTime DateDebut { get; set; }

        [Required]
        public DateTime DateFin { get; set; }

        public bool EstActuel { get; set; } = false;

        [MaxLength(200)]
        public string? Description { get; set; }

        [Required]
        public Guid ClubId { get; set; }
    }

    public class NommerMembreComiteDto
    {
        [Required]
        public string MembreId { get; set; } = string.Empty;

        [Required]
        public Guid PosteComiteId { get; set; }

        [Required]
        public Guid MandatId { get; set; }

        public DateTime? DateNomination { get; set; }

        [MaxLength(500)]
        public string? Commentaires { get; set; }
    }

    public class NommerMembreCommissionDto
    {
        [Required]
        public string MembreId { get; set; } = string.Empty;

        [Required]
        public Guid CommissionId { get; set; }

        [Required]
        public Guid MandatId { get; set; }

        public bool EstResponsable { get; set; } = false;

        public DateTime? DateNomination { get; set; }

        [MaxLength(500)]
        public string? Commentaires { get; set; }
    }

    public class DemissionDto
    {
        public DateTime? DateDemission { get; set; }

        [MaxLength(500)]
        public string? Commentaires { get; set; }
    }

    #endregion
}