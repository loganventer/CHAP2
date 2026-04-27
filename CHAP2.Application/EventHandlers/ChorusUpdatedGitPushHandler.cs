using CHAP2.Application.Interfaces;
using CHAP2.Domain.Events;
using CHAP2.Shared.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CHAP2.Application.EventHandlers;

/// <summary>
/// On chorus update: push the new file contents to the edits branch on
/// GitHub. Same fail-soft semantics as <see cref="ChorusCreatedGitPushHandler"/>.
/// </summary>
public sealed class ChorusUpdatedGitPushHandler : IDomainEventHandler<ChorusUpdatedEvent>
{
    private readonly IChorusGitHubSync _gitHub;
    private readonly IChorusFileGateway _files;
    private readonly GitSyncOptions _options;
    private readonly ILogger<ChorusUpdatedGitPushHandler> _logger;

    public ChorusUpdatedGitPushHandler(
        IChorusGitHubSync gitHub,
        IChorusFileGateway files,
        IOptions<GitSyncOptions> options,
        ILogger<ChorusUpdatedGitPushHandler> logger)
    {
        _gitHub = gitHub ?? throw new ArgumentNullException(nameof(gitHub));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(ChorusUpdatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return;

        var content = await _files.ReadAsync(domainEvent.ChorusId, cancellationToken);
        if (content is null)
        {
            _logger.LogWarning(
                "ChorusUpdated push skipped: file for {Id} not on disk (concurrent delete?).",
                domainEvent.ChorusId);
            return;
        }

        var fileName = _files.GetFileName(domainEvent.ChorusId);
        var message = $"Update chorus '{domainEvent.ChorusName}' ({domainEvent.ChorusId})";

        try
        {
            var result = await _gitHub.PushFileAsync(fileName, content, message, cancellationToken);
            if (!result.Succeeded)
            {
                _logger.LogWarning(
                    "Per-edit push for chorus {Id} did not succeed ({Status}): {Error}. Daily mirror will recover.",
                    domainEvent.ChorusId, result.Status, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex, "Per-edit push for chorus {Id} threw; daily mirror will recover.", domainEvent.ChorusId);
        }
    }
}
