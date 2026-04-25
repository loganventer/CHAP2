using CHAP2.Domain.Entities;
using CHAP2.Domain.ValueObjects;

namespace CHAP2.Application.Interfaces;

public interface IBibleChapterRepository
{
    Task<BibleChapter?> GetChapterAsync(BibleReference reference, CancellationToken cancellationToken = default);
}
