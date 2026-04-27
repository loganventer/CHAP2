using CHAP2.Application.Interfaces;
using CHAP2.Domain.Events;
using CHAP2.Shared.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CHAP2.Application.EventHandlers;

/// <summary>
/// On chorus create: push the freshly-written file to the configured
/// edits branch on GitHub so the new chorus appears in the staging
/// branch immediately, before the user moves on to the next action.
/// Failures are logged but swallowed -- the daily background mirror
/// will pick up anything that didn't make it.
/// </summary>
public sealed class ChorusCreatedGitPushHandler : IDomainEventHandler<ChorusCreatedEvent>
{
    private readonly IChorusGitHubSync _gitHub;
    private readonly IChorusFileGateway _files;
    private readonly GitSyncOptions _options;
    private readonly ILogger<ChorusCreatedGitPushHandler> _logger;

    public ChorusCreatedGitPushHandler(
        IChorusGitHubSync gitHub,
        IChorusFileGateway files,
        IOptions<GitSyncOptions> options,
        ILogger<ChorusCreatedGitPushHandler> logger)
    {
        _gitHub = gitHub ?? throw new ArgumentNullException(nameof(gitHub));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(ChorusCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return;

        var content = await _files.ReadAsync(domainEvent.ChorusId, cancellationToken);
        if (content is null)
        {
            _logger.LogWarning(
                "ChorusCreated push skipped: file for {Id} not on disk yet (race or write failed).",
                domainEvent.ChorusId);
            return;
        }

        var fileName = _files.GetFileName(domainEvent.ChorusId);
        var message = $"Add chorus '{domainEvent.ChorusName}' ({domainEvent.ChorusId})";

        try
        {
            var result = await _gitHub.PushFileAsync(fileName, content, message, cancellationToken);
            if (!result.Succeeded)
            {
                _logger.LogWarning(
                    "Per-edit push for new chorus {Id} did not succeed ({Status}): {Error}. Daily mirror will recover.",
                    domainEvent.ChorusId, result.Status, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex, "Per-edit push for new chorus {Id} threw; daily mirror will recover.", domainEvent.ChorusId);
        }
    }
}
