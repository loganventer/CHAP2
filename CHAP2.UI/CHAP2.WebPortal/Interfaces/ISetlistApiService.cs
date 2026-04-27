using CHAP2.Shared.DTOs;

namespace CHAP2.WebPortal.Interfaces;

public interface ISetlistApiService
{
    Task<IReadOnlyList<SetlistSummaryDto>> ListMineAsync(CancellationToken cancellationToken = default);
    Task<SetlistDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SetlistDto?> SaveByNameAsync(string name, IReadOnlyList<SetlistItemPayloadDto> items, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the current user's working draft, or null if none exists.</summary>
    Task<SetlistDto?> GetWorkingDraftAsync(CancellationToken cancellationToken = default);

    /// <summary>Replaces the current user's working draft items.</summary>
    Task<SetlistDto?> SaveWorkingDraftAsync(IReadOnlyList<SetlistItemPayloadDto> items, CancellationToken cancellationToken = default);
}
