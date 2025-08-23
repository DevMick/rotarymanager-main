using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/galas")]
    [ApiController]
    [Authorize]
    public class GalaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GalaController> _logger;

        public GalaController(
            ApplicationDbContext context,
            ILogger<GalaController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/galas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GalaDto>>> GetGalas([FromQuery] string? recherche = null)
        {
            try
            {
                var query = _context.Galas.AsQueryable();

                // Filtre par terme de recherche
                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(g => g.Libelle.ToLower().Contains(termeLower) ||
                                           g.Lieu.ToLower().Contains(termeLower));
                }

                var galas = await query
                    .OrderByDescending(g => g.Date)
                    .Select(g => new GalaDto
                    {
                        Id = g.Id,
                        Libelle = g.Libelle,
                        Date = g.Date,
                        Lieu = g.Lieu,
                        NombreTables = g.NombreTables,
                        NombreInvites = g.Invites.Count(),
                        NombreTicketsVendus = g.Tickets.Sum(t => t.Quantite),
                        NombreTombolasVendues = g.Tombolas.Sum(t => t.Quantite)
                    })
                    .ToListAsync();

                return Ok(galas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des galas");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des galas");
            }
        }

        // GET: api/galas/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GalaDetailDto>> GetGala(Guid id)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant du gala est invalide");
                }

                var gala = await _context.Galas
                    .Include(g => g.Invites)
                        .ThenInclude(i => i.TableAffectations)
                            .ThenInclude(ta => ta.GalaTable)
                    .Include(g => g.Tables)
                        .ThenInclude(t => t.TableAffectations)
                    .Include(g => g.Tickets)
                        .ThenInclude(t => t.Membre)
                    .Include(g => g.Tombolas)
                        .ThenInclude(t => t.Membre)
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (gala == null)
                {
                    return NotFound($"Gala avec l'ID {id} introuvable");
                }

                // Construction manuelle du DTO pour éviter les erreurs de projection
                var result = new GalaDetailDto
                {
                    Id = gala.Id,
                    Libelle = gala.Libelle,
                    Date = gala.Date,
                    Lieu = gala.Lieu,
                    NombreTables = gala.NombreTables,
                    NombreSouchesTickets = gala.NombreSouchesTickets,
                    QuantiteParSoucheTickets = gala.QuantiteParSoucheTickets,
                    NombreSouchesTombola = gala.NombreSouchesTombola,
                    QuantiteParSoucheTombola = gala.QuantiteParSoucheTombola,
                    NombreInvites = gala.Invites?.Count ?? 0,
                    NombreTicketsVendus = gala.Tickets?.Sum(t => t.Quantite) ?? 0,
                    NombreTombolasVendues = gala.Tombolas?.Sum(t => t.Quantite) ?? 0
                };

                // Mapping des invités avec gestion des null
                if (gala.Invites != null)
                {
                    result.Invites = gala.Invites.Select(i => new GalaInviteDto
                    {
                        Id = i.Id,
                        Nom_Prenom = i.Nom_Prenom ?? string.Empty,
                        Present = i.Present,
                        TableAffectee = i.TableAffectations?.FirstOrDefault()?.GalaTable?.TableLibelle
                    })
                    .OrderBy(i => i.Nom_Prenom)
                    .ToList();
                }

                // Mapping des tables avec gestion des null
                if (gala.Tables != null)
                {
                    result.Tables = gala.Tables.Select(t => new GalaTableDto
                    {
                        Id = t.Id,
                        TableLibelle = t.TableLibelle ?? string.Empty,
                        NombreInvites = t.TableAffectations?.Count ?? 0
                    })
                    .OrderBy(t => t.TableLibelle)
                    .ToList();
                }

                // Mapping des tickets avec gestion des null
                if (gala.Tickets != null)
                {
                    result.Tickets = gala.Tickets
                        .Where(t => t.Membre != null) // Filtrer les tickets sans membre
                        .Select(t => new GalaTicketDto
                        {
                            Id = t.Id,
                            MembreNom = $"{t.Membre.FirstName ?? ""} {t.Membre.LastName ?? ""}".Trim(),
                            Quantite = t.Quantite
                        })
                        .OrderBy(t => t.MembreNom)
                        .ToList();
                }

                // Mapping des tombolas avec gestion des null
                if (gala.Tombolas != null)
                {
                    result.Tombolas = gala.Tombolas
                        .Where(t => t.Membre != null) // Filtrer les tombolas sans membre
                        .Select(t => new GalaTombolaDto
                        {
                            Id = t.Id,
                            MembreNom = $"{t.Membre.FirstName ?? ""} {t.Membre.LastName ?? ""}".Trim(),
                            Quantite = t.Quantite
                        })
                        .OrderBy(t => t.MembreNom)
                        .ToList();
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du gala {GalaId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération du gala");
            }
        }

        // POST: api/galas
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<ActionResult<GalaDto>> CreateGala([FromBody] CreateGalaRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Parser la date depuis string vers DateTime
                if (!DateTime.TryParse(request.Date, out DateTime parsedDate))
                {
                    return BadRequest("Format de date invalide. Utilisez le format YYYY-MM-DD");
                }

                // Créer une date UTC pour la comparaison
                var dateForComparison = DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc);

                // Vérifier l'unicité du libellé à la même date
                var existingGala = await _context.Galas
                    .AnyAsync(g => g.Libelle.ToLower() == request.Libelle.ToLower() &&
                                  g.Date.Date == dateForComparison.Date);

                if (existingGala)
                {
                    return BadRequest($"Un gala avec le libellé '{request.Libelle}' existe déjà à cette date");
                }

                // Créer la date finale en UTC avec l'heure 19h
                var finalDate = DateTime.SpecifyKind(parsedDate.Date.AddHours(19), DateTimeKind.Utc);

                var gala = new Gala
                {
                    Id = Guid.NewGuid(),
                    Libelle = request.Libelle,
                    Date = finalDate,
                    Lieu = request.Lieu,
                    NombreTables = request.NombreTables,
                    NombreSouchesTickets = request.NombreSouchesTickets,
                    QuantiteParSoucheTickets = request.QuantiteParSoucheTickets,
                    NombreSouchesTombola = request.NombreSouchesTombola,
                    QuantiteParSoucheTombola = request.QuantiteParSoucheTombola
                };

                _context.Galas.Add(gala);

                // Créer automatiquement les tables
                await CreerTablesAutomatiquement(gala.Id, gala.NombreTables);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Gala '{Libelle}' créé avec l'ID {Id} avec {NombreTables} tables",
                    gala.Libelle, gala.Id, request.NombreTables);

                var result = new GalaDto
                {
                    Id = gala.Id,
                    Libelle = gala.Libelle,
                    Date = gala.Date,
                    Lieu = gala.Lieu,
                    NombreTables = gala.NombreTables,
                    NombreInvites = 0,
                    NombreTicketsVendus = 0,
                    NombreTombolasVendues = 0
                };

                return CreatedAtAction(nameof(GetGala), new { id = gala.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du gala");
                return StatusCode(500, "Une erreur est survenue lors de la création du gala");
            }
        }

        // PUT: api/galas/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> UpdateGala(Guid id, [FromBody] UpdateGalaRequest request)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant du gala est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var gala = await _context.Galas.FindAsync(id);
                if (gala == null)
                {
                    return NotFound($"Gala avec l'ID {id} introuvable");
                }

                // Vérifier l'unicité du libellé si modifié
                if (!string.IsNullOrEmpty(request.Libelle))
                {
                    DateTime dateToCheck = gala.Date;
                    if (!string.IsNullOrEmpty(request.Date))
                    {
                        var parsedUpdateDateUtc = ParseToUtc(request.Date);
                        if (parsedUpdateDateUtc.HasValue)
                        {
                            dateToCheck = parsedUpdateDateUtc.Value;
                        }
                        else
                        {
                            return BadRequest("Format de date invalide. Utilisez le format YYYY-MM-DD");
                        }
                    }

                    var libelleToCheck = request.Libelle ?? gala.Libelle;

                    // Vérifier si c'est une modification qui nécessite une vérification d'unicité
                    if (request.Libelle.ToLower() != gala.Libelle.ToLower() ||
                        (!string.IsNullOrEmpty(request.Date) && dateToCheck.Date != gala.Date.Date))
                    {
                        var existingGala = await _context.Galas
                            .AnyAsync(g => g.Id != id &&
                                          g.Libelle.ToLower() == libelleToCheck.ToLower() &&
                                          g.Date.Date == dateToCheck.Date);

                        if (existingGala)
                        {
                            return BadRequest($"Un gala avec le libellé '{libelleToCheck}' existe déjà à cette date");
                        }
                    }
                }

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.Libelle))
                    gala.Libelle = request.Libelle;

                if (!string.IsNullOrEmpty(request.Date))
                {
                    var parsedUpdateDateUtc = ParseToUtc(request.Date);
                    if (parsedUpdateDateUtc.HasValue)
                    {
                        gala.Date = parsedUpdateDateUtc.Value;
                    }
                    else
                    {
                        return BadRequest("Format de date invalide. Utilisez le format YYYY-MM-DD");
                    }
                }

                if (!string.IsNullOrEmpty(request.Lieu))
                    gala.Lieu = request.Lieu;
                if (request.NombreTables.HasValue)
                    gala.NombreTables = request.NombreTables.Value;
                if (request.NombreSouchesTickets.HasValue)
                    gala.NombreSouchesTickets = request.NombreSouchesTickets.Value;
                if (request.QuantiteParSoucheTickets.HasValue)
                    gala.QuantiteParSoucheTickets = request.QuantiteParSoucheTickets.Value;
                if (request.NombreSouchesTombola.HasValue)
                    gala.NombreSouchesTombola = request.NombreSouchesTombola.Value;
                if (request.QuantiteParSoucheTombola.HasValue)
                    gala.QuantiteParSoucheTombola = request.QuantiteParSoucheTombola.Value;

                _context.Entry(gala).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Gala {Id} mis à jour avec succès", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du gala {GalaId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour du gala");
            }
        }

        // DELETE: api/galas/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> DeleteGala(Guid id)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant du gala est invalide");
                }

                var gala = await _context.Galas
                    .Include(g => g.Invites)
                    .Include(g => g.Tables)
                    .Include(g => g.Tickets)
                    .Include(g => g.Tombolas)
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (gala == null)
                {
                    return NotFound($"Gala avec l'ID {id} introuvable");
                }

                // Vérifier si le gala peut être supprimé
                var totalElements = gala.Invites.Count + gala.Tickets.Sum(t => t.Quantite) + gala.Tombolas.Sum(t => t.Quantite);
                if (totalElements > 0)
                {
                    return BadRequest($"Impossible de supprimer le gala '{gala.Libelle}' car il contient des données " +
                                    $"({gala.Invites.Count} invité(s), {gala.Tickets.Sum(t => t.Quantite)} ticket(s), " +
                                    $"{gala.Tombolas.Sum(t => t.Quantite)} tombola(s)). " +
                                    "Veuillez d'abord supprimer toutes les données associées.");
                }

                _context.Galas.Remove(gala);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Gala '{Libelle}' supprimé avec l'ID {Id}", gala.Libelle, id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du gala {GalaId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la suppression du gala");
            }
        }

        // GET: api/galas/statistiques
        [HttpGet("statistiques")]
        public async Task<ActionResult<GalaStatistiquesDto>> GetStatistiques()
        {
            try
            {
                var statistiques = await _context.Galas
                    .Select(g => new
                    {
                        g.Id,
                        g.Libelle,
                        g.Date,
                        g.Lieu,
                        NombreInvites = g.Invites.Count(),
                        NombreTicketsVendus = g.Tickets.Sum(t => t.Quantite),
                        NombreTombolasVendues = g.Tombolas.Sum(t => t.Quantite),
                        NombreTables = g.NombreTables
                    })
                    .ToListAsync();

                var result = new GalaStatistiquesDto
                {
                    NombreGalas = statistiques.Count,
                    NombreTotalInvites = statistiques.Sum(s => s.NombreInvites),
                    NombreTotalTicketsVendus = statistiques.Sum(s => s.NombreTicketsVendus),
                    NombreTotalTombolasVendues = statistiques.Sum(s => s.NombreTombolasVendues),
                    GalasDetails = statistiques.Select(s => new GalaStatistiqueDetailDto
                    {
                        Id = s.Id,
                        Libelle = s.Libelle,
                        Date = s.Date,
                        Lieu = s.Lieu,
                        NombreInvites = s.NombreInvites,
                        NombreTicketsVendus = s.NombreTicketsVendus,
                        NombreTombolasVendues = s.NombreTombolasVendues,
                        NombreTables = s.NombreTables
                    }).OrderByDescending(s => s.Date).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques des galas");
                return StatusCode(500, "Une erreur est survenue lors de la récupération des statistiques");
            }
        }

        // ===== MÉTHODES UTILITAIRES PRIVÉES =====

        /// <summary>
        /// Crée automatiquement les tables pour un gala
        /// </summary>
        /// <param name="galaId">ID du gala</param>
        /// <param name="nombreTables">Nombre de tables à créer</param>
        /// <param name="prefixe">Préfixe pour les libellés des tables (optionnel, défaut: "Table")</param>
        /// <returns>Liste des tables créées</returns>
        private async Task<List<GalaTable>> CreerTablesAutomatiquement(Guid galaId, int nombreTables, string prefixe = "Table")
        {
            var tablesCreees = new List<GalaTable>();

            for (int i = 1; i <= nombreTables; i++)
            {
                var table = new GalaTable
                {
                    Id = Guid.NewGuid(),
                    GalaId = galaId,
                    TableLibelle = $"{prefixe} {i}" // Format simple (Table 1, Table 2, etc.)
                };

                _context.GalaTables.Add(table);
                tablesCreees.Add(table);
            }

            _logger.LogInformation("Création automatique de {NombreTables} tables pour le gala {GalaId} avec le préfixe '{Prefixe}'",
                nombreTables, galaId, prefixe);

            return tablesCreees;
        }

        /// <summary>
        /// Convertit une date string en DateTime UTC avec heure par défaut
        /// </summary>
        /// <param name="dateString">Date au format YYYY-MM-DD</param>
        /// <param name="defaultHour">Heure par défaut (19h)</param>
        /// <returns>DateTime UTC ou null si parsing échoue</returns>
        private static DateTime? ParseToUtc(string dateString, int defaultHour = 19)
        {
            if (DateTime.TryParse(dateString, out DateTime parsedDate))
            {
                return DateTime.SpecifyKind(parsedDate.Date.AddHours(defaultHour), DateTimeKind.Utc);
            }
            return null;
        }
    }

    // DTOs pour les galas
    public class GalaDto
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Lieu { get; set; } = string.Empty;
        public int NombreTables { get; set; }
        public int NombreInvites { get; set; }
        public int NombreTicketsVendus { get; set; }
        public int NombreTombolasVendues { get; set; }
    }

    public class GalaDetailDto : GalaDto
    {
        public int NombreSouchesTickets { get; set; }
        public int QuantiteParSoucheTickets { get; set; }
        public int NombreSouchesTombola { get; set; }
        public int QuantiteParSoucheTombola { get; set; }
        public List<GalaInviteDto> Invites { get; set; } = new List<GalaInviteDto>();
        public List<GalaTableDto> Tables { get; set; } = new List<GalaTableDto>();
        public List<GalaTicketDto> Tickets { get; set; } = new List<GalaTicketDto>();
        public List<GalaTombolaDto> Tombolas { get; set; } = new List<GalaTombolaDto>();
    }

    public class GalaInviteDto
    {
        public Guid Id { get; set; }
        public string Nom_Prenom { get; set; } = string.Empty;
        public bool? Present { get; set; }
        public string? TableAffectee { get; set; }
    }

    public class GalaTableDto
    {
        public Guid Id { get; set; }
        public string TableLibelle { get; set; } = string.Empty;
        public int NombreInvites { get; set; }
    }

    public class GalaTicketDto
    {
        public Guid Id { get; set; }
        public string MembreNom { get; set; } = string.Empty;
        public int Quantite { get; set; }
    }

    public class GalaTombolaDto
    {
        public Guid Id { get; set; }
        public string MembreNom { get; set; } = string.Empty;
        public int Quantite { get; set; }
    }

    public class CreateGalaRequest
    {
        [Required(ErrorMessage = "Le libellé est obligatoire")]
        [MaxLength(200, ErrorMessage = "Le libellé ne peut pas dépasser 200 caractères")]
        public string Libelle { get; set; } = string.Empty;

        [Required(ErrorMessage = "La date est obligatoire")]
        public string Date { get; set; } = string.Empty; // Accepter comme string et parser manuellement

        [Required(ErrorMessage = "Le lieu est obligatoire")]
        [MaxLength(300, ErrorMessage = "Le lieu ne peut pas dépasser 300 caractères")]
        public string Lieu { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nombre de tables est obligatoire")]
        [Range(1, 100, ErrorMessage = "Le nombre de tables doit être entre 1 et 100")]
        public int NombreTables { get; set; }

        [Required(ErrorMessage = "Le nombre de souches de tickets est obligatoire")]
        [Range(1, int.MaxValue, ErrorMessage = "Le nombre de souches de tickets doit être supérieur à 0")]
        public int NombreSouchesTickets { get; set; }

        [Required(ErrorMessage = "La quantité par souche de tickets est obligatoire")]
        [Range(1, int.MaxValue, ErrorMessage = "La quantité par souche de tickets doit être supérieure à 0")]
        public int QuantiteParSoucheTickets { get; set; }

        [Required(ErrorMessage = "Le nombre de souches de tombola est obligatoire")]
        [Range(1, int.MaxValue, ErrorMessage = "Le nombre de souches de tombola doit être supérieur à 0")]
        public int NombreSouchesTombola { get; set; }

        [Required(ErrorMessage = "La quantité par souche de tombola est obligatoire")]
        [Range(1, int.MaxValue, ErrorMessage = "La quantité par souche de tombola doit être supérieure à 0")]
        public int QuantiteParSoucheTombola { get; set; }
    }

    public class UpdateGalaRequest
    {
        [MaxLength(200, ErrorMessage = "Le libellé ne peut pas dépasser 200 caractères")]
        public string? Libelle { get; set; }

        public string? Date { get; set; } // String pour compatibilité

        [MaxLength(300, ErrorMessage = "Le lieu ne peut pas dépasser 300 caractères")]
        public string? Lieu { get; set; }

        [Range(1, 100, ErrorMessage = "Le nombre de tables doit être entre 1 et 100")]
        public int? NombreTables { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Le nombre de souches de tickets doit être supérieur à 0")]
        public int? NombreSouchesTickets { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La quantité par souche de tickets doit être supérieure à 0")]
        public int? QuantiteParSoucheTickets { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Le nombre de souches de tombola doit être supérieur à 0")]
        public int? NombreSouchesTombola { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La quantité par souche de tombola doit être supérieure à 0")]
        public int? QuantiteParSoucheTombola { get; set; }
    }

    public class GalaStatistiquesDto
    {
        public int NombreGalas { get; set; }
        public int NombreTotalInvites { get; set; }
        public int NombreTotalTicketsVendus { get; set; }
        public int NombreTotalTombolasVendues { get; set; }
        public List<GalaStatistiqueDetailDto> GalasDetails { get; set; } = new List<GalaStatistiqueDetailDto>();
    }

    public class GalaStatistiqueDetailDto
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Lieu { get; set; } = string.Empty;
        public int NombreInvites { get; set; }
        public int NombreTicketsVendus { get; set; }
        public int NombreTombolasVendues { get; set; }
        public int NombreTables { get; set; }
    }
}