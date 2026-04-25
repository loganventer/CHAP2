using CHAP2.Shared.DTOs;

namespace CHAP2.WebPortal.Interfaces;

public interface IBibleApiService
{
    Task<List<BibleBookDto>> GetBooksAsync(CancellationToken cancellationToken = default);
    Task<BibleChapterDto?> GetChapterAsync(string bookId, int chapter, CancellationToken cancellationToken = default);
    Task<BibleSearchResponseDto> SearchAsync(string query, int max = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Open a streaming search against the API and copy the raw SSE
    /// bytes into <paramref name="destination"/> as they arrive. Used by
    /// the WebPortal's SSE proxy endpoint to forward events to the
    /// browser without buffering.
    /// </summary>
    Task StreamSearchAsync(string query, Stream destination, CancellationToken cancellationToken = default);

    Task<BibleReferenceDto?> ResolveReferenceAsync(string reference, CancellationToken cancellationToken = default);
}
