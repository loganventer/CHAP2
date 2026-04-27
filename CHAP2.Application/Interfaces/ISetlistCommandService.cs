using CHAP2.Domain.Entities;

namespace CHAP2.Application.Interfaces;

public interface ISetlistCommandService
{
    Task<Setlist> CreateMineAsync(string name, CancellationToken cancellationToken = default);
    Task<Setlist> RenameAsync(Guid setlistId, string newName, CancellationToken cancellationToken = default);
    Task<Setlist> AppendChorusAsync(Guid setlistId, Guid chorusId, CancellationToken cancellationToken = default);
    Task<Setlist> RemoveItemAsync(Guid setlistId, Guid itemId, CancellationToken cancellationToken = default);
    Task<Setlist> ReorderAsync(Guid setlistId, IReadOnlyList<Guid> itemIdsInOrder, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid setlistId, CancellationToken cancellationToken = default);
}
