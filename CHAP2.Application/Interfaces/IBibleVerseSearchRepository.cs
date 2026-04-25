using CHAP2.Domain.Entities;

namespace CHAP2.Application.Interfaces;

public interface IBibleVerseSearchRepository
{
    Task<IReadOnlyList<BibleVerse>> SearchVersesAsync(string query, int max, CancellationToken cancellationToken = default);
}
