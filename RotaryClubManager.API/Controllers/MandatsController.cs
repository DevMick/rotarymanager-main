using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using RotaryClubManager.Infrastructure.Services;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers;

[Route("api/clubs/{clubId}/mandats")]
[ApiController]
[Authorize]
public class MandatsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public MandatsController(ApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    // Récupérer tous les mandats d'un club
    [HttpGet]
    public async Task<IActionResult> GetMandats(Guid clubId)
    {
        if (!await CanAccessClub(clubId))
        {
            return Forbid();
        }

        _tenantService.SetCurrentTenantId(clubId);

        var mandats = await _context.Mandats
            .Where(m => m.ClubId == clubId)
            .OrderByDescending(m => m.Annee)
            .Select(m => new
            {
                m.Id,
                m.Annee,
                m.DateDebut,
                m.DateFin,
                m.EstActuel,
                m.Description,
                m.MontantCotisation,
                PeriodeComplete = $"{m.Annee} ({m.DateDebut.ToShortDateString()} - {m.DateFin.ToShortDateString()})"
            })
            .ToListAsync();

        return Ok(mandats);
    }

    // Récupérer le mandat actuel
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentMandat(Guid clubId)
    {
        if (!await CanAccessClub(clubId))
        {
            return Forbid();
        }

        _tenantService.SetCurrentTenantId(clubId);

        var mandatActuel = await _context.Mandats
            .Where(m => m.ClubId == clubId && m.EstActuel)
            .Select(m => new
            {
                m.Id,
                m.Annee,
                m.DateDebut,
                m.DateFin,
                m.EstActuel,
                m.Description,
                m.MontantCotisation,
                PeriodeComplete = $"{m.Annee} ({m.DateDebut.ToShortDateString()} - {m.DateFin.ToShortDateString()})"
            })
            .FirstOrDefaultAsync();

        if (mandatActuel == null)
        {
            return NotFound("Aucun mandat actuel trouvé");
        }

        return Ok(mandatActuel);
    }

    // Créer un nouveau mandat
    [HttpPost]
    [Authorize(Roles = "Admin,President")]
    public async Task<IActionResult> CreateMandat(Guid clubId, [FromBody] CreateMandatRequest request)
    {
        if (!await CanManageClub(clubId))
        {
            return Forbid();
        }

        // Vérifier que l'année n'existe pas déjà
        var existingMandat = await _context.Mandats
            .AnyAsync(m => m.ClubId == clubId && m.Annee == request.Annee);

        if (existingMandat)
        {
            return BadRequest($"Un mandat existe déjà pour l'année {request.Annee}");
        }

        // Désactiver le mandat actuel
        var mandatActuel = await _context.Mandats
            .Where(m => m.ClubId == clubId && m.EstActuel)
            .FirstOrDefaultAsync();

        if (mandatActuel != null)
        {
            mandatActuel.EstActuel = false;
            mandatActuel.DateFin = DateTime.UtcNow;
        }

        // Créer le nouveau mandat - CONVERSION EXPLICITE DES DATES EN UTC
        var mandat = new Mandat
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Annee = request.Annee,
            DateDebut = DateTime.SpecifyKind(request.DateDebut, DateTimeKind.Utc),
            DateFin = DateTime.SpecifyKind(request.DateFin, DateTimeKind.Utc),
            EstActuel = request.EstActuel,
            Description = request.Description,
            MontantCotisation = request.MontantCotisation
        };

        _context.Mandats.Add(mandat);
        await _context.SaveChangesAsync();

        // Retourner l'objet avec le champ calculé
        var mandatResponse = new
        {
            mandat.Id,
            mandat.Annee,
            mandat.DateDebut,
            mandat.DateFin,
            mandat.EstActuel,
            mandat.Description,
            mandat.MontantCotisation,
            PeriodeComplete = mandat.PeriodeComplete
        };

        return CreatedAtAction(nameof(GetCurrentMandat), new { clubId }, mandatResponse);
    }

    // Activer un mandat (le définir comme actuel)
    [HttpPut("{mandatId}/activate")]
    [Authorize(Roles = "Admin,President")]
    public async Task<IActionResult> ActivateMandat(Guid clubId, Guid mandatId)
    {
        if (!await CanManageClub(clubId))
        {
            return Forbid();
        }

        // Désactiver tous les mandats du club
        await _context.Mandats
            .Where(m => m.ClubId == clubId)
            .ForEachAsync(m => m.EstActuel = false);

        // Activer le mandat spécifié
        var mandat = await _context.Mandats
            .FirstOrDefaultAsync(m => m.Id == mandatId && m.ClubId == clubId);

        if (mandat == null)
        {
            return NotFound();
        }

        mandat.EstActuel = true;
        await _context.SaveChangesAsync();

        return Ok("Mandat activé avec succès");
    }

    // Méthodes d'aide pour vérifier les autorisations
    private async Task<bool> CanAccessClub(Guid clubId)
    {
        if (User.IsInRole("Admin"))
            return true;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return await _context.UserClubs
            .AnyAsync(uc => uc.ClubId == clubId && uc.UserId == userId);
    }

    private async Task<bool> CanManageClub(Guid clubId)
    {
        if (User.IsInRole("Admin"))
            return true;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return false;

        // Vérifier si l'utilisateur appartient au club
        var userInClub = await _context.UserClubs
            .AnyAsync(uc => uc.UserId == userId && uc.ClubId == clubId);

        if (!userInClub)
            return false;

        return User.IsInRole("President") || User.IsInRole("Secretary");
    }

    // Mettre à jour un mandat existant
    [HttpPut("{mandatId}")]
    [Authorize(Roles = "Admin,President")]
    public async Task<IActionResult> UpdateMandat(Guid clubId, Guid mandatId, [FromBody] UpdateMandatRequest request)
    {
        if (!await CanManageClub(clubId))
        {
            return Forbid();
        }

        _tenantService.SetCurrentTenantId(clubId);

        var mandat = await _context.Mandats
            .FirstOrDefaultAsync(m => m.Id == mandatId && m.ClubId == clubId);

        if (mandat == null)
        {
            return NotFound("Mandat non trouvé");
        }

        // Vérifier que l'année n'existe pas déjà pour un autre mandat
        var existingMandat = await _context.Mandats
            .AnyAsync(m => m.ClubId == clubId && m.Annee == request.Annee && m.Id != mandatId);

        if (existingMandat)
        {
            return BadRequest($"Un autre mandat existe déjà pour l'année {request.Annee}");
        }

        // Mise à jour des informations du mandat
        mandat.Annee = request.Annee;
        mandat.DateDebut = DateTime.SpecifyKind(request.DateDebut, DateTimeKind.Utc);
        mandat.DateFin = DateTime.SpecifyKind(request.DateFin, DateTimeKind.Utc);
        mandat.Description = request.Description;
        mandat.MontantCotisation = request.MontantCotisation;

        // Si ce mandat devient le mandat actuel, désactiver les autres
        if (request.EstActuel && !mandat.EstActuel)
        {
            await _context.Mandats
                .Where(m => m.ClubId == clubId && m.Id != mandatId)
                .ForEachAsync(m => m.EstActuel = false);

            mandat.EstActuel = true;
        }
        // Si ce mandat n'est plus actuel, vérifier qu'il y a un autre mandat actuel
        else if (!request.EstActuel && mandat.EstActuel)
        {
            // On ne désactive pas si c'est le seul mandat
            var mandatsCount = await _context.Mandats
                .CountAsync(m => m.ClubId == clubId);

            if (mandatsCount <= 1)
            {
                return BadRequest("Impossible de désactiver le seul mandat existant");
            }

            mandat.EstActuel = false;
        }

        await _context.SaveChangesAsync();

        // Retourner l'objet avec le champ calculé
        var mandatResponse = new
        {
            mandat.Id,
            mandat.Annee,
            mandat.DateDebut,
            mandat.DateFin,
            mandat.EstActuel,
            mandat.Description,
            mandat.MontantCotisation,
            PeriodeComplete = mandat.PeriodeComplete
        };

        return Ok(mandatResponse);
    }

    // Supprimer un mandat
    [HttpDelete("{mandatId}")]
    [Authorize(Roles = "Admin,President")]
    public async Task<IActionResult> DeleteMandat(Guid clubId, Guid mandatId)
    {
        if (!await CanManageClub(clubId))
        {
            return Forbid();
        }

        _tenantService.SetCurrentTenantId(clubId);

        var mandat = await _context.Mandats
            .FirstOrDefaultAsync(m => m.Id == mandatId && m.ClubId == clubId);

        if (mandat == null)
        {
            return NotFound("Mandat non trouvé");
        }

        // Vérifier si c'est un mandat actuel
        if (mandat.EstActuel)
        {
            // Compter le nombre total de mandats pour ce club
            var mandatsCount = await _context.Mandats
                .CountAsync(m => m.ClubId == clubId);

            if (mandatsCount <= 1)
            {
                return BadRequest("Impossible de supprimer le seul mandat actuel");
            }

            // Trouver le mandat le plus récent (autre que celui-ci) pour l'activer
            var newCurrentMandat = await _context.Mandats
                .Where(m => m.ClubId == clubId && m.Id != mandatId)
                .OrderByDescending(m => m.Annee)
                .FirstOrDefaultAsync();

            if (newCurrentMandat != null)
            {
                newCurrentMandat.EstActuel = true;
                // Mettre à jour la date de début pour le nouveau mandat actuel
                newCurrentMandat.DateDebut = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            }
        }

        _context.Mandats.Remove(mandat);
        await _context.SaveChangesAsync();

        return Ok("Mandat supprimé avec succès");
    }
}

// Classes de requête pour les mandats
public class CreateMandatRequest
{
    public int Annee { get; set; }
    public DateTime DateDebut { get; set; }
    public DateTime DateFin { get; set; }
    public bool EstActuel { get; set; } = true;
    public string Description { get; set; } = string.Empty;
    public int MontantCotisation { get; set; }
}

public class UpdateMandatRequest
{
    public int Annee { get; set; }
    public DateTime DateDebut { get; set; }
    public DateTime DateFin { get; set; }
    public bool EstActuel { get; set; }
    public string Description { get; set; } = string.Empty;
    public int MontantCotisation { get; set; }
}