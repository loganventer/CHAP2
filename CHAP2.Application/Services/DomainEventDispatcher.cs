using CHAP2.Application.Interfaces;
using CHAP2.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.Services;

/// <summary>
/// Dispatches domain events to their registered handlers using explicit handler injection
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IEnumerable<IDomainEventHandler<ChorusCreatedEvent>> _createdHandlers;
    private readonly IEnumerable<IDomainEventHandler<ChorusUpdatedEvent>> _updatedHandlers;
    private readonly IEnumerable<IDomainEventHandler<ChorusDeletedEvent>> _deletedHandlers;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(
        IEnumerable<IDomainEventHandler<ChorusCreatedEvent>> createdHandlers,
        IEnumerable<IDomainEventHandler<ChorusUpdatedEvent>> updatedHandlers,
        IEnumerable<IDomainEventHandler<ChorusDeletedEvent>> deletedHandlers,
        ILogger<DomainEventDispatcher> logger)
    {
        _createdHandlers = createdHandlers;
        _updatedHandlers = updatedHandlers;
        _deletedHandlers = deletedHandlers;
        _logger = logger;
    }

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var eventType = domainEvent.GetType();
        _logger.LogDebug("Dispatching domain event: {EventType}", eventType.Name);

        var handlerCount = 0;

        switch (domainEvent)
        {
            case ChorusCreatedEvent createdEvent:
                foreach (var handler in _createdHandlers)
                {
                    try
                    {
                        await handler.HandleAsync(createdEvent, cancellationToken);
                        handlerCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error handling domain event {EventType} with handler {HandlerType}",
                            eventType.Name, handler.GetType().Name);
                        throw;
                    }
                }
                break;

            case ChorusUpdatedEvent updatedEvent:
                foreach (var handler in _updatedHandlers)
                {
                    try
                    {
                        await handler.HandleAsync(updatedEvent, cancellationToken);
                        handlerCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error handling domain event {EventType} with handler {HandlerType}",
                            eventType.Name, handler.GetType().Name);
                        throw;
                    }
                }
                break;

            case ChorusDeletedEvent deletedEvent:
                foreach (var handler in _deletedHandlers)
                {
                    try
                    {
                        await handler.HandleAsync(deletedEvent, cancellationToken);
                        handlerCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error handling domain event {EventType} with handler {HandlerType}",
                            eventType.Name, handler.GetType().Name);
                        throw;
                    }
                }
                break;

            default:
                _logger.LogWarning("No handlers registered for domain event type: {EventType}", eventType.Name);
                break;
        }

        _logger.LogInformation("Domain event {EventType} dispatched to {HandlerCount} handler(s)",
            eventType.Name, handlerCount);
    }

    public async Task DispatchAndClearAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents.ToList())
        {
            await DispatchAsync(domainEvent, cancellationToken);
        }
    }
}
