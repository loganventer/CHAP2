using CHAP2.WebPortal.DTOs;
using CHAP2.WebPortal.Interfaces;
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
    private readonly IChorusApiService _chorusApiService;
    private readonly ILogger<IntelligentSearchService> _logger;

    public IntelligentSearchService(
        IVectorSearchService vectorSearchService,
        IOllamaService ollamaService,
        IChorusApiService chorusApiService,
        ILogger<IntelligentSearchService> logger)
    {
        _vectorSearchService = vectorSearchService;
        _ollamaService = ollamaService;
        _chorusApiService = chorusApiService;
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

            // Step 4: Generate explanations for each result
            await GenerateExplanationsAsync(query, searchResults);

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
        _logger.LogInformation("Step 1: Generating query understanding...");
        var queryUnderstanding = await GenerateQueryUnderstandingAsync(query);
        _logger.LogInformation("Query understanding generated: {Understanding}", queryUnderstanding);
        yield return System.Text.Json.JsonSerializer.Serialize(new { type = "queryUnderstanding", queryUnderstanding });
        
        // Step 2: Search vector database
        _logger.LogInformation("Step 2: Searching vector database...");
        var searchResults = await _vectorSearchService.SearchSimilarAsync(queryUnderstanding, maxResults);
        _logger.LogInformation("Vector search completed. Found {Count} results", searchResults.Count);
        
        // Debug: Log what the vector search returned
        foreach (var result in searchResults)
        {
            _logger.LogInformation("Vector result: ID={Id}, Name={Name}, Score={Score}", result.Id, result.Name, result.Score);
            
            // Test if the ID is a valid GUID
            if (!Guid.TryParse(result.Id, out _))
            {
                _logger.LogWarning("Vector search returned invalid GUID: {Id}", result.Id);
            }
        }
        
        // Step 2.5: Fetch full details from API for each result
        _logger.LogInformation("Step 2.5: Fetching full chorus details from API...");
        
        // Test API connectivity first
        var isConnected = await _chorusApiService.TestConnectivityAsync();
        _logger.LogInformation("API connectivity test result: {IsConnected}", isConnected);
        
        var detailedResults = new List<ChorusSearchResult>();
        for (int i = 0; i < searchResults.Count; i++)
        {
            var detailedResult = await FetchChorusDetailsAsync(searchResults[i].Id);
            if (detailedResult != null)
            {
                detailedResult.Score = searchResults[i].Score; // Preserve the search score
                detailedResults.Add(detailedResult);
                _logger.LogInformation("Added detailed result: {Name}", detailedResult.Name);
            }
            else
            {
                // Fallback to vector search result if API call fails
                _logger.LogWarning("API fetch failed for ID {Id}, using vector result: {Name}", searchResults[i].Id, searchResults[i].Name);
                detailedResults.Add(searchResults[i]);
            }
        }
        
        yield return System.Text.Json.JsonSerializer.Serialize(new { type = "searchResults", searchResults = detailedResults });
        
        if (!detailedResults.Any())
        {
            _logger.LogInformation("No search results found, sending error message");
            yield return System.Text.Json.JsonSerializer.Serialize(new { type = "error", message = "I couldn't find any choruses matching your query. Please try different search terms." });
            yield break;
        }

        // Step 3: Generate explanations for each result individually
        _logger.LogInformation("Step 3: Generating explanations for {Count} results", detailedResults.Count);
        for (int i = 0; i < detailedResults.Count; i++)
        {
            _logger.LogInformation("Generating explanation {Index}/{Total} for chorus: {Name}", i + 1, detailedResults.Count, detailedResults[i].Name);
            var explanation = await GenerateSingleExplanationAsync(query, detailedResults[i]);
            yield return System.Text.Json.JsonSerializer.Serialize(new { type = "explanation", index = i, explanation });
        }

        // Step 4: Generate AI analysis
        _logger.LogInformation("Step 4: Generating AI analysis...");
        var aiAnalysis = await GenerateAnalysisAsync(query, detailedResults);
        yield return System.Text.Json.JsonSerializer.Serialize(new { type = "aiAnalysis", analysis = aiAnalysis });

        // Step 5: Complete
        _logger.LogInformation("Step 5: Streaming search completed successfully");
        yield return System.Text.Json.JsonSerializer.Serialize(new { type = "complete" });
    }

    private async Task<string> GenerateQueryUnderstandingAsync(string query)
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

        var response = await _ollamaService.GenerateResponseAsync(prompt);
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

    private async Task<string> GenerateAnalysisAsync(string query, List<ChorusSearchResult> results)
    {
        var prompt = CreateAnalysisPrompt(query, results);
        return await _ollamaService.GenerateResponseAsync(prompt);
    }

    private async Task GenerateExplanationsAsync(string query, List<ChorusSearchResult> results)
    {
        var prompt = CreateExplanationPrompt(query, results);
        var response = await _ollamaService.GenerateResponseAsync(prompt);
        
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
        promptBuilder.AppendLine("IMPORTANT: Analyze the language of each chorus text and provide your explanation in the SAME language.");
        promptBuilder.AppendLine("If the chorus text is in Afrikaans, respond in Afrikaans. If it's in English, respond in English.");
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

        promptBuilder.AppendLine("Explanations (one per line, no numbering, just the relevant aspect):");

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

    private async Task<string> GenerateSingleExplanationAsync(string query, ChorusSearchResult result)
    {
        var prompt = $@"You are an expert at explaining religious choruses.

Consider the search context when explaining this chorus, but do NOT mention the search query in your response.

IMPORTANT: Analyze the language of the chorus text below and provide your explanation in the SAME language.
If the chorus text is in Afrikaans, respond in Afrikaans. If it's in English, respond in English.

Chorus to explain:
**{result.Name}**
Text: {result.ChorusText.Substring(0, Math.Min(150, result.ChorusText.Length))}...

Provide a brief explanation of this chorus's most relevant aspect given the search context. Focus on the single most important connection. Keep it to 1 sentence maximum.

Explanation:";

        var response = await _ollamaService.GenerateResponseAsync(prompt);
        return response.Trim();
    }
} 