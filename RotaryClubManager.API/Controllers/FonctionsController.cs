using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/fonctions")]
    [ApiController]
    [Authorize]
    public class FonctionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FonctionsController> _logger;

        public FonctionsController(
            ApplicationDbContext context,
            ILogger<FonctionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/fonctions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FonctionDto>>> GetFonctions([FromQuery] string? search = null)
        {
            try
            {
                var query = _context.Fonctions.AsQueryable();

                // Filtrer par terme de recherche si fourni
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(f => f.NomFonction.ToLower().Contains(search.ToLower()));
                }

                var fonctions = await query
                    .OrderBy(f => f.NomFonction)
                    .Select(f => new FonctionDto
                    {
                        Id = f.Id,
                        NomFonction = f.NomFonction
                    })
                    .ToListAsync();

                return Ok(fonctions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des fonctions");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des fonctions");
            }
        }

        // GET: api/fonctions/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<FonctionDto>> GetFonction(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la fonction est invalide");
                }

                var fonction = await _context.Fonctions
                    .Where(f => f.Id == id)
                    .Select(f => new FonctionDto
                    {
                        Id = f.Id,
                        NomFonction = f.NomFonction
                    })
                    .FirstOrDefaultAsync();

                if (fonction == null)
                {
                    return NotFound($"Fonction avec l'ID {id} non trouvée");
                }

                return Ok(fonction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la fonction {FonctionId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de la fonction");
            }
        }

        // POST: api/fonctions
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<FonctionDto>> PostFonction([FromBody] CreateFonctionRequest request)
        {
            try
            {
                // Vérifier l'unicité du nom
                var existingFonction = await _context.Fonctions
                    .AnyAsync(f => f.NomFonction.ToLower() == request.NomFonction.ToLower());

                if (existingFonction)
                {
                    return BadRequest($"Une fonction avec le nom '{request.NomFonction}' existe déjà");
                }

                // Créer la fonction
                var fonction = new Fonction
                {
                    Id = Guid.NewGuid(),
                    NomFonction = request.NomFonction
                };

                _context.Fonctions.Add(fonction);
                await _context.SaveChangesAsync();

                var fonctionDto = new FonctionDto
                {
                    Id = fonction.Id,
                    NomFonction = fonction.NomFonction
                };

                _logger.LogInformation("Fonction {FonctionNom} créée avec l'ID {FonctionId}",
                    request.NomFonction, fonction.Id);

                return CreatedAtAction(
                    nameof(GetFonction),
                    new { id = fonction.Id },
                    fonctionDto
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la fonction {FonctionNom}", request.NomFonction);
                return StatusCode(500, "Une erreur est survenue lors de la création de la fonction");
            }
        }

        // PUT: api/fonctions/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutFonction(Guid id, [FromBody] UpdateFonctionRequest request)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la fonction est invalide");
                }

                var fonction = await _context.Fonctions.FindAsync(id);

                if (fonction == null)
                {
                    return NotFound($"Fonction avec l'ID {id} non trouvée");
                }

                // Vérifier l'unicité du nom si modifié
                if (!string.IsNullOrEmpty(request.NomFonction) &&
                    request.NomFonction.ToLower() != fonction.NomFonction.ToLower())
                {
                    var existingFonction = await _context.Fonctions
                        .AnyAsync(f => f.NomFonction.ToLower() == request.NomFonction.ToLower() && f.Id != id);

                    if (existingFonction)
                    {
                        return BadRequest($"Une fonction avec le nom '{request.NomFonction}' existe déjà");
                    }
                }

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.NomFonction))
                    fonction.NomFonction = request.NomFonction;

                _context.Entry(fonction).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Fonction {FonctionId} mise à jour avec succès", id);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await FonctionExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la fonction {FonctionId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de la fonction");
            }
        }

        // DELETE: api/fonctions/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteFonction(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de la fonction est invalide");
                }

                var fonction = await _context.Fonctions.FindAsync(id);

                if (fonction == null)
                {
                    return NotFound($"Fonction avec l'ID {id} non trouvée");
                }

                // Vérifier s'il y a des membres utilisant cette fonction
                var hasComiteMembres = await _context.ComiteMembres.AnyAsync(cm => cm.FonctionId == id);
                if (hasComiteMembres)
                {
                    return BadRequest("Impossible de supprimer cette fonction car elle est utilisée par des membres de comité");
                }

                _context.Fonctions.Remove(fonction);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Fonction {FonctionId} supprimée", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la fonction {FonctionId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de la fonction");
            }
        }

        // GET: api/fonctions/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<FonctionDto>>> SearchFonctions([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrEmpty(searchTerm))
                {
                    return BadRequest("Le terme de recherche est requis");
                }

                if (searchTerm.Length < 2)
                {
                    return BadRequest("Le terme de recherche doit contenir au moins 2 caractères");
                }

                var fonctions = await _context.Fonctions
                    .Where(f => f.NomFonction.ToLower().Contains(searchTerm.ToLower()))
                    .OrderBy(f => f.NomFonction)
                    .Select(f => new FonctionDto
                    {
                        Id = f.Id,
                        NomFonction = f.NomFonction
                    })
                    .ToListAsync();

                return Ok(fonctions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche de fonctions avec le terme {SearchTerm}", searchTerm);
                return StatusCode(500, "Une erreur est survenue lors de la recherche");
            }
        }

        // Méthodes d'aide
        private async Task<bool> FonctionExists(Guid id)
        {
            return await _context.Fonctions.AnyAsync(e => e.Id == id);
        }
    }

    // DTOs mis à jour
    public class FonctionDto
    {
        public Guid Id { get; set; }
        public string NomFonction { get; set; } = string.Empty;
    }

    public class CreateFonctionRequest
    {
        [Required]
        public string NomFonction { get; set; } = string.Empty;
    }

    public class UpdateFonctionRequest
    {
        public string? NomFonction { get; set; }
    }
}