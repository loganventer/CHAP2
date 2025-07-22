using CHAP2.WebPortal.Configuration;
using CHAP2.WebPortal.DTOs;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Security.Cryptography;
using System.Text;

namespace CHAP2.WebPortal.Services;

public class VectorSearchService : IVectorSearchService
{
    private readonly QdrantSettings _settings;
    private readonly ILogger<VectorSearchService> _logger;
    private QdrantClient? _client;
    private const int VECTOR_DIMENSION = 1536;

    public VectorSearchService(QdrantSettings settings, ILogger<VectorSearchService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<List<ChorusSearchResult>> SearchSimilarAsync(string query, int maxResults = 5)
    {
        try
        {
            await InitializeClientAsync();
            
            // Generate embedding for the query
            var queryEmbedding = await GenerateEmbeddingAsync(query);
            
            // Search in Qdrant with higher limit to get more candidates
            var searchResults = await _client!.SearchAsync(
                collectionName: _settings.CollectionName,
                vector: queryEmbedding.ToArray(),
                limit: (ulong)(maxResults * 3) // Get more candidates for filtering
            );

            var results = new List<ChorusSearchResult>();
            var queryLower = query.ToLowerInvariant();
            
            // Define semantic keywords for better matching
            var semanticKeywords = new List<string> { "god", "lord", "jesus", "christ", "praise", "worship", "love", "grace", "faith", "prayer", "great", "mighty", "powerful" };
            
            foreach (var result in searchResults)
            {
                var payloadDict = result.Payload.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                var name = GetPayloadValue(payloadDict, "name") ?? "";
                var chorusText = GetPayloadValue(payloadDict, "chorusText") ?? "";
                var contentLower = $"{name} {chorusText}".ToLowerInvariant();
                
                // Calculate semantic similarity score
                var semanticScore = CalculateSemanticSimilarity(queryLower, contentLower, semanticKeywords);
                
                // Combine vector score with semantic score
                var combinedScore = (result.Score * 0.3f) + (semanticScore * 0.7f);
                
                var chorusResult = new ChorusSearchResult
                {
                    Id = result.Id.Uuid,
                    Score = combinedScore,
                    Name = name,
                    ChorusText = chorusText,
                    Key = ParseIntSafely(GetPayloadValue(payloadDict, "key")),
                    Type = ParseIntSafely(GetPayloadValue(payloadDict, "type")),
                    TimeSignature = ParseIntSafely(GetPayloadValue(payloadDict, "timeSignature")),
                    CreatedAt = ParseDateTimeSafely(GetPayloadValue(payloadDict, "createdAt"))
                };
                
                results.Add(chorusResult);
            }
            
            // Sort by combined score and take top results
            results = results.OrderByDescending(r => r.Score).Take(maxResults).ToList();

            _logger.LogInformation("Found {Count} similar results for query: {Query}", results.Count, query);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for similar choruses");
            throw;
        }
    }
    
    private float CalculateSemanticSimilarity(string query, string content, List<string> keywords)
    {
        var score = 0.0f;
        
        // Check for keyword matches
        foreach (var keyword in keywords)
        {
            if (query.Contains(keyword) && content.Contains(keyword))
            {
                score += 0.2f;
            }
        }
        
        // Check for word overlap
        var queryWords = query.Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var contentWords = content.Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        
        var overlap = queryWords.Count(qw => contentWords.Any(cw => cw.Contains(qw) || qw.Contains(cw)));
        score += (float)overlap / queryWords.Length * 0.3f;
        
        // Check for exact phrase matches
        if (content.Contains(query))
        {
            score += 0.5f;
        }
        
        return Math.Min(score, 1.0f);
    }

    public async Task<List<float>> GenerateEmbeddingAsync(string text)
    {
        try
        {
            // Use a more semantic approach for embedding generation
            var words = text.ToLowerInvariant().Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            var embedding = new List<float>();
            
            // Define semantic keyword groups
            var keywordGroups = new Dictionary<string, float>
            {
                // Religious/Spiritual themes
                { "god", 0.8f }, { "lord", 0.8f }, { "jesus", 0.9f }, { "christ", 0.9f }, { "holy", 0.7f }, { "spirit", 0.7f },
                { "praise", 0.6f }, { "worship", 0.6f }, { "prayer", 0.6f }, { "faith", 0.6f }, { "grace", 0.6f }, { "love", 0.5f },
                { "heaven", 0.7f }, { "salvation", 0.7f }, { "redemption", 0.7f }, { "blessing", 0.6f },
                
                // Musical elements
                { "sing", 0.4f }, { "song", 0.4f }, { "music", 0.4f }, { "melody", 0.4f }, { "chorus", 0.4f },
                { "key", 0.3f }, { "major", 0.3f }, { "minor", 0.3f }, { "tempo", 0.3f },
                
                // Emotional/Descriptive words
                { "great", 0.3f }, { "mighty", 0.4f }, { "powerful", 0.4f }, { "wonderful", 0.3f }, { "amazing", 0.3f },
                { "beautiful", 0.3f }, { "glorious", 0.4f }, { "majestic", 0.4f }, { "eternal", 0.4f },
                
                // Language indicators
                { "die", 0.2f }, { "van", 0.2f }, { "en", 0.2f }, { "is", 0.2f }, { "the", 0.1f }, { "and", 0.1f }
            };
            
            // Generate embedding based on word frequency and semantic importance
            for (int i = 0; i < VECTOR_DIMENSION; i++)
            {
                var value = 0.0f;
                var random = new Random(i); // Deterministic based on position
                
                foreach (var word in words)
                {
                    if (keywordGroups.ContainsKey(word))
                    {
                        value += keywordGroups[word] * random.Next(100) / 100.0f;
                    }
                    else
                    {
                        value += random.Next(100) / 100.0f * 0.1f; // Lower weight for non-keywords
                    }
                }
                
                embedding.Add(value / Math.Max(words.Length, 1));
            }
            
            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding for text");
            throw;
        }
    }

    public async Task<List<ChorusSearchResult>> GetAllChorusesAsync()
    {
        try
        {
            await InitializeClientAsync();
            
            // Get all points from the collection
            var scrollResponse = await _client!.ScrollAsync(
                collectionName: _settings.CollectionName,
                limit: 1000 // Get all choruses (assuming less than 1000)
            );

            var results = new List<ChorusSearchResult>();
            
            foreach (var point in scrollResponse.Result)
            {
                var payloadDict = point.Payload.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                
                var chorusResult = new ChorusSearchResult
                {
                    Id = point.Id.Uuid,
                    Score = 1.0f, // Default score for all choruses
                    Name = GetPayloadValue(payloadDict, "name") ?? "",
                    ChorusText = GetPayloadValue(payloadDict, "chorusText") ?? "",
                    Key = ParseIntSafely(GetPayloadValue(payloadDict, "key")),
                    Type = ParseIntSafely(GetPayloadValue(payloadDict, "type")),
                    TimeSignature = ParseIntSafely(GetPayloadValue(payloadDict, "timeSignature")),
                    CreatedAt = ParseDateTimeSafely(GetPayloadValue(payloadDict, "createdAt"))
                };
                
                results.Add(chorusResult);
            }

            _logger.LogInformation("Retrieved {Count} choruses from vector database", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all choruses from vector database");
            throw;
        }
    }

    private async Task InitializeClientAsync()
    {
        if (_client == null)
        {
            _client = new QdrantClient(_settings.Host, _settings.Port + 1); // Use gRPC port
            _logger.LogInformation("Initialized Qdrant client for collection: {CollectionName}", _settings.CollectionName);
        }
    }

    private static string? GetPayloadValue(Dictionary<string, Value> payload, string key)
    {
        return payload.TryGetValue(key, out var value) ? value.StringValue : null;
    }

    private static int ParseIntSafely(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;
        
        return int.TryParse(value, out var result) ? result : 0;
    }

    private static DateTime ParseDateTimeSafely(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return DateTime.UtcNow;
        
        return DateTime.TryParse(value, out var result) ? result : DateTime.UtcNow;
    }
} 