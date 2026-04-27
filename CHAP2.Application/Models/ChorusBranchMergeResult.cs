namespace CHAP2.Application.Models;

public enum ChorusBranchMergeStatus
{
    Merged = 0,
    AlreadyUpToDate = 1,
    Conflict = 2,
    Failed = 3,
}

/// <summary>
/// Outcome of a GitHub server-side merge of one branch into another.
/// Mirrors the responses of POST /repos/{owner}/{repo}/merges:
///   201 -> Merged + commit SHA
///   204 -> AlreadyUpToDate
///   409 -> Conflict
///   404 / other -> Failed with error
/// </summary>
public sealed record ChorusBranchMergeResult(
    ChorusBranchMergeStatus Status,
    string? MergeCommitSha,
    string? Error)
{
    public bool Succeeded => Status is ChorusBranchMergeStatus.Merged or ChorusBranchMergeStatus.AlreadyUpToDate;

    public static ChorusBranchMergeResult Merged(string commitSha) => new(ChorusBranchMergeStatus.Merged, commitSha, null);
    public static ChorusBranchMergeResult AlreadyUpToDate() => new(ChorusBranchMergeStatus.AlreadyUpToDate, null, null);
    public static ChorusBranchMergeResult Conflict(string detail) => new(ChorusBranchMergeStatus.Conflict, null, detail);
    public static ChorusBranchMergeResult Failure(string error) => new(ChorusBranchMergeStatus.Failed, null, error);
}
