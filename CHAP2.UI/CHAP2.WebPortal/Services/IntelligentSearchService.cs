using CHAP2.WebPortal.DTOs;
using Microsoft.Extensions.Logging;

namespace CHAP2.WebPortal.Services;

public interface IIntelligentSearchService
{
    Task<IntelligentSearchResult> SearchWithIntelligenceAsync(string query, int maxResults = 10);
    IAsyncEnumerable<string> SearchWithIntelligenceStreamingAsync(string query, int maxResults = 10);
}

public class IntelligentSearchResult
{
    public List<ChorusSearchResult> SearchResults { get; set; } = new();
    public string AiAnalysis { get; set; } = string.Empty;
    public bool HasAiAnalysis { get; set; } = false;
    public string QueryUnderstanding { get; set; } = string.Empty;
}

public class IntelligentSearchService : IIntelligentSearchService
{
    private readonly IVectorSearchService _vectorSearchService;
    private readonly IOllamaService _ollamaService;
    private readonly ILogger<IntelligentSearchService> _logger;

    public IntelligentSearchService(
        IVectorSearchService vectorSearchService,
        IOllamaService ollamaService,
        ILogger<IntelligentSearchService> logger)
    {
        _vectorSearchService = vectorSearchService;
        _ollamaService = ollamaService;
        _logger = logger;
    }

    public async Task<IntelligentSearchResult> SearchWithIntelligenceAsync(string query, int maxResults = 10)
    {
        try
        {
            _logger.LogInformation("Performing intelligent search for query: {Query}", query);

            // Step 1: Use LLM to understand the query
            var queryUnderstanding = await GenerateQueryUnderstandingAsync(query);
            _logger.LogInformation("Query understanding: {Understanding}", queryUnderstanding);

            // Step 2: Search vector database
            var searchResults = await _vectorSearchService.SearchSimilarAsync(queryUnderstanding, maxResults);
            _logger.LogInformation("Found {Count} relevant choruses", searchResults.Count);

            if (!searchResults.Any())
            {
                return new IntelligentSearchResult
                {
                    SearchResults = new List<ChorusSearchResult>(),
                    AiAnalysis = "I couldn't find any choruses matching your query. Please try different search terms.",
                    HasAiAnalysis = false,
                    QueryUnderstanding = queryUnderstanding
                };
            }

            // Step 3: Generate AI analysis (without context parameter)
            var aiAnalysis = await GenerateAnalysisAsync(query, searchResults);

            return new IntelligentSearchResult
            {
                SearchResults = searchResults,
                AiAnalysis = aiAnalysis,
                HasAiAnalysis = true,
                QueryUnderstanding = queryUnderstanding
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during intelligent search for query: {Query}", query);
            return new IntelligentSearchResult
            {
                SearchResults = new List<ChorusSearchResult>(),
                AiAnalysis = "Sorry, I encountered an error while searching. Please try again.",
                HasAiAnalysis = false,
                QueryUnderstanding = "Error occurred"
            };
        }
    }

    public async IAsyncEnumerable<string> SearchWithIntelligenceStreamingAsync(string query, int maxResults = 10)
    {
        _logger.LogInformation("Performing streaming intelligent search for query: {Query}", query);

        // Step 1: Use LLM to understand the query
        var queryUnderstanding = await GenerateQueryUnderstandingAsync(query);
        yield return $"Understanding query: {queryUnderstanding}";
        
        // Step 2: Search vector database
        var searchResults = await _vectorSearchService.SearchSimilarAsync(queryUnderstanding, maxResults);
        yield return $"Found {searchResults.Count} relevant choruses";
        
        if (!searchResults.Any())
        {
            yield return "I couldn't find any choruses matching your query. Please try different search terms.";
            yield break;
        }

        // Step 3: Stream AI analysis (without context parameter)
        yield return "Analyzing results with AI...";
        var prompt = CreateAnalysisPrompt(query, searchResults);
        await foreach (var chunk in _ollamaService.GenerateStreamingResponseAsync(prompt))
        {
            yield return chunk;
        }
    }

    private async Task<string> GenerateQueryUnderstandingAsync(string query)
    {
        var prompt = "You are a helpful assistant that understands both Afrikaans and English queries about religious choruses and hymns.\n\n" +
                    $"The user is asking: \"{query}\"\n\n" +
                    "Your task is to:\n" +
                    "1. Understand the query (whether it's in Afrikaans or English)\n" +
                    "2. Generate search terms that would help find relevant choruses\n" +
                    "3. Consider both Afrikaans and English versions of concepts\n" +
                    "4. Focus on religious, musical, and thematic keywords\n\n" +
                    "For example:\n" +
                    "- \"vind koortjies oor die skepping\" → \"creation, skepping, God's work, nature, universe, Genesis\"\n" +
                    "- \"choruses about Jesus\" → \"Jesus, Christ, Savior, Lord, Messiah, redemption\"\n" +
                    "- \"praise songs\" → \"praise, worship, glory, hallelujah, amen, lof\"\n\n" +
                    "Generate a concise set of search terms (comma-separated) that would help find relevant choruses:";

        var response = await _ollamaService.GenerateResponseAsync(prompt);
        return response.Trim();
    }

    private async Task<string> GenerateAnalysisAsync(string query, List<ChorusSearchResult> results)
    {
        var prompt = CreateAnalysisPrompt(query, results);
        return await _ollamaService.GenerateResponseAsync(prompt);
    }

    private string CreateAnalysisPrompt(string query, List<ChorusSearchResult> results)
    {
        var contextBuilder = new System.Text.StringBuilder();
        contextBuilder.AppendLine("Here are the relevant choruses found:");
        contextBuilder.AppendLine();

        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            contextBuilder.AppendLine($"{i + 1}. **{result.Name}** (Score: {result.Score:F3})");
            contextBuilder.AppendLine($"   ID: {result.Id}");
            contextBuilder.AppendLine($"   Key: {result.Key}, Type: {result.Type}, Time Signature: {result.TimeSignature}");
            contextBuilder.AppendLine($"   Text: {result.ChorusText}");
            contextBuilder.AppendLine($"   Created: {result.CreatedAt:yyyy-MM-dd}");
            contextBuilder.AppendLine();
        }

        return $@"You are a helpful assistant that analyzes search results from a collection of religious choruses and hymns.

The user is asking: ""{query}""

I have found {results.Count} relevant choruses in the database. Please analyze these results and provide insights about:

1. How the choruses relate to the user's query
2. The religious and musical themes present
3. Any patterns or connections between the choruses
4. Specific recommendations based on the user's needs

If the query is in Afrikaans, respond in Afrikaans. If it's in English, respond in English.

Context:
{contextBuilder}

Please provide a helpful analysis of these search results. Mention specific choruses by name and explain their relevance to the query.";
    }
} 