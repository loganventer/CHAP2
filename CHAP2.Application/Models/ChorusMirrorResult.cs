namespace CHAP2.Application.Models;

/// <summary>
/// Outcome of one mirror cycle (local disk -> GitHub).
/// </summary>
public sealed record ChorusMirrorResult(
    int Created,
    int Updated,
    int Deleted,
    string? CommitSha,
    string? Error)
{
    public static ChorusMirrorResult NoChange(string? sha) => new(0, 0, 0, sha, null);
    public static ChorusMirrorResult Failure(string error) => new(0, 0, 0, null, error);
    public bool Succeeded => Error is null;
    public int TotalChanges => Created + Updated + Deleted;
}
