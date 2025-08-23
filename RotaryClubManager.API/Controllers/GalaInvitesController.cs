using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace RotaryClubManager.API.Controllers
{
    [Route("api/gala-invites")]
    [ApiController]
    [Authorize] 
    public class GalaInvitesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GalaInvitesController> _logger;

        public GalaInvitesController(
            ApplicationDbContext context,
            ILogger<GalaInvitesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/gala-invites/gala/{galaId}
        [HttpGet("gala/{galaId:guid}")]
        public async Task<ActionResult<IEnumerable<GalaInvitesDto>>> GetInvitesByGala(
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

                var query = _context.GalaInvites
                    .Include(i => i.TableAffectations)
                        .ThenInclude(ta => ta.GalaTable)
                    .Where(i => i.GalaId == galaId);

                // Filtre par terme de recherche
                if (!string.IsNullOrEmpty(recherche))
                {
                    var termeLower = recherche.ToLower();
                    query = query.Where(i => i.Nom_Prenom.ToLower().Contains(termeLower));
                }

                var invites = await query
                    .OrderBy(i => i.Nom_Prenom)
                    .Select(i => new GalaInvitesDto
                    {
                        Id = i.Id,
                        GalaId = i.GalaId,
                        Nom_Prenom = i.Nom_Prenom,
                        Present = i.Present,
                        TableAffectee = i.TableAffectations.FirstOrDefault() != null
                            ? i.TableAffectations.First().GalaTable.TableLibelle
                            : null,
                        TableId = i.TableAffectations.FirstOrDefault() != null
                            ? i.TableAffectations.First().GalaTable.Id
                            : null
                    })
                    .ToListAsync();

                return Ok(invites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des invités du gala {GalaId}", galaId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des invités");
            }
        }

        // GET: api/gala-invites/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<GalaInvitesDetailDto>> GetInvite(Guid id)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'invité est invalide");
                }

                var invite = await _context.GalaInvites
                    .Include(i => i.Gala)
                    .Include(i => i.TableAffectations)
                        .ThenInclude(ta => ta.GalaTable)
                    .Where(i => i.Id == id)
                    .Select(i => new GalaInvitesDetailDto
                    {
                        Id = i.Id,
                        GalaId = i.GalaId,
                        GalaLibelle = i.Gala.Libelle,
                        Nom_Prenom = i.Nom_Prenom,
                        TableAffectee = i.TableAffectations.FirstOrDefault() != null
                            ? i.TableAffectations.First().GalaTable.TableLibelle
                            : null,
                        TableId = i.TableAffectations.FirstOrDefault() != null
                            ? i.TableAffectations.First().GalaTable.Id
                            : null
                    })
                    .FirstOrDefaultAsync();

                if (invite == null)
                {
                    return NotFound($"Invité avec l'ID {id} introuvable");
                }

                return Ok(invite);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'invité {InviteId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la récupération de l'invité");
            }
        }

        // POST: api/gala-invites
        [HttpPost]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<ActionResult<GalaInvitesDto>> CreateInvite([FromBody] CreateGalaInvitesRequest request)
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

                // Vérifier l'unicité du nom dans le gala
                var existingInvite = await _context.GalaInvites
                    .AnyAsync(i => i.GalaId == request.GalaId &&
                                  i.Nom_Prenom.ToLower() == request.Nom_Prenom.ToLower());

                if (existingInvite)
                {
                    return BadRequest($"Un invité avec le nom '{request.Nom_Prenom}' existe déjà pour ce gala");
                }

                var invite = new GalaInvites
                {
                    Id = Guid.NewGuid(),
                    GalaId = request.GalaId,
                    Nom_Prenom = request.Nom_Prenom,
                    Present = request.Present
                };

                _context.GalaInvites.Add(invite);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Invité '{Nom_Prenom}' créé avec l'ID {Id} pour le gala {GalaId}",
                    invite.Nom_Prenom, invite.Id, invite.GalaId);

                var result = new GalaInvitesDto
                {
                    Id = invite.Id,
                    GalaId = invite.GalaId,
                    Nom_Prenom = invite.Nom_Prenom,
                    Present = invite.Present,
                    TableAffectee = null,
                    TableId = null
                };

                return CreatedAtAction(nameof(GetInvite), new { id = invite.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'invité");
                return StatusCode(500, "Une erreur est survenue lors de la création de l'invité");
            }
        }

        // POST: api/gala-invites/import-excel-file
        [HttpPost("import-excel-file")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<ActionResult<ImportBulkResultDto>> ImportFromExcelFile([FromForm] ImportExcelFileRequest request)
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

                // Vérifier le fichier
                if (request.FichierExcel == null || request.FichierExcel.Length == 0)
                {
                    return BadRequest("Aucun fichier Excel fourni");
                }

                // Vérifier l'extension du fichier
                var extension = Path.GetExtension(request.FichierExcel.FileName).ToLowerInvariant();
                if (extension != ".xlsx" && extension != ".xls")
                {
                    return BadRequest("Seuls les fichiers Excel (.xlsx, .xls) sont acceptés");
                }

                // Traiter le fichier Excel
                var result = await TraiterFichierExcelAvecOpenXml(request.FichierExcel, request.GalaId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'import Excel pour le gala {GalaId}", request.GalaId);
                return StatusCode(500, new ImportBulkResultDto
                {
                    EstSucces = false,
                    Erreurs = new List<string> { $"Erreur générale: {ex.Message}" }
                });
            }
        }

        /// <summary>
        /// Traite le fichier Excel avec DocumentFormat.OpenXml
        /// </summary>
        private async Task<ImportBulkResultDto> TraiterFichierExcelAvecOpenXml(IFormFile fichierExcel, Guid galaId)
        {
            var result = new ImportBulkResultDto();
            var invitesACreer = new List<GalaInvites>();
            var erreursLignes = new List<string>();

            try
            {
                using var stream = new MemoryStream();
                await fichierExcel.CopyToAsync(stream);
                stream.Position = 0;

                _logger.LogInformation("Début du traitement du fichier Excel pour le gala {GalaId}", galaId);

                using var document = SpreadsheetDocument.Open(stream, false);
                var workbookPart = document.WorkbookPart;
                var worksheetPart = workbookPart?.WorksheetParts.FirstOrDefault();

                if (worksheetPart == null)
                {
                    throw new ArgumentException("Le fichier Excel ne contient aucune feuille de calcul");
                }

                var worksheet = worksheetPart.Worksheet;
                var sheetData = worksheet.GetFirstChild<SheetData>();

                if (sheetData == null)
                {
                    throw new ArgumentException("La feuille de calcul est vide");
                }

                // Récupérer les invités existants pour vérifier les doublons
                var invitesExistants = await _context.GalaInvites
                    .Where(i => i.GalaId == galaId)
                    .Select(i => i.Nom_Prenom.ToLower())
                    .ToListAsync();

                var rowIndex = 0;

                // Lire toutes les lignes
                foreach (Row row in sheetData.Elements<Row>())
                {
                    rowIndex++;

                    try
                    {
                        // Récupérer la première cellule (colonne A)
                        var firstCell = row.Elements<Cell>().FirstOrDefault(c => GetColumnName(c.CellReference?.Value) == "A");

                        if (firstCell == null)
                        {
                            continue; // Pas de données dans la colonne A
                        }

                        var cellValue = GetCellValue(firstCell, workbookPart);

                        if (string.IsNullOrEmpty(cellValue))
                        {
                            continue; // Ignorer les cellules vides
                        }

                        var nomPrenom = cellValue.Trim();

                        _logger.LogDebug("Traitement ligne {Row}: '{Nom}' (longueur: {Length})",
                            rowIndex, nomPrenom, nomPrenom.Length);

                        // Vérifier la longueur
                        if (nomPrenom.Length > 200)
                        {
                            erreursLignes.Add($"Ligne {rowIndex}: Le nom '{nomPrenom}' dépasse 200 caractères");
                            result.NombreErreurs++;
                            continue;
                        }

                        // Vérifier si l'invité existe déjà
                        if (invitesExistants.Contains(nomPrenom.ToLower()) ||
                            invitesACreer.Any(i => i.Nom_Prenom.ToLower() == nomPrenom.ToLower()))
                        {
                            erreursLignes.Add($"Ligne {rowIndex}: L'invité '{nomPrenom}' existe déjà ou est en doublon dans le fichier");
                            result.NombreDoublons++;
                            continue;
                        }

                        // Ajouter à la liste des invités à créer
                        var invite = new GalaInvites
                        {
                            Id = Guid.NewGuid(),
                            GalaId = galaId,
                            Nom_Prenom = nomPrenom
                        };

                        invitesACreer.Add(invite);
                        result.NombreLignesTraitees++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors du traitement de la ligne {Row}", rowIndex);
                        erreursLignes.Add($"Ligne {rowIndex}: Erreur lors du traitement - {ex.Message}");
                        result.NombreErreurs++;
                    }
                }

                // Sauvegarder en base si il y a des invités à créer
                if (invitesACreer.Any())
                {
                    try
                    {
                        _logger.LogInformation("Tentative d'insertion de {Count} invités en base", invitesACreer.Count);

                        await _context.GalaInvites.AddRangeAsync(invitesACreer);
                        var saveResult = await _context.SaveChangesAsync();

                        result.NombreInvitesCrees = saveResult;

                        _logger.LogInformation("Insertion réussie: {Count} invités créés", saveResult);
                    }
                    catch (DbUpdateException dbEx)
                    {
                        _logger.LogError(dbEx, "Erreur de base de données lors de l'insertion");

                        var errorDetails = new List<string>();
                        var innerEx = dbEx.InnerException;
                        while (innerEx != null)
                        {
                            errorDetails.Add(innerEx.Message);
                            innerEx = innerEx.InnerException;
                        }

                        var detailedError = string.Join(" | ", errorDetails);
                        erreursLignes.Add($"Erreur de base de données: {detailedError}");
                        result.NombreErreurs++;

                        // Nettoyer le contexte
                        foreach (var entry in _context.ChangeTracker.Entries())
                        {
                            entry.State = EntityState.Detached;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur générale lors de la sauvegarde");
                        erreursLignes.Add($"Erreur lors de la sauvegarde: {ex.Message}");
                        result.NombreErreurs++;
                    }
                }

                result.Erreurs = erreursLignes;
                result.EstSucces = result.NombreInvitesCrees > 0;

                _logger.LogInformation("Import Excel terminé pour le gala {GalaId}: {NombreInvitesCrees} invités créés, {NombreErreurs} erreurs, {NombreDoublons} doublons",
                    galaId, result.NombreInvitesCrees, result.NombreErreurs, result.NombreDoublons);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement du fichier Excel pour le gala {GalaId}", galaId);
                result.EstSucces = false;
                result.Erreurs.Add($"Erreur générale: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Obtient la valeur d'une cellule Excel
        /// </summary>
        private string GetCellValue(Cell cell, WorkbookPart workbookPart)
        {
            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                var sharedStringTablePart = workbookPart.SharedStringTablePart;
                if (sharedStringTablePart != null)
                {
                    var sharedStringTable = sharedStringTablePart.SharedStringTable;
                    if (int.TryParse(cell.InnerText, out int index))
                    {
                        return sharedStringTable.Elements<SharedStringItem>().ElementAt(index).InnerText;
                    }
                }
            }

            return cell.InnerText ?? string.Empty;
        }

        /// <summary>
        /// Extrait le nom de la colonne à partir d'une référence de cellule
        /// </summary>
        private string GetColumnName(string? cellReference)
        {
            if (string.IsNullOrEmpty(cellReference))
                return string.Empty;

            var columnName = string.Empty;
            foreach (char c in cellReference)
            {
                if (char.IsLetter(c))
                    columnName += c;
                else
                    break;
            }
            return columnName;
        }

        // Les autres méthodes restent identiques...
        // PUT, DELETE, etc...

        // PUT: api/gala-invites/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> UpdateInvite(Guid id, [FromBody] UpdateGalaInvitesRequest request)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'invité est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var invite = await _context.GalaInvites.FindAsync(id);
                if (invite == null)
                {
                    return NotFound($"Invité avec l'ID {id} introuvable");
                }

                // Vérifier l'unicité du nom si modifié
                if (!string.IsNullOrEmpty(request.Nom_Prenom) &&
                    request.Nom_Prenom.ToLower() != invite.Nom_Prenom.ToLower())
                {
                    var existingInvite = await _context.GalaInvites
                        .AnyAsync(i => i.Id != id &&
                                      i.GalaId == invite.GalaId &&
                                      i.Nom_Prenom.ToLower() == request.Nom_Prenom.ToLower());

                    if (existingInvite)
                    {
                        return BadRequest($"Un invité avec le nom '{request.Nom_Prenom}' existe déjà pour ce gala");
                    }
                }

                // Mettre à jour les propriétés
                if (!string.IsNullOrEmpty(request.Nom_Prenom))
                    invite.Nom_Prenom = request.Nom_Prenom;

                if (request.Present.HasValue)
                    invite.Present = request.Present;

                _context.Entry(invite).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Invité {Id} mis à jour avec succès", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'invité {InviteId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la mise à jour de l'invité");
            }
        }

        // DELETE: api/gala-invites/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> DeleteInvite(Guid id)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'invité est invalide");
                }

                var invite = await _context.GalaInvites
                    .Include(i => i.TableAffectations)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invite == null)
                {
                    return NotFound($"Invité avec l'ID {id} introuvable");
                }

                // Supprimer d'abord les affectations de table (cascade sera géré par EF)
                _context.GalaInvites.Remove(invite);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Invité '{Nom_Prenom}' supprimé avec l'ID {Id}", invite.Nom_Prenom, id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'invité {InviteId}", id);
                return StatusCode(500, "Une erreur est survenue lors de la suppression de l'invité");
            }
        }

        // POST: api/gala-invites/{id}/affecter-table
        [HttpPost("{id:guid}/affecter-table")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> AffecterTable(Guid id, [FromBody] AffecterTableRequest request)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'invité est invalide");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var invite = await _context.GalaInvites
                    .Include(i => i.TableAffectations)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invite == null)
                {
                    return NotFound($"Invité avec l'ID {id} introuvable");
                }

                // Vérifier que la table existe et appartient au même gala
                var table = await _context.GalaTables
                    .FirstOrDefaultAsync(t => t.Id == request.TableId && t.GalaId == invite.GalaId);

                if (table == null)
                {
                    return BadRequest($"Table avec l'ID {request.TableId} introuvable ou n'appartient pas au même gala");
                }

                // Supprimer l'affectation précédente s'il y en a une
                var existingAffectation = invite.TableAffectations.FirstOrDefault();
                if (existingAffectation != null)
                {
                    _context.GalaTableAffectations.Remove(existingAffectation);
                }

                // Créer la nouvelle affectation
                var nouvelleAffectation = new GalaTableAffectation
                {
                    Id = Guid.NewGuid(),
                    GalaTableId = request.TableId,
                    GalaInvitesId = id
                };

                _context.GalaTableAffectations.Add(nouvelleAffectation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Invité {InviteId} affecté à la table {TableId}", id, request.TableId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'affectation de l'invité {InviteId} à la table", id);
                return StatusCode(500, "Une erreur est survenue lors de l'affectation à la table");
            }
        }

        // DELETE: api/gala-invites/{id}/retirer-table
        [HttpDelete("{id:guid}/retirer-table")]
        [Authorize(Roles = "Admin,President,Secretary")]
        public async Task<IActionResult> RetirerTable(Guid id)
        {
            try
            {
                // Validation des paramètres
                if (id == Guid.Empty)
                {
                    return BadRequest("L'identifiant de l'invité est invalide");
                }

                var invite = await _context.GalaInvites
                    .Include(i => i.TableAffectations)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invite == null)
                {
                    return NotFound($"Invité avec l'ID {id} introuvable");
                }

                var affectation = invite.TableAffectations.FirstOrDefault();
                if (affectation == null)
                {
                    return BadRequest("Cet invité n'est affecté à aucune table");
                }

                _context.GalaTableAffectations.Remove(affectation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Invité {InviteId} retiré de sa table", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du retrait de l'invité {InviteId} de sa table", id);
                return StatusCode(500, "Une erreur est survenue lors du retrait de la table");
            }
        }

        // GET: api/gala-invites/gala/{galaId}/sans-table
        [HttpGet("gala/{galaId:guid}/sans-table")]
        public async Task<ActionResult<IEnumerable<GalaInvitesDto>>> GetInvitesSansTable(Guid galaId)
        {
            try
            {
                // Vérifier que le gala existe
                var galaExists = await _context.Galas.AnyAsync(g => g.Id == galaId);
                if (!galaExists)
                {
                    return NotFound($"Gala avec l'ID {galaId} introuvable");
                }

                var invitesSansTable = await _context.GalaInvites
                    .Where(i => i.GalaId == galaId && !i.TableAffectations.Any())
                    .OrderBy(i => i.Nom_Prenom)
                    .Select(i => new GalaInvitesDto
                    {
                        Id = i.Id,
                        GalaId = i.GalaId,
                        Nom_Prenom = i.Nom_Prenom,
                        Present = i.Present,
                        TableAffectee = null,
                        TableId = null
                    })
                    .ToListAsync();

                return Ok(invitesSansTable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des invités sans table du gala {GalaId}", galaId);
                return StatusCode(500, "Une erreur est survenue lors de la récupération des invités sans table");
            }
        }
    }

    // DTOs
    public class ImportExcelFileRequest
    {
        [Required(ErrorMessage = "L'ID du gala est obligatoire")]
        public Guid GalaId { get; set; }

        [Required(ErrorMessage = "Le fichier Excel est obligatoire")]
        public IFormFile FichierExcel { get; set; } = null!;
    }

    public class GalaInvitesDto
    {
        public Guid Id { get; set; }
        public Guid GalaId { get; set; }
        public string Nom_Prenom { get; set; } = string.Empty;
        public bool? Present { get; set; }
        public string? TableAffectee { get; set; }
        public Guid? TableId { get; set; }
    }

    public class GalaInvitesDetailDto : GalaInvitesDto
    {
        public string GalaLibelle { get; set; } = string.Empty;
    }

    public class CreateGalaInvitesRequest
    {
        [Required(ErrorMessage = "L'ID du gala est obligatoire")]
        public Guid GalaId { get; set; }

        [Required(ErrorMessage = "Le nom et prénom sont obligatoires")]
        [MaxLength(200, ErrorMessage = "Le nom et prénom ne peuvent pas dépasser 200 caractères")]
        public string Nom_Prenom { get; set; } = string.Empty;

        public bool? Present { get; set; } = false;
    }

    public class UpdateGalaInvitesRequest
    {
        [MaxLength(200, ErrorMessage = "Le nom et prénom ne peuvent pas dépasser 200 caractères")]
        public string? Nom_Prenom { get; set; }

        public bool? Present { get; set; }
    }

    public class AffecterTableRequest
    {
        [Required(ErrorMessage = "L'ID de la table est obligatoire")]
        public Guid TableId { get; set; }
    }

    public class ImportBulkResultDto
    {
        public bool EstSucces { get; set; }
        public int NombreLignesTraitees { get; set; }
        public int NombreInvitesCrees { get; set; }
        public int NombreErreurs { get; set; }
        public int NombreDoublons { get; set; }
        public List<string> Erreurs { get; set; } = new List<string>();

        public string Resume =>
            $"Import terminé: {NombreInvitesCrees} invité(s) créé(s), {NombreErreurs} erreur(s), {NombreDoublons} doublon(s) sur {NombreLignesTraitees} ligne(s) traitée(s)";
    }
}