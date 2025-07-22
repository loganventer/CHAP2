using CHAP2.WebPortal.DTOs;

namespace CHAP2.WebPortal.Services;

public interface IVectorSearchService
{
    Task<List<ChorusSearchResult>> SearchSimilarAsync(string query, int maxResults = 5);
    Task<List<float>> GenerateEmbeddingAsync(string text);
    Task<List<ChorusSearchResult>> GetAllChorusesAsync();
} 