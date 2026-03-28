using CHAP2.WebPortal.DTOs;

namespace CHAP2.WebPortal.Services;

public interface IIntelligentSearchService
{
    Task<IntelligentSearchResult> SearchWithIntelligenceAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> SearchWithIntelligenceStreamingAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default);
    Task<List<ChorusSearchResult>> SearchAsync(string query, int maxResults = 10, CancellationToken cancellationToken = default);
    Task<string> GenerateAnalysisAsync(string query, List<ChorusSearchResult> results, CancellationToken cancellationToken = default);
}
