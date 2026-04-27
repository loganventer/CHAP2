using CHAP2.Application.Interfaces;
using CHAP2.Application.Models;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.Services;

/// <summary>
/// The only place that knows the *order* of operations for a chorus
/// sync. Composes <see cref="IChorusGitHubSync"/> for the GitHub side
/// and the configured local data directory. Pure composition -- no
/// HTTP, no disk plumbing, no schedule.
/// </summary>
public sealed class ChorusGitSyncOrchestrator : IChorusGitSyncOrchestrator
{
    private readonly IChorusGitHubSync _gitHub;
    private readonly string _localDirectory;
    private readonly string _editsBranch;
    private readonly string? _mainBranch;
    private readonly Func<DateTime> _utcNow;
    private readonly ILogger<ChorusGitSyncOrchestrator> _logger;

    public ChorusGitSyncOrchestrator(
        IChorusGitHubSync gitHub,
        string localDirectory,
        string editsBranch,
        string? mainBranch,
        ILogger<ChorusGitSyncOrchestrator> logger)
        : this(gitHub, localDirectory, editsBranch, mainBranch, () => DateTime.UtcNow, logger) { }

    /// <summary>Test seam.</summary>
    internal ChorusGitSyncOrchestrator(
        IChorusGitHubSync gitHub,
        string localDirectory,
        string editsBranch,
        string? mainBranch,
        Func<DateTime> utcNow,
        ILogger<ChorusGitSyncOrchestrator> logger)
    {
        _gitHub = gitHub ?? throw new ArgumentNullException(nameof(gitHub));
        _localDirectory = !string.IsNullOrWhiteSpace(localDirectory)
            ? localDirectory
            : throw new ArgumentException("localDirectory required", nameof(localDirectory));
        _editsBranch = !string.IsNullOrWhiteSpace(editsBranch)
            ? editsBranch
            : throw new ArgumentException("editsBranch required", nameof(editsBranch));
        _mainBranch = string.IsNullOrWhiteSpace(mainBranch) ? null : mainBranch;
        _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ChorusGitSyncResult> SyncNowAsync(
        IProgress<ChorusGitSyncProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = $"Chorus sync — {_utcNow():yyyy-MM-dd HH:mm} UTC";
            var mirror = await _gitHub.MirrorAsync(_localDirectory, message, progress, cancellationToken);

            if (!mirror.Succeeded)
            {
                return ChorusGitSyncResult.Failure(mirror.Error ?? "unknown");
            }

            _logger.LogInformation(
                "Chorus sync complete: {Created} new, {Updated} updated, {Deleted} deleted",
                mirror.Created, mirror.Updated, mirror.Deleted);

            return new ChorusGitSyncResult(
                Pulled: true,
                FilesCommitted: mirror.Created + mirror.Updated + mirror.Deleted,
                Pushed: mirror.TotalChanges > 0,
                Error: null);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Chorus sync cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chorus sync failed: {Reason}", ex.Message);
            progress?.Report(new ChorusGitSyncProgress(ChorusGitSyncStage.Failed, ex.Message, DateTime.UtcNow));
            return ChorusGitSyncResult.Failure(ex.Message);
        }
    }

    public async Task<ChorusGitPromoteResult> PromoteAsync(CancellationToken cancellationToken = default)
    {
        if (_mainBranch is null)
            return ChorusGitPromoteResult.Disabled("Two-branch promotion is not enabled (GitSync:MainBranch is empty).");
        if (string.Equals(_editsBranch, _mainBranch, StringComparison.Ordinal))
            return ChorusGitPromoteResult.Disabled("Edits branch and main branch are the same; nothing to promote.");

        try
        {
            var message = $"Promote chorus edits — {_utcNow():yyyy-MM-dd HH:mm} UTC";
            var merge = await _gitHub.MergeIntoAsync(_mainBranch, message, cancellationToken);
            _logger.LogInformation(
                "Promote {From} -> {To}: {Status}",
                _editsBranch, _mainBranch, merge.Status);
            return ChorusGitPromoteResult.From(_editsBranch, _mainBranch, merge);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Promote {From} -> {To} failed: {Reason}", _editsBranch, _mainBranch, ex.Message);
            return ChorusGitPromoteResult.Failure(_editsBranch, _mainBranch, ex.Message);
        }
    }
}
