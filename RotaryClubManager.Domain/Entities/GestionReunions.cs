using RotaryClubManager.Domain.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RotaryClubManager.Domain.Entities.InviteReunion;
namespace RotaryClubManager.Domain.Entities
{
    public class TypeReunion
    {
        public Guid Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Libelle { get; set; } = string.Empty;
        // Navigation property - Une type de réunion peut avoir plusieurs réunions
        public virtual ICollection<Reunion> Reunions { get; set; } = new List<Reunion>();
    }
    public class Reunion
    {
        public Guid Id { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public TimeSpan Heure { get; set; }

        // Propriété calculée pour obtenir la DateTime complète
        [NotMapped] // Pas stockée en base, calculée à la volée
        public DateTime DateTimeComplete
        {
            get => Date.Date.Add(Heure);
            set
            {
                Date = value.Date;
                Heure = value.TimeOfDay;
            }
        }

        // Relation avec le type de réunion
        [Required]
        public Guid TypeReunionId { get; set; }
        public virtual TypeReunion TypeReunion { get; set; } = null!;
        // Relation avec le club
        public Guid ClubId { get; set; }
        public virtual Club Club { get; set; } = null!;

        // Navigation properties
        public virtual ICollection<OrdreDuJour> OrdresDuJour { get; set; } = new List<OrdreDuJour>();
        public virtual ICollection<ListePresence> ListesPresence { get; set; } = new List<ListePresence>();
        public virtual ICollection<InviteReunion> Invites { get; set; } = new List<InviteReunion>();
        public virtual ICollection<ReunionDocument> Documents { get; set; } = new List<ReunionDocument>();
    }
    public class OrdreDuJour
    {
        public Guid Id { get; set; }
        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;
        public string? Rapport { get; set; }
        // Relation avec la réunion
        [Required]
        public Guid ReunionId { get; set; }
        public virtual Reunion Reunion { get; set; } = null!;

    }
    public class ListePresence
    {
        public Guid Id { get; set; }
        // Relation avec le membre
        [Required]
        public string MembreId { get; set; } = string.Empty;
        public virtual ApplicationUser Membre { get; set; } = null!;
        // Relation avec la réunion
        [Required]
        public Guid ReunionId { get; set; }
        public virtual Reunion Reunion { get; set; } = null!;

    }
    public class InviteReunion
    {
        public Guid Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Nom { get; set; } = string.Empty;
        [Required]
        [MaxLength(100)]
        public string Prenom { get; set; } = string.Empty;
        [MaxLength(255)]
        [EmailAddress]
        public string? Email { get; set; }
        [MaxLength(20)]
        public string? Telephone { get; set; }
        [MaxLength(200)]
        public string? Organisation { get; set; }
        // Relation avec la réunion
        [Required]
        public Guid ReunionId { get; set; }
        public virtual Reunion Reunion { get; set; } = null!;
    }

    public class ReunionDocument
    {
        public Guid Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
        // Relation avec la réunion
        [Required]
        public Guid ReunionId { get; set; }
        public virtual Reunion Reunion { get; set; } = null!;
        [Required]
        public byte[] Document { get; set; } = Array.Empty<byte>();
    }
    public class OrdreJourRapport
    {
        public Guid Id { get; set; }

        // Relation avec l'ordre du jour
        [Required]
        public Guid OrdreDuJourId { get; set; }
        public virtual OrdreDuJour OrdreDuJour { get; set; } = null!;

        [Required]
        public string Texte { get; set; } = string.Empty;
        public string? Divers { get; set; } = string.Empty;
    }

}