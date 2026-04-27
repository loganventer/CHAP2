namespace CHAP2.Application.Models;

/// <summary>
/// Orchestrator-level outcome for promoting the edits branch to the
/// protected main branch. Wraps the GitHub merge result with a
/// "disabled" reason when promotion isn't configured for this deployment.
/// </summary>
public sealed record ChorusGitPromoteResult(
    bool Succeeded,
    string FromBranch,
    string ToBranch,
    ChorusBranchMergeResult? Merge,
    string? Error)
{
    public static ChorusGitPromoteResult Disabled(string reason) =>
        new(false, string.Empty, string.Empty, null, reason);

    public static ChorusGitPromoteResult From(string fromBranch, string toBranch, ChorusBranchMergeResult merge) =>
        new(merge.Succeeded, fromBranch, toBranch, merge, merge.Succeeded ? null : merge.Error);

    public static ChorusGitPromoteResult Failure(string fromBranch, string toBranch, string error) =>
        new(false, fromBranch, toBranch, null, error);
}
