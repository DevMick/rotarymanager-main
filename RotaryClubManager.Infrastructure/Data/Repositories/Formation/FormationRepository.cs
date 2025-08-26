using Microsoft.EntityFrameworkCore;
using RotaryClubManager.Domain.Entities.Formation;
using RotaryClubManager.Domain.Entities;
using RotaryClubManager.Domain.Identity;
using RotaryClubManager.Infrastructure.Data;
using RotaryClubManager.Application.Services.Formation;

namespace RotaryClubManager.Infrastructure.Data.Repositories.Formation
{
    public class FormationRepository : IFormationRepository
    {
        private readonly ApplicationDbContext _context;

        public FormationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Document Formation
        public async Task<DocumentFormation?> GetDocumentByIdAsync(Guid id)
        {
            return await _context.DocumentsFormation
                .Include(d => d.Uploadeur)
                .Include(d => d.Club)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<DocumentFormation?> GetDocumentByIdAndClubAsync(Guid id, Guid clubId)
        {
            return await _context.DocumentsFormation
                .Include(d => d.Uploadeur)
                .Include(d => d.Club)
                .FirstOrDefaultAsync(d => d.Id == id && d.ClubId == clubId);
        }

        public async Task<List<DocumentFormation>> GetDocumentsByClubAsync(Guid clubId)
        {
            return await _context.DocumentsFormation
                .Include(d => d.Uploadeur)
                .Include(d => d.Club)
                .Where(d => d.ClubId == clubId)
                .OrderByDescending(d => d.DateUpload)
                .ToListAsync();
        }

        public async Task<DocumentFormation> CreateDocumentAsync(DocumentFormation document)
        {
            _context.DocumentsFormation.Add(document);
            await _context.SaveChangesAsync();
            return document;
        }

        public async Task<DocumentFormation> UpdateDocumentAsync(DocumentFormation document)
        {
            _context.DocumentsFormation.Update(document);
            await _context.SaveChangesAsync();
            return document;
        }

        public async Task<bool> DeleteDocumentAsync(Guid id)
        {
            var document = await _context.DocumentsFormation.FindAsync(id);
            if (document == null) return false;
            
            _context.DocumentsFormation.Remove(document);
            await _context.SaveChangesAsync();
            return true;
        }

        // Chunk Document
        public async Task<List<ChunkDocument>> GetChunksByDocumentAsync(Guid documentId)
        {
            return await _context.ChunksDocument
                .Where(c => c.DocumentFormationId == documentId)
                .OrderBy(c => c.IndexChunk)
                .ToListAsync();
        }

        public async Task<ChunkDocument> CreateChunkAsync(ChunkDocument chunk)
        {
            _context.ChunksDocument.Add(chunk);
            await _context.SaveChangesAsync();
            return chunk;
        }

        public async Task<List<ChunkDocument>> CreateChunksAsync(List<ChunkDocument> chunks)
        {
            _context.ChunksDocument.AddRange(chunks);
            await _context.SaveChangesAsync();
            return chunks;
        }

        public async Task<bool> DeleteChunksByDocumentAsync(Guid documentId)
        {
            var chunks = await _context.ChunksDocument.Where(c => c.DocumentFormationId == documentId).ToListAsync();
            _context.ChunksDocument.RemoveRange(chunks);
            await _context.SaveChangesAsync();
            return true;
        }

        // Session Formation
        public async Task<SessionFormation?> GetSessionByIdAsync(Guid id)
        {
            return await _context.SessionsFormation
                .Include(s => s.Membre)
                .Include(s => s.DocumentFormation)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<SessionFormation?> GetSessionByIdAndUserAsync(Guid id, string userId)
        {
            return await _context.SessionsFormation
                .Include(s => s.Membre)
                .Include(s => s.DocumentFormation)
                .FirstOrDefaultAsync(s => s.Id == id && s.MembreId == userId);
        }

        public async Task<List<SessionFormation>> GetSessionsByUserAsync(string userId)
        {
            return await _context.SessionsFormation
                .Include(s => s.DocumentFormation)
                .Where(s => s.MembreId == userId)
                .OrderByDescending(s => s.DateDebut)
                .ToListAsync();
        }

        public async Task<List<SessionFormation>> GetSessionsByClubAsync(Guid clubId)
        {
            return await _context.SessionsFormation
                .Include(s => s.Membre)
                .Include(s => s.DocumentFormation)
                .Where(s => s.DocumentFormation.ClubId == clubId)
                .OrderByDescending(s => s.DateDebut)
                .ToListAsync();
        }

        public async Task<SessionFormation> CreateSessionAsync(SessionFormation session)
        {
            _context.SessionsFormation.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<SessionFormation> UpdateSessionAsync(SessionFormation session)
        {
            _context.SessionsFormation.Update(session);
            await _context.SaveChangesAsync();
            return session;
        }

        // Question Formation
        public async Task<List<QuestionFormation>> GetQuestionsBySessionAsync(Guid sessionId)
        {
            return await _context.QuestionsFormation
                .Include(q => q.ChunkDocument)
                .Where(q => q.SessionFormationId == sessionId)
                .OrderBy(q => q.Difficulte)
                .ToListAsync();
        }

        public async Task<QuestionFormation> CreateQuestionAsync(QuestionFormation question)
        {
            _context.QuestionsFormation.Add(question);
            await _context.SaveChangesAsync();
            return question;
        }

        public async Task<List<QuestionFormation>> CreateQuestionsAsync(List<QuestionFormation> questions)
        {
            _context.QuestionsFormation.AddRange(questions);
            await _context.SaveChangesAsync();
            return questions;
        }

        // Reponse Utilisateur
        public async Task<ReponseUtilisateur> CreateReponseAsync(ReponseUtilisateur reponse)
        {
            _context.ReponsesUtilisateur.Add(reponse);
            await _context.SaveChangesAsync();
            return reponse;
        }

        public async Task<List<ReponseUtilisateur>> GetReponsesBySessionAsync(Guid sessionId)
        {
            return await _context.ReponsesUtilisateur
                .Include(r => r.QuestionFormation)
                .Where(r => r.QuestionFormation.SessionFormationId == sessionId)
                .ToListAsync();
        }

        // Badge Formation
        public async Task<List<BadgeFormation>> GetBadgesByUserAsync(string userId)
        {
            return await _context.BadgesFormation
                .Where(b => b.MembreId == userId)
                .OrderByDescending(b => b.DateObtention)
                .ToListAsync();
        }

        public async Task<BadgeFormation> CreateBadgeAsync(BadgeFormation badge)
        {
            _context.BadgesFormation.Add(badge);
            await _context.SaveChangesAsync();
            return badge;
        }

        public async Task<bool> BadgeExistsAsync(string userId, TypeBadge type, string? documentId = null)
        {
            return await _context.BadgesFormation
                .AnyAsync(b => b.MembreId == userId && b.Type == type && 
                              (documentId == null || b.DocumentFormationId == documentId));
        }

        // Recherche sémantique
        public async Task<List<ChunkDocument>> SearchChunksByEmbeddingAsync(float[] queryEmbedding, Guid clubId, int limit = 10)
        {
            // Note: Cette requête nécessite l'extension pgvector
            // Pour l'instant, retournons une liste vide
            // L'implémentation complète sera ajoutée après configuration pgvector
            return new List<ChunkDocument>();
        }

        public async Task<List<ChunkDocument>> SearchChunksByEmbeddingAndDocumentAsync(float[] queryEmbedding, Guid documentId, int limit = 5)
        {
            // Note: Cette requête nécessite l'extension pgvector
            // Pour l'instant, retournons une liste vide
            return new List<ChunkDocument>();
        }

        // Opérations sur les entités liées
        public async Task<bool> ClubExistsAsync(Guid clubId)
        {
            return await _context.Clubs.AnyAsync(c => c.Id == clubId);
        }

        public async Task<bool> UserExistsAsync(string userId)
        {
            return await _context.Users.AnyAsync(u => u.Id == userId);
        }

        public async Task<DocumentFormation?> GetDocumentFormationByIdAsync(Guid documentId)
        {
            return await _context.DocumentsFormation.FindAsync(documentId);
        }

        public async Task<QuestionFormation?> GetQuestionFormationByIdAsync(Guid questionId)
        {
            return await _context.QuestionsFormation.FindAsync(questionId);
        }

        public async Task<ReponseUtilisateur?> GetExistingReponseAsync(Guid questionId, Guid sessionId)
        {
            return await _context.ReponsesUtilisateur
                .FirstOrDefaultAsync(r => r.QuestionFormationId == questionId &&
                                        r.SessionFormationId == sessionId);
        }

        public async Task<List<ApplicationUser>> GetUsersByClubAsync(Guid clubId)
        {
            return await _context.Users
                .Where(u => u.UserClubs.Any(uc => uc.ClubId == clubId))
                .ToListAsync();
        }

        public async Task<List<DocumentFormation>> GetDocumentsByIdsAsync(List<Guid> documentIds)
        {
            return await _context.DocumentsFormation
                .Where(d => documentIds.Contains(d.Id))
                .ToListAsync();
        }

        public async Task<List<SessionFormation>> GetSessionsByDocumentAsync(Guid documentId)
        {
            return await _context.SessionsFormation
                .Where(s => s.DocumentFormationId == documentId)
                .ToListAsync();
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<Club?> GetClubByIdAsync(Guid clubId)
        {
            return await _context.Clubs.FindAsync(clubId);
        }

        // Opérations spécifiques pour EmbeddingService
        public async Task<DocumentFormation?> GetDocumentWithChunksAsync(Guid documentId)
        {
            return await _context.DocumentsFormation
                .Include(d => d.Chunks)
                .FirstOrDefaultAsync(d => d.Id == documentId);
        }

        public async Task<ChunkDocument> SaveChunkAsync(ChunkDocument chunk)
        {
            _context.ChunksDocument.Add(chunk);
            await _context.SaveChangesAsync();
            return chunk;
        }

        // Opérations spécifiques pour QuestionGeneratorService
        public async Task<SessionFormation?> GetSessionWithDocumentAndChunksAsync(Guid sessionId)
        {
            return await _context.SessionsFormation
                .Include(s => s.DocumentFormation)
                .ThenInclude(d => d.Chunks)
                .FirstOrDefaultAsync(s => s.Id == sessionId);
        }

        public async Task<List<QuestionFormation>> SaveQuestionsAsync(List<QuestionFormation> questions)
        {
            _context.QuestionsFormation.AddRange(questions);
            await _context.SaveChangesAsync();
            return questions;
        }

        public async Task<bool> DeleteSessionAsync(Guid sessionId)
        {
            var session = await _context.SessionsFormation.FindAsync(sessionId);
            if (session == null)
                return false;

            _context.SessionsFormation.Remove(session);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
