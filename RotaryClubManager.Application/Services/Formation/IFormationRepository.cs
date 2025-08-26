using RotaryClubManager.Domain.Entities.Formation;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Domain.Identity;

namespace RotaryClubManager.Application.Services.Formation
{
    public interface IFormationRepository
    {
        // Document Formation
        Task<DocumentFormation?> GetDocumentByIdAsync(Guid id);
        Task<DocumentFormation?> GetDocumentByIdAndClubAsync(Guid id, Guid clubId);
        Task<List<DocumentFormation>> GetDocumentsByClubAsync(Guid clubId);
        Task<DocumentFormation> CreateDocumentAsync(DocumentFormation document);
        Task<DocumentFormation> UpdateDocumentAsync(DocumentFormation document);
        Task<bool> DeleteDocumentAsync(Guid id);
        
        // Chunk Document
        Task<List<ChunkDocument>> GetChunksByDocumentAsync(Guid documentId);
        Task<ChunkDocument> CreateChunkAsync(ChunkDocument chunk);
        Task<List<ChunkDocument>> CreateChunksAsync(List<ChunkDocument> chunks);
        Task<bool> DeleteChunksByDocumentAsync(Guid documentId);
        
        // Session Formation
        Task<SessionFormation?> GetSessionByIdAsync(Guid id);
        Task<SessionFormation?> GetSessionByIdAndUserAsync(Guid id, string userId);
        Task<List<SessionFormation>> GetSessionsByUserAsync(string userId);
        Task<List<SessionFormation>> GetSessionsByClubAsync(Guid clubId);
        Task<SessionFormation> CreateSessionAsync(SessionFormation session);
        Task<SessionFormation> UpdateSessionAsync(SessionFormation session);
        
        // Question Formation
        Task<List<QuestionFormation>> GetQuestionsBySessionAsync(Guid sessionId);
        Task<QuestionFormation> CreateQuestionAsync(QuestionFormation question);
        Task<List<QuestionFormation>> CreateQuestionsAsync(List<QuestionFormation> questions);
        
        // Reponse Utilisateur
        Task<ReponseUtilisateur> CreateReponseAsync(ReponseUtilisateur reponse);
        Task<List<ReponseUtilisateur>> GetReponsesBySessionAsync(Guid sessionId);
        
        // Badge Formation
        Task<List<BadgeFormation>> GetBadgesByUserAsync(string userId);
        Task<BadgeFormation> CreateBadgeAsync(BadgeFormation badge);
        Task<bool> BadgeExistsAsync(string userId, TypeBadge type, string? documentId = null);
        
        // Recherche sémantique
        Task<List<ChunkDocument>> SearchChunksByEmbeddingAsync(float[] queryEmbedding, Guid clubId, int limit = 10);
        Task<List<ChunkDocument>> SearchChunksByEmbeddingAndDocumentAsync(float[] queryEmbedding, Guid documentId, int limit = 5);

        // Opérations sur les entités liées
        Task<bool> ClubExistsAsync(Guid clubId);
        Task<bool> UserExistsAsync(string userId);
        Task<DocumentFormation?> GetDocumentFormationByIdAsync(Guid documentId);
        Task<QuestionFormation?> GetQuestionFormationByIdAsync(Guid questionId);
        Task<ReponseUtilisateur?> GetExistingReponseAsync(Guid questionId, Guid sessionId);
        Task<List<ApplicationUser>> GetUsersByClubAsync(Guid clubId);
        Task<List<DocumentFormation>> GetDocumentsByIdsAsync(List<Guid> documentIds);
        Task<List<SessionFormation>> GetSessionsByDocumentAsync(Guid documentId);
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
        Task<Club?> GetClubByIdAsync(Guid clubId);

        // Opérations spécifiques pour EmbeddingService
        Task<DocumentFormation?> GetDocumentWithChunksAsync(Guid documentId);
        Task<ChunkDocument> SaveChunkAsync(ChunkDocument chunk);

        // Opérations spécifiques pour QuestionGeneratorService
        Task<SessionFormation?> GetSessionWithDocumentAndChunksAsync(Guid sessionId);
        Task<List<QuestionFormation>> SaveQuestionsAsync(List<QuestionFormation> questions);
        Task<bool> DeleteSessionAsync(Guid sessionId);
    }
}
