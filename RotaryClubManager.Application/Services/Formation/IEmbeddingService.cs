using RotaryClubManager.Domain.Entities.Formation;

namespace RotaryClubManager.Application.Services.Formation
{
    public interface IEmbeddingService
    {
        Task<float[]> GenerateEmbeddingAsync(string text);
        Task<List<ChunkDocument>> SearchSemanticAsync(string query, Guid documentId, int limit = 5);
        Task<List<ChunkDocument>> SearchSemanticByClubAsync(string query, Guid clubId, int limit = 10);
        Task<bool> ProcessDocumentChunksAsync(Guid documentId);
        Task<List<string>> ExtractTextFromPdfAsync(Stream pdfStream);
        Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = 1000);
    }
}
