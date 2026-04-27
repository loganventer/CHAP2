using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.Services;

public class SetlistCommandService : ISetlistCommandService
{
    private readonly ISetlistRepository _repository;
    private readonly ISetlistOwnershipPolicy _ownership;
    private readonly ICurrentUserService _currentUser;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<SetlistCommandService> _logger;

    public SetlistCommandService(
        ISetlistRepository repository,
        ISetlistOwnershipPolicy ownership,
        ICurrentUserService currentUser,
        IDomainEventDispatcher eventDispatcher,
        ILogger<SetlistCommandService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _ownership = ownership ?? throw new ArgumentNullException(nameof(ownership));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Setlist> CreateMineAsync(string name, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating setlist '{Name}' for {OwnerId}", name, _currentUser.UserId);
        var setlist = Setlist.Create(_currentUser.UserId, name);
        await _repository.AddAsync(setlist, cancellationToken);
        await DispatchAndClearAsync(setlist, cancellationToken);
        return setlist;
    }

    public async Task<Setlist> RenameAsync(Guid setlistId, string newName, CancellationToken cancellationToken = default)
    {
        var setlist = await LoadAndAuthorizeAsync(setlistId, cancellationToken);
        setlist.Rename(newName);
        await _repository.UpdateAsync(setlist, cancellationToken);
        await DispatchAndClearAsync(setlist, cancellationToken);
        return setlist;
    }

    public async Task<Setlist> AppendChorusAsync(Guid setlistId, Guid chorusId, CancellationToken cancellationToken = default)
    {
        var setlist = await LoadAndAuthorizeAsync(setlistId, cancellationToken);
        setlist.AppendChorus(chorusId);
        await _repository.UpdateAsync(setlist, cancellationToken);
        await DispatchAndClearAsync(setlist, cancellationToken);
        return setlist;
    }

    public async Task<Setlist> RemoveItemAsync(Guid setlistId, Guid itemId, CancellationToken cancellationToken = default)
    {
        var setlist = await LoadAndAuthorizeAsync(setlistId, cancellationToken);
        setlist.RemoveItem(itemId);
        await _repository.UpdateAsync(setlist, cancellationToken);
        await DispatchAndClearAsync(setlist, cancellationToken);
        return setlist;
    }

    public async Task<Setlist> ReorderAsync(Guid setlistId, IReadOnlyList<Guid> itemIdsInOrder, CancellationToken cancellationToken = default)
    {
        var setlist = await LoadAndAuthorizeAsync(setlistId, cancellationToken);
        setlist.Reorder(itemIdsInOrder);
        await _repository.UpdateAsync(setlist, cancellationToken);
        await DispatchAndClearAsync(setlist, cancellationToken);
        return setlist;
    }

    public async Task DeleteAsync(Guid setlistId, CancellationToken cancellationToken = default)
    {
        var setlist = await LoadAndAuthorizeAsync(setlistId, cancellationToken);
        setlist.MarkDeleted();
        await _repository.DeleteAsync(setlistId, cancellationToken);
        await DispatchAndClearAsync(setlist, cancellationToken);
    }

    private async Task<Setlist> LoadAndAuthorizeAsync(Guid setlistId, CancellationToken cancellationToken)
    {
        var setlist = await _repository.GetByIdAsync(setlistId, cancellationToken)
            ?? throw new SetlistNotFoundException(setlistId);
        _ownership.EnsureCanAccess(setlist);
        return setlist;
    }

    private async Task DispatchAndClearAsync(Setlist setlist, CancellationToken cancellationToken)
    {
        await _eventDispatcher.DispatchAndClearAsync(setlist.DomainEvents.ToList(), cancellationToken);
        setlist.ClearDomainEvents();
    }
}
