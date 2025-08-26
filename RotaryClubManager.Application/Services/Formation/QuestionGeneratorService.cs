using RotaryClubManager.Domain.Entities.Formation;
using RotaryClubManager.Application.DTOs.Formation;
using RotaryClubManager.Application.Services.Formation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace RotaryClubManager.Application.Services.Formation
{
    public class QuestionGeneratorService : IQuestionGeneratorService
    {
        private readonly IFormationRepository _formationRepository;
        private readonly IConfiguration _configuration;
        private readonly IAIService _aiService;
        private readonly ILogger<QuestionGeneratorService> _logger;

        public QuestionGeneratorService(
            IFormationRepository formationRepository,
            IConfiguration configuration,
            IAIService aiService,
            ILogger<QuestionGeneratorService> logger)
        {
            _formationRepository = formationRepository;
            _configuration = configuration;
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<List<QuestionFormation>> GenerateQuestionsAsync(ChunkDocument chunk, TypeQuestion type, int count = 3)
        {
            try
            {
                _logger.LogInformation($"Génération de {count} questions pour le chunk {chunk.Id}");

                // Générer les questions avec l'IA basées sur le contenu du chunk
                var generatedQuestions = await _aiService.GenerateQuestionsFromContentAsync(chunk.Contenu, count);

                var questions = new List<QuestionFormation>();

                foreach (var genQuestion in generatedQuestions)
                {
                    var question = new QuestionFormation
                    {
                        Id = Guid.NewGuid(),
                        ChunkDocumentId = chunk.Id,
                        TexteQuestion = genQuestion.Texte,
                        Type = genQuestion.Type,
                        ReponseCorrecte = genQuestion.ReponseCorrecte,
                        Difficulte = genQuestion.Difficulte,
                        Options = genQuestion.Options.Any() ? JsonSerializer.Serialize(genQuestion.Options) : null
                    };

                    questions.Add(question);
                }

                _logger.LogInformation($"Généré {questions.Count} questions avec succès");
                return questions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la génération de questions pour le chunk {chunk.Id}");

                // Fallback: créer des questions basiques si l'IA échoue
                return await GenerateFallbackQuestionsAsync(chunk, type, count);
            }
        }

        public async Task<List<QuestionFormation>> GenerateAdaptiveQuestionsAsync(Guid sessionId, int count = 5)
        {
            // TODO: Implémenter la génération de questions adaptatives
            // Pour l'instant, retourner une liste vide
            return new List<QuestionFormation>();
        }

        public async Task<string> GenerateExplanationAsync(string question, string correctAnswer, string userAnswer)
        {
            try
            {
                _logger.LogInformation("Génération d'explication avec l'IA");
                return await _aiService.GenerateExplanationAsync(question, correctAnswer, userAnswer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération d'explication");
                return $"Explication: La réponse correcte est '{correctAnswer}'. Votre réponse était '{userAnswer}'.";
            }
        }

        public async Task<List<QuestionFormation>> GenerateQuestionsForSessionAsync(Guid sessionId, int difficultyLevel = 1)
        {
            _logger.LogInformation($"=== DEBUT GenerateQuestionsForSessionAsync ===");
            _logger.LogInformation($"SessionId: {sessionId}");

            try
            {
                var session = await _formationRepository.GetSessionWithDocumentAndChunksAsync(sessionId);
                _logger.LogInformation($"Session récupérée: {session != null}");

                if (session == null)
                {
                    throw new ArgumentException($"Session {sessionId} non trouvée");
                }

                _logger.LogInformation($"Document: {session.DocumentFormation?.Titre}");
                _logger.LogInformation($"Nombre de chunks: {session.DocumentFormation?.Chunks?.Count ?? 0}");

                var questions = new List<QuestionFormation>();
                var questionCount = int.Parse(_configuration["AIService:QuestionGeneration:DefaultQuestionCount"] ?? "10");

                // Approche simplifiée : utiliser un chunk existant ou créer des questions sans chunk spécifique
                if (session.DocumentFormation?.Chunks != null && session.DocumentFormation.Chunks.Any())
                {
                    _logger.LogInformation("Génération de questions basées sur le contenu du document avec IA");

                    var firstChunk = session.DocumentFormation.Chunks.First();

                    // Combiner le contenu de tous les chunks pour une génération plus cohérente
                    var combinedContent = string.Join("\n\n",
                        session.DocumentFormation.Chunks
                            .Where(c => !string.IsNullOrWhiteSpace(c.Contenu))
                            .Take(5) // Limiter à 5 chunks pour éviter de dépasser les limites de tokens
                            .Select(c => c.Contenu));

                    if (!string.IsNullOrWhiteSpace(combinedContent))
                    {
                        try
                        {
                            // Générer les questions avec l'IA
                            var generatedQuestions = await _aiService.GenerateQuestionsFromContentAsync(combinedContent, questionCount);

                            foreach (var genQuestion in generatedQuestions)
                            {
                                var question = new QuestionFormation
                                {
                                    Id = Guid.NewGuid(),
                                    SessionFormationId = sessionId,
                                    ChunkDocumentId = firstChunk.Id,
                                    TexteQuestion = genQuestion.Texte,
                                    Type = genQuestion.Type,
                                    ReponseCorrecte = genQuestion.ReponseCorrecte,
                                    Difficulte = genQuestion.Difficulte,
                                    Options = genQuestion.Options.Any() ? JsonSerializer.Serialize(genQuestion.Options) : null
                                };

                                questions.Add(question);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Erreur lors de la génération IA, utilisation du fallback");
                            questions = await GenerateSimpleDefaultQuestionsAsync(sessionId, firstChunk.Id);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Contenu des chunks vide, génération de questions par défaut");
                        questions = await GenerateSimpleDefaultQuestionsAsync(sessionId, firstChunk.Id);
                    }
                }
                else
                {
                    _logger.LogInformation("Aucun chunk trouvé, création d'un chunk par défaut");
                    try
                    {
                        // Créer un chunk par défaut
                        var defaultChunkId = await CreateDefaultChunkAsync(session.DocumentFormation.Id);
                        questions = await GenerateSimpleDefaultQuestionsAsync(sessionId, defaultChunkId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors de la création du chunk par défaut");
                        throw new InvalidOperationException("Impossible de créer les questions de formation", ex);
                    }
                }

                Console.WriteLine($"Nombre de questions générées: {questions.Count}");

                // Sauvegarder les questions
                var savedQuestions = await _formationRepository.SaveQuestionsAsync(questions);
                Console.WriteLine($"Questions sauvegardées: {savedQuestions.Count}");
                Console.WriteLine("=== FIN GenerateQuestionsForSessionAsync ===");

                return savedQuestions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERREUR dans GenerateQuestionsForSessionAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Relancer l'exception au lieu de retourner une liste vide
            }
        }



        private async Task<List<QuestionFormation>> GenerateFallbackQuestionsAsync(ChunkDocument chunk, TypeQuestion type, int count)
        {
            _logger.LogInformation($"Génération de questions fallback pour le chunk {chunk.Id}");

            var questions = new List<QuestionFormation>();

            for (int i = 0; i < count; i++)
            {
                var question = new QuestionFormation
                {
                    Id = Guid.NewGuid(),
                    ChunkDocumentId = chunk.Id,
                    TexteQuestion = $"Question {i + 1} basée sur le contenu du document",
                    Type = type,
                    ReponseCorrecte = "Réponse correcte",
                    Difficulte = 1
                };

                questions.Add(question);
            }

            return questions;
        }

        private async Task<Guid> CreateDefaultChunkAsync(Guid documentId)
        {
            _logger.LogInformation($"Création d'un chunk par défaut pour le document {documentId}");

            var defaultChunk = new ChunkDocument
            {
                Id = Guid.NewGuid(),
                DocumentFormationId = documentId,
                Contenu = "Contenu par défaut pour les questions de formation Rotary",
                IndexChunk = 1
            };

            var savedChunk = await _formationRepository.SaveChunkAsync(defaultChunk);
            return savedChunk.Id;
        }

        private async Task<List<QuestionFormation>> GenerateSimpleDefaultQuestionsAsync(Guid sessionId, Guid chunkId)
        {
            _logger.LogInformation($"Génération de questions par défaut avec chunk {chunkId}");

            var questions = new List<QuestionFormation>();

            // Questions par défaut basées sur le Rotary
            var defaultQuestions = new[]
            {
                new {
                    Texte = "Quelle est la devise du Rotary International ?",
                    Type = TypeQuestion.QCM,
                    Reponse = "Service Above Self",
                    Options = new Dictionary<string, string> { {"A", "Servir d'abord"}, {"B", "Service Above Self"}, {"C", "Rotary Serving Humanity"}, {"D", "One Rotary"} }
                },
                new {
                    Texte = "Le Rotary International a été fondé en 1905.",
                    Type = TypeQuestion.VraiFaux,
                    Reponse = "true",
                    Options = new Dictionary<string, string> { {"true", "Vrai"}, {"false", "Faux"} }
                },
                new {
                    Texte = "Combien y a-t-il d'Avenues du Service dans le Rotary ?",
                    Type = TypeQuestion.QCM,
                    Reponse = "C",
                    Options = new Dictionary<string, string> { {"A", "3"}, {"B", "4"}, {"C", "5"}, {"D", "6"} }
                },
                new {
                    Texte = "L'objectif principal du Rotary est de servir autrui et promouvoir l'intégrité.",
                    Type = TypeQuestion.VraiFaux,
                    Reponse = "true",
                    Options = new Dictionary<string, string> { {"true", "Vrai"}, {"false", "Faux"} }
                },
                new {
                    Texte = "Quelle est la mission principale du Rotary International ?",
                    Type = TypeQuestion.QuestionOuverte,
                    Reponse = "Servir autrui, promouvoir l'intégrité et favoriser la paix mondiale",
                    Options = new Dictionary<string, string>()
                }
            };

            for (int i = 0; i < Math.Min(defaultQuestions.Length, 10); i++)
            {
                var defaultQ = defaultQuestions[i];
                var question = new QuestionFormation
                {
                    Id = Guid.NewGuid(),
                    SessionFormationId = sessionId,
                    ChunkDocumentId = chunkId,
                    TexteQuestion = defaultQ.Texte,
                    Type = defaultQ.Type,
                    ReponseCorrecte = defaultQ.Reponse,
                    Difficulte = 1,
                    Options = defaultQ.Options.Any() ? JsonSerializer.Serialize(defaultQ.Options) : null
                };

                questions.Add(question);
            }

            _logger.LogInformation($"Questions par défaut générées: {questions.Count}");
            return questions;
        }


    }
}
