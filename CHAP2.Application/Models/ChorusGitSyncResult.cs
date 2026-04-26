namespace CHAP2.Application.Models;

/// <summary>
/// Final outcome of one sync cycle. Returned by the orchestrator;
/// callers log it and pass it back to the user where applicable.
/// </summary>
public sealed record ChorusGitSyncResult(
    bool Pulled,
    int FilesCommitted,
    bool Pushed,
    string? Error)
{
    public static ChorusGitSyncResult Disabled() => new(false, 0, false, "git-sync-disabled");
    public static ChorusGitSyncResult Failure(string error) => new(false, 0, false, error);
    public bool Succeeded => Error is null;
}
