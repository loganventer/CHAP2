using CHAP2.Common.Models;

namespace CHAP2.Common.Interfaces;

public interface ISearchService
{
    /// <summary>
    /// Search choruses by name with different matching strategies
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <param name="searchMode">The search mode (exact, contains, regex)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching choruses</returns>
    Task<IReadOnlyList<Chorus>> SearchByNameAsync(string searchTerm, SearchMode searchMode = SearchMode.Contains, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Search choruses by text content with different matching strategies
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <param name="searchMode">The search mode (exact, contains, regex)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching choruses</returns>
    Task<IReadOnlyList<Chorus>> SearchByTextAsync(string searchTerm, SearchMode searchMode = SearchMode.Contains, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Search choruses by musical key with different matching strategies
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <param name="searchMode">The search mode (exact, contains, regex)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching choruses</returns>
    Task<IReadOnlyList<Chorus>> SearchByKeyAsync(string searchTerm, SearchMode searchMode = SearchMode.Contains, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Comprehensive search across both name and text content
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <param name="searchMode">The search mode (exact, contains, regex)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching choruses</returns>
    Task<IReadOnlyList<Chorus>> SearchAllAsync(string searchTerm, SearchMode searchMode = SearchMode.Contains, CancellationToken cancellationToken = default);
}

public enum SearchMode
{
    Exact,      // Exact match (case-insensitive)
    Contains,    // Contains search (case-insensitive)
    Regex        // Regular expression search
} 