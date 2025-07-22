using CHAP2.Console.Vectorize.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CHAP2.Console.Vectorize.Services;

public class OpenAIVectorizationService : IVectorizationService
{
    private readonly OpenAISettings _settings;
    private readonly ILogger<OpenAIVectorizationService> _logger;
    private readonly HttpClient _httpClient;

    public OpenAIVectorizationService(
        OpenAISettings settings,
        ILogger<OpenAIVectorizationService> logger,
        HttpClient httpClient)
    {
        _settings = settings;
        _logger = logger;
        _httpClient = httpClient;
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
                var embedding = await GenerateOpenAIEmbeddingAsync(text);
                embeddings.Add(embedding);
            }
            
            _logger.LogDebug("Generated {Count} embeddings using OpenAI", embeddings.Count);
            return embeddings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embeddings for {Count} texts", texts.Count);
            throw;
        }
    }

    private async Task<List<float>> GenerateOpenAIEmbeddingAsync(string text)
    {
        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            _logger.LogWarning("OpenAI API key not configured, falling back to hash-based embedding");
            return GenerateHashBasedEmbedding(text);
        }

        try
        {
            var request = new
            {
                input = text,
                model = _settings.Model
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/embeddings", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("OpenAI API request failed: {StatusCode}, {Error}", response.StatusCode, errorContent);
                return GenerateHashBasedEmbedding(text);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var embeddingResponse = JsonSerializer.Deserialize<OpenAIEmbeddingResponse>(responseContent);

            if (embeddingResponse?.Data?.FirstOrDefault()?.Embedding != null)
            {
                return embeddingResponse.Data.First().Embedding;
            }

            _logger.LogWarning("OpenAI response did not contain valid embedding data");
            return GenerateHashBasedEmbedding(text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI API, falling back to hash-based embedding");
            return GenerateHashBasedEmbedding(text);
        }
    }

    private List<float> GenerateHashBasedEmbedding(string text)
    {
        // Fallback hash-based embedding (same as FreeVectorizationService)
        var normalizedText = text.ToLowerInvariant().Trim();
        
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalizedText));
        
        var embedding = new List<float>();
        var random = new Random(BitConverter.ToInt32(hashBytes, 0));
        
        for (int i = 0; i < 1536; i++)
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

    private class OpenAIEmbeddingResponse
    {
        public List<OpenAIEmbeddingData> Data { get; set; } = new();
    }

    private class OpenAIEmbeddingData
    {
        public List<float> Embedding { get; set; } = new();
    }
} 