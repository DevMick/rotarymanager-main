using Microsoft.Extensions.Configuration;
using RotaryClubManager.Application.DTOs.Formation;
using RotaryClubManager.Domain.Entities.Formation;
using System.Security.Claims;
using System.Text.Json;

namespace RotaryClubManager.Application.Services.Formation
{
    public class FormationService : IFormationService
    {
        private readonly IFormationRepository _formationRepository;
        private readonly IEmbeddingService _embeddingService;
        private readonly IQuestionGeneratorService _questionGeneratorService;
        private readonly IConfiguration _configuration;

        public FormationService(
            IFormationRepository formationRepository,
            IEmbeddingService embeddingService,
            IQuestionGeneratorService questionGeneratorService,
            IConfiguration configuration)
        {
            _formationRepository = formationRepository;
            _embeddingService = embeddingService;
            _questionGeneratorService = questionGeneratorService;
            _configuration = configuration;
        }

        // ===== GESTION DES DOCUMENTS DE FORMATION =====

        public async Task<DocumentFormationDto> UploadDocumentAsync(Guid clubId, string userId, UploadFileDto file, CreateDocumentFormationDto createDto)
        {
            Console.WriteLine($"=== DEBUT UploadDocumentAsync ===");
            Console.WriteLine($"ClubId: {clubId}, UserId: {userId}");
            Console.WriteLine($"Fichier: {file.FileName}, Taille: {file.Length} bytes");

            // Vérifier que le club existe
            if (!await _formationRepository.ClubExistsAsync(clubId))
                throw new ArgumentException("Club non trouvé");

            // Vérifier que l'utilisateur existe
            if (!await _formationRepository.UserExistsAsync(userId))
                throw new ArgumentException("Utilisateur non trouvé");

            // Créer le chemin de fichier
            var uploadPath = _configuration["Formation:Upload:StoragePath"] ?? "uploads/formation/";
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadPath, fileName);

            // Créer le répertoire s'il n'existe pas
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            Console.WriteLine($"Sauvegarde du fichier: {filePath}");

            // Sauvegarder le fichier
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.FileStream.CopyToAsync(fileStream);
            }

            // Créer l'entité DocumentFormation
            var document = new DocumentFormation
            {
                Id = Guid.NewGuid(),
                Titre = createDto.Titre,
                Description = createDto.Description,
                CheminFichier = filePath,
                DateUpload = DateTime.UtcNow,
                UploadePar = userId,
                ClubId = clubId,
                EstActif = true,
                Type = createDto.Type
            };

            // Sauvegarder le document
            var savedDocument = await _formationRepository.CreateDocumentAsync(document);
            Console.WriteLine($"Document sauvegardé en base: {savedDocument.Id}");

            // Traiter le document de manière SYNCHRONE (au lieu d'asynchrone)
            Console.WriteLine("Début du traitement synchrone du document...");
            try
            {
                var processingResult = await _embeddingService.ProcessDocumentChunksAsync(savedDocument.Id);
                Console.WriteLine($"Traitement terminé, résultat: {processingResult}");
            }
            catch (Exception ex)
            {
                // Log l'erreur mais ne pas faire échouer l'upload
                Console.WriteLine($"ERREUR lors du traitement du document {savedDocument.Id}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("Mapping vers DTO...");
            var result = await MapToDocumentFormationDto(savedDocument);
            Console.WriteLine($"Chunks mappés: {result.NombreChunks}");
            Console.WriteLine($"=== FIN UploadDocumentAsync ===");

            return result;
        }

        public async Task<DocumentFormationDto> GetDocumentAsync(Guid documentId, Guid clubId)
        {
            var document = await _formationRepository.GetDocumentByIdAndClubAsync(documentId, clubId);
            if (document == null)
                return null!;

            return await MapToDocumentFormationDto(document);
        }

        public async Task<List<DocumentFormationDto>> GetDocumentsByClubAsync(Guid clubId)
        {
            var documents = await _formationRepository.GetDocumentsByClubAsync(clubId);
            var dtos = new List<DocumentFormationDto>();

            foreach (var document in documents)
            {
                dtos.Add(await MapToDocumentFormationDto(document));
            }

            return dtos;
        }

        public async Task<DocumentFormationDto> UpdateDocumentAsync(Guid documentId, Guid clubId, UpdateDocumentFormationDto updateDto)
        {
            var document = await _formationRepository.GetDocumentByIdAndClubAsync(documentId, clubId);
            if (document == null)
                return null!;

            document.Titre = updateDto.Titre;
            document.Description = updateDto.Description;
            document.EstActif = updateDto.EstActif;
            document.Type = updateDto.Type;

            var updatedDocument = await _formationRepository.UpdateDocumentAsync(document);
            return await MapToDocumentFormationDto(updatedDocument);
        }

        public async Task<bool> DeleteDocumentAsync(Guid documentId, Guid clubId)
        {
            var document = await _formationRepository.GetDocumentByIdAndClubAsync(documentId, clubId);
            if (document == null)
                return false;

            // Supprimer le fichier physique
            if (File.Exists(document.CheminFichier))
            {
                File.Delete(document.CheminFichier);
            }

            // Supprimer les chunks associés
            await _formationRepository.DeleteChunksByDocumentAsync(documentId);

            // Supprimer le document
            return await _formationRepository.DeleteDocumentAsync(documentId);
        }

        // ===== GESTION DES SESSIONS DE FORMATION =====

        public async Task<SessionFormationDto> StartSessionAsync(string userId, CreateSessionFormationDto createDto)
        {
            Console.WriteLine($"=== DEBUT StartSessionAsync ===");
            Console.WriteLine($"UserId: {userId}, DocumentId: {createDto.DocumentFormationId}");

            // Vérifier que le document existe
            var document = await _formationRepository.GetDocumentFormationByIdAsync(createDto.DocumentFormationId);
            if (document == null)
                throw new ArgumentException("Document de formation non trouvé");

            // Vérifier que l'utilisateur existe
            if (!await _formationRepository.UserExistsAsync(userId))
                throw new ArgumentException("Utilisateur non trouvé");

            // Créer la session
            var session = new SessionFormation
            {
                Id = Guid.NewGuid(),
                MembreId = userId,
                DocumentFormationId = createDto.DocumentFormationId,
                DateDebut = DateTime.UtcNow,
                ScoreActuel = 0,
                ScoreObjectif = createDto.ScoreObjectif,
                Statut = StatutSession.EnCours
            };

            var savedSession = await _formationRepository.CreateSessionAsync(session);
            Console.WriteLine($"Session créée: {savedSession.Id}");

            // GÉNÉRATION COMPLÈTE ET SYNCHRONE DES QUESTIONS
            Console.WriteLine("Début de la génération synchrone des questions...");
            try
            {
                await _questionGeneratorService.GenerateQuestionsForSessionAsync(savedSession.Id);
                Console.WriteLine("Questions générées avec succès");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERREUR lors de la génération des questions: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // En cas d'erreur, supprimer la session et relancer l'exception
                await _formationRepository.DeleteSessionAsync(savedSession.Id);
                throw new InvalidOperationException($"Erreur lors de la génération des questions: {ex.Message}", ex);
            }

            Console.WriteLine("=== FIN StartSessionAsync ===");
            return await MapToSessionFormationDto(savedSession);
        }

        public async Task<SessionFormationDto> GetSessionAsync(Guid sessionId, string userId)
        {
            var session = await _formationRepository.GetSessionByIdAndUserAsync(sessionId, userId);
            if (session == null)
                return null!;

            return await MapToSessionFormationDto(session);
        }

        public async Task<List<SessionFormationDto>> GetSessionsByUserAsync(string userId)
        {
            var sessions = await _formationRepository.GetSessionsByUserAsync(userId);
            var dtos = new List<SessionFormationDto>();

            foreach (var session in sessions)
            {
                dtos.Add(await MapToSessionFormationDto(session));
            }

            return dtos;
        }

        public async Task<List<SessionFormationDto>> GetSessionsByClubAsync(Guid clubId)
        {
            var sessions = await _formationRepository.GetSessionsByClubAsync(clubId);
            var dtos = new List<SessionFormationDto>();

            foreach (var session in sessions)
            {
                dtos.Add(await MapToSessionFormationDto(session));
            }

            return dtos;
        }

        public async Task<SessionFormationDto> UpdateSessionAsync(Guid sessionId, string userId, UpdateSessionFormationDto updateDto)
        {
            var session = await _formationRepository.GetSessionByIdAndUserAsync(sessionId, userId);
            if (session == null)
                return null!;

            session.ScoreActuel = updateDto.ScoreActuel;
            session.Statut = updateDto.Statut;

            if (updateDto.Statut == StatutSession.Terminee || updateDto.Statut == StatutSession.Reussie)
            {
                session.DateFin = DateTime.UtcNow;
            }

            var updatedSession = await _formationRepository.UpdateSessionAsync(session);
            return await MapToSessionFormationDto(updatedSession);
        }

        // ===== GESTION DES QUESTIONS ET RÉPONSES =====

        public async Task<List<QuestionFormationDto>> GetQuestionsForSessionAsync(Guid sessionId, string userId)
        {
            // Vérifier que la session appartient à l'utilisateur
            var session = await _formationRepository.GetSessionByIdAndUserAsync(sessionId, userId);
            if (session == null)
                throw new ArgumentException("Session non trouvée");

            var questions = await _formationRepository.GetQuestionsBySessionAsync(sessionId);
            var reponses = await _formationRepository.GetReponsesBySessionAsync(sessionId);

            var dtos = new List<QuestionFormationDto>();

            foreach (var question in questions)
            {
                var reponse = reponses.FirstOrDefault(r => r.QuestionFormationId == question.Id);
                
                var dto = new QuestionFormationDto
                {
                    Id = question.Id,
                    SessionFormationId = question.SessionFormationId,
                    ChunkDocumentId = question.ChunkDocumentId,
                    TexteQuestion = question.TexteQuestion,
                    Type = question.Type,
                    ReponseCorrecte = question.ReponseCorrecte,
                    Difficulte = question.Difficulte,
                    EstRepondue = reponse != null,
                    ReponseUtilisateurEstCorrecte = reponse?.EstCorrecte
                };

                if (!string.IsNullOrEmpty(question.Options))
                {
                    dto.Options = JsonSerializer.Deserialize<JsonElement>(question.Options);
                }

                dtos.Add(dto);
            }

            return dtos;
        }

        public async Task<ResultatReponseDto> SubmitReponseAsync(Guid sessionId, string userId, ReponseUtilisateurDto reponseDto)
        {
            // Vérifier que la session appartient à l'utilisateur
            var session = await _formationRepository.GetSessionByIdAndUserAsync(sessionId, userId);
            if (session == null)
                throw new ArgumentException("Session non trouvée");

            // Vérifier que la question existe
            var question = await _formationRepository.GetQuestionFormationByIdAsync(reponseDto.QuestionFormationId);
            if (question == null)
                throw new ArgumentException("Question non trouvée");

            // Vérifier que la question appartient à la session
            if (question.SessionFormationId != sessionId)
                throw new ArgumentException("Question ne correspond pas à la session");

            // Vérifier si l'utilisateur a déjà répondu à cette question
            var existingReponse = await _formationRepository.GetExistingReponseAsync(reponseDto.QuestionFormationId, sessionId);

            if (existingReponse != null)
                throw new ArgumentException("Réponse déjà soumise pour cette question");

            // Évaluer la réponse
            var estCorrecte = EvaluateReponse(question, reponseDto.ReponseTexte);
            var scoreGagne = estCorrecte ? CalculateScore(question.Difficulte) : 0;

            // Créer la réponse
            var reponse = new ReponseUtilisateur
            {
                Id = Guid.NewGuid(),
                SessionFormationId = sessionId,
                QuestionFormationId = reponseDto.QuestionFormationId,
                ReponseTexte = reponseDto.ReponseTexte,
                EstCorrecte = estCorrecte,
                TempsReponseMs = reponseDto.TempsReponseMs,
                DateReponse = DateTime.UtcNow
            };

            await _formationRepository.CreateReponseAsync(reponse);

            // Mettre à jour le score de la session
            session.ScoreActuel += scoreGagne;
            await _formationRepository.UpdateSessionAsync(session);

            // Vérifier si la session est terminée
            var questions = await _formationRepository.GetQuestionsBySessionAsync(sessionId);
            var reponses = await _formationRepository.GetReponsesBySessionAsync(sessionId);
            var sessionTerminee = reponses.Count >= questions.Count;
            var objectifAtteint = session.ScoreActuel >= session.ScoreObjectif;

            if (sessionTerminee)
            {
                session.Statut = objectifAtteint ? StatutSession.Reussie : StatutSession.Terminee;
                session.DateFin = DateTime.UtcNow;
                await _formationRepository.UpdateSessionAsync(session);

                // Attribuer des badges si nécessaire
                if (objectifAtteint)
                {
                    await AssignBadgesAsync(userId, session.DocumentFormationId);
                }
            }

            return new ResultatReponseDto
            {
                EstCorrecte = estCorrecte,
                ReponseCorrecte = question.ReponseCorrecte,
                Explication = await _questionGeneratorService.GenerateExplanationAsync(
                    question.TexteQuestion, question.ReponseCorrecte, reponseDto.ReponseTexte),
                ScoreGagne = scoreGagne,
                ScoreTotal = session.ScoreActuel,
                SessionTerminee = sessionTerminee,
                ObjectifAtteint = objectifAtteint
            };
        }

        // ===== GESTION DES BADGES ET PROGRESSION =====

        public async Task<List<BadgeFormationDto>> GetBadgesByUserAsync(string userId)
        {
            var badges = await _formationRepository.GetBadgesByUserAsync(userId);
            var dtos = new List<BadgeFormationDto>();

            foreach (var badge in badges)
            {
                var dto = new BadgeFormationDto
                {
                    Id = badge.Id,
                    MembreId = badge.MembreId,
                    NomMembre = badge.Membre?.UserName ?? "Utilisateur inconnu",
                    Type = badge.Type,
                    DocumentFormationId = badge.DocumentFormationId,
                    TitreDocument = null, // À implémenter si nécessaire via une jointure
                    DateObtention = badge.DateObtention,
                    PointsGagnes = badge.PointsGagnes
                };

                dtos.Add(dto);
            }

            return dtos;
        }

        public async Task<ProgressionFormationDto> GetProgressionByUserAsync(string userId)
        {
            var user = await _formationRepository.GetUserByIdAsync(userId);
            if (user == null)
                throw new ArgumentException("Utilisateur non trouvé");

            var sessions = await _formationRepository.GetSessionsByUserAsync(userId);
            var badges = await _formationRepository.GetBadgesByUserAsync(userId);

            var progression = new ProgressionFormationDto
            {
                MembreId = userId,
                NomMembre = user.UserName ?? "Utilisateur inconnu",
                TotalPoints = badges.Sum(b => b.PointsGagnes),
                NombreBadges = badges.Count,
                FormationsCompletees = sessions.Count(s => s.Statut == StatutSession.Reussie),
                FormationsEnCours = sessions.Count(s => s.Statut == StatutSession.EnCours),
                ScoreMoyen = sessions.Any() ? sessions.Average(s => s.ScoreActuel) : 0,
                Badges = await GetBadgesByUserAsync(userId)
            };

            return progression;
        }

        public async Task<List<ProgressionFormationDto>> GetProgressionByClubAsync(Guid clubId)
        {
            var club = await _formationRepository.GetClubByIdAsync(clubId);
            if (club == null)
                throw new ArgumentException("Club non trouvé");

            var membres = await _formationRepository.GetUsersByClubAsync(clubId);

            var progressions = new List<ProgressionFormationDto>();

            foreach (var membre in membres)
            {
                try
                {
                    var progression = await GetProgressionByUserAsync(membre.Id);
                    progressions.Add(progression);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors du calcul de la progression pour {membre.Id}: {ex.Message}");
                }
            }

            return progressions.OrderByDescending(p => p.TotalPoints).ToList();
        }

        // ===== RECHERCHE SÉMANTIQUE =====

        public async Task<List<DocumentFormationDto>> SearchDocumentsAsync(Guid clubId, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<DocumentFormationDto>();

            var chunks = await _embeddingService.SearchSemanticByClubAsync(query, clubId);
            var documentIds = chunks.Select(c => c.DocumentFormationId).Distinct().ToList();

            var documents = await _formationRepository.GetDocumentsByIdsAsync(documentIds);

            var dtos = new List<DocumentFormationDto>();
            foreach (var document in documents)
            {
                dtos.Add(await MapToDocumentFormationDto(document));
            }

            return dtos;
        }

        // ===== MÉTHODES PRIVÉES =====

        private async Task<DocumentFormationDto> MapToDocumentFormationDto(DocumentFormation document)
        {
            var chunks = await _formationRepository.GetChunksByDocumentAsync(document.Id);
            var sessions = await _formationRepository.GetSessionsByDocumentAsync(document.Id);

            return new DocumentFormationDto
            {
                Id = document.Id,
                Titre = document.Titre,
                Description = document.Description,
                CheminFichier = document.CheminFichier,
                DateUpload = document.DateUpload,
                UploadePar = document.UploadePar,
                NomUploadeur = document.Uploadeur?.UserName ?? "Utilisateur inconnu",
                ClubId = document.ClubId,
                EstActif = document.EstActif,
                Type = document.Type,
                NombreChunks = chunks.Count,
                NombreSessions = sessions.Count
            };
        }

        private async Task<SessionFormationDto> MapToSessionFormationDto(SessionFormation session)
        {
            var questions = await _formationRepository.GetQuestionsBySessionAsync(session.Id);
            var reponses = await _formationRepository.GetReponsesBySessionAsync(session.Id);
            var reponsesCorrectes = reponses.Count(r => r.EstCorrecte);

            return new SessionFormationDto
            {
                Id = session.Id,
                MembreId = session.MembreId,
                NomMembre = session.Membre?.UserName ?? "Utilisateur inconnu",
                DocumentFormationId = session.DocumentFormationId,
                TitreDocument = session.DocumentFormation?.Titre ?? "Document inconnu",
                DateDebut = session.DateDebut,
                DateFin = session.DateFin,
                ScoreActuel = session.ScoreActuel,
                ScoreObjectif = session.ScoreObjectif,
                Statut = session.Statut,
                NombreQuestions = questions.Count,
                NombreReponsesCorrectes = reponsesCorrectes,
                PourcentageReussite = questions.Count > 0 ? (double)reponsesCorrectes / questions.Count * 100 : 0
            };
        }

        private bool EvaluateReponse(QuestionFormation question, string reponseUtilisateur)
        {
            return question.ReponseCorrecte.Equals(reponseUtilisateur, StringComparison.OrdinalIgnoreCase);
        }

        private int CalculateScore(int difficulte)
        {
            return difficulte * 10; // Score basé sur la difficulté
        }

        private async Task AssignBadgesAsync(string userId, Guid documentFormationId)
        {
            // Badge pour avoir terminé une formation
            if (!await _formationRepository.BadgeExistsAsync(userId, TypeBadge.FormationComplete, documentFormationId.ToString()))
            {
                var badge = new BadgeFormation
                {
                    Id = Guid.NewGuid(),
                    MembreId = userId,
                    Type = TypeBadge.FormationComplete,
                    DocumentFormationId = documentFormationId.ToString(),
                    DateObtention = DateTime.UtcNow,
                    PointsGagnes = 50
                };

                await _formationRepository.CreateBadgeAsync(badge);
            }

            // Vérifier les autres types de badges
            var sessions = await _formationRepository.GetSessionsByUserAsync(userId);
            var sessionsReussies = sessions.Count(s => s.Statut == StatutSession.Reussie);

            // Badge pour 5 formations réussies
            if (sessionsReussies >= 5 && !await _formationRepository.BadgeExistsAsync(userId, TypeBadge.Expert))
            {
                var badge = new BadgeFormation
                {
                    Id = Guid.NewGuid(),
                    MembreId = userId,
                    Type = TypeBadge.Expert,
                    DateObtention = DateTime.UtcNow,
                    PointsGagnes = 100
                };

                await _formationRepository.CreateBadgeAsync(badge);
            }

            // Badge pour 10 formations réussies
            if (sessionsReussies >= 10 && !await _formationRepository.BadgeExistsAsync(userId, TypeBadge.Leader))
            {
                var badge = new BadgeFormation
                {
                    Id = Guid.NewGuid(),
                    MembreId = userId,
                    Type = TypeBadge.Leader,
                    DateObtention = DateTime.UtcNow,
                    PointsGagnes = 200
                };

                await _formationRepository.CreateBadgeAsync(badge);
            }
        }
    }
}
