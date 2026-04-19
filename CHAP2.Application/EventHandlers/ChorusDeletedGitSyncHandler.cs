using CHAP2.Application.Interfaces;
using CHAP2.Domain.Events;

namespace CHAP2.Application.EventHandlers;

/// <summary>
/// Enqueues a git sync (commit + push) when a chorus is deleted, so the
/// removal propagates to the remote for the next deploy build.
/// </summary>
public class ChorusDeletedGitSyncHandler : IDomainEventHandler<ChorusDeletedEvent>
{
    private readonly IChorusGitSync _gitSync;

    public ChorusDeletedGitSyncHandler(IChorusGitSync gitSync)
    {
        _gitSync = gitSync;
    }

    public Task HandleAsync(ChorusDeletedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _gitSync.EnqueueDelete(domainEvent.ChorusId, domainEvent.ChorusName);
        return Task.CompletedTask;
    }
}
