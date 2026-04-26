using CHAP2.Application.Helpers;
using CHAP2.Application.Interfaces;
using CHAP2.Application.Models;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.Services;

/// <summary>
/// The only place in the codebase that knows the *order* of operations
/// for a chorus sync. Composes <see cref="IGitWorkingTree"/> for the
/// git plumbing -- doesn't shell out itself, doesn't read configuration,
/// doesn't know the schedule.
/// </summary>
public sealed class ChorusGitSyncOrchestrator : IChorusGitSyncOrchestrator
{
    private readonly IGitWorkingTree _tree;
    private readonly Func<DateTime> _utcNow;
    private readonly ILogger<ChorusGitSyncOrchestrator> _logger;

    public ChorusGitSyncOrchestrator(
        IGitWorkingTree tree,
        ILogger<ChorusGitSyncOrchestrator> logger)
        : this(tree, () => DateTime.UtcNow, logger) { }

    /// <summary>Test seam: lets unit tests inject a fake clock.</summary>
    internal ChorusGitSyncOrchestrator(
        IGitWorkingTree tree,
        Func<DateTime> utcNow,
        ILogger<ChorusGitSyncOrchestrator> logger)
    {
        _tree = tree ?? throw new ArgumentNullException(nameof(tree));
        _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ChorusGitSyncResult> SyncNowAsync(
        IProgress<ChorusGitSyncProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _tree.EnsureCloneAsync(cancellationToken);

            Report(progress, ChorusGitSyncStage.Pulling, "Pulling latest from origin (local wins on conflict)");
            await _tree.PullLocalWinsAsync(cancellationToken);

            Report(progress, ChorusGitSyncStage.Staging, "Staging local changes");
            await _tree.StageAllAsync(cancellationToken);

            var committed = false;
            var fileCount = 0;
            if (await _tree.HasStagedChangesAsync(cancellationToken))
            {
                fileCount = await _tree.StagedFileCountAsync(cancellationToken);
                var message = $"Daily chorus sync — {_utcNow():yyyy-MM-dd HH:mm} UTC ({fileCount} file{(fileCount == 1 ? "" : "s")})";
                Report(progress, ChorusGitSyncStage.Committing, message);
                await _tree.CommitAsync(message, cancellationToken);
                committed = true;
            }

            var pushed = false;
            if (committed || await _tree.LocalIsAheadOfRemoteAsync(cancellationToken))
            {
                Report(progress, ChorusGitSyncStage.Pushing, "Pushing to origin");
                await _tree.PushAsync(cancellationToken);
                pushed = true;
            }

            var summary = pushed
                ? $"Pushed {fileCount} file{(fileCount == 1 ? "" : "s")} to GitHub"
                : "Already up to date";
            Report(progress, ChorusGitSyncStage.Done, summary);
            _logger.LogInformation("Chorus git sync complete: {Summary}", summary);
            return new ChorusGitSyncResult(Pulled: true, FilesCommitted: fileCount, Pushed: pushed, Error: null);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Chorus git sync cancelled");
            throw;
        }
        catch (Exception ex)
        {
            var sanitized = SecretMask.Apply(ex.Message);
            _logger.LogError(ex, "Chorus git sync failed: {Reason}", sanitized);
            Report(progress, ChorusGitSyncStage.Failed, sanitized);
            return ChorusGitSyncResult.Failure(sanitized);
        }
    }

    private static void Report(IProgress<ChorusGitSyncProgress>? sink, ChorusGitSyncStage stage, string message)
        => sink?.Report(new ChorusGitSyncProgress(stage, message, DateTime.UtcNow));
}
