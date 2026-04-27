using CHAP2.Domain.Entities;

namespace CHAP2.Application.Interfaces;

public interface ISetlistQueryService
{
    /// <summary>Named setlists owned by the current user (excludes the working draft).</summary>
    Task<IReadOnlyList<Setlist>> GetMineAsync(CancellationToken cancellationToken = default);
    Task<Setlist?> GetByIdAsync(Guid setlistId, CancellationToken cancellationToken = default);
    Task<Setlist?> GetWorkingDraftAsync(CancellationToken cancellationToken = default);
}
