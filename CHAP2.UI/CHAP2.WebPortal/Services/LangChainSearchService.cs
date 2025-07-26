using CHAP2.WebPortal.DTOs;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CHAP2.WebPortal.Services;

public interface ILangChainSearchService
{
    Task<List<ChorusSearchResult>> SearchAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default);
    Task<IntelligentSearchResult> SearchWithIntelligenceAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> SearchWithIntelligenceStreamingAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default);
}

public class LangChainSearchService : ILangChainSearchService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LangChainSearchService> _logger;
    private readonly string _baseUrl;

    public LangChainSearchService(HttpClient httpClient, ILogger<LangChainSearchService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["LangChainService:BaseUrl"] ?? "http://localhost:8000";
    }

    public async Task<List<ChorusSearchResult>> SearchAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Performing LangChain search for query: {Query}", query);

            var request = new
            {
                query = query,
                k = maxResults
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/search", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var results = JsonSerializer.Deserialize<List<LangChainSearchResult>>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var chorusResults = results?.Select(r => new ChorusSearchResult
            {
                Id = r.Id ?? "",
                Name = r.Metadata?.Name ?? "",
                ChorusText = r.Text ?? "",
                Key = r.Metadata?.Key ?? 0,
                Type = r.Metadata?.Type ?? 0,
                TimeSignature = r.Metadata?.TimeSignature ?? 0,
                Score = r.Score ?? 0.0f,
                Explanation = r.Explanation ?? ""
            }).ToList() ?? new List<ChorusSearchResult>();

            _logger.LogInformation("LangChain search returned {Count} results for query: {Query}", chorusResults.Count, query);
            return chorusResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during LangChain search for query: {Query}", query);
            throw;
        }
    }

    public async Task<IntelligentSearchResult> SearchWithIntelligenceAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Performing intelligent LangChain search for query: {Query}", query);

            var request = new
            {
                query = query,
                k = maxResults,
                include_analysis = true
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/search_intelligent", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("LangChain service response: {Response}", responseContent);
            
            var result = JsonSerializer.Deserialize<LangChainIntelligentResult>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogInformation("Deserialized result - SearchResults count: {Count}, AiAnalysis: {AiAnalysis}, QueryUnderstanding: {QueryUnderstanding}", 
                result?.SearchResults?.Count ?? 0, result?.AiAnalysis, result?.QueryUnderstanding);

            var chorusResults = result?.SearchResults?.Select(r => new ChorusSearchResult
            {
                Id = r.Id ?? "",
                Name = r.Metadata?.Name ?? "",
                ChorusText = r.Text ?? "",
                Key = r.Metadata?.Key ?? 0,
                Type = r.Metadata?.Type ?? 0,
                TimeSignature = r.Metadata?.TimeSignature ?? 0,
                Score = r.Score ?? 0.0f,
                Explanation = r.Explanation ?? ""
            }).ToList() ?? new List<ChorusSearchResult>();

            return new IntelligentSearchResult
            {
                SearchResults = chorusResults,
                AiAnalysis = result?.AiAnalysis ?? "",
                HasAiAnalysis = !string.IsNullOrEmpty(result?.AiAnalysis),
                QueryUnderstanding = result?.QueryUnderstanding ?? query
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during intelligent LangChain search for query: {Query}", query);
            return new IntelligentSearchResult
            {
                SearchResults = new List<ChorusSearchResult>(),
                AiAnalysis = "Sorry, I encountered an error while searching. Please try again.",
                HasAiAnalysis = false,
                QueryUnderstanding = "Error occurred"
            };
        }
    }

    public async IAsyncEnumerable<string> SearchWithIntelligenceStreamingAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing streaming intelligent LangChain search for query: {Query}", query);

        var request = new
        {
            query = query,
            k = maxResults,
            stream = true
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}/search_intelligent_stream", content, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("LangChain streaming search failed with status: {StatusCode}", response.StatusCode);
            yield return JsonSerializer.Serialize(new { type = "error", message = "Search failed. Please try again." });
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (!string.IsNullOrEmpty(line) && line.StartsWith("data: "))
            {
                var data = line.Substring(6); // Remove "data: " prefix
                if (data != "[DONE]")
                {
                    yield return data;
                }
            }
        }
    }

    // DTOs for LangChain service responses
    private class LangChainSearchResult
    {
        public string? Id { get; set; }
        public string? Text { get; set; }
        public float? Score { get; set; }
        public string? Explanation { get; set; }
        public LangChainMetadata? Metadata { get; set; }
    }

    private class LangChainIntelligentResult
    {
        [JsonPropertyName("search_results")]
        public List<LangChainSearchResult>? SearchResults { get; set; }
        
        [JsonPropertyName("ai_analysis")]
        public string? AiAnalysis { get; set; }
        
        [JsonPropertyName("query_understanding")]
        public string? QueryUnderstanding { get; set; }
    }

    private class LangChainMetadata
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("key")]
        public int Key { get; set; }
        
        [JsonPropertyName("type")]
        public int Type { get; set; }
        
        [JsonPropertyName("timeSignature")]
        public int TimeSignature { get; set; }
    }
} 