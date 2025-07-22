using CHAP2.Console.Prompt.DTOs;

namespace CHAP2.Console.Prompt.Services;

public interface IVectorSearchService
{
    Task<List<ChorusSearchResult>> SearchSimilarAsync(string query, int maxResults = 5);
    Task<List<float>> GenerateEmbeddingAsync(string text);
} 