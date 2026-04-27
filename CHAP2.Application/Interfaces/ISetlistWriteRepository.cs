using CHAP2.Domain.Entities;

namespace CHAP2.Application.Interfaces;

public interface ISetlistWriteRepository
{
    Task<Setlist> AddAsync(Setlist setlist, CancellationToken cancellationToken = default);
    Task<Setlist> UpdateAsync(Setlist setlist, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
