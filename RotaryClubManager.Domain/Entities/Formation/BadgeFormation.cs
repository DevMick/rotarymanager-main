using System.ComponentModel.DataAnnotations;
using RotaryClubManager.Domain.Identity;

namespace RotaryClubManager.Domain.Entities.Formation
{
    public class BadgeFormation
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(450)]
        public string MembreId { get; set; } = string.Empty;
        
        public TypeBadge Type { get; set; }
        
        [MaxLength(450)]
        public string? DocumentFormationId { get; set; }
        
        public DateTime DateObtention { get; set; } = DateTime.UtcNow;
        
        public int PointsGagnes { get; set; } = 0;
        
        // Navigation properties
        public virtual ApplicationUser Membre { get; set; } = null!;
    }

    public enum TypeBadge
    {
        PremierQuiz = 1,
        FormationComplete = 2,
        ScoreParfait = 3,
        Rapidite = 4,
        Perseverance = 5,
        Expert = 6,
        Leader = 7
    }
}
