using CHAP2.Application.Models;

namespace CHAP2.Application.Interfaces;

/// <summary>
/// Runs one full chorus git sync cycle: pull -> stage -> commit -> push.
/// Reports progress through <paramref name="progress"/> when supplied
/// (the SSE force-sync endpoint subscribes; the daily background worker
/// passes null).
/// </summary>
public interface IChorusGitSyncOrchestrator
{
    Task<ChorusGitSyncResult> SyncNowAsync(
        IProgress<ChorusGitSyncProgress>? progress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Promote the staged edits branch into the configured protected
    /// branch ("main"). Admin-triggered. Returns
    /// <see cref="ChorusGitPromoteResult.Disabled"/> when the deployment
    /// hasn't configured a separate main branch (single-branch mode).
    /// </summary>
    Task<ChorusGitPromoteResult> PromoteAsync(CancellationToken cancellationToken = default);
}
