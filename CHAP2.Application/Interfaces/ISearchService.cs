using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;

namespace CHAP2.Application.Interfaces;

public interface ISearchService
{
    Task<IReadOnlyList<Chorus>> SearchByNameAsync(string searchTerm, SearchMode searchMode = SearchMode.Contains, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Chorus>> SearchByTextAsync(string searchTerm, SearchMode searchMode = SearchMode.Contains, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Chorus>> SearchByKeyAsync(string searchTerm, SearchMode searchMode = SearchMode.Contains, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Chorus>> SearchAllAsync(string searchTerm, SearchMode searchMode = SearchMode.Contains, CancellationToken cancellationToken = default);
    void InvalidateCache();
}

public enum SearchMode
{
    Exact,      // Exact match (case-insensitive)
    Contains,    // Contains search (case-insensitive)
    Regex        // Regular expression search
} 