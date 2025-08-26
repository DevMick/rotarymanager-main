using System.ComponentModel.DataAnnotations;
using RotaryClubManager.Domain.Identity;

namespace RotaryClubManager.Domain.Entities.Formation
{
    public class DocumentFormation
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Titre { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string CheminFichier { get; set; } = string.Empty;
        
        public DateTime DateUpload { get; set; } = DateTime.UtcNow;
        
        [Required]
        [MaxLength(450)]
        public string UploadePar { get; set; } = string.Empty;
        
        [Required]
        public Guid ClubId { get; set; }
        
        public bool EstActif { get; set; } = true;
        
        public TypeDocumentFormation Type { get; set; }
        
        // Navigation properties
        public virtual ApplicationUser Uploadeur { get; set; } = null!;
        public virtual Club Club { get; set; } = null!;
        public virtual ICollection<ChunkDocument> Chunks { get; set; } = new List<ChunkDocument>();
        public virtual ICollection<SessionFormation> Sessions { get; set; } = new List<SessionFormation>();
    }

    public enum TypeDocumentFormation
    {
        ManuelRotary = 1,
        ProcedureClub = 2,
        FormationLeadership = 3,
        ReglementInterieur = 4,
        GuideProjet = 5,
        Autre = 6
    }
}
