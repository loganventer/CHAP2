using CHAP2.Application.Interfaces;
using CHAP2.Domain.Events;

namespace CHAP2.Application.EventHandlers;

/// <summary>
/// Enqueues a git sync (commit + push) when a chorus is updated, so the
/// edited JSON file persists to the remote for the next deploy build.
/// </summary>
public class ChorusUpdatedGitSyncHandler : IDomainEventHandler<ChorusUpdatedEvent>
{
    private readonly IChorusGitSync _gitSync;

    public ChorusUpdatedGitSyncHandler(IChorusGitSync gitSync)
    {
        _gitSync = gitSync;
    }

    public Task HandleAsync(ChorusUpdatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _gitSync.EnqueueUpsert(domainEvent.ChorusId, domainEvent.ChorusName);
        return Task.CompletedTask;
    }
}
