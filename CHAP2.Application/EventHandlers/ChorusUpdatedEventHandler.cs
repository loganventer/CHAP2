using CHAP2.Application.Interfaces;
using CHAP2.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.EventHandlers;

/// <summary>
/// Handles the ChorusUpdatedEvent domain event
/// </summary>
public class ChorusUpdatedEventHandler : IDomainEventHandler<ChorusUpdatedEvent>
{
    private readonly ILogger<ChorusUpdatedEventHandler> _logger;

    public ChorusUpdatedEventHandler(ILogger<ChorusUpdatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ChorusUpdatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Chorus updated: {ChorusId} - {ChorusName} at {OccurredOn}",
            domainEvent.ChorusId,
            domainEvent.ChorusName,
            domainEvent.OccurredOn);

        // Add additional handling logic here, such as:
        // - Invalidating caches
        // - Updating search indexes
        // - Sending notifications

        return Task.CompletedTask;
    }
}
