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
    [Authorize] // Require authentication for all endpoints
    public class CotisationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CotisationController> _logger;

        public CotisationController(ApplicationDbContext context, ILogger<CotisationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Récupère toutes les cotisations
        /// </summary>
        /// <returns>Liste de toutes les cotisations</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetCotisations()
        {
            try
            {
                var cotisations = await _context.Cotisations
                    .Include(c => c.Membre)
                    .Include(c => c.Mandat)
                        .ThenInclude(m => m.Club)
                    .OrderByDescending(c => c.Mandat.Annee)
                    .ThenBy(c => c.Membre.LastName)
                    .Select(c => new
                    {
                        id = c.Id,
                        montant = c.Montant,
                        membreId = c.MembreId,
                        membre = new
                        {
                            id = c.Membre.Id,
                            firstName = c.Membre.FirstName,
                            lastName = c.Membre.LastName,
                            fullName = $"{c.Membre.FirstName} {c.Membre.LastName}",
                            email = c.Membre.Email,
                            phoneNumber = c.Membre.PhoneNumber,
                            profilePictureUrl = c.Membre.ProfilePictureUrl,
                            joinedDate = c.Membre.JoinedDate,
                            isActive = c.Membre.IsActive
                        },
                        mandatId = c.MandatId,
                        mandat = new
                        {
                            id = c.Mandat.Id,
                            annee = c.Mandat.Annee,
                            dateDebut = c.Mandat.DateDebut,
                            dateFin = c.Mandat.DateFin,
                            description = c.Mandat.Description,
                            estActuel = c.Mandat.EstActuel,
                            periodeComplete = c.Mandat.PeriodeComplete,
                            clubId = c.Mandat.ClubId,
                            club = new
                            {
                                id = c.Mandat.Club.Id,
                                name = c.Mandat.Club.Name,
                            }
                        }
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    count = cotisations.Count,
                    data = cotisations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des cotisations");
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors de la récupération des cotisations",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Récupère une cotisation par son ID
        /// </summary>
        /// <param name="id">ID de la cotisation</param>
        /// <returns>Détails de la cotisation</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetCotisation(Guid id)
        {
            try
            {
                var cotisation = await _context.Cotisations
                    .Include(c => c.Membre)
                    .Include(c => c.Mandat)
                        .ThenInclude(m => m.Club)
                    .Where(c => c.Id == id)
                    .Select(c => new
                    {
                        id = c.Id,
                        montant = c.Montant,
                        membreId = c.MembreId,
                        membre = new
                        {
                            id = c.Membre.Id,
                            firstName = c.Membre.FirstName,
                            lastName = c.Membre.LastName,
                            fullName = $"{c.Membre.FirstName} {c.Membre.LastName}",
                            email = c.Membre.Email,
                            phoneNumber = c.Membre.PhoneNumber,
                            profilePictureUrl = c.Membre.ProfilePictureUrl,
                            joinedDate = c.Membre.JoinedDate,
                            isActive = c.Membre.IsActive
                        },
                        mandatId = c.MandatId,
                        mandat = new
                        {
                            id = c.Mandat.Id,
                            annee = c.Mandat.Annee,
                            dateDebut = c.Mandat.DateDebut,
                            dateFin = c.Mandat.DateFin,
                            description = c.Mandat.Description,
                            estActuel = c.Mandat.EstActuel,
                            periodeComplete = c.Mandat.PeriodeComplete,
                            clubId = c.Mandat.ClubId,
                            club = new
                            {
                                id = c.Mandat.Club.Id,
                                name = c.Mandat.Club.Name,
                            }
                        }
                    })
                    .FirstOrDefaultAsync();

                if (cotisation == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Cotisation non trouvée"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = cotisation
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la cotisation {CotisationId}", id);
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors de la récupération de la cotisation",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Récupère toutes les cotisations d'un mandat spécifique
        /// </summary>
        /// <param name="mandatId">ID du mandat</param>
        /// <returns>Liste des cotisations du mandat</returns>
        [HttpGet("mandat/{mandatId}")]
        public async Task<ActionResult<object>> GetCotisationsByMandat(Guid mandatId)
        {
            try
            {
                var cotisations = await _context.Cotisations
                    .Include(c => c.Membre)
                    .Include(c => c.Mandat)
                        .ThenInclude(m => m.Club)
                    .Where(c => c.MandatId == mandatId)
                    .OrderBy(c => c.Membre.LastName)
                    .Select(c => new
                    {
                        id = c.Id,
                        montant = c.Montant,
                        membreId = c.MembreId,
                        membre = new
                        {
                            id = c.Membre.Id,
                            firstName = c.Membre.FirstName,
                            lastName = c.Membre.LastName,
                            fullName = $"{c.Membre.FirstName} {c.Membre.LastName}",
                            email = c.Membre.Email,
                            phoneNumber = c.Membre.PhoneNumber,
                            profilePictureUrl = c.Membre.ProfilePictureUrl,
                            joinedDate = c.Membre.JoinedDate,
                            isActive = c.Membre.IsActive
                        },
                        mandatId = c.MandatId,
                        mandat = new
                        {
                            id = c.Mandat.Id,
                            annee = c.Mandat.Annee,
                            dateDebut = c.Mandat.DateDebut,
                            dateFin = c.Mandat.DateFin,
                            description = c.Mandat.Description,
                            estActuel = c.Mandat.EstActuel,
                            periodeComplete = c.Mandat.PeriodeComplete,
                            clubId = c.Mandat.ClubId,
                            club = new
                            {
                                id = c.Mandat.Club.Id,
                                name = c.Mandat.Club.Name,
                            }
                        }
                    })
                    .ToListAsync();

                // Calculer les statistiques
                var totalMontant = cotisations.Sum(c => c.montant);
                var nombreCotisations = cotisations.Count;

                return Ok(new
                {
                    success = true,
                    mandatId = mandatId,
                    count = nombreCotisations,
                    totalMontant = totalMontant,
                    moyenneMontant = nombreCotisations > 0 ? totalMontant / nombreCotisations : 0,
                    data = cotisations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des cotisations du mandat {MandatId}", mandatId);
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors de la récupération des cotisations du mandat",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Récupère toutes les cotisations d'un membre spécifique
        /// </summary>
        /// <param name="membreId">ID du membre</param>
        /// <returns>Liste des cotisations du membre</returns>
        [HttpGet("membre/{membreId}")]
        public async Task<ActionResult<object>> GetCotisationsByMembre(string membreId)
        {
            try
            {
                var cotisations = await _context.Cotisations
                    .Include(c => c.Membre)
                    .Include(c => c.Mandat)
                        .ThenInclude(m => m.Club)
                    .Where(c => c.MembreId == membreId)
                    .OrderByDescending(c => c.Mandat.Annee)
                    .Select(c => new
                    {
                        id = c.Id,
                        montant = c.Montant,
                        membreId = c.MembreId,
                        membre = new
                        {
                            id = c.Membre.Id,
                            firstName = c.Membre.FirstName,
                            lastName = c.Membre.LastName,
                            fullName = $"{c.Membre.FirstName} {c.Membre.LastName}",
                            email = c.Membre.Email
                        },
                        mandatId = c.MandatId,
                        mandat = new
                        {
                            id = c.Mandat.Id,
                            annee = c.Mandat.Annee,
                            dateDebut = c.Mandat.DateDebut,
                            dateFin = c.Mandat.DateFin,
                            description = c.Mandat.Description,
                            estActuel = c.Mandat.EstActuel,
                            periodeComplete = c.Mandat.PeriodeComplete,
                            club = new
                            {
                                id = c.Mandat.Club.Id,
                                name = c.Mandat.Club.Name,
                            }
                        }
                    })
                    .ToListAsync();

                // Calculer les statistiques
                var totalMontant = cotisations.Sum(c => c.montant);
                var nombreCotisations = cotisations.Count;

                return Ok(new
                {
                    success = true,
                    membreId = membreId,
                    count = nombreCotisations,
                    totalMontant = totalMontant,
                    moyenneMontant = nombreCotisations > 0 ? totalMontant / nombreCotisations : 0,
                    data = cotisations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des cotisations du membre {MembreId}", membreId);
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors de la récupération des cotisations du membre",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Crée une nouvelle cotisation
        /// </summary>
        /// <param name="cotisationDto">Données de la cotisation à créer</param>
        /// <returns>Cotisation créée</returns>
        [HttpPost]
        public async Task<ActionResult<object>> PostCotisation([FromBody] CreateCotisationDto cotisationDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Données invalides",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                // Vérifier si le membre existe
                var membreExists = await _context.Users.AnyAsync(u => u.Id == cotisationDto.MembreId);
                if (!membreExists)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Le membre spécifié n'existe pas."
                    });
                }

                // Vérifier si le mandat existe
                var mandatExists = await _context.Mandats.AnyAsync(m => m.Id == cotisationDto.MandatId);
                if (!mandatExists)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Le mandat spécifié n'existe pas."
                    });
                }

                // Vérifier s'il existe déjà une cotisation pour ce membre et ce mandat
                var existingCotisation = await _context.Cotisations
                    .FirstOrDefaultAsync(c => c.MembreId == cotisationDto.MembreId && c.MandatId == cotisationDto.MandatId);

                if (existingCotisation != null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Une cotisation existe déjà pour ce membre et ce mandat."
                    });
                }

                var cotisation = new Cotisation
                {
                    Id = Guid.NewGuid(),
                    Montant = cotisationDto.Montant,
                    MembreId = cotisationDto.MembreId,
                    MandatId = cotisationDto.MandatId
                };

                _context.Cotisations.Add(cotisation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cotisation créée avec succès: {CotisationId} pour le membre {MembreId} et le mandat {MandatId}",
                    cotisation.Id, cotisationDto.MembreId, cotisationDto.MandatId);

                return CreatedAtAction(nameof(GetCotisation), new { id = cotisation.Id }, new
                {
                    success = true,
                    message = "Cotisation créée avec succès",
                    data = new
                    {
                        id = cotisation.Id,
                        montant = cotisation.Montant,
                        membreId = cotisation.MembreId,
                        mandatId = cotisation.MandatId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la cotisation");
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors de la création de la cotisation",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Met à jour une cotisation existante
        /// </summary>
        /// <param name="id">ID de la cotisation à modifier</param>
        /// <param name="cotisationDto">Nouvelles données de la cotisation</param>
        /// <returns>Résultat de la mise à jour</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCotisation(Guid id, [FromBody] UpdateCotisationDto cotisationDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Données invalides",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                var cotisation = await _context.Cotisations.FindAsync(id);
                if (cotisation == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Cotisation non trouvée"
                    });
                }

                // Vérifier si le membre existe (si changé)
                if (!string.IsNullOrEmpty(cotisationDto.MembreId) && cotisationDto.MembreId != cotisation.MembreId)
                {
                    var membreExists = await _context.Users.AnyAsync(u => u.Id == cotisationDto.MembreId);
                    if (!membreExists)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Le membre spécifié n'existe pas."
                        });
                    }

                    // Vérifier qu'il n'existe pas déjà une cotisation pour ce nouveau membre et ce mandat
                    var duplicateCotisation = await _context.Cotisations
                        .FirstOrDefaultAsync(c => c.MembreId == cotisationDto.MembreId &&
                                           c.MandatId == cotisation.MandatId &&
                                           c.Id != id);
                    if (duplicateCotisation != null)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Une cotisation existe déjà pour ce membre et ce mandat."
                        });
                    }
                }

                // Vérifier si le mandat existe (si changé)
                if (cotisationDto.MandatId.HasValue && cotisationDto.MandatId != cotisation.MandatId)
                {
                    var mandatExists = await _context.Mandats.AnyAsync(m => m.Id == cotisationDto.MandatId.Value);
                    if (!mandatExists)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Le mandat spécifié n'existe pas."
                        });
                    }

                    // Vérifier qu'il n'existe pas déjà une cotisation pour ce membre et ce nouveau mandat
                    var duplicateCotisation = await _context.Cotisations
                        .FirstOrDefaultAsync(c => c.MembreId == cotisation.MembreId &&
                                           c.MandatId == cotisationDto.MandatId.Value &&
                                           c.Id != id);
                    if (duplicateCotisation != null)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Une cotisation existe déjà pour ce membre et ce mandat."
                        });
                    }
                }

                // Mettre à jour les propriétés
                if (cotisationDto.Montant.HasValue)
                    cotisation.Montant = cotisationDto.Montant.Value;

                if (!string.IsNullOrEmpty(cotisationDto.MembreId))
                    cotisation.MembreId = cotisationDto.MembreId;

                if (cotisationDto.MandatId.HasValue)
                    cotisation.MandatId = cotisationDto.MandatId.Value;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Cotisation {CotisationId} mise à jour avec succès", id);

                return Ok(new
                {
                    success = true,
                    message = "Cotisation mise à jour avec succès"
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CotisationExists(id))
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Cotisation non trouvée"
                    });
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la cotisation {CotisationId}", id);
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors de la mise à jour de la cotisation",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Supprime une cotisation
        /// </summary>
        /// <param name="id">ID de la cotisation à supprimer</param>
        /// <returns>Résultat de la suppression</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Seuls les admins peuvent supprimer
        public async Task<IActionResult> DeleteCotisation(Guid id)
        {
            try
            {
                var cotisation = await _context.Cotisations.FindAsync(id);
                if (cotisation == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Cotisation non trouvée"
                    });
                }

                _context.Cotisations.Remove(cotisation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cotisation {CotisationId} supprimée avec succès", id);

                return Ok(new
                {
                    success = true,
                    message = "Cotisation supprimée avec succès"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la cotisation {CotisationId}", id);
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors de la suppression de la cotisation",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Crée des cotisations pour tous les membres d'un mandat
        /// </summary>
        /// <param name="dto">Données pour la création en masse</param>
        /// <returns>Résultat de la création en masse</returns>
        [HttpPost("bulk-create")]
        [Authorize(Roles = "Admin")] // Seuls les admins peuvent créer en masse
        public async Task<ActionResult<BulkCreateResult>> CreateCotisationsForAllMembers([FromBody] BulkCreateCotisationDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Données invalides",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                // Vérifier si le mandat existe
                var mandat = await _context.Mandats
                    .Include(m => m.Club)
                    .FirstOrDefaultAsync(m => m.Id == dto.MandatId);

                if (mandat == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Le mandat spécifié n'existe pas."
                    });
                }

                // Récupérer tous les membres actifs du club du mandat
                var clubId = mandat.ClubId;
                var membres = await _context.UserClubs
                    .Include(uc => uc.User)
                    .Where(uc => uc.ClubId == clubId && uc.User.IsActive)
                    .Select(uc => uc.User)
                    .ToListAsync();

                if (!membres.Any())
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Aucun membre actif trouvé dans ce club."
                    });
                }

                // Récupérer les cotisations existantes pour ce mandat
                var cotisationsExistantes = await _context.Cotisations
                    .Where(c => c.MandatId == dto.MandatId)
                    .Select(c => c.MembreId)
                    .ToListAsync();

                var cotisationsACreer = new List<Cotisation>();
                var membresIgnores = new List<MembreIgnore>();

                foreach (var membre in membres)
                {
                    // Ignorer les membres qui ont déjà une cotisation pour ce mandat
                    if (cotisationsExistantes.Contains(membre.Id))
                    {
                        membresIgnores.Add(new MembreIgnore
                        {
                            Id = membre.Id,
                            Name = $"{membre.FirstName} {membre.LastName}",
                            Email = membre.Email,
                            Reason = "Cotisation déjà existante"
                        });
                        continue;
                    }

                    cotisationsACreer.Add(new Cotisation
                    {
                        Id = Guid.NewGuid(),
                        Montant = dto.Montant,
                        MembreId = membre.Id,
                        MandatId = dto.MandatId
                    });
                }

                if (cotisationsACreer.Any())
                {
                    _context.Cotisations.AddRange(cotisationsACreer);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Création en masse de cotisations: {Count} créées, {Ignored} ignorées pour le mandat {MandatId}",
                    cotisationsACreer.Count, membresIgnores.Count, dto.MandatId);

                var result = new BulkCreateResult
                {
                    Success = true,
                    CotisationsCrees = cotisationsACreer.Count,
                    MembresIgnores = membresIgnores.Count,
                    MembresIgnoresDetails = membresIgnores,
                    MandatInfo = new
                    {
                        Id = mandat.Id,
                        Annee = mandat.Annee,
                        Description = mandat.Description,
                        Club = mandat.Club?.Name
                    },
                    Message = $"{cotisationsACreer.Count} cotisation(s) créée(s) avec succès. {membresIgnores.Count} membre(s) ignoré(s)."
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création en masse de cotisations pour le mandat {MandatId}", dto.MandatId);
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors de la création en masse de cotisations",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtient les statistiques des cotisations par mandat
        /// </summary>
        /// <param name="mandatId">ID du mandat (optionnel)</param>
        /// <returns>Statistiques des cotisations</returns>
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetCotisationStatistics([FromQuery] Guid? mandatId = null)
        {
            try
            {
                var query = _context.Cotisations
                    .Include(c => c.Mandat)
                    .AsQueryable();

                if (mandatId.HasValue)
                {
                    query = query.Where(c => c.MandatId == mandatId.Value);
                }

                var stats = await query
                    .GroupBy(c => new { c.MandatId, c.Mandat.Annee, c.Mandat.Description })
                    .Select(g => new
                    {
                        mandatId = g.Key.MandatId,
                        annee = g.Key.Annee,
                        description = g.Key.Description,
                        nombreCotisations = g.Count(),
                        montantTotal = g.Sum(c => c.Montant),
                        montantMoyen = g.Average(c => c.Montant),
                        montantMin = g.Min(c => c.Montant),
                        montantMax = g.Max(c => c.Montant)
                    })
                    .OrderByDescending(s => s.annee)
                    .ToListAsync();

                var totalGeneral = await query.SumAsync(c => c.Montant);
                var nombreTotalCotisations = await query.CountAsync();

                return Ok(new
                {
                    success = true,
                    totalGeneral = totalGeneral,
                    nombreTotalCotisations = nombreTotalCotisations,
                    moyenneGenerale = nombreTotalCotisations > 0 ? totalGeneral / nombreTotalCotisations : 0,
                    statistiquesParMandat = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques de cotisations");
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors de la récupération des statistiques",
                    error = ex.Message
                });
            }
        }

        private bool CotisationExists(Guid id)
        {
            return _context.Cotisations.Any(e => e.Id == id);
        }

        /// <summary>
        /// Obtient la situation globale des cotisations (montants dus, payés et solde)
        /// </summary>
        /// <returns>Situation financière globale des cotisations</returns>
        [HttpGet("situation")]
        public async Task<ActionResult<object>> GetSituationCotisations()
        {
            try
            {
                // Calcul du montant total des cotisations dues (tous mandats confondus)
                var montantTotalCotisations = await _context.Cotisations
                    .SumAsync(c => c.Montant);

                // Calcul du montant total des paiements (tous mandats confondus)
                var montantTotalPaiements = await _context.PaiementCotisations
                    .SumAsync(p => p.Montant);

                // Calcul du solde (cotisations - paiements)
                var solde = montantTotalCotisations - montantTotalPaiements;

                // Statistiques complémentaires
                var nombreCotisations = await _context.Cotisations.CountAsync();
                var nombrePaiements = await _context.PaiementCotisations.CountAsync();

                // Détails par mandat
                var situationParMandat = await _context.Cotisations
                    .Include(c => c.Mandat)
                    .GroupBy(c => new { c.MandatId, c.Mandat.Annee, c.Mandat.Description })
                    .Select(g => new
                    {
                        mandatId = g.Key.MandatId,
                        annee = g.Key.Annee,
                        description = g.Key.Description,
                        montantCotisations = g.Sum(c => c.Montant),
                        nombreCotisations = g.Count()
                    })
                    .OrderByDescending(s => s.annee)
                    .ToListAsync();

                // Ajouter les paiements par mandat (en supposant qu'on peut lier via le membre et la période)
                var situationDetaillee = new List<object>();
                foreach (var mandat in situationParMandat)
                {
                    // Pour chaque mandat, calculer les paiements correspondants
                    // Note: Cette logique peut nécessiter un ajustement selon votre modèle de données
                    var paiementsMandat = await _context.PaiementCotisations
                        .Where(p => _context.Cotisations
                            .Any(c => c.MandatId == mandat.mandatId && c.MembreId == p.MembreId))
                        .SumAsync(p => p.Montant);

                    situationDetaillee.Add(new
                    {
                        mandatId = mandat.mandatId,
                        annee = mandat.annee,
                        description = mandat.description,
                        montantCotisations = mandat.montantCotisations,
                        montantPaiements = paiementsMandat,
                        solde = mandat.montantCotisations - paiementsMandat,
                        nombreCotisations = mandat.nombreCotisations,
                        tauxRecouvrement = mandat.montantCotisations > 0
                            ? Math.Round((double)paiementsMandat / mandat.montantCotisations * 100, 2)
                            : 0
                    });
                }

                var result = new
                {
                    success = true,
                    resume = new
                    {
                        montantTotalCotisations = montantTotalCotisations,
                        montantTotalPaiements = montantTotalPaiements,
                        soldeGlobal = solde,
                        nombreTotalCotisations = nombreCotisations,
                        nombreTotalPaiements = nombrePaiements,
                        tauxRecouvrementGlobal = montantTotalCotisations > 0
                            ? Math.Round((double)montantTotalPaiements / montantTotalCotisations * 100, 2)
                            : 0,
                        montantMoyenCotisation = nombreCotisations > 0
                            ? Math.Round((double)montantTotalCotisations / nombreCotisations, 2)
                            : 0,
                        montantMoyenPaiement = nombrePaiements > 0
                            ? Math.Round((double)montantTotalPaiements / nombrePaiements, 2)
                            : 0
                    },
                    detailsParMandat = situationDetaillee
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la situation des cotisations");
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors de la récupération de la situation des cotisations",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtient la situation des cotisations pour un membre spécifique
        /// </summary>
        /// <param name="membreId">ID du membre</param>
        /// <returns>Situation des cotisations du membre</returns>
        [HttpGet("situation/membre/{membreId}")]
        public async Task<ActionResult<object>> GetSituationCotisationsMembre(string membreId)
        {
            try
            {
                // Vérifier si le membre existe
                var membreExists = await _context.Users.AnyAsync(u => u.Id == membreId);
                if (!membreExists)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Membre non trouvé"
                    });
                }

                // Cotisations du membre
                var cotisationsMembre = await _context.Cotisations
                    .Include(c => c.Mandat)
                    .Where(c => c.MembreId == membreId)
                    .ToListAsync();

                // Paiements du membre
                var paiementsMembre = await _context.PaiementCotisations
                    .Where(p => p.MembreId == membreId)
                    .ToListAsync();

                var montantTotalCotisations = cotisationsMembre.Sum(c => c.Montant);
                var montantTotalPaiements = paiementsMembre.Sum(p => p.Montant);
                var soldeMembre = montantTotalCotisations - montantTotalPaiements;

                // Détail par mandat pour ce membre
                var detailParMandat = cotisationsMembre
                    .GroupBy(c => new { c.MandatId, c.Mandat.Annee, c.Mandat.Description })
                    .Select(g => new
                    {
                        mandatId = g.Key.MandatId,
                        annee = g.Key.Annee,
                        description = g.Key.Description,
                        montantCotisation = g.Sum(c => c.Montant),
                        // Pour les paiements, nous devrons faire une logique plus complexe 
                        // si vous voulez les associer à des mandats spécifiques
                        estPaye = false // À adapter selon votre logique métier
                    })
                    .OrderByDescending(d => d.annee)
                    .ToList();

                var result = new
                {
                    success = true,
                    membreId = membreId,
                    resume = new
                    {
                        montantTotalCotisations = montantTotalCotisations,
                        montantTotalPaiements = montantTotalPaiements,
                        solde = soldeMembre,
                        nombreCotisations = cotisationsMembre.Count,
                        nombrePaiements = paiementsMembre.Count,
                        tauxRecouvrement = montantTotalCotisations > 0
                            ? Math.Round((double)montantTotalPaiements / montantTotalCotisations * 100, 2)
                            : 0
                    },
                    cotisations = detailParMandat,
                    historiquePaiements = paiementsMembre
                        .OrderByDescending(p => p.Date)
                        .Select(p => new
                        {
                            id = p.Id,
                            montant = p.Montant,
                            date = p.Date,
                            commentaires = p.Commentaires
                        })
                        .ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la situation des cotisations pour le membre {MembreId}", membreId);
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors de la récupération de la situation des cotisations du membre",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtient la situation des cotisations pour un club spécifique
        /// </summary>
        /// <param name="clubId">ID du club</param>
        /// <returns>Situation des cotisations du club</returns>
        [HttpGet("situation/club/{clubId}")]
        public async Task<ActionResult<object>> GetSituationCotisationsClub(Guid clubId)
        {
            try
            {
                // Vérifier si le club existe
                var clubExists = await _context.Clubs.AnyAsync(c => c.Id == clubId);
                if (!clubExists)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Club non trouvé"
                    });
                }

                // Cotisations du club (via les mandats)
                var cotisationsClub = await _context.Cotisations
                    .Include(c => c.Mandat)
                    .Include(c => c.Membre)
                    .Where(c => c.Mandat.ClubId == clubId)
                    .ToListAsync();

                // Paiements du club
                var paiementsClub = await _context.PaiementCotisations
                    .Where(p => p.ClubId == clubId)
                    .ToListAsync();

                var montantTotalCotisations = cotisationsClub.Sum(c => c.Montant);
                var montantTotalPaiements = paiementsClub.Sum(p => p.Montant);
                var soldeClub = montantTotalCotisations - montantTotalPaiements;

                // Statistiques par mandat
                var statistiquesParMandat = cotisationsClub
                    .GroupBy(c => new { c.MandatId, c.Mandat.Annee, c.Mandat.Description })
                    .Select(g => new
                    {
                        mandatId = g.Key.MandatId,
                        annee = g.Key.Annee,
                        description = g.Key.Description,
                        montantCotisations = g.Sum(c => c.Montant),
                        nombreCotisations = g.Count(),
                        nombreMembres = g.Select(c => c.MembreId).Distinct().Count()
                    })
                    .OrderByDescending(s => s.annee)
                    .ToList();

                var result = new
                {
                    success = true,
                    clubId = clubId,
                    resume = new
                    {
                        montantTotalCotisations = montantTotalCotisations,
                        montantTotalPaiements = montantTotalPaiements,
                        solde = soldeClub,
                        nombreTotalCotisations = cotisationsClub.Count,
                        nombreTotalPaiements = paiementsClub.Count,
                        nombreMembresAvecCotisations = cotisationsClub.Select(c => c.MembreId).Distinct().Count(),
                        tauxRecouvrement = montantTotalCotisations > 0
                            ? Math.Round((double)montantTotalPaiements / montantTotalCotisations * 100, 2)
                            : 0
                    },
                    statistiquesParMandat = statistiquesParMandat
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la situation des cotisations pour le club {ClubId}", clubId);
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors de la récupération de la situation des cotisations du club",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtient la situation de tous les membres d'un club spécifique
        /// </summary>
        /// <param name="clubId">ID du club</param>
        /// <param name="includeInactive">Inclure les membres inactifs (défaut: false)</param>
        /// <returns>Situation de tous les membres du club avec cotisations et paiements</returns>
        [HttpGet("situation/club/{clubId}/membres")]
        public async Task<ActionResult<object>> GetSituationMembresClub(Guid clubId, [FromQuery] bool includeInactive = false)
        {
            try
            {
                // Vérifier si le club existe
                var club = await _context.Clubs.FirstOrDefaultAsync(c => c.Id == clubId);
                if (club == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Club non trouvé"
                    });
                }

                // 1. Récupérer tous les membres du club
                var membresQuery = _context.UserClubs
                    .Include(uc => uc.User)
                    .Where(uc => uc.ClubId == clubId);

                if (!includeInactive)
                {
                    membresQuery = membresQuery.Where(uc => uc.User.IsActive);
                }

                var membresClub = await membresQuery
                    .Select(uc => uc.User)
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .ToListAsync();

                if (!membresClub.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        clubId = clubId,
                        clubName = club.Name,
                        message = "Aucun membre trouvé dans ce club",
                        membres = new List<object>()
                    });
                }

                // 2. Récupérer toutes les cotisations du club (tous mandats confondus)
                var cotisationsClub = await _context.Cotisations
                    .Include(c => c.Mandat)
                    .Where(c => c.Mandat.ClubId == clubId)
                    .ToListAsync();

                // 3. Récupérer tous les paiements du club
                var paiementsClub = await _context.PaiementCotisations
                    .Where(p => p.ClubId == clubId)
                    .ToListAsync();

                // 4. Créer la situation pour chaque membre
                var situationMembres = membresClub.Select(membre =>
                {
                    // Cotisations du membre (tous mandats du club)
                    var cotisationsMembre = cotisationsClub.Where(c => c.MembreId == membre.Id).ToList();

                    // Paiements du membre pour ce club
                    var paiementsMembre = paiementsClub.Where(p => p.MembreId == membre.Id).ToList();

                    var montantTotalCotisations = cotisationsMembre.Sum(c => c.Montant);
                    var montantTotalPaiements = paiementsMembre.Sum(p => p.Montant);
                    var solde = montantTotalCotisations - montantTotalPaiements;

                    return new
                    {
                        membreId = membre.Id,
                        nomComplet = $"{membre.FirstName} {membre.LastName}",
                        montantTotalCotisations = montantTotalCotisations,
                        montantTotalPaiements = montantTotalPaiements,
                        solde = solde,
                        // Informations supplémentaires utiles
                        email = membre.Email,
                        phoneNumber = membre.PhoneNumber,
                        isActive = membre.IsActive,
                        nombreCotisations = cotisationsMembre.Count,
                        nombrePaiements = paiementsMembre.Count,
                        tauxRecouvrement = montantTotalCotisations > 0
                            ? Math.Round((double)montantTotalPaiements / montantTotalCotisations * 100, 2)
                            : 0,
                        // Statut du membre
                        statut = DeterminerStatutMembre(montantTotalCotisations, montantTotalPaiements, solde)
                    };
                }).ToList();

                // 5. Calculer les statistiques globales du club
                var totalCotisationsClub = situationMembres.Sum(m => m.montantTotalCotisations);
                var totalPaiementsClub = situationMembres.Sum(m => m.montantTotalPaiements);
                var soldeGlobalClub = totalCotisationsClub - totalPaiementsClub;

                var statistiquesClub = new
                {
                    nombreMembres = situationMembres.Count,
                    totalCotisations = totalCotisationsClub,
                    totalPaiements = totalPaiementsClub,
                    soldeGlobal = soldeGlobalClub,
                    tauxRecouvrementGlobal = totalCotisationsClub > 0
                        ? Math.Round((double)totalPaiementsClub / totalCotisationsClub * 100, 2)
                        : 0,
                    nombresParStatut = new
                    {
                        aJour = situationMembres.Count(m => m.statut == "À jour"),
                        partiellementPaye = situationMembres.Count(m => m.statut == "Partiellement payé"),
                        enRetard = situationMembres.Count(m => m.statut == "En retard"),
                        aucuneCotisation = situationMembres.Count(m => m.statut == "Aucune cotisation")
                    }
                };

                return Ok(new
                {
                    success = true,
                    clubId = clubId,
                    clubName = club.Name,
                    statistiques = statistiquesClub,
                    membres = situationMembres
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la situation des membres du club {ClubId}", clubId);
                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors de la récupération de la situation des membres du club",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Méthode helper pour déterminer le statut d'un membre
        /// </summary>
        /// <param name="montantCotisations">Montant total des cotisations</param>
        /// <param name="montantPaiements">Montant total des paiements</param>
        /// <param name="solde">Solde (cotisations - paiements)</param>
        /// <returns>Statut du membre</returns>
        private static string DeterminerStatutMembre(int montantCotisations, int montantPaiements, int solde)
        {
            if (montantCotisations == 0)
                return "Aucune cotisation";

            if (solde <= 0)
                return "À jour";

            if (montantPaiements > 0)
                return "Partiellement payé";

            return "En retard";
        }
    }

    // DTOs pour les requêtes
    public class CreateCotisationDto
    {
        [Required(ErrorMessage = "Le montant est requis")]
        [Range(0, int.MaxValue, ErrorMessage = "Le montant doit être positif")]
        public int Montant { get; set; }

        [Required(ErrorMessage = "L'ID du membre est requis")]
        public string MembreId { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'ID du mandat est requis")]
        public Guid MandatId { get; set; }
    }

    public class UpdateCotisationDto
    {
        [Range(0, int.MaxValue, ErrorMessage = "Le montant doit être positif")]
        public int? Montant { get; set; }

        public string? MembreId { get; set; }

        public Guid? MandatId { get; set; }
    }

    public class BulkCreateCotisationDto
    {
        [Required(ErrorMessage = "L'ID du mandat est requis")]
        public Guid MandatId { get; set; }

        [Required(ErrorMessage = "Le montant est requis")]
        [Range(0, int.MaxValue, ErrorMessage = "Le montant doit être positif")]
        public int Montant { get; set; } = 480000; // Montant par défaut
    }

    // DTOs pour les réponses
    public class BulkCreateResult
    {
        public bool Success { get; set; }
        public int CotisationsCrees { get; set; }
        public int MembresIgnores { get; set; }
        public List<MembreIgnore> MembresIgnoresDetails { get; set; } = new();
        public object? MandatInfo { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class MembreIgnore
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}