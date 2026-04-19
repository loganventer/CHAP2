using CHAP2.Application.Interfaces;

namespace CHAP2.Infrastructure.Git;

/// <summary>
/// Walks up from a start path looking for a directory that contains a
/// `.git` entry (directory or file, to support worktrees). Pure
/// filesystem work — no subprocess calls.
/// </summary>
public sealed class GitRepositoryLocator : IGitRepositoryLocator
{
    public string? FindRepoRoot(string startPath)
    {
        if (string.IsNullOrWhiteSpace(startPath)) return null;

        var absolute = Path.IsPathRooted(startPath)
            ? startPath
            : Path.Combine(Directory.GetCurrentDirectory(), startPath);

        var dir = new DirectoryInfo(absolute);
        while (dir != null)
        {
            var gitPath = Path.Combine(dir.FullName, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }
}
