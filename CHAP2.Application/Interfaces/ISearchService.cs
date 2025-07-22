using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;

namespace CHAP2.Application.Interfaces;

public interface ISearchService
{
    Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
    Task<SearchResult> SearchWithAiAsync(SearchRequest request, CancellationToken cancellationToken = default);
}

public record SearchRequest(
    string Query,
    SearchMode Mode = SearchMode.Contains,
    SearchScope Scope = SearchScope.All,
    int MaxResults = 50,
    bool UseAi = false
);

public record SearchResult(
    IReadOnlyList<Chorus> Results,
    int TotalCount,
    string? Error = null,
    Dictionary<string, object>? Metadata = null
); 