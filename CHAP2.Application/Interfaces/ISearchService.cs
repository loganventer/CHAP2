namespace CHAP2.Application.Interfaces;

public interface ISearchService
{
    Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
    Task<SearchResult> SearchWithAiAsync(SearchRequest request, CancellationToken cancellationToken = default);
}
