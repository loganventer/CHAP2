namespace CHAP2.Application.Interfaces;

/// <summary>
/// A single git working tree -- a directory on disk that mirrors a
/// remote branch. Narrow surface so the orchestrator (which knows the
/// order of operations) doesn't have to know git plumbing, and the
/// concrete implementation (which knows git plumbing) doesn't have to
/// know workflow.
///
/// Implementations are expected to be safe to call concurrently for
/// reads but the orchestrator never calls writes concurrently for the
/// same tree.
/// </summary>
public interface IGitWorkingTree
{
    /// <summary>
    /// Returns true if the tree is already a git working copy of the
    /// configured remote+branch.
    /// </summary>
    Task<bool> IsClonedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clone the configured remote+branch into the tree path. Idempotent
    /// when the tree is already cloned (no-op).
    /// </summary>
    Task EnsureCloneAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pull the latest from the remote with a "local always wins"
    /// strategy (-X ours). Existing local files survive even when the
    /// remote has a competing change for the same path.
    /// </summary>
    Task PullLocalWinsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stage every change under the working tree (git add -A).
    /// </summary>
    Task StageAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// True if the index has staged changes ready to commit.
    /// </summary>
    Task<bool> HasStagedChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Number of distinct files in the staged index. Used so the
    /// orchestrator can report "Synced N files".
    /// </summary>
    Task<int> StagedFileCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commit the staged index with the supplied message under the
    /// configured author identity.
    /// </summary>
    Task CommitAsync(string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Push the local branch to the configured remote.
    /// </summary>
    Task PushAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// True if the local branch has commits the remote doesn't have yet.
    /// Lets the orchestrator decide to push even when no new local
    /// changes were committed this cycle (covers the case where a
    /// previous push failed and we're catching up).
    /// </summary>
    Task<bool> LocalIsAheadOfRemoteAsync(CancellationToken cancellationToken = default);
}
