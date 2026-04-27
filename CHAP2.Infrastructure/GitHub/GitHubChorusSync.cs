using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CHAP2.Application.Helpers;
using CHAP2.Application.Interfaces;
using CHAP2.Application.Models;
using Microsoft.Extensions.Logging;

namespace CHAP2.Infrastructure.GitHub;

/// <summary>
/// Mirrors a local chorus directory to a GitHub branch via the Git Data
/// API: one commit per sync regardless of file count. No git binary,
/// no .git on disk -- just HTTPS.
///
/// Composition over inheritance: this class composes <see cref="HttpClient"/>
/// for transport and <see cref="GitBlobHasher"/> for SHA computation.
/// Single responsibility: the GitHub-side mirror; the orchestrator
/// owns the schedule, the bootstrapper owns first-start setup.
/// </summary>
public sealed class GitHubChorusSync : IChorusGitHubSync
{
    private const string ApiBase = "https://api.github.com";

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly string _owner;
    private readonly string _repo;
    private readonly string _branch;
    private readonly string? _autoCreateFrom;      // when _branch 404s, create it from this base
    private readonly string _remotePathPrefix;     // e.g. "data/chorus"
    private readonly string _authorName;
    private readonly string _authorEmail;
    private readonly Func<string?> _readToken;     // late-binds the PAT so a UI rotation takes effect immediately
    private readonly ILogger<GitHubChorusSync> _logger;

    public GitHubChorusSync(
        HttpClient httpClient,
        string owner,
        string repo,
        string branch,
        string? autoCreateFrom,
        string remotePathPrefix,
        string authorName,
        string authorEmail,
        Func<string?> tokenAccessor,
        ILogger<GitHubChorusSync> logger)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _owner = !string.IsNullOrWhiteSpace(owner) ? owner : throw new ArgumentException("owner required", nameof(owner));
        _repo = !string.IsNullOrWhiteSpace(repo) ? repo : throw new ArgumentException("repo required", nameof(repo));
        _branch = !string.IsNullOrWhiteSpace(branch) ? branch : "main";
        _autoCreateFrom = string.IsNullOrWhiteSpace(autoCreateFrom) || string.Equals(autoCreateFrom, _branch, StringComparison.Ordinal)
            ? null
            : autoCreateFrom;
        _remotePathPrefix = (remotePathPrefix ?? "").Trim('/');
        _authorName = !string.IsNullOrWhiteSpace(authorName) ? authorName : "CHAP2 API";
        _authorEmail = !string.IsNullOrWhiteSpace(authorEmail) ? authorEmail : "chap2-api@noreply.local";
        _readToken = tokenAccessor ?? throw new ArgumentNullException(nameof(tokenAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ---------- public surface ------------------------------------------------

    public async Task BootstrapAsync(string localDirectory, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(localDirectory);

        var refSha = await GetBranchHeadAsync(cancellationToken);
        var treeSha = await GetCommitTreeAsync(refSha, cancellationToken);
        var entries = await ListChorusTreeAsync(treeSha, cancellationToken);

        _logger.LogInformation("Bootstrapping {Count} chorus file(s) from GitHub into {Path}", entries.Count, localDirectory);
        var downloaded = 0;
        var skipped = 0;
        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(entry.Path);
            var localPath = Path.Combine(localDirectory, fileName);
            if (File.Exists(localPath))
            {
                var existing = await File.ReadAllBytesAsync(localPath, cancellationToken);
                if (GitBlobHasher.Compute(existing) == entry.Sha) { skipped++; continue; }
            }
            // Download from raw.githubusercontent.com (CDN). This does not
            // count against the GitHub REST API rate limit (60/hr anon,
            // 5000/hr authed), which would otherwise throttle us out
            // around the 60th file on a fresh disk before a PAT is set.
            var content = await DownloadRawAsync(entry.Path, cancellationToken);
            await File.WriteAllBytesAsync(localPath, content, cancellationToken);
            downloaded++;
        }
        _logger.LogInformation("Bootstrap complete: {Downloaded} downloaded, {Skipped} already current", downloaded, skipped);
    }

    public async Task<ChorusMirrorResult> MirrorAsync(
        string localDirectory,
        string commitMessage,
        IProgress<ChorusGitSyncProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        Report(progress, ChorusGitSyncStage.Pulling, "Reading remote tree");
        var refSha = await GetBranchHeadAsync(cancellationToken);
        var treeSha = await GetCommitTreeAsync(refSha, cancellationToken);
        var remote = (await ListChorusTreeAsync(treeSha, cancellationToken))
            .ToDictionary(e => Path.GetFileName(e.Path), StringComparer.Ordinal);

        Report(progress, ChorusGitSyncStage.Staging, "Comparing with local");
        var local = Directory.Exists(localDirectory)
            ? Directory.EnumerateFiles(localDirectory, "*.json", SearchOption.TopDirectoryOnly).ToList()
            : new List<string>();

        // Safety / self-heal: if the disk is empty but the remote has
        // files, the previous bootstrap almost certainly failed (rate
        // limit, no PAT yet, etc). Mirroring in this state would push
        // a "delete every chorus" commit and wipe the repo. Instead,
        // pull everything and exit -- the daily cycle will mirror normally
        // once the disk is populated.
        if (local.Count == 0 && remote.Count > 0)
        {
            _logger.LogWarning(
                "Disk is empty but GitHub has {Count} chorus file(s); refusing to mirror as deletes -- bootstrapping instead",
                remote.Count);
            Report(progress, ChorusGitSyncStage.Pulling,
                $"Disk empty; bootstrapping {remote.Count} file(s) from GitHub");
            await BootstrapAsync(localDirectory, cancellationToken);
            Report(progress, ChorusGitSyncStage.Done, $"Bootstrapped {remote.Count} file(s); nothing to push");
            return ChorusMirrorResult.NoChange(refSha);
        }

        var changes = new List<TreeChange>();
        var created = 0;
        var updated = 0;

        foreach (var path in local)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(path);
            var content = await File.ReadAllBytesAsync(path, cancellationToken);
            var localSha = GitBlobHasher.Compute(content);

            if (remote.TryGetValue(fileName, out var remoteEntry))
            {
                if (remoteEntry.Sha == localSha) { remote.Remove(fileName); continue; } // unchanged
                var blobSha = await CreateBlobAsync(content, cancellationToken);
                changes.Add(TreeChange.Upsert($"{_remotePathPrefix}/{fileName}", blobSha));
                updated++;
                remote.Remove(fileName);
            }
            else
            {
                var blobSha = await CreateBlobAsync(content, cancellationToken);
                changes.Add(TreeChange.Upsert($"{_remotePathPrefix}/{fileName}", blobSha));
                created++;
            }
        }

        // Whatever's left in `remote` exists on GitHub but not on disk -> delete remotely.
        var deleted = 0;
        foreach (var leftover in remote.Values)
        {
            changes.Add(TreeChange.Delete($"{_remotePathPrefix}/{leftover.Path.Split('/').Last()}"));
            deleted++;
        }

        if (changes.Count == 0)
        {
            Report(progress, ChorusGitSyncStage.Done, "Already up to date");
            return ChorusMirrorResult.NoChange(refSha);
        }

        Report(progress, ChorusGitSyncStage.Committing, $"Committing {changes.Count} change(s)");
        var newTreeSha = await CreateTreeAsync(treeSha, changes, cancellationToken);
        var newCommitSha = await CreateCommitAsync(newTreeSha, refSha, commitMessage, cancellationToken);

        Report(progress, ChorusGitSyncStage.Pushing, "Updating branch");
        await UpdateBranchAsync(newCommitSha, force: true, cancellationToken);

        Report(progress, ChorusGitSyncStage.Done,
            $"Pushed {created} new, {updated} updated, {deleted} deleted");
        return new ChorusMirrorResult(created, updated, deleted, newCommitSha, null);
    }

    // ---------- GitHub API plumbing -------------------------------------------

    private HttpRequestMessage NewRequest(HttpMethod method, string relativeUrl)
    {
        var req = new HttpRequestMessage(method, ApiBase + relativeUrl);
        req.Headers.Accept.ParseAdd("application/vnd.github+json");
        req.Headers.UserAgent.ParseAdd("CHAP2-API/1.0");
        req.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        var token = _readToken();
        if (!string.IsNullOrWhiteSpace(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    private async Task<string> GetBranchHeadAsync(CancellationToken ct)
    {
        using var req = NewRequest(HttpMethod.Get, $"/repos/{_owner}/{_repo}/git/refs/heads/{_branch}");
        using var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (resp.IsSuccessStatusCode)
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("object").GetProperty("sha").GetString()!;
        }

        // First-time auto-bootstrap: if the edits branch doesn't exist yet
        // and we know what to base it on (typically MainBranch), create it
        // server-side instead of failing the sync.
        if ((int)resp.StatusCode == 404 && _autoCreateFrom is not null)
        {
            _logger.LogInformation(
                "Branch {Branch} not found on remote; creating it from {Base}.", _branch, _autoCreateFrom);
            return await CreateBranchFromAsync(_autoCreateFrom, ct);
        }

        throw new InvalidOperationException(
            $"GitHub API GET refs/heads/{_branch} returned {(int)resp.StatusCode}: {Truncate(body, 400)}");
    }

    private async Task<string> CreateBranchFromAsync(string baseBranch, CancellationToken ct)
    {
        using var headReq = NewRequest(HttpMethod.Get, $"/repos/{_owner}/{_repo}/git/refs/heads/{baseBranch}");
        var headDoc = await SendJsonAsync(headReq, ct);
        var baseSha = headDoc.RootElement.GetProperty("object").GetProperty("sha").GetString()!;

        using var createReq = NewRequest(HttpMethod.Post, $"/repos/{_owner}/{_repo}/git/refs");
        createReq.Content = JsonContent.Create(new
        {
            @ref = $"refs/heads/{_branch}",
            sha = baseSha,
        }, options: JsonOpts);
        await SendJsonAsync(createReq, ct);

        _logger.LogInformation("Created branch {Branch} from {Base} at {Sha}", _branch, baseBranch, baseSha);
        return baseSha;
    }

    private async Task<string> GetCommitTreeAsync(string commitSha, CancellationToken ct)
    {
        using var req = NewRequest(HttpMethod.Get, $"/repos/{_owner}/{_repo}/git/commits/{commitSha}");
        var doc = await SendJsonAsync(req, ct);
        return doc.RootElement.GetProperty("tree").GetProperty("sha").GetString()!;
    }

    private async Task<List<RemoteTreeEntry>> ListChorusTreeAsync(string rootTreeSha, CancellationToken ct)
    {
        using var req = NewRequest(HttpMethod.Get, $"/repos/{_owner}/{_repo}/git/trees/{rootTreeSha}?recursive=1");
        var doc = await SendJsonAsync(req, ct);
        var entries = new List<RemoteTreeEntry>();
        foreach (var item in doc.RootElement.GetProperty("tree").EnumerateArray())
        {
            var type = item.GetProperty("type").GetString();
            if (type != "blob") continue;
            var path = item.GetProperty("path").GetString()!;
            if (!path.StartsWith(_remotePathPrefix + "/", StringComparison.Ordinal)) continue;
            // Only top-level files inside the prefix (skip subdirs).
            if (path.IndexOf('/', _remotePathPrefix.Length + 1) >= 0) continue;
            entries.Add(new RemoteTreeEntry(path, item.GetProperty("sha").GetString()!));
        }
        return entries;
    }

    private async Task<byte[]> DownloadRawAsync(string repoPath, CancellationToken ct)
    {
        // raw.githubusercontent.com serves the file as-is, no API quota.
        // Public repos require no auth; private repos accept the same
        // Bearer token via the optional Authorization header.
        var url = $"https://raw.githubusercontent.com/{_owner}/{_repo}/{_branch}/{repoPath}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        var token = _readToken();
        if (!string.IsNullOrWhiteSpace(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"raw.githubusercontent.com GET {repoPath} returned {(int)resp.StatusCode}: {Truncate(body, 200)}");
        }
        return await resp.Content.ReadAsByteArrayAsync(ct);
    }

    private async Task<string> CreateBlobAsync(byte[] content, CancellationToken ct)
    {
        using var req = NewRequest(HttpMethod.Post, $"/repos/{_owner}/{_repo}/git/blobs");
        req.Content = JsonContent.Create(new
        {
            content = Convert.ToBase64String(content),
            encoding = "base64",
        }, options: JsonOpts);
        var doc = await SendJsonAsync(req, ct);
        return doc.RootElement.GetProperty("sha").GetString()!;
    }

    private async Task<string> CreateTreeAsync(string baseTreeSha, IEnumerable<TreeChange> changes, CancellationToken ct)
    {
        using var req = NewRequest(HttpMethod.Post, $"/repos/{_owner}/{_repo}/git/trees");
        req.Content = JsonContent.Create(new
        {
            base_tree = baseTreeSha,
            tree = changes.Select(c => c.IsDelete
                ? (object)new { path = c.Path, mode = "100644", type = "blob", sha = (string?)null }
                : new { path = c.Path, mode = "100644", type = "blob", sha = c.BlobSha }),
        }, options: JsonOpts);
        var doc = await SendJsonAsync(req, ct);
        return doc.RootElement.GetProperty("sha").GetString()!;
    }

    private async Task<string> CreateCommitAsync(string treeSha, string parentSha, string message, CancellationToken ct)
    {
        using var req = NewRequest(HttpMethod.Post, $"/repos/{_owner}/{_repo}/git/commits");
        req.Content = JsonContent.Create(new
        {
            message,
            tree = treeSha,
            parents = new[] { parentSha },
            author = new { name = _authorName, email = _authorEmail, date = DateTime.UtcNow.ToString("o") },
        }, options: JsonOpts);
        var doc = await SendJsonAsync(req, ct);
        return doc.RootElement.GetProperty("sha").GetString()!;
    }

    private async Task UpdateBranchAsync(string newCommitSha, bool force, CancellationToken ct)
    {
        using var req = NewRequest(HttpMethod.Patch, $"/repos/{_owner}/{_repo}/git/refs/heads/{_branch}");
        req.Content = JsonContent.Create(new { sha = newCommitSha, force }, options: JsonOpts);
        await SendJsonAsync(req, ct);
    }

    public async Task<ChorusBranchMergeResult> MergeIntoAsync(
        string targetBranch,
        string commitMessage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetBranch))
            return ChorusBranchMergeResult.Failure("targetBranch is required.");

        using var req = NewRequest(HttpMethod.Post, $"/repos/{_owner}/{_repo}/merges");
        req.Content = JsonContent.Create(new
        {
            @base = targetBranch,
            head = _branch,
            commit_message = commitMessage,
        }, options: JsonOpts);

        using var resp = await _http.SendAsync(req, cancellationToken);
        var body = await resp.Content.ReadAsStringAsync(cancellationToken);

        if ((int)resp.StatusCode == 201)
        {
            using var doc = JsonDocument.Parse(body);
            var sha = doc.RootElement.TryGetProperty("sha", out var shaProp) ? shaProp.GetString() : null;
            _logger.LogInformation("Merged {Head} into {Base} as {Sha}", _branch, targetBranch, sha);
            return ChorusBranchMergeResult.Merged(sha ?? string.Empty);
        }

        if ((int)resp.StatusCode == 204)
        {
            _logger.LogInformation("{Base} already contains {Head}; nothing to merge.", targetBranch, _branch);
            return ChorusBranchMergeResult.AlreadyUpToDate();
        }

        if ((int)resp.StatusCode == 409)
        {
            _logger.LogWarning("Merge {Head} -> {Base} returned conflict: {Body}", _branch, targetBranch, Truncate(body, 200));
            return ChorusBranchMergeResult.Conflict("Merge conflict — resolve on GitHub before retrying.");
        }

        var error = $"GitHub merge {_branch}->{targetBranch} returned {(int)resp.StatusCode}: {Truncate(body, 200)}";
        _logger.LogWarning(error);
        return ChorusBranchMergeResult.Failure(error);
    }

    private async Task<JsonDocument> SendJsonAsync(HttpRequestMessage req, CancellationToken ct)
    {
        using var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"GitHub API {req.Method} {req.RequestUri?.PathAndQuery} returned {(int)resp.StatusCode}: {Truncate(body, 400)}");
        return JsonDocument.Parse(body);
    }

    private static void Report(IProgress<ChorusGitSyncProgress>? sink, ChorusGitSyncStage stage, string message)
        => sink?.Report(new ChorusGitSyncProgress(stage, message, DateTime.UtcNow));

    private static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max) + "…";
}
