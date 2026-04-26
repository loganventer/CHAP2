using CHAP2.Application.Helpers;
using CHAP2.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CHAP2.Infrastructure.Git;

/// <summary>
/// Concrete <see cref="IGitWorkingTree"/> for a single chorus data
/// directory. Composes <see cref="IGitCommandRunner"/> for the actual
/// process plumbing -- this class only knows the git command sequences
/// for each operation and how to inject the PAT into the remote URL
/// for clone / push.
/// </summary>
public sealed class GitWorkingTree : IGitWorkingTree
{
    private readonly string _treePath;
    private readonly string _remoteUrlPlain;
    private readonly string _branch;
    private readonly string _authorName;
    private readonly string _authorEmail;
    private readonly string? _githubToken;
    private readonly string? _sparsePath;
    private readonly IGitCommandRunner _runner;
    private readonly ILogger<GitWorkingTree> _logger;

    public GitWorkingTree(
        string treePath,
        string remoteUrl,
        string branch,
        string authorName,
        string authorEmail,
        string? githubToken,
        string? sparseCheckoutPath,
        IGitCommandRunner runner,
        ILogger<GitWorkingTree> logger)
    {
        _treePath = !string.IsNullOrWhiteSpace(treePath)
            ? treePath
            : throw new ArgumentException("treePath required", nameof(treePath));
        _remoteUrlPlain = !string.IsNullOrWhiteSpace(remoteUrl)
            ? remoteUrl
            : throw new ArgumentException("remoteUrl required", nameof(remoteUrl));
        _branch = !string.IsNullOrWhiteSpace(branch) ? branch : "main";
        _authorName = !string.IsNullOrWhiteSpace(authorName) ? authorName : "CHAP2 API";
        _authorEmail = !string.IsNullOrWhiteSpace(authorEmail) ? authorEmail : "chap2-api@noreply.local";
        _githubToken = string.IsNullOrWhiteSpace(githubToken) ? null : githubToken;
        _sparsePath = string.IsNullOrWhiteSpace(sparseCheckoutPath) ? null : sparseCheckoutPath;
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<bool> IsClonedAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Directory.Exists(Path.Combine(_treePath, ".git")));

    public async Task EnsureCloneAsync(CancellationToken cancellationToken = default)
    {
        if (await IsClonedAsync(cancellationToken)) return;

        // Clone into a parent directory so git creates the .git folder
        // at the expected path. If the target directory already exists
        // and isn't empty, clone into a temp dir and move .git over so
        // any seeded files survive.
        Directory.CreateDirectory(_treePath);
        var hasContent = Directory.EnumerateFileSystemEntries(_treePath).Any();
        if (!hasContent)
        {
            await CloneInto(_treePath, cancellationToken);
            return;
        }

        var temp = Path.Combine(Path.GetTempPath(), "chap2-bootstrap-" + Guid.NewGuid().ToString("N"));
        try
        {
            await CloneInto(temp, cancellationToken);
            // Adopt the cloned .git so the existing working tree becomes
            // a working copy. Repository contents take precedence over
            // any locally-seeded duplicates with the same path.
            var sourceGit = Path.Combine(temp, ".git");
            var targetGit = Path.Combine(_treePath, ".git");
            if (Directory.Exists(targetGit)) Directory.Delete(targetGit, recursive: true);
            Directory.Move(sourceGit, targetGit);
            // Reset working tree to match HEAD (overwrites any local
            // edits that conflict with the clone -- cleanly bootstraps
            // the disk to the remote state).
            var checkout = await _runner.RunAsync(_treePath, "checkout", "--force", _branch);
            if (checkout.ExitCode != 0) ThrowGit("checkout", checkout);
        }
        finally
        {
            if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        }
    }

    public async Task PullLocalWinsAsync(CancellationToken cancellationToken = default)
    {
        await ConfigureIdentity();
        await ConfigureRemote();
        var pull = await _runner.RunAsync(_treePath, "pull", "--no-rebase", "-X", "ours", "origin", _branch);
        if (pull.ExitCode != 0)
            ThrowGit("pull", pull);
    }

    public async Task StageAllAsync(CancellationToken cancellationToken = default)
    {
        var add = await _runner.RunAsync(_treePath, "add", "-A");
        if (add.ExitCode != 0) ThrowGit("add", add);
    }

    public async Task<bool> HasStagedChangesAsync(CancellationToken cancellationToken = default)
    {
        // Exit code 1 from `git diff --cached --quiet` means "yes, there are changes".
        var diff = await _runner.RunAsync(_treePath, "diff", "--cached", "--quiet");
        return diff.ExitCode == 1;
    }

    public async Task<int> StagedFileCountAsync(CancellationToken cancellationToken = default)
    {
        var names = await _runner.RunAsync(_treePath, "diff", "--cached", "--name-only");
        if (names.ExitCode != 0) ThrowGit("diff", names);
        if (string.IsNullOrWhiteSpace(names.StdOut)) return 0;
        return names.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    public async Task CommitAsync(string message, CancellationToken cancellationToken = default)
    {
        await ConfigureIdentity();
        var commit = await _runner.RunAsync(_treePath, "commit", "-m", message);
        if (commit.ExitCode != 0) ThrowGit("commit", commit);
    }

    public async Task PushAsync(CancellationToken cancellationToken = default)
    {
        await ConfigureRemote();
        var push = await _runner.RunAsync(_treePath, "push", "origin", $"HEAD:{_branch}");
        if (push.ExitCode != 0) ThrowGit("push", push);
    }

    public async Task<bool> LocalIsAheadOfRemoteAsync(CancellationToken cancellationToken = default)
    {
        var fetch = await _runner.RunAsync(_treePath, "fetch", "origin", _branch);
        if (fetch.ExitCode != 0) ThrowGit("fetch", fetch);
        // git rev-list --count origin/main..HEAD == number of local-only commits.
        var rev = await _runner.RunAsync(_treePath, "rev-list", "--count", $"origin/{_branch}..HEAD");
        if (rev.ExitCode != 0) ThrowGit("rev-list", rev);
        return int.TryParse(rev.StdOut.Trim(), out var count) && count > 0;
    }

    private async Task CloneInto(string targetDir, CancellationToken cancellationToken)
    {
        // Sparse + blobless: keeps the working tree to just the slice
        // we mutate (data/chorus) and the .git store to refs+trees only
        // (no blob history). Disk footprint stays a few MB even though
        // the source repo is much larger.
        var args = new List<string>
        {
            "clone",
            "--depth", "1",
            "--filter=blob:none",
            "--sparse",
            "--branch", _branch,
            BuildAuthRemoteUrl(),
            targetDir,
        };
        var result = await _runner.RunAsync(Directory.GetParent(targetDir)?.FullName ?? "/", args.ToArray());
        if (result.ExitCode != 0) ThrowGit("clone", result);

        if (_sparsePath is not null)
        {
            var sparse = await _runner.RunAsync(targetDir, "sparse-checkout", "set", _sparsePath);
            if (sparse.ExitCode != 0) ThrowGit("sparse-checkout", sparse);
        }
    }

    private async Task ConfigureIdentity()
    {
        await _runner.RunAsync(_treePath, "config", "user.name", _authorName);
        await _runner.RunAsync(_treePath, "config", "user.email", _authorEmail);
    }

    private async Task ConfigureRemote()
    {
        // Reset origin to the auth-injected URL on every push/pull so a
        // rotated PAT takes effect on the next sync without restart.
        await _runner.RunAsync(_treePath, "remote", "set-url", "origin", BuildAuthRemoteUrl());
    }

    private string BuildAuthRemoteUrl()
    {
        if (_githubToken is null) return _remoteUrlPlain;
        // https://x-access-token:TOKEN@github.com/owner/repo.git
        var ix = _remoteUrlPlain.IndexOf("://", StringComparison.Ordinal);
        if (ix < 0) return _remoteUrlPlain;
        var prefix = _remoteUrlPlain.Substring(0, ix + 3);
        var rest = _remoteUrlPlain.Substring(ix + 3);
        return $"{prefix}x-access-token:{_githubToken}@{rest}";
    }

    private void ThrowGit(string operation, GitCommandResult result)
    {
        var combined = string.IsNullOrWhiteSpace(result.StdErr) ? result.StdOut : result.StdErr;
        var sanitized = SecretMask.Apply(combined ?? string.Empty).Trim();
        throw new InvalidOperationException($"git {operation} failed (exit {result.ExitCode}): {sanitized}");
    }
}
