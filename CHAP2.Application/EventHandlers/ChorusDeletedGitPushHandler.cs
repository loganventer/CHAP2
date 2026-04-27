using CHAP2.Application.Interfaces;
using CHAP2.Domain.Events;
using CHAP2.Shared.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CHAP2.Application.EventHandlers;

/// <summary>
/// On chorus delete: remove the file from the edits branch on GitHub via
/// the Contents API (one delete commit). Same fail-soft semantics as
/// <see cref="ChorusCreatedGitPushHandler"/>.
/// </summary>
public sealed class ChorusDeletedGitPushHandler : IDomainEventHandler<ChorusDeletedEvent>
{
    private readonly IChorusGitHubSync _gitHub;
    private readonly IChorusFileGateway _files;
    private readonly GitSyncOptions _options;
    private readonly ILogger<ChorusDeletedGitPushHandler> _logger;

    public ChorusDeletedGitPushHandler(
        IChorusGitHubSync gitHub,
        IChorusFileGateway files,
        IOptions<GitSyncOptions> options,
        ILogger<ChorusDeletedGitPushHandler> logger)
    {
        _gitHub = gitHub ?? throw new ArgumentNullException(nameof(gitHub));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(ChorusDeletedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return;

        var fileName = _files.GetFileName(domainEvent.ChorusId);
        var message = $"Delete chorus '{domainEvent.ChorusName}' ({domainEvent.ChorusId})";

        try
        {
            var result = await _gitHub.DeleteFileAsync(fileName, message, cancellationToken);
            if (!result.Succeeded)
            {
                _logger.LogWarning(
                    "Per-edit delete for chorus {Id} did not succeed ({Status}): {Error}. Daily mirror will recover.",
                    domainEvent.ChorusId, result.Status, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex, "Per-edit delete for chorus {Id} threw; daily mirror will recover.", domainEvent.ChorusId);
        }
    }
}
