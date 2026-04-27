using CHAP2.Domain.Entities;
using CHAP2.Domain.ValueObjects;

namespace CHAP2.Application.Interfaces;

public interface ISetlistCommandService
{
    Task<Setlist> CreateMineAsync(string name, CancellationToken cancellationToken = default);
    Task<Setlist> RenameAsync(Guid setlistId, string newName, CancellationToken cancellationToken = default);
    Task<Setlist> AppendChorusAsync(Guid setlistId, Guid chorusId, CancellationToken cancellationToken = default);
    Task<Setlist> RemoveItemAsync(Guid setlistId, Guid itemId, CancellationToken cancellationToken = default);
    Task<Setlist> ReorderAsync(Guid setlistId, IReadOnlyList<Guid> itemIdsInOrder, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid setlistId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts a setlist by name for the current user: replaces the items
    /// of an existing same-named setlist, or creates a new one. Atomic.
    /// </summary>
    Task<Setlist> SaveByNameAsync(string name, IReadOnlyList<SetlistItemPayload> items, CancellationToken cancellationToken = default);
}
