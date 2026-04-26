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
}
