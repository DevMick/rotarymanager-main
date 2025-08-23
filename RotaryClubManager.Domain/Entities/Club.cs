using RotaryClubManager.Domain.Identity;
using System.ComponentModel.DataAnnotations;

namespace RotaryClubManager.Domain.Entities
{
    using System.ComponentModel.DataAnnotations;

    public class Club
    {
        public Guid Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? DateCreation { get; set; }
        public int? NumeroClub { get; set; }
        public string? NumeroTelephone { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        public string? LieuReunion { get; set; }
        public string? ParrainePar { get; set; }
        public string? JourReunion { get; set; }
        public TimeSpan? HeureReunion { get; set; }
        public string? Frequence { get; set; }
        public string? Adresse { get; set; }

        // Navigation properties
        public virtual ICollection<ApplicationUser> Membres { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<Mandat> Mandats { get; set; } = new List<Mandat>();
        // ... autres collections existantes ...
    }

    public class Cotisation
    {
        public Guid Id { get; set; }

        public int Montant { get; set; }

        // Relation avec le membre
        public string MembreId { get; set; } = string.Empty;
        public virtual ApplicationUser Membre { get; set; } = null!;

        // Relation avec le mandat
        public Guid MandatId { get; set; }
        public virtual Mandat Mandat { get; set; } = null!;
    }

    public class PaiementCotisation
    {
        public Guid Id { get; set; }

        public int Montant { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string? Commentaires { get; set; }

        // Relation avec le membre
        public string MembreId { get; set; } = string.Empty;
        public virtual ApplicationUser Membre { get; set; } = null!;
        // Relation avec le club
        public Guid ClubId { get; set; }
        public virtual Club Club { get; set; } = null!;

    }
}