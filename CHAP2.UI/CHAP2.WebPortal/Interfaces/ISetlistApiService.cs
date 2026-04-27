using CHAP2.Shared.DTOs;

namespace CHAP2.WebPortal.Interfaces;

public interface ISetlistApiService
{
    Task<IReadOnlyList<SetlistDto>> GetMineAsync(CancellationToken cancellationToken = default);
    Task<SetlistDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SetlistDto?> CreateAsync(string name, CancellationToken cancellationToken = default);
    Task<SetlistDto?> RenameAsync(Guid id, string newName, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SetlistDto?> AppendChorusAsync(Guid id, Guid chorusId, CancellationToken cancellationToken = default);
    Task<SetlistDto?> RemoveItemAsync(Guid id, Guid itemId, CancellationToken cancellationToken = default);
    Task<SetlistDto?> ReorderAsync(Guid id, IReadOnlyList<Guid> itemIdsInOrder, CancellationToken cancellationToken = default);
}
