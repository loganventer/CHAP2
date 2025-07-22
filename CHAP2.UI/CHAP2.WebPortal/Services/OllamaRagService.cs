using CHAP2.WebPortal.Services;
using CHAP2.WebPortal.DTOs;
using Microsoft.Extensions.Logging;

namespace CHAP2.WebPortal.Services;

public interface IOllamaRagService
{
    Task<string> SearchWithRagAsync(string query, int maxResults = 5);
    IAsyncEnumerable<string> SearchWithRagStreamingAsync(string query, int maxResults = 5);
}

public class OllamaRagService : IOllamaRagService
{
    private readonly IVectorSearchService _vectorSearchService;
    private readonly IOllamaService _ollamaService;
    private readonly ILogger<OllamaRagService> _logger;

    public OllamaRagService(
        IVectorSearchService vectorSearchService,
        IOllamaService ollamaService,
        ILogger<OllamaRagService> logger)
    {
        _vectorSearchService = vectorSearchService;
        _ollamaService = ollamaService;
        _logger = logger;
    }

    public async Task<string> SearchWithRagAsync(string query, int maxResults = 5)
    {
        try
        {
            _logger.LogInformation("Performing RAG search for query: {Query}", query);

            // Step 1: Search vector store
            var searchResults = await _vectorSearchService.SearchSimilarAsync(query, maxResults);
            _logger.LogInformation("Vector search found {Count} results for query: {Query}", searchResults.Count, query);
            
            foreach (var result in searchResults)
            {
                _logger.LogDebug("Search result: ID={Id}, Name={Name}, Score={Score}", result.Id, result.Name, result.Score);
            }
            
            if (!searchResults.Any())
            {
                _logger.LogWarning("No vector search results found for query: {Query}", query);
                return "I couldn't find any relevant choruses for your query. Please try a different search term.";
            }

            // Step 2: Create prompt with embedded context
            var prompt = CreateRagPrompt(query, searchResults);
            _logger.LogInformation("Sending prompt to Ollama: {Prompt}", prompt);
            
            // Step 3: Generate response using Ollama (without context parameter)
            var response = await _ollamaService.GenerateResponseAsync(prompt);
            _logger.LogInformation("Raw AI response: {Response}", response);
            
            _logger.LogInformation("RAG search completed for query: {Query}", query);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during RAG search for query: {Query}", query);
            return "Sorry, I encountered an error while searching. Please try again.";
        }
    }

    public async IAsyncEnumerable<string> SearchWithRagStreamingAsync(string query, int maxResults = 5)
    {
        _logger.LogInformation("Performing streaming RAG search for query: {Query}", query);

        // Step 1: Search vector store
        var searchResults = await _vectorSearchService.SearchSimilarAsync(query, maxResults);
        _logger.LogInformation("Vector search found {Count} results for query: {Query}", searchResults.Count, query);
        
        foreach (var result in searchResults)
        {
            _logger.LogDebug("Search result: ID={Id}, Name={Name}, Score={Score}", result.Id, result.Name, result.Score);
        }
        
        if (!searchResults.Any())
        {
            _logger.LogWarning("No vector search results found for query: {Query}", query);
            yield return "I couldn't find any relevant choruses for your query. Please try a different search term.";
            yield break;
        }

        // Step 2: Create prompt with embedded context
        var prompt = CreateRagPrompt(query, searchResults);
        _logger.LogInformation("Sending streaming prompt to Ollama: {Prompt}", prompt);
        
        // Step 3: Generate streaming response using Ollama (without context parameter)
        var fullResponse = new System.Text.StringBuilder();
        await foreach (var chunk in _ollamaService.GenerateStreamingResponseAsync(prompt))
        {
            fullResponse.Append(chunk);
            yield return chunk;
        }
        
        _logger.LogInformation("Complete streaming AI response: {Response}", fullResponse.ToString());
    
        _logger.LogInformation("Streaming RAG search completed for query: {Query}", query);
    }

    private string CreateRagPrompt(string query, List<ChorusSearchResult> results)
    {
        var contextBuilder = new System.Text.StringBuilder();
        contextBuilder.AppendLine("Relevant choruses:");
        contextBuilder.AppendLine();

        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            contextBuilder.AppendLine($"{i + 1}. **{result.Name}** (Score: {result.Score:F3})");
            contextBuilder.AppendLine($"   Key: {result.Key}, Type: {result.Type}");
            contextBuilder.AppendLine($"   Text: {result.ChorusText}");
            contextBuilder.AppendLine();
        }

        return $@"You are an expert on religious choruses and hymns. Provide a concise, accurate response.

User Question: ""{query}""

Found {results.Count} relevant choruses. Provide:

1. **Direct Answer**: Brief response to the user's question
2. **Top Matches**: 2-3 most relevant choruses with brief explanations
3. **Key Themes**: Main religious/spiritual themes found

Keep response concise (2-3 paragraphs). If query is in Afrikaans, respond in Afrikaans.
Only mention choruses from the context provided.

Context:
{contextBuilder}

Provide a focused, helpful response:";
    }
} 