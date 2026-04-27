using CHAP2.Shared.DTOs;

namespace CHAP2.WebPortal.Interfaces;

public interface ISetlistApiService
{
    Task<IReadOnlyList<SetlistSummaryDto>> ListMineAsync(CancellationToken cancellationToken = default);
    Task<SetlistDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SetlistDto?> SaveByNameAsync(string name, IReadOnlyList<SetlistItemPayloadDto> items, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
