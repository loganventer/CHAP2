using CHAP2.Console.Vectorize.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace CHAP2.Console.Vectorize.Services;

public class ImprovedHashVectorizationService : IVectorizationService
{
    private readonly ILogger<ImprovedHashVectorizationService> _logger;
    private const int VECTOR_DIMENSION = 1536;

    // Semantic keywords for religious/musical content
    private static readonly Dictionary<string, int[]> SemanticKeywords = new()
    {
        // Religious terms
        ["god"] = new[] { 0, 1, 2, 3, 4, 5 },
        ["lord"] = new[] { 0, 1, 2, 3, 4, 5 },
        ["jesus"] = new[] { 6, 7, 8, 9, 10, 11 },
        ["christ"] = new[] { 6, 7, 8, 9, 10, 11 },
        ["savior"] = new[] { 12, 13, 14, 15, 16, 17 },
        ["redeemer"] = new[] { 12, 13, 14, 15, 16, 17 },
        
        // Worship terms
        ["praise"] = new[] { 18, 19, 20, 21, 22, 23 },
        ["worship"] = new[] { 18, 19, 20, 21, 22, 23 },
        ["glory"] = new[] { 24, 25, 26, 27, 28, 29 },
        ["honor"] = new[] { 24, 25, 26, 27, 28, 29 },
        
        // Love and grace
        ["love"] = new[] { 30, 31, 32, 33, 34, 35 },
        ["grace"] = new[] { 30, 31, 32, 33, 34, 35 },
        ["mercy"] = new[] { 36, 37, 38, 39, 40, 41 },
        ["kindness"] = new[] { 36, 37, 38, 39, 40, 41 },
        
        // Faith and trust
        ["faith"] = new[] { 42, 43, 44, 45, 46, 47 },
        ["trust"] = new[] { 42, 43, 44, 45, 46, 47 },
        ["hope"] = new[] { 48, 49, 50, 51, 52, 53 },
        ["belief"] = new[] { 48, 49, 50, 51, 52, 53 },
        
        // Prayer and spiritual life
        ["prayer"] = new[] { 54, 55, 56, 57, 58, 59 },
        ["pray"] = new[] { 54, 55, 56, 57, 58, 59 },
        ["worship"] = new[] { 60, 61, 62, 63, 64, 65 },
        
        // Power and greatness
        ["great"] = new[] { 66, 67, 68, 69, 70, 71 },
        ["mighty"] = new[] { 66, 67, 68, 69, 70, 71 },
        ["powerful"] = new[] { 72, 73, 74, 75, 76, 77 },
        ["awesome"] = new[] { 72, 73, 74, 75, 76, 77 },
        
        // Creation and nature
        ["creation"] = new[] { 78, 79, 80, 81, 82, 83 },
        ["world"] = new[] { 78, 79, 80, 81, 82, 83 },
        ["heaven"] = new[] { 84, 85, 86, 87, 88, 89 },
        
        // Salvation and redemption
        ["salvation"] = new[] { 90, 91, 92, 93, 94, 95 },
        ["redemption"] = new[] { 90, 91, 92, 93, 94, 95 },
        ["deliverance"] = new[] { 96, 97, 98, 99, 100, 101 },
        
        // Music and worship
        ["sing"] = new[] { 102, 103, 104, 105, 106, 107 },
        ["music"] = new[] { 102, 103, 104, 105, 106, 107 },
        ["song"] = new[] { 108, 109, 110, 111, 112, 113 },
        
        // Afrikaans terms
        ["heer"] = new[] { 0, 1, 2, 3, 4, 5 },
        ["prys"] = new[] { 18, 19, 20, 21, 22, 23 },
        ["liefde"] = new[] { 30, 31, 32, 33, 34, 35 },
        ["genade"] = new[] { 30, 31, 32, 33, 34, 35 },
        ["geloof"] = new[] { 42, 43, 44, 45, 46, 47 },
        ["gebed"] = new[] { 54, 55, 56, 57, 58, 59 }
    };

    public ImprovedHashVectorizationService(ILogger<ImprovedHashVectorizationService> logger)
    {
        _logger = logger;
    }

    public async Task<List<float>> GenerateEmbeddingAsync(string text)
    {
        try
        {
            var embeddings = await GenerateEmbeddingsAsync(new List<string> { text });
            return embeddings.FirstOrDefault() ?? new List<float>();
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
                var embedding = GenerateSemanticEmbedding(text);
                embeddings.Add(embedding);
            }
            
            _logger.LogDebug("Generated {Count} semantic embeddings", embeddings.Count);
            return embeddings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embeddings for {Count} texts", texts.Count);
            throw;
        }
    }

    private List<float> GenerateSemanticEmbedding(string text)
    {
        // Normalize text
        var normalizedText = text.ToLowerInvariant().Trim();
        
        // Initialize embedding vector
        var embedding = new float[VECTOR_DIMENSION];
        
        // Extract semantic features
        var semanticFeatures = ExtractSemanticFeatures(normalizedText);
        
        // Apply semantic features to embedding
        foreach (var feature in semanticFeatures)
        {
            foreach (var position in feature.Value)
            {
                if (position < VECTOR_DIMENSION)
                {
                    embedding[position] += feature.Key;
                }
            }
        }
        
        // Add some randomness based on text hash for uniqueness
        var textHash = ComputeTextHash(normalizedText);
        var random = new Random(textHash);
        
        for (int i = 0; i < VECTOR_DIMENSION; i++)
        {
            // Add small random component to ensure uniqueness
            embedding[i] += (float)(random.NextDouble() * 0.1 - 0.05);
        }
        
        // Normalize the vector to unit length
        var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] /= magnitude;
            }
        }
        
        return embedding.ToList();
    }

    private Dictionary<float, int[]> ExtractSemanticFeatures(string text)
    {
        var features = new Dictionary<float, int[]>();
        
        // Check for semantic keywords
        foreach (var keyword in SemanticKeywords)
        {
            if (text.Contains(keyword.Key))
            {
                features[1.0f] = keyword.Value;
            }
        }
        
        // Extract word frequency features
        var words = Regex.Split(text, @"\W+").Where(w => w.Length > 2).ToList();
        var wordFreq = words.GroupBy(w => w).ToDictionary(g => g.Key, g => g.Count());
        
        // Add frequency-based features
        foreach (var word in wordFreq.Take(10)) // Top 10 most frequent words
        {
            var hash = ComputeWordHash(word.Key);
            var positions = new int[6];
            for (int i = 0; i < 6; i++)
            {
                positions[i] = (hash + i * 100) % VECTOR_DIMENSION;
            }
            features[(float)word.Value / words.Count] = positions;
        }
        
        // Add text length features
        var lengthFeature = Math.Min(text.Length / 100.0f, 1.0f);
        var lengthPositions = new int[6];
        for (int i = 0; i < 6; i++)
        {
            lengthPositions[i] = (text.Length + i * 50) % VECTOR_DIMENSION;
        }
        features[lengthFeature] = lengthPositions;
        
        return features;
    }

    private int ComputeTextHash(string text)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return BitConverter.ToInt32(hashBytes, 0);
    }

    private int ComputeWordHash(string word)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(word));
        return BitConverter.ToInt32(hashBytes, 0);
    }
} 