using CHAP2.Shared.DTOs;

namespace CHAP2.WebPortal.Interfaces;

public interface IBibleApiService
{
    Task<List<BibleBookDto>> GetBooksAsync(CancellationToken cancellationToken = default);
    Task<BibleChapterDto?> GetChapterAsync(string bookId, int chapter, CancellationToken cancellationToken = default);
    Task<BibleSearchResponseDto> SearchAsync(string query, int max = 100, CancellationToken cancellationToken = default);
    Task<BibleReferenceDto?> ResolveReferenceAsync(string reference, CancellationToken cancellationToken = default);
}
