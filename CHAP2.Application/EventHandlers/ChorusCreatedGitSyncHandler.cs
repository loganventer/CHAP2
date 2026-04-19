using CHAP2.Application.Interfaces;
using CHAP2.Domain.Events;

namespace CHAP2.Application.EventHandlers;

/// <summary>
/// Enqueues a git sync (commit + push) when a chorus is created, so the
/// new JSON file persists to the remote for the next deploy build.
/// </summary>
public class ChorusCreatedGitSyncHandler : IDomainEventHandler<ChorusCreatedEvent>
{
    private readonly IChorusGitSync _gitSync;

    public ChorusCreatedGitSyncHandler(IChorusGitSync gitSync)
    {
        _gitSync = gitSync;
    }

    public Task HandleAsync(ChorusCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _gitSync.EnqueueUpsert(domainEvent.ChorusId, domainEvent.ChorusName);
        return Task.CompletedTask;
    }
}
