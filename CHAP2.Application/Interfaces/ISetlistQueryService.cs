using CHAP2.Domain.Entities;

namespace CHAP2.Application.Interfaces;

public interface ISetlistQueryService
{
    Task<IReadOnlyList<Setlist>> GetMineAsync(CancellationToken cancellationToken = default);
    Task<Setlist?> GetByIdAsync(Guid setlistId, CancellationToken cancellationToken = default);
}
