using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/fonctions/{fonctionId}/roles-responsabilites")]
    [ApiController]
    [Authorize]
    public class FonctionRolesResponsabilitesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FonctionRolesResponsabilitesController> _logger;

        public FonctionRolesResponsabilitesController(
            ApplicationDbContext context,
            ILogger<FonctionRolesResponsabilitesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/fonctions/{fonctionId}/roles-responsabilites
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FonctionRoleResponsabiliteDto>>> GetRolesResponsabilites(Guid fonctionId)
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

                var rolesResponsabilites = await _context.FonctionRolesResponsabilites
                    .Include(fr => fr.Fonction)
                    .Where(fr => fr.FonctionId == fonctionId)
                    .OrderBy(fr => fr.Libelle)
                    .Select(fr => new FonctionRoleResponsabiliteDto
                    {
                        Id = fr.Id,
                        Libelle = fr.Libelle,
                        Description = fr.Description,
                        FonctionId = fr.FonctionId,
                        FonctionNom = fr.Fonction.NomFonction
                    })
                    .ToListAsync();

                return Ok(rolesResponsabilites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des rôles/responsabilités de la fonction {FonctionId}", fonctionId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des rôles/responsabilités");
            }
        }

        // GET: api/fonctions/{fonctionId}/roles-responsabilites/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<FonctionRoleResponsabiliteDto>> GetRoleResponsabilite(Guid fonctionId, Guid id)
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
                    return BadRequest("L'identifiant du rôle/responsabilité est invalide");
                }

                var roleResponsabilite = await _context.FonctionRolesResponsabilites
                    .Include(fr => fr.Fonction)
                    .Where(fr => fr.Id == id && fr.FonctionId == fonctionId)
                    .Select(fr => new FonctionRoleResponsabiliteDto
                    {
                        Id = fr.Id,
                        Libelle = fr.Libelle,
                        Description = fr.Description,
                        FonctionId = fr.FonctionId,
                        FonctionNom = fr.Fonction.NomFonction
                    })
                    .FirstOrDefaultAsync();

                if (roleResponsabilite == null)
                {
                    return NotFound($"Rôle/responsabilité avec l'ID {id} non trouvé pour la fonction {fonctionId}");
                }

                return Ok(roleResponsabilite);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du rôle/responsabilité {Id} de la fonction {FonctionId}", id, fonctionId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du rôle/responsabilité");
            }
        }

        // POST: api/fonctions/{fonctionId}/roles-responsabilites
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<ActionResult<FonctionRoleResponsabiliteDto>> CreateRoleResponsabilite(
            Guid fonctionId,
            [FromBody] CreateFonctionRoleResponsabiliteRequest request)
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
                var existingRole = await _context.FonctionRolesResponsabilites
                    .AnyAsync(fr => fr.FonctionId == fonctionId &&
                                   fr.Libelle.ToLower() == request.Libelle.ToLower());

                if (existingRole)
                {
                    return BadRequest($"Un rôle/responsabilité avec le libellé '{request.Libelle}' existe déjà pour cette fonction");
                }

                var roleResponsabilite = new FonctionRolesResponsabilites
                {
                    Id = Guid.NewGuid(),
                    Libelle = request.Libelle,
                    Description = request.Description,
                    FonctionId = fonctionId
                };

                _context.FonctionRolesResponsabilites.Add(roleResponsabilite);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Rôle/responsabilité '{Libelle}' créé pour la fonction {FonctionId} avec l'ID {Id}",
                    roleResponsabilite.Libelle, fonctionId, roleResponsabilite.Id);

                var result = new FonctionRoleResponsabiliteDto
                {
                    Id = roleResponsabilite.Id,
                    Libelle = roleResponsabilite.Libelle,
                    Description = roleResponsabilite.Description,
                    FonctionId = roleResponsabilite.FonctionId,
                    FonctionNom = fonction.NomFonction
                };

                return CreatedAtAction(nameof(GetRoleResponsabilite),
                    new { fonctionId, id = roleResponsabilite.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du rôle/responsabilité pour la fonction {FonctionId}", fonctionId);
                return StatusCode(500, "Une erreur est survenue lors de la création du rôle/responsabilité");
            }
        }

        // PUT: api/fonctions/{fonctionId}/roles-responsabilites/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> UpdateRoleResponsabilite(
            Guid fonctionId,
            Guid id,
            [FromBody] UpdateFonctionRoleResponsabiliteRequest request)
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
                    return BadRequest("L'identifiant du rôle/responsabilité est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var roleResponsabilite = await _context.FonctionRolesResponsabilites
                    .FirstOrDefaultAsync(fr => fr.Id == id && fr.FonctionId == fonctionId);

                if (roleResponsabilite == null)
                {
                    return NotFound($"Rôle/responsabilité avec l'ID {id} non trouvé pour la fonction {fonctionId}");
                }

                // Vérifier l'unicité du libellé si modifié
                if (!string.IsNullOrEmpty(request.Libelle) &&
                    request.Libelle.ToLower() != roleResponsabilite.Libelle.ToLower())
                {
                    var existingRole = await _context.FonctionRolesResponsabilites
                        .AnyAsync(fr => fr.Id != id &&
                                       fr.FonctionId == fonctionId &&
                                       fr.Libelle.ToLower() == request.Libelle.ToLower());

                    if (existingRole)
                    {
                        return BadRequest($"Un rôle/responsabilité avec le libellé '{request.Libelle}' existe déjà pour cette fonction");
                    }
                }

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.Libelle))
                    roleResponsabilite.Libelle = request.Libelle;

                if (request.Description != null)
                    roleResponsabilite.Description = request.Description;

                _context.Entry(roleResponsabilite).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Rôle/responsabilité {Id} mis à jour avec succès", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du rôle/responsabilité {Id}", id);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour du rôle/responsabilité");
            }
        }

        // DELETE: api/fonctions/{fonctionId}/roles-responsabilites/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> DeleteRoleResponsabilite(Guid fonctionId, Guid id)
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
                    return BadRequest("L'identifiant du rôle/responsabilité est invalide");
                }

                var roleResponsabilite = await _context.FonctionRolesResponsabilites
                    .FirstOrDefaultAsync(fr => fr.Id == id && fr.FonctionId == fonctionId);

                if (roleResponsabilite == null)
                {
                    return NotFound($"Rôle/responsabilité avec l'ID {id} non trouvé pour la fonction {fonctionId}");
                }

                _context.FonctionRolesResponsabilites.Remove(roleResponsabilite);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Rôle/responsabilité {Id} supprimé de la fonction {FonctionId}", id, fonctionId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du rôle/responsabilité {Id}", id);
                return StatusCode(500, "Une erreur est survenue lors de la suppression du rôle/responsabilité");
            }
        }

        // GET: api/fonctions/{fonctionId}/roles-responsabilites/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<FonctionRoleResponsabiliteDto>>> SearchRolesResponsabilites(
            Guid fonctionId,
            [FromQuery] string? terme = null)
        {
            try
            {
                // Validation des paramètres
                if (fonctionId == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la fonction est invalide");
                }

                var query = _context.FonctionRolesResponsabilites
                    .Include(fr => fr.Fonction)
                    .Where(fr => fr.FonctionId == fonctionId);

                // Filtre par terme de recherche
                if (!string.IsNullOrEmpty(terme))
                {
                    var termeLower = terme.ToLower();
                    query = query.Where(fr => fr.Libelle.ToLower().Contains(termeLower) ||
                                            (fr.Description != null && fr.Description.ToLower().Contains(termeLower)));
                }

                var results = await query
                    .OrderBy(fr => fr.Libelle)
                    .Select(fr => new FonctionRoleResponsabiliteDto
                    {
                        Id = fr.Id,
                        Libelle = fr.Libelle,
                        Description = fr.Description,
                        FonctionId = fr.FonctionId,
                        FonctionNom = fr.Fonction.NomFonction
                    })
                    .ToListAsync();

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche des rôles/responsabilités de la fonction {FonctionId}", fonctionId);
                return StatusCode(500, "Une erreur est survenue lors de la recherche");
            }
        }
    }

    // DTOs pour les rôles/responsabilités de fonction
    public class FonctionRoleResponsabiliteDto
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid FonctionId { get; set; }
        public string FonctionNom { get; set; } = string.Empty;
    }

    public class CreateFonctionRoleResponsabiliteRequest
    {
        [Required(ErrorMessage = "Le libellé est obligatoire")]
        public string Libelle { get; set; } = string.Empty;

        public string? Description { get; set; }
    }

    public class UpdateFonctionRoleResponsabiliteRequest
    {
        public string? Libelle { get; set; }

        public string? Description { get; set; }
    }
}