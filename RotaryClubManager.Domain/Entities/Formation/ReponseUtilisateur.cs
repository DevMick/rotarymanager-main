using System.ComponentModel.DataAnnotations;

namespace RotaryClubManager.Domain.Entities.Formation
{
    public class ReponseUtilisateur
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid SessionFormationId { get; set; }
        
        [Required]
        public Guid QuestionFormationId { get; set; }
        
        [Required]
        [MaxLength(1000)]
        public string ReponseTexte { get; set; } = string.Empty;
        
        public bool EstCorrecte { get; set; }
        
        public int TempsReponseMs { get; set; }
        
        public DateTime DateReponse { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual SessionFormation SessionFormation { get; set; } = null!;
        public virtual QuestionFormation QuestionFormation { get; set; } = null!;
    }
}
