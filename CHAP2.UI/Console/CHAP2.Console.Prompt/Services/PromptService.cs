using CHAP2.Console.Prompt.Configuration;
using CHAP2.Console.Prompt.DTOs;
using Microsoft.Extensions.Logging;

namespace CHAP2.Console.Prompt.Services;

public class PromptService : IPromptService
{
    private readonly IVectorSearchService _vectorSearchService;
    private readonly IOllamaService _ollamaService;
    private readonly PromptSettings _settings;
    private readonly ILogger<PromptService> _logger;

    public PromptService(
        IVectorSearchService vectorSearchService,
        IOllamaService ollamaService,
        PromptSettings settings,
        ILogger<PromptService> logger)
    {
        _vectorSearchService = vectorSearchService;
        _ollamaService = ollamaService;
        _settings = settings;
        _logger = logger;
    }

    public async Task<string> AskQuestionAsync(string question)
    {
        try
        {
            _logger.LogInformation("Processing question: {Question}", question);

            // Search for relevant choruses
            var searchResults = await _vectorSearchService.SearchSimilarAsync(question, _settings.MaxResults);
            
            if (!searchResults.Any())
            {
                return "I couldn't find any relevant choruses for your question. Please try rephrasing your query.";
            }

            // Build context from search results
            var context = BuildContextFromResults(searchResults);
            
            // Create prompt with context
            var prompt = CreatePromptWithContext(question, context);
            
            // Generate response using Ollama
            var response = await _ollamaService.GenerateResponseAsync(prompt);
            
            _logger.LogInformation("Generated response for question: {Question}", question);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing question: {Question}", question);
            return $"Sorry, I encountered an error while processing your question: {ex.Message}";
        }
    }

    public async Task<List<ChorusSearchResult>> SearchChorusesAsync(string query)
    {
        try
        {
            _logger.LogInformation("Searching for choruses with query: {Query}", query);
            var results = await _vectorSearchService.SearchSimilarAsync(query, _settings.MaxResults);
            
            _logger.LogInformation("Found {Count} choruses for query: {Query}", results.Count, query);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for choruses with query: {Query}", query);
            throw;
        }
    }

    private static string BuildContextFromResults(List<ChorusSearchResult> results)
    {
        var contextBuilder = new System.Text.StringBuilder();
        contextBuilder.AppendLine("Here are some relevant choruses from the database:");
        contextBuilder.AppendLine();

        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            contextBuilder.AppendLine($"{i + 1}. **{result.Name}** (Score: {result.Score:F3})");
            contextBuilder.AppendLine($"   Key: {result.Key}, Type: {result.Type}, Time Signature: {result.TimeSignature}");
            contextBuilder.AppendLine($"   Text: {result.ChorusText}");
            contextBuilder.AppendLine($"   Created: {result.CreatedAt:yyyy-MM-dd}");
            contextBuilder.AppendLine();
        }

        return contextBuilder.ToString();
    }

    private static string CreatePromptWithContext(string question, string context)
    {
        return $@"You are a helpful assistant that answers questions about a collection of choruses. 
Use the following context to answer the user's question. If the context doesn't contain enough information, 
say so and suggest what additional information might be needed.

Context:
{context}

User Question: {question}

Please provide a helpful and accurate response based on the chorus information provided above.";
    }
} 