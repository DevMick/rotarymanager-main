using System.ComponentModel.DataAnnotations;

namespace RotaryClubManager.Domain.Entities
{
    // Modèle Commission - Template générique des commissions Rotary
    public class Commission
    {
        public Guid Id { get; set; }

        [Required]
        public string Nom { get; set; } = string.Empty; // Action, Administration, Effectif, etc.

        public string Description { get; set; } = string.Empty;

        [Required]
        public string RoleEtResponsabilite { get; set; } = string.Empty;

        // Relation avec le club
        public Guid ClubId { get; set; }
        public virtual Club Club { get; set; } = null!;
    }
}