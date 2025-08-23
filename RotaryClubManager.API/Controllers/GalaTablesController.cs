using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/gala-tables")]
    [ApiController]
    [Authorize]
    public class GalaTableController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GalaTableController> _logger;

        public GalaTableController(
            ApplicationDbContext context,
            ILogger<GalaTableController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/gala-tables/gala/{galaId}
        [HttpGet("gala/{galaId:guid}")]
        public async Task<ActionResult<IEnumerable<GalaTableInfoDto>>> GetTablesByGala(
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

                var query = _context.GalaTables
                    .Include(t => t.TableAffectations)
                        .ThenInclude(ta => ta.GalaInvites)
                    .Where(t => t.GalaId == galaId);

                // Filtre par terme de recherche
                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(t => t.TableLibelle.ToLower().Contains(termeLower));
                }

                var tables = await query
                    .OrderBy(t => t.TableLibelle)
                    .Select(t => new GalaTableInfoDto
                    {
                        Id = t.Id,
                        GalaId = t.GalaId,
                        TableLibelle = t.TableLibelle,
                        NombreInvites = t.TableAffectations.Count(),
                        Invites = t.TableAffectations.Select(ta => new InviteTableDto
                        {
                            Id = ta.GalaInvites.Id,
                            Nom_Prenom = ta.GalaInvites.Nom_Prenom
                        }).OrderBy(i => i.Nom_Prenom).ToList()
                    })
                    .ToListAsync();

                return Ok(tables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des tables du gala {GalaId}", galaId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des tables");
            }
        }

        // GET: api/gala-tables/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GalaTableDetailInfoDto>> GetTable(Guid id)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la table est invalide");
                }

                var table = await _context.GalaTables
                    .Include(t => t.Gala)
                    .Include(t => t.TableAffectations)
                        .ThenInclude(ta => ta.GalaInvites)
                    .Where(t => t.Id == id)
                    .Select(t => new GalaTableDetailInfoDto
                    {
                        Id = t.Id,
                        GalaId = t.GalaId,
                        GalaLibelle = t.Gala.Libelle,
                        TableLibelle = t.TableLibelle,
                        NombreInvites = t.TableAffectations.Count(),
                        Invites = t.TableAffectations.Select(ta => new InviteTableDto
                        {
                            Id = ta.GalaInvites.Id,
                            Nom_Prenom = ta.GalaInvites.Nom_Prenom
                        }).OrderBy(i => i.Nom_Prenom).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (table == null)
                {
                    return NotFound($"Table avec l'ID {id} introuvable");
                }

                return Ok(table);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la table {TableId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de la table");
            }
        }

        // POST: api/gala-tables
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<ActionResult<GalaTableInfoDto>> CreateTable([FromBody] CreateGalaTableRequest request)
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

                // Vérifier l'unicité du libellé dans le gala
                var existingTable = await _context.GalaTables
                    .AnyAsync(t => t.GalaId == request.GalaId &&
                                  t.TableLibelle.ToLower() == request.TableLibelle.ToLower());

                if (existingTable)
                {
                    return BadRequest($"Une table avec le libellé '{request.TableLibelle}' existe déjà pour ce gala");
                }

                var table = new GalaTable
                {
                    Id = Guid.NewGuid(),
                    GalaId = request.GalaId,
                    TableLibelle = request.TableLibelle
                };

                _context.GalaTables.Add(table);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Table '{TableLibelle}' créée avec l'ID {Id} pour le gala {GalaId}",
                    table.TableLibelle, table.Id, table.GalaId);

                var result = new GalaTableInfoDto
                {
                    Id = table.Id,
                    GalaId = table.GalaId,
                    TableLibelle = table.TableLibelle,
                    NombreInvites = 0,
                    Invites = new List<InviteTableDto>()
                };

                return CreatedAtAction(nameof(GetTable), new { id = table.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la table");
                return StatusCode(500, "Une erreur est survenue lors de la création de la table");
            }
        }

        // PUT: api/gala-tables/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> UpdateTable(Guid id, [FromBody] UpdateGalaTableRequest request)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la table est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var table = await _context.GalaTables.FindAsync(id);
                if (table == null)
                {
                    return NotFound($"Table avec l'ID {id} introuvable");
                }

                // Vérifier l'unicité du libellé si modifié
                if (!string.IsNullOrEmpty(request.TableLibelle) &&
                    request.TableLibelle.ToLower() != table.TableLibelle.ToLower())
                {
                    var existingTable = await _context.GalaTables
                        .AnyAsync(t => t.Id != id &&
                                      t.GalaId == table.GalaId &&
                                      t.TableLibelle.ToLower() == request.TableLibelle.ToLower());

                    if (existingTable)
                    {
                        return BadRequest($"Une table avec le libellé '{request.TableLibelle}' existe déjà pour ce gala");
                    }
                }

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.TableLibelle))
                    table.TableLibelle = request.TableLibelle;

                _context.Entry(table).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Table {Id} mise à jour avec succès", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la table {TableId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de la table");
            }
        }

        // DELETE: api/gala-tables/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> DeleteTable(Guid id)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la table est invalide");
                }

                var table = await _context.GalaTables
                    .Include(t => t.TableAffectations)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (table == null)
                {
                    return NotFound($"Table avec l'ID {id} introuvable");
                }

                // Vérifier si la table a des invités affectés
                if (table.TableAffectations.Any())
                {
                    return BadRequest($"Impossible de supprimer la table '{table.TableLibelle}' car elle a " +
                                    $"{table.TableAffectations.Count} invité(s) affecté(s). " +
                                    "Veuillez d'abord retirer tous les invités de cette table.");
                }

                _context.GalaTables.Remove(table);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Table '{TableLibelle}' supprimée avec l'ID {Id}", table.TableLibelle, id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la table {TableId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de la table");
            }
        }

        // POST: api/gala-tables/{id}/ajouter-invite
        [HttpPost("{id:guid}/ajouter-invite")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> AjouterInvite(Guid id, [FromBody] AjouterInviteTableRequest request)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la table est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var table = await _context.GalaTables.FindAsync(id);
                if (table == null)
                {
                    return NotFound($"Table avec l'ID {id} introuvable");
                }

                // Vérifier que l'invité existe et appartient au même gala
                var invite = await _context.GalaInvites
                    .Include(i => i.TableAffectations)
                    .FirstOrDefaultAsync(i => i.Id == request.InviteId && i.GalaId == table.GalaId);

                if (invite == null)
                {
                    return BadRequest($"Invité avec l'ID {request.InviteId} introuvable ou n'appartient pas au même gala");
                }

                // Vérifier si l'invité n'est pas déjà affecté à une table
                var existingAffectation = invite.TableAffectations.FirstOrDefault();
                if (existingAffectation != null)
                {
                    return BadRequest($"L'invité est déjà affecté à une table. Utilisez l'endpoint de réaffectation si nécessaire.");
                }

                // Créer l'affectation
                var affectation = new GalaTableAffectation
                {
                    Id = Guid.NewGuid(),
                    GalaTableId = id,
                    GalaInvitesId = request.InviteId
                };

                _context.GalaTableAffectations.Add(affectation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Invité {InviteId} ajouté à la table {TableId}", request.InviteId, id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout de l'invité {InviteId} à la table {TableId}",
                    request?.InviteId, id);
                return StatusCode(500, "Une erreur est survenue lors de l'ajout de l'invité à la table");
            }
        }

        // DELETE: api/gala-tables/{id}/retirer-invite/{inviteId}
        [HttpDelete("{id:guid}/retirer-invite/{inviteId:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> RetirerInvite(Guid id, Guid inviteId)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty || inviteId == Guid.Empty)
                {
                    return BadRequest("Les identifiants de la table et de l'invité sont invalides");
                }

                var affectation = await _context.GalaTableAffectations
                    .FirstOrDefaultAsync(ta => ta.GalaTableId == id && ta.GalaInvitesId == inviteId);

                if (affectation == null)
                {
                    return NotFound($"Aucune affectation trouvée entre la table {id} et l'invité {inviteId}");
                }

                _context.GalaTableAffectations.Remove(affectation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Invité {InviteId} retiré de la table {TableId}", inviteId, id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du retrait de l'invité {InviteId} de la table {TableId}",
                    inviteId, id);
                return StatusCode(500, "Une erreur est survenue lors du retrait de l'invité de la table");
            }
        }

        // GET: api/gala-tables/gala/{galaId}/disponibles
        [HttpGet("gala/{galaId:guid}/disponibles")]
        public async Task<ActionResult<IEnumerable<GalaTableInfoDto>>> GetTablesDisponibles(Guid galaId, [FromQuery] int? capaciteMax = null)
        {
            try
            {
                // Vérifier que le gala existe
                var galaExists = await _context.Galas.AnyAsync(g => g.Id == galaId);
                if (!galaExists)
                {
                    return NotFound($"Gala avec l'ID {galaId} introuvable");
                }

                var query = _context.GalaTables
                    .Include(t => t.TableAffectations)
                    .Where(t => t.GalaId == galaId);

                // Filtre par capacité maximale si spécifiée
                if (capaciteMax.HasValue)
                {
                    query = query.Where(t => t.TableAffectations.Count() < capaciteMax.Value);
                }

                var tablesDisponibles = await query
                    .OrderBy(t => t.TableLibelle)
                    .Select(t => new GalaTableInfoDto
                    {
                        Id = t.Id,
                        GalaId = t.GalaId,
                        TableLibelle = t.TableLibelle,
                        NombreInvites = t.TableAffectations.Count(),
                        Invites = t.TableAffectations.Select(ta => new InviteTableDto
                        {
                            Id = ta.GalaInvites.Id,
                            Nom_Prenom = ta.GalaInvites.Nom_Prenom
                        }).OrderBy(i => i.Nom_Prenom).ToList()
                    })
                    .ToListAsync();

                return Ok(tablesDisponibles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des tables disponibles du gala {GalaId}", galaId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des tables disponibles");
            }
        }

        // GET: api/gala-tables/gala/{galaId}/statistiques
        [HttpGet("gala/{galaId:guid}/statistiques")]
        public async Task<ActionResult<GalaTableStatistiquesDto>> GetStatistiquesTablesByGala(Guid galaId)
        {
            try
            {
                // Vérifier que le gala existe
                var galaExists = await _context.Galas.AnyAsync(g => g.Id == galaId);
                if (!galaExists)
                {
                    return NotFound($"Gala avec l'ID {galaId} introuvable");
                }

                var statistiques = await _context.GalaTables
                    .Where(t => t.GalaId == galaId)
                    .Select(t => new
                    {
                        t.Id,
                        t.TableLibelle,
                        NombreInvites = t.TableAffectations.Count()
                    })
                    .ToListAsync();

                var result = new GalaTableStatistiquesDto
                {
                    GalaId = galaId,
                    NombreTotalTables = statistiques.Count,
                    NombreTotalInvitesAffectes = statistiques.Sum(s => s.NombreInvites),
                    NombreTablesVides = statistiques.Count(s => s.NombreInvites == 0),
                    NombreTablesOccupees = statistiques.Count(s => s.NombreInvites > 0),
                    NombreMoyenInvitesParTable = statistiques.Count > 0
                        ? Math.Round((double)statistiques.Sum(s => s.NombreInvites) / statistiques.Count, 2)
                        : 0,
                    TablesDetails = statistiques.Select(s => new GalaTableStatistiqueDetailDto
                    {
                        Id = s.Id,
                        TableLibelle = s.TableLibelle,
                        NombreInvites = s.NombreInvites
                    }).OrderBy(s => s.TableLibelle).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques des tables du gala {GalaId}", galaId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
            }
        }
    }

    // DTOs pour les tables
    public class GalaTableInfoDto
    {
        public Guid Id { get; set; }
        public Guid GalaId { get; set; }
        public string TableLibelle { get; set; } = string.Empty;
        public int NombreInvites { get; set; }
        public List<InviteTableDto> Invites { get; set; } = new List<InviteTableDto>();
    }

    public class GalaTableDetailInfoDto : GalaTableInfoDto
    {
        public string GalaLibelle { get; set; } = string.Empty;
    }

    public class InviteTableDto
    {
        public Guid Id { get; set; }
        public string Nom_Prenom { get; set; } = string.Empty;
    }

    public class CreateGalaTableRequest
    {
        [Required(ErrorMessage = "L'ID du gala est obligatoire")]
        public Guid GalaId { get; set; }

        [Required(ErrorMessage = "Le libellé de la table est obligatoire")]
        [MaxLength(100, ErrorMessage = "Le libellé de la table ne peut pas dépasser 100 caractères")]
        public string TableLibelle { get; set; } = string.Empty;
    }

    public class UpdateGalaTableRequest
    {
        [MaxLength(100, ErrorMessage = "Le libellé de la table ne peut pas dépasser 100 caractères")]
        public string? TableLibelle { get; set; }
    }

    public class AjouterInviteTableRequest
    {
        [Required(ErrorMessage = "L'ID de l'invité est obligatoire")]
        public Guid InviteId { get; set; }
    }

    public class GalaTableStatistiquesDto
    {
        public Guid GalaId { get; set; }
        public int NombreTotalTables { get; set; }
        public int NombreTotalInvitesAffectes { get; set; }
        public int NombreTablesVides { get; set; }
        public int NombreTablesOccupees { get; set; }
        public double NombreMoyenInvitesParTable { get; set; }
        public List<GalaTableStatistiqueDetailDto> TablesDetails { get; set; } = new List<GalaTableStatistiqueDetailDto>();
    }

    public class GalaTableStatistiqueDetailDto
    {
        public Guid Id { get; set; }
        public string TableLibelle { get; set; } = string.Empty;
        public int NombreInvites { get; set; }
    }
}