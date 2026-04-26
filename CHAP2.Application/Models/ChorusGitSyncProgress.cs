namespace CHAP2.Application.Models;

/// <summary>
/// One progress event emitted while a chorus git sync is running.
/// Consumers (the background worker logs them; the SSE force-sync
/// endpoint forwards them to the browser) treat these as fire-and-forget.
/// </summary>
public sealed record ChorusGitSyncProgress(
    ChorusGitSyncStage Stage,
    string Message,
    DateTime AtUtc);
