namespace CHAP2.Application.Interfaces;

/// <summary>
/// Locates the git working-tree root that contains a given path.
/// Returns null when the path is not inside a git repo (e.g. Docker /
/// Render runtime where only the data folder is mounted).
/// </summary>
public interface IGitRepositoryLocator
{
    string? FindRepoRoot(string startPath);
}
