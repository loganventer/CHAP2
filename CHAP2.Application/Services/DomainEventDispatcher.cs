using CHAP2.Application.Interfaces;
using CHAP2.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.Services;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var eventType = domainEvent.GetType();
        var handlerInterface = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var handlerEnumerable = typeof(IEnumerable<>).MakeGenericType(handlerInterface);
        var handlers = (IEnumerable<object>)_serviceProvider.GetRequiredService(handlerEnumerable);
        var handleMethod = handlerInterface.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;

        var dispatched = 0;
        foreach (var handler in handlers)
        {
            try
            {
                var task = (Task)handleMethod.Invoke(handler, new object[] { domainEvent, cancellationToken })!;
                await task;
                dispatched++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling domain event {EventType} with handler {HandlerType}",
                    eventType.Name, handler.GetType().Name);
                throw;
            }
        }

        _logger.LogInformation("Domain event {EventType} dispatched to {HandlerCount} handler(s)",
            eventType.Name, dispatched);
    }

    public async Task DispatchAndClearAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await DispatchAsync(domainEvent, cancellationToken);
        }
    }
}
