using CHAP2.Domain.Entities;

namespace CHAP2.Application.Interfaces;

public interface IBibleBookRepository
{
    Task<IReadOnlyList<BibleBook>> GetAllBooksAsync(CancellationToken cancellationToken = default);
    Task<BibleBook?> GetBookByIdAsync(string bookId, CancellationToken cancellationToken = default);
    Task<BibleBook?> GetBookByNameAsync(string name, CancellationToken cancellationToken = default);
}
