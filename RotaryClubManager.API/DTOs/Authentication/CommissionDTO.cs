using System.ComponentModel.DataAnnotations;

namespace RotaryClubManager.API.DTOs
{
    // DTOs pour les commissions
    public class AffecterCommissionDto
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

        // Statistiques des membres (au lieu de la liste complète)
        public int NombreMembresActifs { get; set; }
        public int NombreResponsables { get; set; }
    }

    public class CommissionDto
    {
        public Guid Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class ClubDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class CommissionStatistiquesDto
    {
        public int TotalCommissions { get; set; }
        public int CommissionsActives { get; set; }
        public int CommissionsInactives { get; set; }
        public int TotalMembresActifs { get; set; }
        public int TotalResponsables { get; set; }
    }

    // DTOs pour les membres de commission
    public class AffecterMembreCommissionDto
    {
        [Required]
        [StringLength(450)]
        public string MembreId { get; set; } = string.Empty;

        [Required]
        public Guid MandatId { get; set; }

        public bool EstResponsable { get; set; } = false;

        public DateTime? DateNomination { get; set; }

        [MaxLength(500)]
        public string? Commentaires { get; set; }
    }

    public class UpdateMembreCommissionDto
    {
        public bool? EstResponsable { get; set; }

        [MaxLength(500)]
        public string? Commentaires { get; set; }

        public DateTime? DateDemission { get; set; }
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
        public string? Description { get; set; }
        public string PeriodeComplete { get; set; } = string.Empty;
    }
}