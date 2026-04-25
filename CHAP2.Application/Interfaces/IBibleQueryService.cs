using CHAP2.Domain.Entities;
using CHAP2.Domain.ValueObjects;

namespace CHAP2.Application.Interfaces;

public interface IBibleQueryService
{
    Task<IReadOnlyList<BibleBook>> GetBooksAsync(CancellationToken cancellationToken = default);
    Task<BibleChapter?> GetChapterAsync(string bookId, int chapter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BibleVerse>> SearchAsync(string query, int max, CancellationToken cancellationToken = default);
    Task<BibleReference?> ResolveReferenceAsync(string input, CancellationToken cancellationToken = default);
}
