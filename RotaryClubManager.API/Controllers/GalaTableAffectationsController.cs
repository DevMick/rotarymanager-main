using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/gala-table-affectations")]
    [ApiController]
    [Authorize]
    public class GalaTableAffectationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GalaTableAffectationController> _logger;

        public GalaTableAffectationController(
            ApplicationDbContext context,
            ILogger<GalaTableAffectationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/gala-table-affectations/gala/{galaId}
        [HttpGet("gala/{galaId:guid}")]
        public async Task<ActionResult<IEnumerable<GalaTableAffectationDto>>> GetAffectationsByGala(Guid galaId)
        {
            try
            {
                // Vérifier que le gala existe
                var galaExists = await _context.Galas.AnyAsync(g => g.Id == galaId);
                if (!galaExists)
                {
                    return NotFound($"Gala avec l'ID {galaId} introuvable");
                }

                var affectations = await _context.GalaTableAffectations
                    .Include(a => a.GalaTable)
                    .Include(a => a.GalaInvites)
                    .Where(a => a.GalaTable.GalaId == galaId)
                    .OrderBy(a => a.GalaTable.TableLibelle)
                    .ThenBy(a => a.GalaInvites.Nom_Prenom)
                    .Select(a => new GalaTableAffectationDto
                    {
                        Id = a.Id,
                        GalaTableId = a.GalaTableId,
                        TableLibelle = a.GalaTable.TableLibelle,
                        GalaInvitesId = a.GalaInvitesId,
                        InviteNom_Prenom = a.GalaInvites.Nom_Prenom,
                        GalaId = a.GalaTable.GalaId,
                        DateAjout = a.DateAjout
                    })
                    .ToListAsync();

                return Ok(affectations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des affectations du gala {GalaId}", galaId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des affectations");
            }
        }

        // GET: api/gala-table-affectations/table/{tableId}
        [HttpGet("table/{tableId:guid}")]
        public async Task<ActionResult<IEnumerable<GalaTableAffectationDto>>> GetAffectationsByTable(Guid tableId)
        {
            try
            {
                // Vérifier que la table existe
                var tableExists = await _context.GalaTables.AnyAsync(t => t.Id == tableId);
                if (!tableExists)
                {
                    return NotFound($"Table avec l'ID {tableId} introuvable");
                }

                var affectations = await _context.GalaTableAffectations
                    .Include(a => a.GalaTable)
                    .Include(a => a.GalaInvites)
                    .Where(a => a.GalaTableId == tableId)
                    .OrderBy(a => a.DateAjout)
                    .ThenBy(a => a.GalaInvites.Nom_Prenom)
                    .Select(a => new GalaTableAffectationDto
                    {
                        Id = a.Id,
                        GalaTableId = a.GalaTableId,
                        TableLibelle = a.GalaTable.TableLibelle,
                        GalaInvitesId = a.GalaInvitesId,
                        InviteNom_Prenom = a.GalaInvites.Nom_Prenom,
                        GalaId = a.GalaTable.GalaId,
                        DateAjout = a.DateAjout
                    })
                    .ToListAsync();

                return Ok(affectations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des affectations de la table {TableId}", tableId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des affectations");
            }
        }

        // GET: api/gala-table-affectations/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GalaTableAffectationDetailDto>> GetAffectation(Guid id)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'affectation est invalide");
                }

                var affectation = await _context.GalaTableAffectations
                    .Include(a => a.GalaTable)
                        .ThenInclude(t => t.Gala)
                    .Include(a => a.GalaInvites)
                    .Where(a => a.Id == id)
                    .Select(a => new GalaTableAffectationDetailDto
                    {
                        Id = a.Id,
                        GalaTableId = a.GalaTableId,
                        TableLibelle = a.GalaTable.TableLibelle,
                        GalaInvitesId = a.GalaInvitesId,
                        InviteNom_Prenom = a.GalaInvites.Nom_Prenom,
                        GalaId = a.GalaTable.GalaId,
                        GalaLibelle = a.GalaTable.Gala.Libelle,
                        DateAjout = a.DateAjout
                    })
                    .FirstOrDefaultAsync();

                if (affectation == null)
                {
                    return NotFound($"Affectation avec l'ID {id} introuvable");
                }

                return Ok(affectation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'affectation {AffectationId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de l'affectation");
            }
        }

        // POST: api/gala-table-affectations
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<ActionResult<GalaTableAffectationDto>> CreateAffectation([FromBody] CreateGalaTableAffectationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier que la table existe
                var table = await _context.GalaTables.FindAsync(request.GalaTableId);
                if (table == null)
                {
                    return BadRequest($"Table avec l'ID {request.GalaTableId} introuvable");
                }

                // Vérifier que l'invité existe et appartient au même gala
                var invite = await _context.GalaInvites
                    .Include(i => i.TableAffectations)
                    .FirstOrDefaultAsync(i => i.Id == request.GalaInvitesId && i.GalaId == table.GalaId);

                if (invite == null)
                {
                    return BadRequest($"Invité avec l'ID {request.GalaInvitesId} introuvable ou n'appartient pas au même gala");
                }

                // Vérifier qu'il n'y a pas déjà une affectation pour cet invité
                var existingAffectation = invite.TableAffectations.FirstOrDefault();
                if (existingAffectation != null)
                {
                    return BadRequest($"L'invité '{invite.Nom_Prenom}' est déjà affecté à une table. " +
                                    "Utilisez l'endpoint de modification pour changer l'affectation.");
                }

                var affectation = new GalaTableAffectation
                {
                    Id = Guid.NewGuid(),
                    GalaTableId = request.GalaTableId,
                    GalaInvitesId = request.GalaInvitesId,
                    DateAjout = DateTime.UtcNow
                };

                _context.GalaTableAffectations.Add(affectation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Affectation créée avec l'ID {Id} - Invité {InviteId} → Table {TableId} le {DateAjout}",
                    affectation.Id, request.GalaInvitesId, request.GalaTableId, affectation.DateAjout);

                // Récupérer l'affectation avec les données complètes
                var result = await _context.GalaTableAffectations
                    .Include(a => a.GalaTable)
                    .Include(a => a.GalaInvites)
                    .Where(a => a.Id == affectation.Id)
                    .Select(a => new GalaTableAffectationDto
                    {
                        Id = a.Id,
                        GalaTableId = a.GalaTableId,
                        TableLibelle = a.GalaTable.TableLibelle,
                        GalaInvitesId = a.GalaInvitesId,
                        InviteNom_Prenom = a.GalaInvites.Nom_Prenom,
                        GalaId = a.GalaTable.GalaId,
                        DateAjout = a.DateAjout
                    })
                    .FirstAsync();

                return CreatedAtAction(nameof(GetAffectation), new { id = affectation.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'affectation");
                return StatusCode(500, "Une erreur est survenue lors de la création de l'affectation");
            }
        }

        // PUT: api/gala-table-affectations/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> UpdateAffectation(Guid id, [FromBody] UpdateGalaTableAffectationRequest request)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'affectation est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var affectation = await _context.GalaTableAffectations
                    .Include(a => a.GalaTable)
                    .Include(a => a.GalaInvites)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (affectation == null)
                {
                    return NotFound($"Affectation avec l'ID {id} introuvable");
                }

                // Si on change de table, vérifier que la nouvelle table existe et appartient au même gala
                if (request.GalaTableId.HasValue && request.GalaTableId.Value != affectation.GalaTableId)
                {
                    var nouvelleTable = await _context.GalaTables
                        .FirstOrDefaultAsync(t => t.Id == request.GalaTableId.Value &&
                                                 t.GalaId == affectation.GalaTable.GalaId);

                    if (nouvelleTable == null)
                    {
                        return BadRequest($"Table avec l'ID {request.GalaTableId.Value} introuvable ou n'appartient pas au même gala");
                    }

                    affectation.GalaTableId = request.GalaTableId.Value;
                    // Note: Nous ne modifions pas DateAjout lors de la mise à jour pour conserver l'historique
                }

                _context.Entry(affectation).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Affectation {Id} mise à jour avec succès", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'affectation {AffectationId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de l'affectation");
            }
        }

        // DELETE: api/gala-table-affectations/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> DeleteAffectation(Guid id)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'affectation est invalide");
                }

                var affectation = await _context.GalaTableAffectations
                    .Include(a => a.GalaTable)
                    .Include(a => a.GalaInvites)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (affectation == null)
                {
                    return NotFound($"Affectation avec l'ID {id} introuvable");
                }

                _context.GalaTableAffectations.Remove(affectation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Affectation supprimée - Invité '{InviteNom}' retiré de la table '{TableLibelle}' (affectation du {DateAjout})",
                    affectation.GalaInvites.Nom_Prenom, affectation.GalaTable.TableLibelle, affectation.DateAjout);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'affectation {AffectationId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de l'affectation");
            }
        }

        // POST: api/gala-table-affectations/bulk-create
        [HttpPost("bulk-create")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<ActionResult<BulkAffectationResultDto>> CreateBulkAffectations([FromBody] BulkCreateAffectationsRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = new BulkAffectationResultDto();
                var affectationsToAdd = new List<GalaTableAffectation>();
                var dateAjout = DateTime.UtcNow; // Même date pour toutes les affectations du batch

                foreach (var affectationRequest in request.Affectations)
                {
                    try
                    {
                        // Vérifier que la table existe
                        var table = await _context.GalaTables.FindAsync(affectationRequest.GalaTableId);
                        if (table == null)
                        {
                            result.Erreurs.Add($"Table avec l'ID {affectationRequest.GalaTableId} introuvable");
                            continue;
                        }

                        // Vérifier que l'invité existe et appartient au même gala
                        var invite = await _context.GalaInvites
                            .Include(i => i.TableAffectations)
                            .FirstOrDefaultAsync(i => i.Id == affectationRequest.GalaInvitesId && i.GalaId == table.GalaId);

                        if (invite == null)
                        {
                            result.Erreurs.Add($"Invité avec l'ID {affectationRequest.GalaInvitesId} introuvable ou n'appartient pas au même gala");
                            continue;
                        }

                        // Vérifier qu'il n'y a pas déjà une affectation
                        var existingAffectation = invite.TableAffectations.FirstOrDefault();
                        if (existingAffectation != null)
                        {
                            result.Erreurs.Add($"L'invité '{invite.Nom_Prenom}' est déjà affecté à une table");
                            continue;
                        }

                        var affectation = new GalaTableAffectation
                        {
                            Id = Guid.NewGuid(),
                            GalaTableId = affectationRequest.GalaTableId,
                            GalaInvitesId = affectationRequest.GalaInvitesId,
                            DateAjout = dateAjout
                        };

                        affectationsToAdd.Add(affectation);
                        result.AffectationsCreees++;
                    }
                    catch (Exception ex)
                    {
                        result.Erreurs.Add($"Erreur lors du traitement de l'affectation : {ex.Message}");
                    }
                }

                if (affectationsToAdd.Any())
                {
                    _context.GalaTableAffectations.AddRange(affectationsToAdd);
                    await _context.SaveChangesAsync();
                }

                result.NombreTotal = request.Affectations.Count;
                result.DateTraitement = dateAjout;

                _logger.LogInformation("Création en masse terminée le {DateTraitement} : {NombreCreees}/{NombreTotal} affectations créées",
                    dateAjout, result.AffectationsCreees, result.NombreTotal);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création en masse des affectations");
                return StatusCode(500, "Une erreur est survenue lors de la création en masse des affectations");
            }
        }

        // POST: api/gala-table-affectations/gala/{galaId}/repartition-automatique
        [HttpPost("gala/{galaId:guid}/repartition-automatique")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<ActionResult<RepartitionAutomatiqueResultDto>> RepartitionAutomatique(Guid galaId)
        {
            try
            {
                // Vérifier que le gala existe
                var gala = await _context.Galas
                    .Include(g => g.Tables)
                    .Include(g => g.Invites.Where(i => !i.TableAffectations.Any()))
                    .FirstOrDefaultAsync(g => g.Id == galaId);

                if (gala == null)
                {
                    return NotFound($"Gala avec l'ID {galaId} introuvable");
                }

                var invitesSansTable = gala.Invites.ToList();
                var tables = gala.Tables.OrderBy(t => t.TableLibelle).ToList();

                if (!invitesSansTable.Any())
                {
                    return BadRequest("Aucun invité sans table trouvé pour ce gala");
                }

                if (!tables.Any())
                {
                    return BadRequest("Aucune table trouvée pour ce gala");
                }

                var dateRepartition = DateTime.UtcNow;
                var result = new RepartitionAutomatiqueResultDto
                {
                    GalaId = galaId,
                    NombreInvitesTraites = invitesSansTable.Count,
                    NombreTablesUtilisees = tables.Count,
                    DateRepartition = dateRepartition
                };

                // Répartition équitable : distribuer les invités de manière circulaire
                var affectationsToAdd = new List<GalaTableAffectation>();

                for (int i = 0; i < invitesSansTable.Count; i++)
                {
                    var invite = invitesSansTable[i];
                    var table = tables[i % tables.Count]; // Distribution circulaire

                    var affectation = new GalaTableAffectation
                    {
                        Id = Guid.NewGuid(),
                        GalaTableId = table.Id,
                        GalaInvitesId = invite.Id,
                        DateAjout = dateRepartition
                    };

                    affectationsToAdd.Add(affectation);
                    result.AffectationsCreees++;
                }

                _context.GalaTableAffectations.AddRange(affectationsToAdd);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Répartition automatique terminée le {DateRepartition} pour le gala {GalaId} : {NombreAffectations} affectations créées",
                    dateRepartition, galaId, result.AffectationsCreees);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la répartition automatique pour le gala {GalaId}", galaId);
                return StatusCode(500, "Une erreur est survenue lors de la répartition automatique");
            }
        }

        // GET: api/gala-table-affectations/gala/{galaId}/historique
        [HttpGet("gala/{galaId:guid}/historique")]
        public async Task<ActionResult<IEnumerable<GalaTableAffectationDto>>> GetHistoriqueAffectations(
            Guid galaId,
            [FromQuery] DateTime? dateDebut = null,
            [FromQuery] DateTime? dateFin = null)
        {
            try
            {
                // Vérifier que le gala existe
                var galaExists = await _context.Galas.AnyAsync(g => g.Id == galaId);
                if (!galaExists)
                {
                    return NotFound($"Gala avec l'ID {galaId} introuvable");
                }

                var query = _context.GalaTableAffectations
                    .Include(a => a.GalaTable)
                    .Include(a => a.GalaInvites)
                    .Where(a => a.GalaTable.GalaId == galaId);

                // Filtres de date
                if (dateDebut.HasValue)
                {
                    query = query.Where(a => a.DateAjout >= dateDebut.Value);
                }

                if (dateFin.HasValue)
                {
                    query = query.Where(a => a.DateAjout <= dateFin.Value.AddDays(1).Date);
                }

                var affectations = await query
                    .OrderByDescending(a => a.DateAjout)
                    .ThenBy(a => a.GalaTable.TableLibelle)
                    .ThenBy(a => a.GalaInvites.Nom_Prenom)
                    .Select(a => new GalaTableAffectationDto
                    {
                        Id = a.Id,
                        GalaTableId = a.GalaTableId,
                        TableLibelle = a.GalaTable.TableLibelle,
                        GalaInvitesId = a.GalaInvitesId,
                        InviteNom_Prenom = a.GalaInvites.Nom_Prenom,
                        GalaId = a.GalaTable.GalaId,
                        DateAjout = a.DateAjout
                    })
                    .ToListAsync();

                return Ok(affectations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'historique des affectations du gala {GalaId}", galaId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de l'historique");
            }
        }
    }

    // DTOs pour les affectations mis à jour
    public class GalaTableAffectationDto
    {
        public Guid Id { get; set; }
        public Guid GalaTableId { get; set; }
        public string TableLibelle { get; set; } = string.Empty;
        public Guid GalaInvitesId { get; set; }
        public string InviteNom_Prenom { get; set; } = string.Empty;
        public Guid GalaId { get; set; }
        public DateTime DateAjout { get; set; }
    }

    public class GalaTableAffectationDetailDto : GalaTableAffectationDto
    {
        public string GalaLibelle { get; set; } = string.Empty;
    }

    public class CreateGalaTableAffectationRequest
    {
        [Required(ErrorMessage = "L'ID de la table est obligatoire")]
        public Guid GalaTableId { get; set; }

        [Required(ErrorMessage = "L'ID de l'invité est obligatoire")]
        public Guid GalaInvitesId { get; set; }
    }

    public class UpdateGalaTableAffectationRequest
    {
        public Guid? GalaTableId { get; set; }
    }

    public class BulkCreateAffectationsRequest
    {
        [Required(ErrorMessage = "La liste des affectations est obligatoire")]
        [MinLength(1, ErrorMessage = "Au moins une affectation est requise")]
        public List<CreateGalaTableAffectationRequest> Affectations { get; set; } = new List<CreateGalaTableAffectationRequest>();
    }

    public class BulkAffectationResultDto
    {
        public int NombreTotal { get; set; }
        public int AffectationsCreees { get; set; }
        public List<string> Erreurs { get; set; } = new List<string>();
        public DateTime DateTraitement { get; set; }
    }

    public class RepartitionAutomatiqueResultDto
    {
        public Guid GalaId { get; set; }
        public int NombreInvitesTraites { get; set; }
        public int NombreTablesUtilisees { get; set; }
        public int AffectationsCreees { get; set; }
        public DateTime DateRepartition { get; set; }
    }
}