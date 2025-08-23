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

    public class Evenement
    {
        public Guid Id { get; set; }
        [Required]
        public string Libelle { get; set; } = string.Empty;
        [Required]
        public DateTime Date { get; set; }
        public string? Lieu { get; set; }
        public string? Description { get; set; }
        public bool EstInterne { get; set; } = true;
        // Relation avec le club
        public Guid ClubId { get; set; }
        public virtual Club Club { get; set; } = null!;
        // Relations de navigation
        public virtual ICollection<EvenementDocument> Documents { get; set; } = new HashSet<EvenementDocument>();
        public virtual ICollection<EvenementImage> Images { get; set; } = new HashSet<EvenementImage>();
        public virtual ICollection<EvenementBudget> Budgets { get; set; } = new HashSet<EvenementBudget>();
        public virtual ICollection<EvenementRecette> Recettes { get; set; } = new HashSet<EvenementRecette>();
    }
    public class EvenementDocument
    {
        public Guid Id { get; set; }
        public string? Libelle { get; set; }
        [Required]
        public byte[] Document { get; set; } = Array.Empty<byte>();
        public DateTime DateAjout { get; set; } = DateTime.UtcNow;
        // Relation avec l'événement
        [Required]
        public Guid EvenementId { get; set; }
        public virtual Evenement Evenement { get; set; } = null!;
    }

    public class EvenementImage
    {
        public Guid Id { get; set; }
        [Required]
        public byte[] Image { get; set; } = Array.Empty<byte>();
        public string? Description { get; set; }
        public DateTime DateAjout { get; set; } = DateTime.UtcNow;
        // Relation avec l'événement
        [Required]
        public Guid EvenementId { get; set; }
        public virtual Evenement Evenement { get; set; } = null!;

    }

    public class EvenementBudget
    {
        public Guid Id { get; set; }
        [Required]
        public string Libelle { get; set; } = string.Empty;

        [Required]
        public decimal MontantBudget { get; set; }
        public decimal MontantRealise { get; set; } = 0;
        // Relation avec l'événement
        [Required]
        public Guid EvenementId { get; set; }
        public virtual Evenement Evenement { get; set; } = null!;
    }

    public class EvenementRecette
    {
        public Guid Id { get; set; }
        [Required]
        public string Libelle { get; set; } = string.Empty;
        [Required]
        public decimal Montant { get; set; }
        // Relation avec l'événement
        [Required]
        public Guid EvenementId { get; set; }
        public virtual Evenement Evenement { get; set; } = null!;
    }
}