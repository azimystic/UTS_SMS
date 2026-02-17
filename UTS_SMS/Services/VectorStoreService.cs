using Microsoft.Extensions.Options;
using UTS_SMS.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UTS_SMS.Services
{
    /// <summary>
    /// Manages vector storage and similarity search using ChromaDB + Gemini Embeddings.
    /// </summary>
    public class VectorStoreService
    {
        private readonly HttpClient _httpClient;
        private readonly AiChatOptions _options;
        private readonly ILogger<VectorStoreService> _logger;
        private readonly HttpClient _geminiClient;
        private string? _collectionId;

        public VectorStoreService(
            IHttpClientFactory httpClientFactory,
            IOptions<AiChatOptions> options,
            ILogger<VectorStoreService> logger)
        {
            _options = options.Value;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("ChromaDB");
            _geminiClient = httpClientFactory.CreateClient("GeminiEmbedding");
        }

        /// <summary>
        /// Ensures the ChromaDB collection exists and returns its ID.
        /// </summary>
        private async Task<string> EnsureCollectionAsync()
        {
            if (_collectionId != null) return _collectionId;

            try
            {
                // Try to get existing collection
                var response = await _httpClient.GetAsync(
                    $"{_options.ChromaDbUrl}/api/v2/collections/name/{_options.ChromaCollectionName}");

                if (response.IsSuccessStatusCode)
                {
                    var col = await response.Content.ReadFromJsonAsync<ChromaCollection>();
                    _collectionId = col?.Id;
                    return _collectionId!;
                }

                // Create collection
                var createPayload = new { name = _options.ChromaCollectionName, metadata = new { description = "School academic materials" } };
                var createResponse = await _httpClient.PostAsJsonAsync(
                    $"{_options.ChromaDbUrl}/api/v2/collections", createPayload);
                createResponse.EnsureSuccessStatusCode();

                var created = await createResponse.Content.ReadFromJsonAsync<ChromaCollection>();
                _collectionId = created?.Id;
                return _collectionId!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure ChromaDB collection");
                throw;
            }
        }

        /// <summary>
        /// Generate embeddings for a list of texts using Gemini Embedding API.
        /// </summary>
        public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts)
        {
            var allEmbeddings = new List<float[]>();

            // Gemini batch embedding supports up to 100 texts at a time
            const int batchSize = 100;
            for (int i = 0; i < texts.Count; i += batchSize)
            {
                var batch = texts.Skip(i).Take(batchSize).ToList();
                var requestBody = new
                {
                    requests = batch.Select(t => new
                    {
                        model = $"models/{_options.EmbeddingModel}",
                        content = new { parts = new[] { new { text = t } } },
                        outputDimensionality = _options.EmbeddingDimensions
                    }).ToArray()
                };

                var response = await _geminiClient.PostAsJsonAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/{_options.EmbeddingModel}:batchEmbedContents?key={_options.GeminiApiKey}",
                    requestBody);

                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<GeminiBatchEmbeddingResponse>();

                if (result?.Embeddings != null)
                {
                    foreach (var emb in result.Embeddings)
                    {
                        allEmbeddings.Add(emb.Values);
                    }
                }
            }

            return allEmbeddings;
        }

        /// <summary>
        /// Generate embedding for a single text (for queries).
        /// </summary>
        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var result = await GenerateEmbeddingsAsync(new List<string> { text });
            return result.FirstOrDefault() ?? Array.Empty<float>();
        }

        /// <summary>
        /// Store document chunks with their embeddings in ChromaDB.
        /// </summary>
        public async Task StoreChunksAsync(List<DocumentChunk> chunks)
        {
            if (chunks.Count == 0) return;

            var collectionId = await EnsureCollectionAsync();

            // Generate embeddings for all chunk texts
            var texts = chunks.Select(c => c.Text).ToList();
            var embeddings = await GenerateEmbeddingsAsync(texts);

            // Prepare ChromaDB upsert payload
            var payload = new
            {
                ids = chunks.Select(c => c.Id).ToList(),
                embeddings = embeddings,
                documents = texts,
                metadatas = chunks.Select(c => new Dictionary<string, object>
                {
                    ["chapterMaterialId"] = c.ChapterMaterialId,
                    ["subjectName"] = c.SubjectName,
                    ["chapterName"] = c.ChapterName,
                    ["originalFileName"] = c.OriginalFileName,
                    ["filePath"] = c.FilePath,
                    ["pageNumber"] = c.PageNumber,
                    ["chunkIndex"] = c.ChunkIndex
                }).ToList()
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_options.ChromaDbUrl}/api/v2/collections/{collectionId}/upsert", payload);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Stored {Count} chunks in ChromaDB", chunks.Count);
        }

        /// <summary>
        /// Search for similar document chunks based on a query string.
        /// </summary>
        public async Task<List<SearchResult>> SearchAsync(string query, int topK = 5, string? subjectFilter = null)
        {
            var collectionId = await EnsureCollectionAsync();

            // Generate query embedding
            var queryEmbedding = await GenerateEmbeddingAsync(query);

            var searchPayload = new Dictionary<string, object>
            {
                ["query_embeddings"] = new[] { queryEmbedding },
                ["n_results"] = topK,
                ["include"] = new[] { "documents", "metadatas", "distances" }
            };

            if (!string.IsNullOrEmpty(subjectFilter))
            {
                searchPayload["where"] = new { subjectName = subjectFilter };
            }

            var response = await _httpClient.PostAsJsonAsync(
                $"{_options.ChromaDbUrl}/api/v2/collections/{collectionId}/query", searchPayload);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ChromaQueryResult>();

            var searchResults = new List<SearchResult>();
            if (result?.Documents != null && result.Documents.Count > 0)
            {
                for (int i = 0; i < result.Documents[0].Count; i++)
                {
                    var metadata = result.Metadatas?[0]?[i];
                    searchResults.Add(new SearchResult
                    {
                        Text = result.Documents[0][i],
                        Score = result.Distances != null && result.Distances[0].Count > i
                            ? 1.0 - result.Distances[0][i]  // ChromaDB returns L2 distance, convert to similarity
                            : 0,
                        FileName = metadata?.GetValueOrDefault("originalFileName")?.ToString() ?? "",
                        FilePath = metadata?.GetValueOrDefault("filePath")?.ToString() ?? "",
                        PageNumber = int.TryParse(metadata?.GetValueOrDefault("pageNumber")?.ToString(), out var p) ? p : 0,
                        ChapterName = metadata?.GetValueOrDefault("chapterName")?.ToString() ?? "",
                        SubjectName = metadata?.GetValueOrDefault("subjectName")?.ToString() ?? ""
                    });
                }
            }

            return searchResults;
        }

        /// <summary>
        /// Check if a document has already been indexed in ChromaDB.
        /// </summary>
        public async Task<bool> IsDocumentIndexedAsync(int chapterMaterialId)
        {
            try
            {
                var collectionId = await EnsureCollectionAsync();

                var payload = new
                {
                    where = new Dictionary<string, object> { ["chapterMaterialId"] = chapterMaterialId },
                    limit = 1,
                    include = Array.Empty<string>()
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_options.ChromaDbUrl}/api/v2/collections/{collectionId}/get", payload);

                if (!response.IsSuccessStatusCode) return false;

                var result = await response.Content.ReadFromJsonAsync<ChromaGetResult>();
                return result?.Ids != null && result.Ids.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Delete all chunks for a specific document from ChromaDB.
        /// </summary>
        public async Task DeleteDocumentChunksAsync(int chapterMaterialId)
        {
            try
            {
                var collectionId = await EnsureCollectionAsync();

                var payload = new
                {
                    where = new Dictionary<string, object> { ["chapterMaterialId"] = chapterMaterialId }
                };

                await _httpClient.PostAsJsonAsync(
                    $"{_options.ChromaDbUrl}/api/v2/collections/{collectionId}/delete", payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete chunks for material {Id}", chapterMaterialId);
            }
        }

        /// <summary>
        /// Get list of all indexed documents with their chunk counts.
        /// </summary>
        public async Task<List<IndexedDocument>> GetIndexedDocumentsAsync()
        {
            try
            {
                var collectionId = await EnsureCollectionAsync();

                var payload = new
                {
                    include = new[] { "metadatas" }
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_options.ChromaDbUrl}/api/v2/collections/{collectionId}/get", payload);

                if (!response.IsSuccessStatusCode) return new List<IndexedDocument>();

                var result = await response.Content.ReadFromJsonAsync<ChromaGetResult>();

                if (result?.Metadatas == null) return new List<IndexedDocument>();

                return result.Metadatas
                    .GroupBy(m => m.GetValueOrDefault("chapterMaterialId")?.ToString() ?? "0")
                    .Select(g => new IndexedDocument
                    {
                        ChapterMaterialId = int.TryParse(g.Key, out var id) ? id : 0,
                        FileName = g.First().GetValueOrDefault("originalFileName")?.ToString() ?? "",
                        SubjectName = g.First().GetValueOrDefault("subjectName")?.ToString() ?? "",
                        ChapterName = g.First().GetValueOrDefault("chapterName")?.ToString() ?? "",
                        ChunkCount = g.Count()
                    })
                    .OrderBy(d => d.SubjectName)
                    .ThenBy(d => d.FileName)
                    .ToList();
            }
            catch
            {
                return new List<IndexedDocument>();
            }
        }
    }

    // ── Response DTOs ───────────────────────────────────────────────────────

    public class SearchResult
    {
        public string Text { get; set; } = string.Empty;
        public double Score { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public string ChapterName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
    }

    public class IndexedDocument
    {
        public int ChapterMaterialId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string ChapterName { get; set; } = string.Empty;
        public int ChunkCount { get; set; }
    }

    // ── Gemini Embedding API DTOs ───────────────────────────────────────────

    public class GeminiBatchEmbeddingResponse
    {
        [JsonPropertyName("embeddings")]
        public List<GeminiEmbedding> Embeddings { get; set; } = new();
    }

    public class GeminiEmbedding
    {
        [JsonPropertyName("values")]
        public float[] Values { get; set; } = Array.Empty<float>();
    }

    // ── ChromaDB response DTOs ──────────────────────────────────────────────

    public class ChromaCollection
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class ChromaQueryResult
    {
        [JsonPropertyName("ids")]
        public List<List<string>>? Ids { get; set; }

        [JsonPropertyName("documents")]
        public List<List<string>>? Documents { get; set; }

        [JsonPropertyName("metadatas")]
        public List<List<Dictionary<string, object>>>? Metadatas { get; set; }

        [JsonPropertyName("distances")]
        public List<List<double>>? Distances { get; set; }
    }

    public class ChromaGetResult
    {
        [JsonPropertyName("ids")]
        public List<string>? Ids { get; set; }

        [JsonPropertyName("metadatas")]
        public List<Dictionary<string, object>>? Metadatas { get; set; }
    }
}
