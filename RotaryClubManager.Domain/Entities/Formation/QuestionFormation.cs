using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RotaryClubManager.Domain.Entities.Formation
{
    public class QuestionFormation
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid SessionFormationId { get; set; }
        
        [Required]
        public Guid ChunkDocumentId { get; set; }
        
        [Required]
        [MaxLength(1000)]
        public string TexteQuestion { get; set; } = string.Empty;
        
        public TypeQuestion Type { get; set; }
        
        [Column(TypeName = "jsonb")]
        public string? Options { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string ReponseCorrecte { get; set; } = string.Empty;
        
        public int Difficulte { get; set; } = 1;
        
        // Navigation properties
        public virtual SessionFormation SessionFormation { get; set; } = null!;
        public virtual ChunkDocument ChunkDocument { get; set; } = null!;
        public virtual ICollection<ReponseUtilisateur> Reponses { get; set; } = new List<ReponseUtilisateur>();
    }

    public enum TypeQuestion
    {
        QCM = 1,
        VraiFaux = 2,
        QuestionOuverte = 3,
        QuestionNumerique = 4
    }
}
