using CHAP2.WebPortal.Services;
using CHAP2.WebPortal.DTOs;
using CHAP2.WebPortal.Interfaces;
using CHAP2.Shared.DTOs;
using CHAP2.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CHAP2.WebPortal.Services;

public interface ITraditionalSearchWithAiService
{
    Task<SearchWithAiResult> SearchWithAiAnalysisAsync(string query, int maxResults = 10, SearchFilters? filters = null);
    IAsyncEnumerable<string> SearchWithAiAnalysisStreamingAsync(string query, int maxResults = 10, SearchFilters? filters = null);
}

public class SearchFilters
{
    public string? Key { get; set; }
    public string? Type { get; set; }
    public string? TimeSignature { get; set; }
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

    public async Task<SearchWithAiResult> SearchWithAiAnalysisAsync(string query, int maxResults = 10, SearchFilters? filters = null)
    {
        try
        {
            _logger.LogInformation("Performing traditional search with AI analysis for query: {Query}", query);
            
            if (filters != null)
            {
                _logger.LogInformation("Applied filters - Key: {Key}, Type: {Type}, TimeSignature: {TimeSignature}", 
                    filters.Key, filters.Type, filters.TimeSignature);
            }

            // Step 1: Search the API for relevant choruses
            var searchResults = await _chorusApiService.SearchChorusesAsync(query, "Contains", "all");
            
            // Step 2: Apply filters if provided
            if (filters != null)
            {
                searchResults = ApplyFilters(searchResults, filters).ToList();
                _logger.LogInformation("After filtering, found {Count} choruses", searchResults.Count);
            }
            
            if (!searchResults.Any())
            {
                _logger.LogWarning("No search results found for query: {Query}", query);
                return new SearchWithAiResult
                {
                    SearchResults = new List<CHAP2.Domain.Entities.Chorus>(),
                    AiAnalysis = "I couldn't find any choruses matching your query and filters. Please try different search terms or adjust your filters.",
                    HasAiAnalysis = true
                };
            }

            // Step 3: Take the top results
            var topResults = searchResults.Take(maxResults).ToList();
            _logger.LogInformation("Found {Count} choruses for query: {Query}", topResults.Count, query);

            // Step 4: Create context from the actual results
            var context = CreateContextFromResults(topResults);
            
            // Step 5: Generate AI analysis
            var prompt = CreateAnalysisPrompt(query, context, filters);
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
            if (filters != null)
            {
                searchResults = ApplyFilters(searchResults, filters).ToList();
            }
            var topResults = searchResults.Take(maxResults).ToList();
            
            return new SearchWithAiResult
            {
                SearchResults = topResults,
                AiAnalysis = "Sorry, I encountered an error while analyzing the results. Here are the search results:",
                HasAiAnalysis = false
            };
        }
    }

    public async IAsyncEnumerable<string> SearchWithAiAnalysisStreamingAsync(string query, int maxResults = 10, SearchFilters? filters = null)
    {
        _logger.LogInformation("Performing streaming traditional search with AI analysis for query: {Query}", query);

        // Step 1: Search the API for relevant choruses
        var searchResults = await _chorusApiService.SearchChorusesAsync(query, "Contains", "all");
        
        // Step 2: Apply filters if provided
        if (filters != null)
        {
            searchResults = ApplyFilters(searchResults, filters).ToList();
            _logger.LogInformation("After filtering, found {Count} choruses", searchResults.Count);
        }
        
        if (!searchResults.Any())
        {
            _logger.LogWarning("No search results found for query: {Query}", query);
            yield return "I couldn't find any choruses matching your query and filters. Please try different search terms or adjust your filters.";
            yield break;
        }

        // Step 3: Take the top results
        var topResults = searchResults.Take(maxResults).ToList();
        _logger.LogInformation("Found {Count} choruses for query: {Query}", topResults.Count, query);

        // Step 4: Create context from the actual results
        var context = CreateContextFromResults(topResults);
        
        // Step 5: Generate streaming AI analysis
        var prompt = CreateAnalysisPrompt(query, context, filters);
        _logger.LogInformation("Sending streaming analysis prompt to Ollama: {Prompt}", prompt);
        
        var fullResponse = new System.Text.StringBuilder();
        await foreach (var chunk in _ollamaService.GenerateStreamingResponseAsync(prompt))
        {
            fullResponse.Append(chunk);
            yield return chunk;
        }
        
        _logger.LogInformation("Complete streaming AI analysis: {Response}", fullResponse.ToString());
    }

    private IEnumerable<CHAP2.Domain.Entities.Chorus> ApplyFilters(IEnumerable<CHAP2.Domain.Entities.Chorus> results, SearchFilters filters)
    {
        var filteredResults = results;

        // Apply key filter
        if (!string.IsNullOrEmpty(filters.Key))
        {
            if (int.TryParse(filters.Key, out int keyValue))
            {
                var musicalKey = (MusicalKey)keyValue;
                filteredResults = filteredResults.Where(c => c.Key == musicalKey);
            }
        }

        // Apply type filter
        if (!string.IsNullOrEmpty(filters.Type))
        {
            if (int.TryParse(filters.Type, out int typeValue))
            {
                var chorusType = (ChorusType)typeValue;
                filteredResults = filteredResults.Where(c => c.Type == chorusType);
            }
        }

        // Apply time signature filter
        if (!string.IsNullOrEmpty(filters.TimeSignature))
        {
            if (int.TryParse(filters.TimeSignature, out int timeSignatureValue))
            {
                var timeSignature = (TimeSignature)timeSignatureValue;
                filteredResults = filteredResults.Where(c => c.TimeSignature == timeSignature);
            }
        }

        return filteredResults;
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

    private string CreateAnalysisPrompt(string query, string context, SearchFilters? filters = null)
    {
        var filterInfo = "";
        if (filters != null)
        {
            var filterDetails = new List<string>();
            
            if (!string.IsNullOrEmpty(filters.Key))
            {
                var keyNames = new Dictionary<string, string>
                {
                    {"1", "C"}, {"2", "C#"}, {"3", "D"}, {"4", "D#"}, {"5", "E"}, {"6", "F"}, {"7", "F#"}, {"8", "G"}, {"9", "G#"}, {"10", "A"}, {"11", "A#"}, {"12", "B"},
                    {"13", "C♭"}, {"14", "D♭"}, {"15", "E♭"}, {"16", "F♭"}, {"17", "G♭"}, {"18", "A♭"}, {"19", "B♭"}
                };
                if (keyNames.ContainsKey(filters.Key))
                {
                    filterDetails.Add($"Key: {keyNames[filters.Key]}");
                }
            }
            
            if (!string.IsNullOrEmpty(filters.Type))
            {
                var typeNames = new Dictionary<string, string> { {"1", "Praise"}, {"2", "Worship"} };
                if (typeNames.ContainsKey(filters.Type))
                {
                    filterDetails.Add($"Type: {typeNames[filters.Type]}");
                }
            }
            
            if (!string.IsNullOrEmpty(filters.TimeSignature))
            {
                var timeNames = new Dictionary<string, string>
                {
                    {"1", "4/4"}, {"2", "3/4"}, {"3", "6/8"}, {"4", "2/4"}, {"5", "4/8"}, {"6", "3/8"}, {"7", "2/2"},
                    {"8", "5/4"}, {"9", "6/4"}, {"10", "9/8"}, {"11", "12/8"}, {"12", "7/4"}, {"13", "8/4"},
                    {"14", "5/8"}, {"15", "7/8"}, {"16", "8/8"}
                };
                if (timeNames.ContainsKey(filters.TimeSignature))
                {
                    filterDetails.Add($"Time Signature: {timeNames[filters.TimeSignature]}");
                }
            }
            
            if (filterDetails.Any())
            {
                filterInfo = $"\nApplied Filters: {string.Join(", ", filterDetails)}";
            }
        }

        return $@"You are a helpful assistant that analyzes search results from a collection of religious choruses and hymns. 
The user is asking: ""{query}""{filterInfo}

CRITICAL LANGUAGE REQUIREMENT:
1. Analyze the language of the user's query carefully
2. If the query contains Afrikaans words/phrases, respond in Afrikaans
3. If the query is purely English, respond in English
4. Match the exact language style and tone of the user's query
5. Use the same language for your response as the user's query

Use the following search results to provide a helpful analysis. Only mention choruses that are actually in the results provided.

Search Results:
{context}

User Query: {query}

Please provide a helpful analysis of these search results. Explain how the choruses relate to the query, mention specific choruses by name, and provide insights about the religious and musical aspects. Do not make up or hallucinate any chorus data - only use the information provided in the search results. Use the SAME language as the user's query.";
    }
} 