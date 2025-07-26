using CHAP2.WebPortal.DTOs;
using CHAP2.WebPortal.Interfaces;
using Microsoft.Extensions.Logging;

namespace CHAP2.WebPortal.Services;

public interface IIntelligentSearchService
{
    Task<IntelligentSearchResult> SearchWithIntelligenceAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> SearchWithIntelligenceStreamingAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default);
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
    private readonly ILangChainSearchService _langChainSearchService;
    private readonly IVectorSearchService _vectorSearchService;
    private readonly IOllamaService _ollamaService;
    private readonly IChorusApiService _chorusApiService;
    private readonly ILogger<IntelligentSearchService> _logger;

    public IntelligentSearchService(
        ILangChainSearchService langChainSearchService,
        IVectorSearchService vectorSearchService,
        IOllamaService ollamaService,
        IChorusApiService chorusApiService,
        ILogger<IntelligentSearchService> logger)
    {
        _langChainSearchService = langChainSearchService;
        _vectorSearchService = vectorSearchService;
        _ollamaService = ollamaService;
        _chorusApiService = chorusApiService;
        _logger = logger;
    }

    public async Task<IntelligentSearchResult> SearchWithIntelligenceAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Performing intelligent search for query: {Query}", query);

            // Use LangChain service for intelligent search
            var result = await _langChainSearchService.SearchWithIntelligenceAsync(query, maxResults, cancellationToken);
            
            _logger.LogInformation("LangChain search returned {Count} results", result.SearchResults.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during intelligent search for query: {Query}", query);
            
            // Fallback to original implementation if LangChain fails
            try
            {
                _logger.LogInformation("Falling back to original search implementation");

            // Step 1: Use LLM to understand the query
            var queryUnderstanding = await GenerateQueryUnderstandingAsync(query, cancellationToken);
            _logger.LogInformation("Query understanding: {Understanding}", queryUnderstanding);

            // Step 2: Search vector database
            var searchResults = await _vectorSearchService.SearchSimilarAsync(queryUnderstanding, maxResults, cancellationToken);
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
            var aiAnalysis = await GenerateAnalysisAsync(query, searchResults, cancellationToken);

            // Step 4: Generate explanations for each result
            await GenerateExplanationsAsync(query, searchResults, cancellationToken);

            return new IntelligentSearchResult
            {
                SearchResults = searchResults,
                AiAnalysis = aiAnalysis,
                HasAiAnalysis = true,
                QueryUnderstanding = queryUnderstanding
            };
        }
            catch (Exception fallbackEx)
        {
                _logger.LogError(fallbackEx, "Fallback search also failed for query: {Query}", query);
            return new IntelligentSearchResult
            {
                SearchResults = new List<ChorusSearchResult>(),
                AiAnalysis = "Sorry, I encountered an error while searching. Please try again.",
                HasAiAnalysis = false,
                QueryUnderstanding = "Error occurred"
            };
            }
        }
    }

    public async IAsyncEnumerable<string> SearchWithIntelligenceStreamingAsync(string query, int maxResults = 10, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing streaming intelligent search for query: {Query}", query);

        // Try LangChain service first
        var langChainEnumerator = _langChainSearchService.SearchWithIntelligenceStreamingAsync(query, maxResults, cancellationToken).GetAsyncEnumerator();
        
        var langChainResults = new List<string>();
        var langChainSuccess = false;
        
        try
        {
            while (await langChainEnumerator.MoveNextAsync())
            {
                // Only check cancellation token at yield points, not during processing
                langChainResults.Add(langChainEnumerator.Current);
            }
            langChainSuccess = true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("LangChain streaming search was cancelled for query: {Query}", query);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during streaming LangChain search for query: {Query}", query);
        }
        finally
        {
            await langChainEnumerator.DisposeAsync();
        }

        // Yield LangChain results if successful
        if (langChainSuccess && langChainResults.Any())
        {
            foreach (var result in langChainResults)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return result;
            }
        }
        else
        {
            // If LangChain failed, fallback to original implementation
            _logger.LogInformation("Falling back to original streaming search implementation");
            await foreach (var result in FallbackStreamingSearchAsync(query, maxResults, cancellationToken))
            {
                yield return result;
            }
        }
    }

    private async IAsyncEnumerable<string> FallbackStreamingSearchAsync(string query, int maxResults, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Step 1: Use LLM to understand the query
        _logger.LogInformation("Step 1: Generating query understanding...");
        var queryUnderstanding = await GenerateQueryUnderstandingAsync(query, cancellationToken);
        _logger.LogInformation("Query understanding generated: {Understanding}", queryUnderstanding);
        yield return System.Text.Json.JsonSerializer.Serialize(new { type = "queryUnderstanding", queryUnderstanding });

        // Step 2: Search vector database
        _logger.LogInformation("Step 2: Searching vector database...");
        
        List<ChorusSearchResult> searchResults;
        bool vectorSearchFailed = false;
        string vectorSearchError = "";
        
        try
        {
            searchResults = await _vectorSearchService.SearchSimilarAsync(queryUnderstanding, maxResults, cancellationToken);
            _logger.LogInformation("Vector search found {Count} results for query: {Query}", searchResults.Count, query);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Vector search was cancelled for query: {Query}", query);
            throw;
        }
        catch (Exception vectorEx)
        {
            _logger.LogError(vectorEx, "Error during vector search for query: {Query}", query);
            vectorSearchFailed = true;
            vectorSearchError = "Vector search failed. Please try again.";
            searchResults = new List<ChorusSearchResult>();
        }

        if (vectorSearchFailed)
        {
            yield return System.Text.Json.JsonSerializer.Serialize(new { type = "error", message = vectorSearchError });
            yield break;
        }

        if (!searchResults.Any())
        {
            _logger.LogInformation("No search results found, sending error message");
            yield return System.Text.Json.JsonSerializer.Serialize(new { type = "error", message = "I couldn't find any choruses matching your query. Please try different search terms." });
            yield break;
        }

        // Step 3: Fetch detailed results
        _logger.LogInformation("Step 3: Fetching detailed results for {Count} search results", searchResults.Count);
        var detailedResults = new List<ChorusSearchResult>();
        for (int i = 0; i < searchResults.Count; i++)
        {
            _logger.LogInformation("Fetching details {Index}/{Total} for chorus: {Name}", i + 1, searchResults.Count, searchResults[i].Name);
            var detailedResult = await FetchChorusDetailsAsync(searchResults[i].Id);
            if (detailedResult != null)
            {
                detailedResult.Score = searchResults[i].Score;
                detailedResults.Add(detailedResult);
            }
        }
        
        yield return System.Text.Json.JsonSerializer.Serialize(new { type = "searchResults", searchResults = detailedResults });
        
        if (!detailedResults.Any())
        {
            _logger.LogInformation("No detailed results found, sending error message");
            yield return System.Text.Json.JsonSerializer.Serialize(new { type = "error", message = "I couldn't find any choruses matching your query. Please try different search terms." });
            yield break;
        }

        // Step 4: Generate explanations for each result individually
        _logger.LogInformation("Step 4: Generating explanations for {Count} results", detailedResults.Count);
        for (int i = 0; i < detailedResults.Count; i++)
        {
            _logger.LogInformation("Generating explanation {Index}/{Total} for chorus: {Name}", i + 1, detailedResults.Count, detailedResults[i].Name);
            var explanation = await GenerateSingleExplanationAsync(query, detailedResults[i], cancellationToken);
            yield return System.Text.Json.JsonSerializer.Serialize(new { type = "explanation", index = i, explanation });
        }

        // Step 5: Generate AI analysis
        _logger.LogInformation("Step 5: Generating AI analysis...");
        var aiAnalysis = await GenerateAnalysisAsync(query, detailedResults, cancellationToken);
        yield return System.Text.Json.JsonSerializer.Serialize(new { type = "aiAnalysis", analysis = aiAnalysis });

        // Step 6: Complete
        _logger.LogInformation("Step 6: Streaming search completed successfully");
        yield return System.Text.Json.JsonSerializer.Serialize(new { type = "complete" });
    }

    private async Task<string> GenerateQueryUnderstandingAsync(string query, CancellationToken cancellationToken = default)
    {
        var prompt = @"You are an expert at analyzing religious chorus queries and generating focused search terms.

The user is asking: """ + query + @"""

Your task is to generate SINGLE-WORD search terms that will find the MOST RELEVANT choruses for this SPECIFIC query. Focus on:

1. **Exact matches**: Single words that directly match the user's query
2. **Core concepts**: Single words representing the main themes and ideas
3. **Synonyms**: Single words that express the same concept
4. **Related terms**: Single words for closely related concepts
5. **Language variations**: Single words in both Afrikaans and English if applicable

IMPORTANT LANGUAGE REQUIREMENTS:
- ONLY return search terms in Afrikaans or English
- If the user's query is in Afrikaans, return Afrikaans terms
- If the user's query is in English, return English terms
- If the user's query is mixed, return terms in both languages
- NEVER return terms in other languages
- NEVER return terms in Latin, Hebrew, or other religious languages
- ONLY use Afrikaans and English

CRITICAL: ONLY SINGLE WORDS
- Generate ONLY single words, not phrases or multi-word terms
- Each term should be one word maximum
- No compound words, no phrases, no multi-word expressions
- Examples: ""Jesus"" not ""Jesus Christ"", ""genade"" not ""God se genade""

IMPORTANT: 
- Focus on the user's SPECIFIC query, not general religious terms
- Generate single words that will find choruses that actually match what the user is looking for
- Avoid generic terms that would return irrelevant results
- If the user asks for ""Jesus"", focus on Jesus-related single words, not general worship terms
- If the user asks for ""grace"", focus on grace-related single words, not general salvation terms

Generate focused single-word search terms that will find the most relevant choruses for this specific query. Separate terms with commas, no explanations:";

        var response = await _ollamaService.GenerateResponseAsync(prompt, cancellationToken);
        var searchTerms = response.Trim();
        
        // Parse the response and take all terms, ensuring they are single words
        var terms = searchTerms.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(t => t.Trim())
                              .Where(t => !string.IsNullOrWhiteSpace(t))
                              .Select(t => t.Split(' ')[0]) // Take only the first word of each term
                              .Where(t => !string.IsNullOrWhiteSpace(t))
                              .ToList();
        
        _logger.LogInformation("Generated {Count} single-word search terms: {Terms}", terms.Count, string.Join(", ", terms));
        return string.Join(", ", terms);
    }

    private async Task<string> GenerateAnalysisAsync(string query, List<ChorusSearchResult> results, CancellationToken cancellationToken = default)
    {
        var prompt = CreateAnalysisPrompt(query, results);
        return await _ollamaService.GenerateResponseAsync(prompt, cancellationToken);
    }

    private async Task GenerateExplanationsAsync(string query, List<ChorusSearchResult> results, CancellationToken cancellationToken = default)
    {
        var prompt = CreateExplanationPrompt(query, results);
        var response = await _ollamaService.GenerateResponseAsync(prompt, cancellationToken);
        
        // Parse the response and assign explanations
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < results.Count && i < lines.Length; i++)
        {
            var explanation = lines[i].Trim();
            // Remove numbering if present (e.g., "1. " or "1) ")
            if (explanation.StartsWith($"{i + 1}.") || explanation.StartsWith($"{i + 1})"))
            {
                explanation = explanation.Substring(explanation.IndexOf(' ') + 1).Trim();
            }
            results[i].Explanation = explanation;
        }
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
            contextBuilder.AppendLine($"   Key: {result.Key}, Type: {result.Type}");
            contextBuilder.AppendLine($"   Text: {result.ChorusText}");
            contextBuilder.AppendLine();
        }

        var prompt = $@"You are an expert analyst of religious choruses and hymns. Provide a concise, accurate analysis.

The user asked: ""{query}""

Found {results.Count} relevant choruses. Analyze:

1. **Relevance**: How well each chorus matches the query
2. **Themes**: Key religious/spiritual themes present
3. **Quality**: Which choruses are most suitable for the user's needs
4. **Recommendations**: Top 2-3 best matches with brief explanations

Keep your response concise (2-3 paragraphs max). If the query is in Afrikaans, respond in Afrikaans.

Context:
{contextBuilder}

Provide a focused analysis highlighting the most relevant choruses:";
        
        return prompt;
    }

    private string CreateExplanationPrompt(string query, List<ChorusSearchResult> results)
    {
        var promptBuilder = new System.Text.StringBuilder();
        promptBuilder.AppendLine("You are an expert at explaining religious choruses.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Consider the search context when explaining each chorus, but do NOT mention the search query in your response.");
        promptBuilder.AppendLine("For each chorus below, provide a brief explanation of its most relevant aspect given the search context.");
        promptBuilder.AppendLine("Focus on the single most important connection. Keep explanations concise (1 sentence maximum).");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("CRITICAL LANGUAGE REQUIREMENT:");
        promptBuilder.AppendLine("1. Analyze the language of each chorus text carefully");
        promptBuilder.AppendLine("2. If the chorus text contains Afrikaans words/phrases, respond in Afrikaans");
        promptBuilder.AppendLine("3. If the chorus text is purely English, respond in English");
        promptBuilder.AppendLine("4. Match the exact language style and tone of the chorus content");
        promptBuilder.AppendLine("5. Use the same language for the explanation as the chorus uses");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Choruses to explain:");
        promptBuilder.AppendLine();

        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            promptBuilder.AppendLine($"{i + 1}. **{result.Name}**");
            promptBuilder.AppendLine($"   Text: {result.ChorusText.Substring(0, Math.Min(200, result.ChorusText.Length))}...");
            promptBuilder.AppendLine();
        }

        promptBuilder.AppendLine("Explanations (one per line, no numbering, match the language of each chorus):");

        return promptBuilder.ToString();
    }

    private async Task<ChorusSearchResult?> FetchChorusDetailsAsync(string chorusId)
    {
        try
        {
            _logger.LogInformation("Fetching chorus details for ID: {ChorusId}", chorusId);
            
            // Use the chorus API service to get full details
            var chorusDetails = await _chorusApiService.GetChorusByIdAsync(chorusId);
            if (chorusDetails != null)
            {
                _logger.LogInformation("Successfully fetched chorus details: Name={Name}, Key={Key}, Type={Type}, TimeSignature={TimeSignature}", 
                    chorusDetails.Name, chorusDetails.Key, chorusDetails.Type, chorusDetails.TimeSignature);
                
                return new ChorusSearchResult
                {
                    Id = chorusDetails.Id.ToString(),
                    Name = chorusDetails.Name,
                    ChorusText = chorusDetails.ChorusText,
                    Key = (int)chorusDetails.Key,
                    Type = (int)chorusDetails.Type,
                    TimeSignature = (int)chorusDetails.TimeSignature,
                    CreatedAt = chorusDetails.CreatedAt,
                    Score = 0, // Will be set by caller
                    Explanation = string.Empty
                };
            }
            else
            {
                _logger.LogWarning("Chorus details returned null for ID: {ChorusId}", chorusId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch chorus details for ID: {ChorusId}", chorusId);
        }
        
        return null;
    }

    private async Task<string> GenerateSingleExplanationAsync(string query, ChorusSearchResult result, CancellationToken cancellationToken = default)
    {
        var prompt = $@"You are an expert at explaining religious choruses.

Consider the search context when explaining this chorus, but do NOT mention the search query in your response.

CRITICAL LANGUAGE REQUIREMENT:
1. Analyze the language of the chorus text below carefully
2. If the chorus text contains Afrikaans words/phrases, respond in Afrikaans
3. If the chorus text is purely English, respond in English
4. Match the exact language style and tone of the chorus content
5. Use the same language for the explanation as the chorus uses

Chorus to explain:
**{result.Name}**
Text: {result.ChorusText.Substring(0, Math.Min(150, result.ChorusText.Length))}...

Provide a brief explanation of this chorus's most relevant aspect given the search context. Focus on the single most important connection. Keep it to 1 sentence maximum. Use the SAME language as the chorus text.

Explanation:";

        var response = await _ollamaService.GenerateResponseAsync(prompt, cancellationToken);
        return response.Trim();
    }
} 