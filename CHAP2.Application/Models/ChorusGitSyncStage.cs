namespace CHAP2.Application.Models;

/// <summary>
/// Discrete phases the chorus git-sync workflow moves through. Reported
/// via <see cref="ChorusGitSyncProgress"/> so a force-sync UI can render
/// a progress bar.
/// </summary>
public enum ChorusGitSyncStage
{
    Pulling,
    Staging,
    Committing,
    Pushing,
    Done,
    Skipped,
    Failed,
}
