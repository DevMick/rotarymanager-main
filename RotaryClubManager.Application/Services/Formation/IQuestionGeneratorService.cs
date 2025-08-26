using RotaryClubManager.Domain.Entities.Formation;
using RotaryClubManager.Application.DTOs.Formation;

namespace RotaryClubManager.Application.Services.Formation
{
    public interface IQuestionGeneratorService
    {
        Task<List<QuestionFormation>> GenerateQuestionsAsync(ChunkDocument chunk, TypeQuestion type, int count = 3);
        Task<List<QuestionFormation>> GenerateAdaptiveQuestionsAsync(Guid sessionId, int count = 5);
        Task<string> GenerateExplanationAsync(string question, string correctAnswer, string userAnswer);
        Task<List<QuestionFormation>> GenerateQuestionsForSessionAsync(Guid sessionId, int difficultyLevel = 1);
    }

    public interface IAIService
    {
        Task<List<GeneratedQuestion>> GenerateQuestionsFromContentAsync(string content, int questionCount = 10);
        Task<string> GenerateExplanationAsync(string question, string correctAnswer, string userAnswer);
    }

    public class GeneratedQuestion
    {
        public string Texte { get; set; } = string.Empty;
        public TypeQuestion Type { get; set; }
        public Dictionary<string, string> Options { get; set; } = new();
        public string ReponseCorrecte { get; set; } = string.Empty;
        public string Explication { get; set; } = string.Empty;
        public int Difficulte { get; set; } = 1;
    }
}
