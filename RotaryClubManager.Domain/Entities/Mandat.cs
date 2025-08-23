using System.ComponentModel.DataAnnotations;

namespace RotaryClubManager.Domain.Entities
{
    // Modèle Mandat (pour référence)
    public class Mandat
    {
        public Guid Id { get; set; }
        [Required]
        public int Annee { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        [MaxLength(200)]
        public string? Description { get; set; }

        [Required]
        public int MontantCotisation { get; set; }

        public bool EstActuel { get; set; } = false;

        // Champ calculé combinant Annee, DateDebut et DateFin
        public string PeriodeComplete => $"{Annee} ({DateDebut.ToShortDateString()} - {DateFin.ToShortDateString()})";

        // Relation avec le club
        public Guid ClubId { get; set; }
        public virtual Club Club { get; set; } = null!;
        // Navigation properties
        public virtual ICollection<MembreCommission> MembresCommission { get; set; } = new List<MembreCommission>();
    }
}