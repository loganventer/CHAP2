using CHAP2.Domain.Events;

namespace CHAP2.Application.Interfaces;

/// <summary>
/// Interface for dispatching domain events to their registered handlers
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches a single domain event to all registered handlers
    /// </summary>
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatches all domain events from an entity and clears the events list
    /// </summary>
    Task DispatchAndClearAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
