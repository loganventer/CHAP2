using CHAP2.Application.Interfaces;
using CHAP2.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.EventHandlers;

/// <summary>
/// Handles the ChorusCreatedEvent domain event
/// </summary>
public class ChorusCreatedEventHandler : IDomainEventHandler<ChorusCreatedEvent>
{
    private readonly ILogger<ChorusCreatedEventHandler> _logger;

    public ChorusCreatedEventHandler(ILogger<ChorusCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ChorusCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Chorus created: {ChorusId} - {ChorusName} at {OccurredOn}",
            domainEvent.ChorusId,
            domainEvent.ChorusName,
            domainEvent.OccurredOn);

        // Add additional handling logic here, such as:
        // - Sending notifications
        // - Updating search indexes
        // - Publishing to message queues
        // - Triggering workflows

        return Task.CompletedTask;
    }
}
