using CHAP2.Application.Models;
using CHAP2.Domain.Entities;

namespace CHAP2.Application.Interfaces;

public interface IBibleVerseSearchRepository
{
    /// <summary>
    /// Returns the top <paramref name="max"/> matches ranked by relevance.
    /// Internally consumes <see cref="StreamSearchAsync"/>, so the ranking
    /// rules stay in one place.
    /// </summary>
    Task<IReadOnlyList<BibleVerse>> SearchVersesAsync(string query, int max, CancellationToken cancellationToken = default);

    /// <summary>
    /// Yields every matching verse paired with its relevance score in
    /// canonical (book ordinal -> chapter -> verse) order, as soon as
    /// each one is found. Consumers stream into the UI; final ranking
    /// happens client-side because higher-scored matches may arrive
    /// later in canonical order than lower-scored ones.
    /// </summary>
    IAsyncEnumerable<BibleVerseSearchHit> StreamSearchAsync(string query, CancellationToken cancellationToken = default);
}
