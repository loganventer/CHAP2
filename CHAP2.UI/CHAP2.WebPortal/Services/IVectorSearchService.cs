using CHAP2.WebPortal.DTOs;

namespace CHAP2.WebPortal.Services;

public interface IVectorSearchService
{
    Task<List<ChorusSearchResult>> SearchSimilarAsync(string query, int maxResults = 5, CancellationToken cancellationToken = default);
    Task<List<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<List<ChorusSearchResult>> GetAllChorusesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a chorus from the vector database by its ID
    /// </summary>
    Task<bool> DeleteAsync(string chorusId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts (inserts or updates) a chorus in the vector database
    /// </summary>
    Task<bool> UpsertAsync(ChorusSearchResult chorus, CancellationToken cancellationToken = default);
} 