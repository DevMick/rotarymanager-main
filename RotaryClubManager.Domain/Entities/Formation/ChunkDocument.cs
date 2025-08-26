using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RotaryClubManager.Domain.Entities.Formation
{
    public class ChunkDocument
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid DocumentFormationId { get; set; }
        
        [Required]
        public string Contenu { get; set; } = string.Empty;
        
        public float[]? Embedding { get; set; }
        
        [Column(TypeName = "jsonb")]
        public string? Metadata { get; set; }
        
        public int IndexChunk { get; set; }
        
        // Navigation properties
        public virtual DocumentFormation DocumentFormation { get; set; } = null!;
        public virtual ICollection<QuestionFormation> Questions { get; set; } = new List<QuestionFormation>();
    }
}
