using CHAP2.Domain.Entities;

namespace CHAP2.Application.Interfaces;

public interface ISetlistReadRepository
{
    Task<Setlist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Setlist>> GetByOwnerAsync(string ownerId, CancellationToken cancellationToken = default);
}
