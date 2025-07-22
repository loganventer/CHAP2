using CHAP2.Console.Prompt.Configuration;
using CHAP2.Console.Prompt.DTOs;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Security.Cryptography;
using System.Text;

namespace CHAP2.Console.Prompt.Services;

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
            
            // Search in Qdrant
            var searchResults = await _client!.SearchAsync(
                collectionName: _settings.CollectionName,
                vector: queryEmbedding.ToArray(),
                limit: (ulong)maxResults
            );

            var results = new List<ChorusSearchResult>();
            
            foreach (var result in searchResults)
            {
                var payloadDict = result.Payload.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                var chorusResult = new ChorusSearchResult
                {
                    Id = result.Id.Uuid,
                    Score = result.Score,
                    Name = GetPayloadValue(payloadDict, "name") ?? "",
                    ChorusText = GetPayloadValue(payloadDict, "chorusText") ?? "",
                    Key = ParseIntSafely(GetPayloadValue(payloadDict, "key")),
                    Type = ParseIntSafely(GetPayloadValue(payloadDict, "type")),
                    TimeSignature = ParseIntSafely(GetPayloadValue(payloadDict, "timeSignature")),
                    CreatedAt = ParseDateTimeSafely(GetPayloadValue(payloadDict, "createdAt"))
                };
                
                results.Add(chorusResult);
            }

            _logger.LogInformation("Found {Count} similar results for query: {Query}", results.Count, query);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for similar choruses");
            throw;
        }
    }

    public async Task<List<float>> GenerateEmbeddingAsync(string text)
    {
        // Use the same hash-based embedding method as the vectorization app
        var normalizedText = text.ToLowerInvariant().Trim();
        
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalizedText));
        
        var embedding = new List<float>();
        var random = new Random(BitConverter.ToInt32(hashBytes, 0));
        
        for (int i = 0; i < VECTOR_DIMENSION; i++)
        {
            var value = (float)(random.NextDouble() * 2 - 1);
            embedding.Add(value);
        }
        
        var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < embedding.Count; i++)
            {
                embedding[i] /= magnitude;
            }
        }
        
        return embedding;
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