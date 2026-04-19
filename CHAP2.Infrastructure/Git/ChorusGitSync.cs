using CHAP2.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CHAP2.Infrastructure.Git;

/// <summary>
/// Orchestrator: after a chorus is added/updated/deleted, compose a commit
/// message, stage the file, commit, and push. Serialises concurrent
/// saves through a single semaphore. Delegates subprocess work to
/// <see cref="IGitCommandRunner"/> and repo discovery to
/// <see cref="IGitRepositoryLocator"/>.
///
/// No-ops silently when the data folder isn't inside a git working tree
/// (Docker / Render runtime) so the HTTP path remains unaffected there.
/// Errors are logged, never thrown.
/// </summary>
public sealed class ChorusGitSync : IChorusGitSync, IDisposable
{
    private readonly string _dataFolder;
    private readonly IGitCommandRunner _git;
    private readonly IGitRepositoryLocator _locator;
    private readonly ILogger<ChorusGitSync> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string? _repoRoot;

    public ChorusGitSync(
        string dataFolder,
        IGitCommandRunner git,
        IGitRepositoryLocator locator,
        ILogger<ChorusGitSync> logger)
    {
        _dataFolder = Path.IsPathRooted(dataFolder)
            ? dataFolder
            : Path.Combine(Directory.GetCurrentDirectory(), dataFolder);
        _git = git;
        _locator = locator;
        _logger = logger;
        _repoRoot = _locator.FindRepoRoot(_dataFolder);

        if (_repoRoot == null)
        {
            _logger.LogInformation(
                "ChorusGitSync disabled: no .git found above {DataFolder}", _dataFolder);
        }
    }

    public void EnqueueUpsert(Guid chorusId, string chorusName)
        => Enqueue(chorusId, chorusName, deleted: false);

    public void EnqueueDelete(Guid chorusId, string chorusName)
        => Enqueue(chorusId, chorusName, deleted: true);

    private void Enqueue(Guid chorusId, string chorusName, bool deleted)
    {
        if (_repoRoot == null) return;

        _ = Task.Run(async () =>
        {
            try { await SyncAsync(chorusId, chorusName, deleted); }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "ChorusGitSync failed for chorus {ChorusId} ({ChorusName})", chorusId, chorusName);
            }
        });
    }

    private async Task SyncAsync(Guid chorusId, string chorusName, bool deleted)
    {
        await _gate.WaitAsync();
        try
        {
            var relPath = Path.GetRelativePath(_repoRoot!, Path.Combine(_dataFolder, $"{chorusId}.json"))
                .Replace(Path.DirectorySeparatorChar, '/');
            var action = deleted ? "Delete" : "Update";
            var safeName = (chorusName ?? string.Empty).Replace('\n', ' ').Replace('\r', ' ');

            var stage = await _git.RunAsync(_repoRoot!, "add", "--", relPath);
            if (stage.ExitCode != 0)
            {
                _logger.LogWarning("git add failed for {Path}: {StdErr}", relPath, stage.StdErr);
                return;
            }

            var commit = await _git.RunAsync(_repoRoot!, "commit", "-m",
                $"{action} chorus: {safeName} ({chorusId})");
            if (commit.ExitCode != 0)
            {
                if (commit.StdOut.Contains("nothing to commit", StringComparison.OrdinalIgnoreCase)
                    || commit.StdErr.Contains("nothing to commit", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("No changes to commit for chorus {ChorusId}", chorusId);
                    return;
                }
                _logger.LogWarning("git commit failed for {ChorusId}: {StdErr}", chorusId, commit.StdErr);
                return;
            }

            var push = await _git.RunAsync(_repoRoot!, "push");
            if (push.ExitCode != 0)
            {
                _logger.LogWarning("git push failed for {ChorusId}: {StdErr}", chorusId, push.StdErr);
                return;
            }

            _logger.LogInformation("Git-synced chorus {ChorusId} ({Action}: {ChorusName})",
                chorusId, action, safeName);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();
}
