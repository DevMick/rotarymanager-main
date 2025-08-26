using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using RotaryClubManager.Domain.Entities.Formation;
using System.Text.Json;

namespace RotaryClubManager.Application.Services.Formation
{
    public class AIService : IAIService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIService> _logger;
        private readonly OpenAIClient _openAIClient;

        public AIService(IConfiguration configuration, ILogger<AIService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Pour l'instant, pas d'initialisation OpenAI pour éviter les erreurs de configuration
            // TODO: Réactiver une fois que la configuration OpenAI est correcte
            _logger.LogInformation("AIService initialisé en mode test (sans OpenAI)");
        }

        public async Task<List<GeneratedQuestion>> GenerateQuestionsFromContentAsync(string content, int questionCount = 10)
        {
            try
            {
                _logger.LogInformation($"Génération de {questionCount} questions à partir du contenu (mode test)");

                // Pour l'instant, générer des questions de test basées sur le contenu
                // TODO: Remplacer par la vraie génération IA une fois que la configuration est correcte

                var questions = new List<GeneratedQuestion>();

                // Questions de base adaptées au contenu
                var baseQuestions = new[]
                {
                    new GeneratedQuestion
                    {
                        Texte = "Quelle est la devise du Rotary International ?",
                        Type = TypeQuestion.QCM,
                        Options = new Dictionary<string, string>
                        {
                            {"A", "Servir d'abord"},
                            {"B", "Service Above Self"},
                            {"C", "Rotary Serving Humanity"},
                            {"D", "One Rotary"}
                        },
                        ReponseCorrecte = "B",
                        Explication = "La devise officielle du Rotary International est 'Service Above Self' (Servir autrui).",
                        Difficulte = 1
                    },
                    new GeneratedQuestion
                    {
                        Texte = "Le Rotary International a été fondé en 1905.",
                        Type = TypeQuestion.VraiFaux,
                        Options = new Dictionary<string, string> { {"true", "Vrai"}, {"false", "Faux"} },
                        ReponseCorrecte = "true",
                        Explication = "Le Rotary International a effectivement été fondé en 1905 par Paul Harris à Chicago.",
                        Difficulte = 1
                    },
                    new GeneratedQuestion
                    {
                        Texte = "Combien y a-t-il d'Avenues du Service dans le Rotary ?",
                        Type = TypeQuestion.QCM,
                        Options = new Dictionary<string, string>
                        {
                            {"A", "3"},
                            {"B", "4"},
                            {"C", "5"},
                            {"D", "6"}
                        },
                        ReponseCorrecte = "C",
                        Explication = "Il y a 5 Avenues du Service dans le Rotary : Service en club, Service professionnel, Service dans la communauté, Service international, et Service des jeunes.",
                        Difficulte = 2
                    }
                };

                // Répéter et varier les questions pour atteindre le nombre demandé
                for (int i = 0; i < questionCount; i++)
                {
                    var baseQuestion = baseQuestions[i % baseQuestions.Length];
                    var question = new GeneratedQuestion
                    {
                        Texte = $"{baseQuestion.Texte}",
                        Type = baseQuestion.Type,
                        Options = new Dictionary<string, string>(baseQuestion.Options),
                        ReponseCorrecte = baseQuestion.ReponseCorrecte,
                        Explication = baseQuestion.Explication,
                        Difficulte = baseQuestion.Difficulte
                    };

                    questions.Add(question);
                }

                _logger.LogInformation($"Généré {questions.Count} questions de test");
                return questions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération de questions");
                throw new InvalidOperationException($"Erreur lors de la génération de questions: {ex.Message}", ex);
            }
        }

        public async Task<string> GenerateExplanationAsync(string question, string correctAnswer, string userAnswer)
        {
            try
            {
                _logger.LogInformation("Génération d'explication (mode test)");

                // Générer une explication simple pour l'instant
                var isCorrect = string.Equals(correctAnswer, userAnswer, StringComparison.OrdinalIgnoreCase);

                if (isCorrect)
                {
                    return $"✅ Excellente réponse ! '{correctAnswer}' est effectivement la bonne réponse. " +
                           "Cette question porte sur les fondamentaux du Rotary International, " +
                           "une organisation dédiée au service communautaire et à la paix mondiale.";
                }
                else
                {
                    return $"❌ La réponse correcte était '{correctAnswer}', vous aviez répondu '{userAnswer}'. " +
                           "Cette question concerne les principes fondamentaux du Rotary International. " +
                           "N'hésitez pas à consulter les ressources de formation pour approfondir vos connaissances.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération d'explication");
                return $"Explication: La réponse correcte est '{correctAnswer}'. Votre réponse était '{userAnswer}'.";
            }
        }


    }
}
