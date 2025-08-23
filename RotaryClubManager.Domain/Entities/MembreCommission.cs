using RotaryClubManager.Domain.Identity;
using System.ComponentModel.DataAnnotations;

namespace RotaryClubManager.Domain.Entities
{
    public class MembreCommission
    {
        public Guid Id { get; set; }

        public bool EstResponsable { get; set; } = false;

        public DateTime DateNomination { get; set; } = DateTime.UtcNow;

        public DateTime? DateDemission { get; set; }

        public bool EstActif { get; set; } = true;

        [MaxLength(500)]
        public string? Commentaires { get; set; }

        // Relation avec la commission (directe)
        public Guid CommissionId { get; set; }
        public virtual Commission Commission { get; set; } = null!;

        // Relation avec le membre - UNE SEULE PROPRIÉTÉ
        public string MembreId { get; set; } = string.Empty;
        public virtual ApplicationUser Membre { get; set; } = null!;

        // Relation avec le mandat
        public Guid MandatId { get; set; }
        public virtual Mandat Mandat { get; set; } = null!;
    }
}