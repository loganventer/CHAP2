using CHAP2.Application.Interfaces;
using CHAP2.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.Services;

/// <summary>
/// Dispatches domain events to their registered handlers using the service provider
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(
        IServiceProvider serviceProvider,
        ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var eventType = domainEvent.GetType();
        _logger.LogDebug("Dispatching domain event: {EventType}", eventType.Name);

        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices(handlerType);

        var handlerCount = 0;
        foreach (var handler in handlers)
        {
            if (handler == null) continue;

            try
            {
                var handleMethod = handlerType.GetMethod("HandleAsync");
                if (handleMethod != null)
                {
                    var task = (Task?)handleMethod.Invoke(handler, new object[] { domainEvent, cancellationToken });
                    if (task != null)
                    {
                        await task;
                        handlerCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling domain event {EventType} with handler {HandlerType}",
                    eventType.Name, handler.GetType().Name);
                throw;
            }
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
