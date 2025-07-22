using CHAP2.WebPortal.DTOs;

namespace CHAP2.WebPortal.Services;

public interface IVectorSearchService
{
    Task<List<ChorusSearchResult>> SearchSimilarAsync(string query, int maxResults = 5, CancellationToken cancellationToken = default);
    Task<List<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<List<ChorusSearchResult>> GetAllChorusesAsync(CancellationToken cancellationToken = default);
} 