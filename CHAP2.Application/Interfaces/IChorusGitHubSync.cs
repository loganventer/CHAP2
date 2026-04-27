using CHAP2.Application.Models;

namespace CHAP2.Application.Interfaces;

/// <summary>
/// Talks to the GitHub Git Data API to mirror the local chorus
/// directory to / from a remote branch. No git binary on disk, no
/// .git directory -- just HTTP.
/// </summary>
public interface IChorusGitHubSync
{
    /// <summary>
    /// Download every chorus JSON under the configured remote prefix
    /// into <paramref name="localDirectory"/>. Used by the disk
    /// bootstrapper on first start; safe to re-run (skips files that
    /// already exist locally with the same content).
    /// </summary>
    Task BootstrapAsync(string localDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mirror the local chorus directory to the remote -- creates new
    /// remote files for local additions, updates remote files whose
    /// content differs, deletes remote files that no longer exist
    /// locally. The whole mirror lands as a single commit.
    /// </summary>
    Task<ChorusMirrorResult> MirrorAsync(
        string localDirectory,
        string commitMessage,
        IProgress<ChorusGitSyncProgress>? progress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Server-side merge of the configured edits branch into <paramref name="targetBranch"/>
    /// via POST /repos/{owner}/{repo}/merges. Used by the admin "promote
    /// to main" action to land staged chorus edits onto the protected
    /// branch without a local clone.
    /// </summary>
    Task<ChorusBranchMergeResult> MergeIntoAsync(
        string targetBranch,
        string commitMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Single-file create-or-update on the configured edits branch via
    /// the Contents API (one commit per call). Used by per-edit handlers
    /// so an edit lands on GitHub before the user moves on, instead of
    /// waiting for the daily mirror.
    /// </summary>
    Task<ChorusFilePushResult> PushFileAsync(
        string fileName,
        byte[] content,
        string commitMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Single-file delete on the configured edits branch via the Contents
    /// API. NotFound semantics if the file isn't on the remote.
    /// </summary>
    Task<ChorusFilePushResult> DeleteFileAsync(
        string fileName,
        string commitMessage,
        CancellationToken cancellationToken = default);
}
