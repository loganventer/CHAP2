using CHAP2.Application.Interfaces;
using CHAP2.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.EventHandlers;

/// <summary>
/// Handles the ChorusDeletedEvent domain event
/// </summary>
public class ChorusDeletedEventHandler : IDomainEventHandler<ChorusDeletedEvent>
{
    private readonly ILogger<ChorusDeletedEventHandler> _logger;

    public ChorusDeletedEventHandler(ILogger<ChorusDeletedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ChorusDeletedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Chorus deleted: {ChorusId} - {ChorusName} at {OccurredOn}",
            domainEvent.ChorusId,
            domainEvent.ChorusName,
            domainEvent.OccurredOn);

        // Add additional handling logic here, such as:
        // - Removing from search indexes
        // - Cleaning up related data
        // - Sending notifications

        return Task.CompletedTask;
    }
}
