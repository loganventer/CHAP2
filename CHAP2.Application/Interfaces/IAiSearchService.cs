namespace CHAP2.Application.Interfaces;

public interface IAiSearchService
{
    Task<List<string>> GenerateSearchTermsAsync(string query, CancellationToken cancellationToken = default);
    Task<string> AnalyzeSearchContextAsync(string query, List<string> searchTerms, CancellationToken cancellationToken = default);
} 