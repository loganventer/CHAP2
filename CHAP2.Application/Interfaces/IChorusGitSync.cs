namespace CHAP2.Application.Interfaces;

/// <summary>
/// Pushes chorus data changes to the git remote so local edits are
/// persisted back to the repository. Implementations must be non-blocking
/// (fire-and-forget) so HTTP request latency is unaffected, and must
/// no-op gracefully when git is unavailable (Docker / Render runtime).
/// </summary>
public interface IChorusGitSync
{
    /// <summary>
    /// Queue a git commit + push for a chorus that was added or updated.
    /// </summary>
    void EnqueueUpsert(Guid chorusId, string chorusName);

    /// <summary>
    /// Queue a git commit + push for a chorus that was deleted.
    /// </summary>
    void EnqueueDelete(Guid chorusId, string chorusName);
}
