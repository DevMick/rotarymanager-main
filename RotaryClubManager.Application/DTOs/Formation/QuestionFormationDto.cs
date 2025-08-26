using RotaryClubManager.Domain.Entities.Formation;
using System.Text.Json;

namespace RotaryClubManager.Application.DTOs.Formation
{
    public class QuestionFormationDto
    {
        public Guid Id { get; set; }
        public Guid SessionFormationId { get; set; }
        public Guid ChunkDocumentId { get; set; }
        public string TexteQuestion { get; set; } = string.Empty;
        public TypeQuestion Type { get; set; }
        public JsonElement? Options { get; set; }
        public string ReponseCorrecte { get; set; } = string.Empty;
        public int Difficulte { get; set; }
        public bool EstRepondue { get; set; }
        public bool? ReponseUtilisateurEstCorrecte { get; set; }
    }

    public class CreateQuestionFormationDto
    {
        public Guid ChunkDocumentId { get; set; }
        public string TexteQuestion { get; set; } = string.Empty;
        public TypeQuestion Type { get; set; }
        public JsonElement? Options { get; set; }
        public string ReponseCorrecte { get; set; } = string.Empty;
        public int Difficulte { get; set; } = 1;
    }

    public class ReponseUtilisateurDto
    {
        public Guid QuestionFormationId { get; set; }
        public string ReponseTexte { get; set; } = string.Empty;
        public int TempsReponseMs { get; set; }
    }

    public class ResultatReponseDto
    {
        public bool EstCorrecte { get; set; }
        public string ReponseCorrecte { get; set; } = string.Empty;
        public string Explication { get; set; } = string.Empty;
        public int ScoreGagne { get; set; }
        public int ScoreTotal { get; set; }
        public bool SessionTerminee { get; set; }
        public bool ObjectifAtteint { get; set; }
    }
}
