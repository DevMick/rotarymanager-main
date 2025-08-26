using RotaryClubManager.Application.DTOs.Formation;

namespace RotaryClubManager.Application.Services.Formation
{
    public interface IFormationService
    {
        // Gestion des documents de formation
        Task<DocumentFormationDto> UploadDocumentAsync(Guid clubId, string userId, UploadFileDto file, CreateDocumentFormationDto createDto);
        Task<DocumentFormationDto> GetDocumentAsync(Guid documentId, Guid clubId);
        Task<List<DocumentFormationDto>> GetDocumentsByClubAsync(Guid clubId);
        Task<DocumentFormationDto> UpdateDocumentAsync(Guid documentId, Guid clubId, UpdateDocumentFormationDto updateDto);
        Task<bool> DeleteDocumentAsync(Guid documentId, Guid clubId);
        
        // Gestion des sessions de formation
        Task<SessionFormationDto> StartSessionAsync(string userId, CreateSessionFormationDto createDto);
        Task<SessionFormationDto> GetSessionAsync(Guid sessionId, string userId);
        Task<List<SessionFormationDto>> GetSessionsByUserAsync(string userId);
        Task<List<SessionFormationDto>> GetSessionsByClubAsync(Guid clubId);
        Task<SessionFormationDto> UpdateSessionAsync(Guid sessionId, string userId, UpdateSessionFormationDto updateDto);
        
        // Gestion des questions et réponses
        Task<List<QuestionFormationDto>> GetQuestionsForSessionAsync(Guid sessionId, string userId);
        Task<ResultatReponseDto> SubmitReponseAsync(Guid sessionId, string userId, ReponseUtilisateurDto reponseDto);
        
        // Gestion des badges et progression
        Task<List<BadgeFormationDto>> GetBadgesByUserAsync(string userId);
        Task<ProgressionFormationDto> GetProgressionByUserAsync(string userId);
        Task<List<ProgressionFormationDto>> GetProgressionByClubAsync(Guid clubId);
        
        // Recherche sémantique
        Task<List<DocumentFormationDto>> SearchDocumentsAsync(Guid clubId, string query);
    }
}
