using RotaryClubManager.Domain.Entities.Formation;
using RotaryClubManager.Application.Services.Formation;
using Microsoft.Extensions.Configuration;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text.RegularExpressions;
using System.Text;
using System.Net.Http;
using System.Text.Json;

namespace RotaryClubManager.Application.Services.Formation
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly IFormationRepository _formationRepository;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public EmbeddingService(IFormationRepository formationRepository, IConfiguration configuration, HttpClient httpClient)
        {
            _formationRepository = formationRepository;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            try
            {
                var apiKey = _configuration["OpenAI:ApiKey"];
                var baseUrl = _configuration["OpenAI:BaseUrl"] ?? "https://openrouter.ai/api/v1";

                if (string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine("Clé OpenRouter manquante, utilisation des embeddings de test");
                    return GenerateTestEmbedding(text);
                }

                var requestBody = new
                {
                    input = text,
                    model = _configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small"
                };

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://rotaryclub.local");
                _httpClient.DefaultRequestHeaders.Add("X-Title", "Rotary Club Formation System");

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{baseUrl}/embeddings", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Erreur OpenRouter: {response.StatusCode} - {errorContent}");
                    return GenerateTestEmbedding(text);
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseContent);

                var embeddingArray = doc.RootElement
                    .GetProperty("data")[0]
                    .GetProperty("embedding")
                    .EnumerateArray()
                    .Select(x => (float)x.GetDouble())
                    .ToArray();

                return embeddingArray;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur génération embedding OpenRouter: {ex.Message}");
                return GenerateTestEmbedding(text);
            }
        }

        private float[] GenerateTestEmbedding(string text)
        {
            // Fallback avec vecteur déterministe
            var hash = text.GetHashCode();
            var random = new Random(hash);
            var embedding = new float[1536];

            for (int i = 0; i < 1536; i++)
            {
                embedding[i] = (float)(random.NextDouble() - 0.5) * 2;
            }

            // Normaliser le vecteur
            var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
            if (magnitude > 0)
            {
                for (int i = 0; i < embedding.Length; i++)
                {
                    embedding[i] /= magnitude;
                }
            }

            return embedding;
        }

        public async Task<List<string>> ExtractTextFromPdfAsync(Stream pdfStream)
        {
            var pages = new List<string>();

            try
            {
                using var pdfDocument = new PdfDocument(new PdfReader(pdfStream));

                for (int pageNum = 1; pageNum <= pdfDocument.GetNumberOfPages(); pageNum++)
                {
                    var page = pdfDocument.GetPage(pageNum);
                    var strategy = new LocationTextExtractionStrategy();
                    var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);

                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        pages.Add(pageText.Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur extraction PDF: {ex.Message}");
                pages.Add("Erreur d'extraction du contenu PDF");
            }

            return pages;
        }

        public async Task<List<string>> ChunkTextAsync(string text, int maxChunkSize = 1000)
        {
            var chunks = new List<string>();

            // Nettoyer le texte
            text = CleanText(text);

            if (string.IsNullOrWhiteSpace(text))
                return chunks;

            // Diviser par paragraphes d'abord
            var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            var currentChunk = "";

            foreach (var paragraph in paragraphs)
            {
                var cleanParagraph = paragraph.Trim();

                // Si ajouter ce paragraphe dépasse la taille max
                if (currentChunk.Length + cleanParagraph.Length + 2 > maxChunkSize)
                {
                    // Sauvegarder le chunk actuel s'il n'est pas vide
                    if (!string.IsNullOrWhiteSpace(currentChunk))
                    {
                        chunks.Add(currentChunk.Trim());
                        currentChunk = "";
                    }

                    // Si le paragraphe est trop long, le diviser par phrases
                    if (cleanParagraph.Length > maxChunkSize)
                    {
                        var sentences = SplitIntoSentences(cleanParagraph);
                        foreach (var sentence in sentences)
                        {
                            if (currentChunk.Length + sentence.Length + 1 > maxChunkSize)
                            {
                                if (!string.IsNullOrWhiteSpace(currentChunk))
                                {
                                    chunks.Add(currentChunk.Trim());
                                    currentChunk = "";
                                }
                            }
                            currentChunk += sentence + " ";
                        }
                    }
                    else
                    {
                        currentChunk = cleanParagraph;
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(currentChunk))
                        currentChunk += "\n\n";
                    currentChunk += cleanParagraph;
                }
            }

            // Ajouter le dernier chunk
            if (!string.IsNullOrWhiteSpace(currentChunk))
            {
                chunks.Add(currentChunk.Trim());
            }

            return chunks;
        }

        public async Task<bool> ProcessDocumentChunksAsync(Guid documentId)
        {
            try
            {
                var document = await _formationRepository.GetDocumentWithChunksAsync(documentId);
                if (document == null)
                {
                    Console.WriteLine($"Document {documentId} non trouvé");
                    return false;
                }

                Console.WriteLine($"Début du traitement du document: {document.Titre}");

                // Lire le fichier PDF
                using var fileStream = new FileStream(document.CheminFichier, FileMode.Open, FileAccess.Read);
                var pages = await ExtractTextFromPdfAsync(fileStream);

                if (!pages.Any() || pages.All(p => string.IsNullOrWhiteSpace(p)))
                {
                    Console.WriteLine("Aucun texte extrait du PDF");
                    return false;
                }

                var allChunks = new List<ChunkDocument>();
                var chunkIndex = 0;

                for (int pageIndex = 0; pageIndex < pages.Count; pageIndex++)
                {
                    var pageText = pages[pageIndex];
                    var textChunks = await ChunkTextAsync(pageText, 800);

                    foreach (var chunkText in textChunks)
                    {
                        if (string.IsNullOrWhiteSpace(chunkText))
                            continue;

                        // Générer l'embedding pour ce chunk
                        var embedding = await GenerateEmbeddingAsync(chunkText);

                        var chunk = new ChunkDocument
                        {
                            Id = Guid.NewGuid(),
                            DocumentFormationId = documentId,
                            Contenu = chunkText,
                            IndexChunk = chunkIndex++,
                            Embedding = embedding,
                            Metadata = JsonSerializer.Serialize(new
                            {
                                page = pageIndex + 1,
                                length = chunkText.Length,
                                created = DateTime.UtcNow
                            })
                        };

                        allChunks.Add(chunk);
                    }
                }

                if (allChunks.Any())
                {
                    // Sauvegarder tous les chunks
                    await _formationRepository.CreateChunksAsync(allChunks);
                    Console.WriteLine($"Traitement terminé: {allChunks.Count} chunks créés pour le document {document.Titre}");
                }
                else
                {
                    Console.WriteLine("Aucun chunk créé");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du traitement du document {documentId}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<List<ChunkDocument>> SearchSemanticAsync(string query, Guid documentId, int limit = 5)
        {
            try
            {
                var queryEmbedding = await GenerateEmbeddingAsync(query);
                return await _formationRepository.SearchChunksByEmbeddingAndDocumentAsync(queryEmbedding, documentId, limit);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur recherche sémantique: {ex.Message}");
                return new List<ChunkDocument>();
            }
        }

        public async Task<List<ChunkDocument>> SearchSemanticByClubAsync(string query, Guid clubId, int limit = 10)
        {
            try
            {
                var queryEmbedding = await GenerateEmbeddingAsync(query);
                return await _formationRepository.SearchChunksByEmbeddingAsync(queryEmbedding, clubId, limit);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur recherche sémantique par club: {ex.Message}");
                return new List<ChunkDocument>();
            }
        }

        private string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Supprimer les caractères de contrôle
            text = Regex.Replace(text, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");

            // Normaliser les espaces et les retours à la ligne
            text = Regex.Replace(text, @"[ \t]+", " ");
            text = Regex.Replace(text, @"\r\n|\r|\n", "\n");

            // Supprimer les lignes vides multiples
            text = Regex.Replace(text, @"\n\s*\n\s*\n+", "\n\n");

            return text.Trim();
        }

        private List<string> SplitIntoSentences(string text)
        {
            // Regex pour diviser en phrases (français et anglais)
            var sentences = Regex.Split(text, @"(?<=[.!?])\s+(?=[A-ZÀ-Ÿ])")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Where(s => s.Length > 10) // Éviter les phrases trop courtes
                .ToList();

            return sentences;
        }
    }
}