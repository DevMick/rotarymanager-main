using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;

namespace RotaryClubManager.API.Controllers
{
    // Contrôleur pour gérer les commissions par club
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CommissionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CommissionsController> _logger;

        public CommissionsController(
            ApplicationDbContext context,
            ILogger<CommissionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/commissions - Récupère toutes les commissions (avec filtre optionnel par club)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CommissionDto>>> GetCommissions([FromQuery] Guid? clubId = null)
        {
            try
            {
                var query = _context.Commissions
                    .Include(c => c.Club)
                    .AsQueryable();

                if (clubId.HasValue)
                {
                    query = query.Where(c => c.ClubId == clubId.Value);
                }

                var commissions = await query
                    .OrderBy(c => c.Club.Name)
                    .ThenBy(c => c.Nom)
                    .ToListAsync();

                var commissionsDto = commissions.Select(c => new CommissionDto
                {
                    Id = c.Id,
                    Nom = c.Nom,
                    Description = c.Description,
                    RoleEtResponsabilite = c.RoleEtResponsabilite,
                    ClubId = c.ClubId,
                    Club = new ClubDto
                    {
                        Id = c.Club.Id,
                        Name = c.Club.Name,
                    }
                }).ToList();

                return Ok(commissionsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des commissions");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des commissions");
            }
        }

        // GET: api/commissions/club/{clubId} - Récupère les commissions d'un club spécifique
        [HttpGet("club/{clubId:guid}")]
        public async Task<ActionResult<IEnumerable<CommissionDto>>> GetCommissionsByClub(Guid clubId)
        {
            try
            {
                // Vérifier que le club existe
                var clubExists = await _context.Clubs.AnyAsync(c => c.Id == clubId);
                if (!clubExists)
                {
                    return NotFound($"Club avec l'ID {clubId} introuvable");
                }

                var commissions = await _context.Commissions
                    .Include(c => c.Club)
                    .Where(c => c.ClubId == clubId)
                    .OrderBy(c => c.Nom)
                    .ToListAsync();

                var commissionsDto = commissions.Select(c => new CommissionDto
                {
                    Id = c.Id,
                    Nom = c.Nom,
                    Description = c.Description,
                    RoleEtResponsabilite = c.RoleEtResponsabilite,
                    ClubId = c.ClubId,
                    Club = new ClubDto
                    {
                        Id = c.Club.Id,
                        Name = c.Club.Name,
                    }
                }).ToList();

                return Ok(commissionsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des commissions du club {ClubId}", clubId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des commissions");
            }
        }

        // GET: api/commissions/{id} - Récupère une commission spécifique
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CommissionDto>> GetCommission(Guid id)
        {
            try
            {
                var commission = await _context.Commissions
                    .Include(c => c.Club)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (commission == null)
                {
                    return NotFound($"Commission avec l'ID {id} introuvable");
                }

                var commissionDto = new CommissionDto
                {
                    Id = commission.Id,
                    Nom = commission.Nom,
                    Description = commission.Description,
                    ClubId = commission.ClubId,
                    Club = new ClubDto
                    {
                        Id = commission.Club.Id,
                        Name = commission.Club.Name,
                    }
                };

                return Ok(commissionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la commission {CommissionId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de la commission");
            }
        }

        // POST: api/commissions - Crée une nouvelle commission (VERSION CORRIGÉE)
        [HttpPost]
        public async Task<ActionResult<CommissionDto>> CreateCommission(CreateCommissionDto dto)
        {
            try
            {
                // Vérifier que le club existe
                var club = await _context.Clubs.FindAsync(dto.ClubId);
                if (club == null)
                {
                    return BadRequest($"Club avec l'ID {dto.ClubId} introuvable");
                }

                // Vérifier l'unicité du nom dans le club
                var existingCommission = await _context.Commissions
                    .FirstOrDefaultAsync(c => c.ClubId == dto.ClubId &&
                                            c.Nom.ToLower() == dto.Nom.ToLower());

                if (existingCommission != null)
                {
                    return BadRequest($"Une commission avec le nom '{dto.Nom}' existe déjà dans ce club");
                }

                var commission = new Commission
                {
                    Id = Guid.NewGuid(),
                    Nom = dto.Nom,
                    Description = dto.Description ?? string.Empty,
                    RoleEtResponsabilite = dto.RoleEtResponsabilite, // ✅ AJOUTÉ
                    ClubId = dto.ClubId
                };

                _context.Commissions.Add(commission);
                await _context.SaveChangesAsync();

                // Recharger avec le club pour la réponse
                await _context.Entry(commission)
                    .Reference(c => c.Club)
                    .LoadAsync();

                _logger.LogInformation("Commission '{CommissionNom}' créée avec succès pour le club {ClubId}",
                    commission.Nom, commission.ClubId);

                var commissionDto = new CommissionDto
                {
                    Id = commission.Id,
                    Nom = commission.Nom,
                    Description = commission.Description,
                    RoleEtResponsabilite = commission.RoleEtResponsabilite, // ✅ AJOUTÉ
                    ClubId = commission.ClubId,
                    Club = new ClubDto
                    {
                        Id = commission.Club.Id,
                        Name = commission.Club.Name,
                    }
                };

                return CreatedAtAction(nameof(GetCommission), new { id = commission.Id }, commissionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la commission");
                return StatusCode(500, "Une erreur est survenue lors de la création de la commission");
            }
        }


        // PUT: api/commissions/{id} - Met à jour une commission
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateCommission(Guid id, UpdateCommissionDto dto)
        {
            try
            {
                var commission = await _context.Commissions.FindAsync(id);
                if (commission == null)
                {
                    return NotFound($"Commission avec l'ID {id} introuvable");
                }

                // Vérifier l'unicité du nom dans le club (excluant la commission actuelle)
                if (!string.IsNullOrEmpty(dto.Nom) && dto.Nom != commission.Nom)
                {
                    var existingCommission = await _context.Commissions
                        .FirstOrDefaultAsync(c => c.Id != id &&
                                                c.ClubId == commission.ClubId &&
                                                c.Nom.ToLower() == dto.Nom.ToLower());

                    if (existingCommission != null)
                    {
                        return BadRequest($"Une commission avec le nom '{dto.Nom}' existe déjà dans ce club");
                    }
                }

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(dto.Nom))
                    commission.Nom = dto.Nom;

                if (dto.Description != null)
                    commission.Description = dto.Description;

                _context.Entry(commission).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Commission {CommissionId} mise à jour avec succès", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la commission {CommissionId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de la commission");
            }
        }

        // DELETE: api/commissions/{id} - Supprime une commission
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteCommission(Guid id)
        {
            try
            {
                var commission = await _context.Commissions
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (commission == null)
                {
                    return NotFound($"Commission avec l'ID {id} introuvable");
                }

                _context.Commissions.Remove(commission);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Commission {CommissionId} supprimée avec succès", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la commission {CommissionId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de la commission");
            }
        }
    }

    // DTOs mis à jour pour les commissions liées aux clubs
    public class CommissionDto
    {
        public Guid Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RoleEtResponsabilite { get; set; } = string.Empty;
        public Guid ClubId { get; set; }
        public ClubDto? Club { get; set; }
    }

    public class CreateCommissionDto
    {
        [Required]
        [MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        [Required]
        public string RoleEtResponsabilite { get; set; } = string.Empty;

        [Required]
        public Guid ClubId { get; set; }
    }

    public class UpdateCommissionDto
    {
        [MaxLength(100)]
        public string? Nom { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }

        public string? RoleEtResponsabilite { get; set; }
    }

    // DTOs pour les instances de commissions dans les clubs (inchangés)
    public class CommissionClubDto
    {
        public Guid Id { get; set; }
        public bool EstActive { get; set; }
        public string? NotesSpecifiques { get; set; }
        public DateTime DateCreation { get; set; }
        public Guid CommissionId { get; set; }
        public Guid ClubId { get; set; }
        public CommissionDto? Commission { get; set; }
        public ClubDto? Club { get; set; }
        public List<MembreCommissionDto>? MembresCommission { get; set; }
        // Statistiques des membres (au lieu de la liste complète)
        public int NombreMembresActifs { get; set; }
        public int NombreResponsables { get; set; }
    }

    public class CreateCommissionClubDto
    {
        [Required]
        public Guid CommissionId { get; set; }

        public bool EstActive { get; set; } = true;

        [MaxLength(500)]
        public string? NotesSpecifiques { get; set; }
    }

    public class UpdateCommissionClubDto
    {
        public bool? EstActive { get; set; }

        [MaxLength(500)]
        public string? NotesSpecifiques { get; set; }
    }

    // DTOs pour éviter les cycles de références
    public class ClubDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class MembreCommissionDto
    {
        public Guid Id { get; set; }
        public bool EstResponsable { get; set; }
        public DateTime DateNomination { get; set; }
        public DateTime? DateDemission { get; set; }
        public bool EstActif { get; set; }
        public string? Commentaires { get; set; }
        public Guid CommissionClubId { get; set; }
        public string MembreId { get; set; } = string.Empty;
        public Guid MandatId { get; set; }
        public MembreDto? Membre { get; set; }
        public MandatDto? Mandat { get; set; }
    }

    public class MembreDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class MandatDto
    {
        public Guid Id { get; set; }
        public int Annee { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public bool EstActuel { get; set; }
        public string PeriodeComplete { get; set; } = string.Empty; // Champ calculé
        public string? Description { get; set; }
    }
}