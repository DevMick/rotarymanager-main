using System.ComponentModel.DataAnnotations;
using RotaryClubManager.Domain.Identity;

namespace RotaryClubManager.Domain.Entities.Formation
{
    public class SessionFormation
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(450)]
        public string MembreId { get; set; } = string.Empty;
        
        [Required]
        public Guid DocumentFormationId { get; set; }
        
        public DateTime DateDebut { get; set; } = DateTime.UtcNow;
        
        public DateTime? DateFin { get; set; }
        
        public int ScoreActuel { get; set; } = 0;
        
        public int ScoreObjectif { get; set; } = 80;
        
        public StatutSession Statut { get; set; } = StatutSession.EnCours;
        
        // Navigation properties
        public virtual ApplicationUser Membre { get; set; } = null!;
        public virtual DocumentFormation DocumentFormation { get; set; } = null!;
        public virtual ICollection<QuestionFormation> Questions { get; set; } = new List<QuestionFormation>();
        public virtual ICollection<ReponseUtilisateur> Reponses { get; set; } = new List<ReponseUtilisateur>();
    }

    public enum StatutSession
    {
        EnCours = 1,
        Terminee = 2,
        Abandonnee = 3,
        Reussie = 4
    }
}
