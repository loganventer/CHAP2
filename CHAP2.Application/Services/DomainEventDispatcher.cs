using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.Services;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly ILogger<DomainEventDispatcher> _logger;
    public DomainEventDispatcher(ILogger<DomainEventDispatcher> logger)
    {
        _logger = logger;
    }
    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Domain event dispatched: {EventType}", domainEvent.GetType().Name);
        // In a real app, you would resolve and invoke event handlers here
        return Task.CompletedTask;
    }
} 