using CHAP2.Domain.Events;

namespace CHAP2.Application.Interfaces;

/// <summary>
/// Interface for handling domain events of a specific type
/// </summary>
/// <typeparam name="TEvent">The type of domain event to handle</typeparam>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
