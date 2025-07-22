using CHAP2.Console.Vectorize.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Security.Cryptography;
using System.Numerics;

namespace CHAP2.Console.Vectorize.Services;

public class FreeVectorizationService : IVectorizationService
{
    private readonly ILogger<FreeVectorizationService> _logger;
    private const int VECTOR_DIMENSION = 1536; // Match OpenAI embedding dimension

    public FreeVectorizationService(ILogger<FreeVectorizationService> logger)
    {
        _logger = logger;
    }

    public async Task<List<float>> GenerateEmbeddingAsync(string text)
    {
        try
        {
            // Create a deterministic embedding using hash-based approach
            var embedding = GenerateHashBasedEmbedding(text);
            
            _logger.LogDebug("Generated embedding for text of length {TextLength}", text.Length);
            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding for text");
            throw;
        }
    }

    public async Task<List<List<float>>> GenerateEmbeddingsAsync(List<string> texts)
    {
        var embeddings = new List<List<float>>();
        
        try
        {
            foreach (var text in texts)
            {
                var embedding = GenerateHashBasedEmbedding(text);
                embeddings.Add(embedding);
            }
            
            _logger.LogDebug("Generated {Count} embeddings", embeddings.Count);
            return embeddings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embeddings for {Count} texts", texts.Count);
            throw;
        }
    }

    private List<float> GenerateHashBasedEmbedding(string text)
    {
        // Normalize text
        var normalizedText = text.ToLowerInvariant().Trim();
        
        // Generate hash
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalizedText));
        
        // Convert hash to vector
        var embedding = new List<float>();
        var random = new Random(BitConverter.ToInt32(hashBytes, 0)); // Seed with hash
        
        for (int i = 0; i < VECTOR_DIMENSION; i++)
        {
            // Generate values between -1 and 1
            var value = (float)(random.NextDouble() * 2 - 1);
            embedding.Add(value);
        }
        
        // Normalize the vector to unit length
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
} 