namespace CHAP2.Application.Models;

public enum ChorusFilePushStatus
{
    Created = 0,
    Updated = 1,
    Deleted = 2,
    NoChange = 3,
    NotFound = 4,
    Failed = 5,
}

/// <summary>
/// Outcome of pushing a single chorus file to GitHub via the per-edit
/// path. NoChange means the remote already matched (idempotent retry);
/// NotFound means the file didn't exist on either side (delete no-op).
/// </summary>
public sealed record ChorusFilePushResult(
    ChorusFilePushStatus Status,
    string? CommitSha,
    string? Error)
{
    public bool Succeeded => Status is ChorusFilePushStatus.Created
        or ChorusFilePushStatus.Updated
        or ChorusFilePushStatus.Deleted
        or ChorusFilePushStatus.NoChange
        or ChorusFilePushStatus.NotFound;

    public static ChorusFilePushResult Created(string sha) => new(ChorusFilePushStatus.Created, sha, null);
    public static ChorusFilePushResult Updated(string sha) => new(ChorusFilePushStatus.Updated, sha, null);
    public static ChorusFilePushResult Deleted(string sha) => new(ChorusFilePushStatus.Deleted, sha, null);
    public static ChorusFilePushResult NoChange() => new(ChorusFilePushStatus.NoChange, null, null);
    public static ChorusFilePushResult NotFound() => new(ChorusFilePushStatus.NotFound, null, null);
    public static ChorusFilePushResult Failure(string error) => new(ChorusFilePushStatus.Failed, null, error);
}
