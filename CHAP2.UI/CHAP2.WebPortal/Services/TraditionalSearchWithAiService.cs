using CHAP2.WebPortal.Services;
using CHAP2.WebPortal.DTOs;
using CHAP2.WebPortal.Interfaces;
using CHAP2.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace CHAP2.WebPortal.Services;

public interface ITraditionalSearchWithAiService
{
    Task<SearchWithAiResult> SearchWithAiAnalysisAsync(string query, int maxResults = 10);
    IAsyncEnumerable<string> SearchWithAiAnalysisStreamingAsync(string query, int maxResults = 10);
}

public class SearchWithAiResult
{
    public List<CHAP2.Domain.Entities.Chorus> SearchResults { get; set; } = new();
    public string AiAnalysis { get; set; } = string.Empty;
    public bool HasAiAnalysis { get; set; } = false;
}

public class TraditionalSearchWithAiService : ITraditionalSearchWithAiService
{
    private readonly IChorusApiService _chorusApiService;
    private readonly IOllamaService _ollamaService;
    private readonly ILogger<TraditionalSearchWithAiService> _logger;

    public TraditionalSearchWithAiService(
        IChorusApiService chorusApiService,
        IOllamaService ollamaService,
        ILogger<TraditionalSearchWithAiService> logger)
    {
        _chorusApiService = chorusApiService;
        _ollamaService = ollamaService;
        _logger = logger;
    }

    public async Task<SearchWithAiResult> SearchWithAiAnalysisAsync(string query, int maxResults = 10)
    {
        try
        {
            _logger.LogInformation("Performing traditional search with AI analysis for query: {Query}", query);

            // Step 1: Search the API for relevant choruses
            var searchResults = await _chorusApiService.SearchChorusesAsync(query, "Contains", "all");
            
            if (!searchResults.Any())
            {
                _logger.LogWarning("No search results found for query: {Query}", query);
                return new SearchWithAiResult
                {
                    SearchResults = new List<CHAP2.Domain.Entities.Chorus>(),
                    AiAnalysis = "I couldn't find any choruses matching your query. Please try different search terms.",
                    HasAiAnalysis = true
                };
            }

            // Step 2: Take the top results
            var topResults = searchResults.Take(maxResults).ToList();
            _logger.LogInformation("Found {Count} choruses for query: {Query}", topResults.Count, query);

            // Step 3: Create context from the actual results
            var context = CreateContextFromResults(topResults);
            
            // Step 4: Generate AI analysis
            var prompt = CreateAnalysisPrompt(query, context);
            _logger.LogInformation("Sending analysis prompt to Ollama: {Prompt}", prompt);
            
            var aiResponse = await _ollamaService.GenerateResponseAsync(prompt);
            _logger.LogInformation("AI analysis response: {Response}", aiResponse);
            
            return new SearchWithAiResult
            {
                SearchResults = topResults,
                AiAnalysis = aiResponse,
                HasAiAnalysis = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during traditional search with AI analysis for query: {Query}", query);
            
            // Return search results even if AI fails
            var searchResults = await _chorusApiService.SearchChorusesAsync(query, "Contains", "all");
            var topResults = searchResults.Take(maxResults).ToList();
            
            return new SearchWithAiResult
            {
                SearchResults = topResults,
                AiAnalysis = "Sorry, I encountered an error while analyzing the results. Here are the search results:",
                HasAiAnalysis = false
            };
        }
    }

    public async IAsyncEnumerable<string> SearchWithAiAnalysisStreamingAsync(string query, int maxResults = 10)
    {
        _logger.LogInformation("Performing streaming traditional search with AI analysis for query: {Query}", query);

        // Step 1: Search the API for relevant choruses
        var searchResults = await _chorusApiService.SearchChorusesAsync(query, "Contains", "all");
        
        if (!searchResults.Any())
        {
            _logger.LogWarning("No search results found for query: {Query}", query);
            yield return "I couldn't find any choruses matching your query. Please try different search terms.";
            yield break;
        }

        // Step 2: Take the top results
        var topResults = searchResults.Take(maxResults).ToList();
        _logger.LogInformation("Found {Count} choruses for query: {Query}", topResults.Count, query);

        // Step 3: Create context from the actual results
        var context = CreateContextFromResults(topResults);
        
        // Step 4: Generate streaming AI analysis
        var prompt = CreateAnalysisPrompt(query, context);
        _logger.LogInformation("Sending streaming analysis prompt to Ollama: {Prompt}", prompt);
        
        var fullResponse = new System.Text.StringBuilder();
        await foreach (var chunk in _ollamaService.GenerateStreamingResponseAsync(prompt))
        {
            fullResponse.Append(chunk);
            yield return chunk;
        }
        
        _logger.LogInformation("Complete streaming AI analysis: {Response}", fullResponse.ToString());
    }

    private string CreateContextFromResults(List<CHAP2.Domain.Entities.Chorus> results)
    {
        var contextBuilder = new System.Text.StringBuilder();
        contextBuilder.AppendLine("Here are the choruses found in the database:");
        contextBuilder.AppendLine();

        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            contextBuilder.AppendLine($"{i + 1}. **{result.Name}**");
            contextBuilder.AppendLine($"   ID: {result.Id}");
            contextBuilder.AppendLine($"   Key: {result.Key}, Type: {result.Type}, Time Signature: {result.TimeSignature}");
            contextBuilder.AppendLine($"   Text: {result.ChorusText}");
            contextBuilder.AppendLine($"   Created: {result.CreatedAt:yyyy-MM-dd}");
            contextBuilder.AppendLine();
        }

        return contextBuilder.ToString();
    }

    private string CreateAnalysisPrompt(string query, string context)
    {
        return $@"You are a helpful assistant that analyzes search results from a collection of religious choruses and hymns. 
The user is asking: ""{query}""

Use the following search results to provide a helpful analysis. Only mention choruses that are actually in the results provided.
If the query is in Afrikaans, respond in Afrikaans. If it's in English, respond in English.

Search Results:
{context}

User Query: {query}

Please provide a helpful analysis of these search results. Explain how the choruses relate to the query, mention specific choruses by name, and provide insights about the religious and musical aspects. Do not make up or hallucinate any chorus data - only use the information provided in the search results.";
    }
} 