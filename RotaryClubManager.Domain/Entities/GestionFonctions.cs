using System.ComponentModel.DataAnnotations;

namespace RotaryClubManager.Domain.Entities
{
    public class FonctionRolesResponsabilites
    {
        public Guid Id { get; set; }

        [Required]
        public string Libelle { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Relation avec la fonction
        [Required]
        public Guid FonctionId { get; set; }
        public virtual Fonction Fonction { get; set; } = null!;

    }

 
    public class FonctionEcheances
    {
        public Guid Id { get; set; }

        [Required]
        public string Libelle { get; set; } = string.Empty;

        [Required]
        public DateTime DateButoir { get; set; }

        [Required]
        public FrequenceType Frequence { get; set; } = FrequenceType.Unique;

        [Required]
        public Guid FonctionId { get; set; }
        public virtual Fonction Fonction { get; set; } = null!;

        
    }

    public enum FrequenceType
    {
       
        Unique = 0,
        Quotidienne = 1,
        Hebdomadaire = 2,
        Mensuelle = 3,
        Trimestrielle = 4,
        Semestrielle = 5,
        Annuelle = 6,
        ParMandat = 7
    }

}