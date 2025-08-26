using RotaryClubManager.Domain.Entities.Formation;

namespace RotaryClubManager.Application.DTOs.Formation
{
    public class DocumentFormationDto
    {
        public Guid Id { get; set; }
        public string Titre { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CheminFichier { get; set; } = string.Empty;
        public DateTime DateUpload { get; set; }
        public string UploadePar { get; set; } = string.Empty;
        public string NomUploadeur { get; set; } = string.Empty;
        public Guid ClubId { get; set; }
        public bool EstActif { get; set; }
        public TypeDocumentFormation Type { get; set; }
        public int NombreChunks { get; set; }
        public int NombreSessions { get; set; }
    }

    public class CreateDocumentFormationDto
    {
        public string Titre { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TypeDocumentFormation Type { get; set; }
        public bool EstActif { get; set; } = true;

    }

    public class UpdateDocumentFormationDto
    {
        public string Titre { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool EstActif { get; set; }
        public TypeDocumentFormation Type { get; set; }
    }
}
