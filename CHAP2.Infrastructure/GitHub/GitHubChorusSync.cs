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
        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(entry.Path);
            var localPath = Path.Combine(localDirectory, fileName);
            if (File.Exists(localPath))
            {
                var existing = await File.ReadAllBytesAsync(localPath, cancellationToken);
                if (GitBlobHasher.Compute(existing) == entry.Sha) continue;   // already up-to-date
            }
            var content = await DownloadBlobAsync(entry.Sha, cancellationToken);
            await File.WriteAllBytesAsync(localPath, content, cancellationToken);
        }
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
        var doc = await SendJsonAsync(req, ct);
        return doc.RootElement.GetProperty("object").GetProperty("sha").GetString()!;
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

    private async Task<byte[]> DownloadBlobAsync(string blobSha, CancellationToken ct)
    {
        using var req = NewRequest(HttpMethod.Get, $"/repos/{_owner}/{_repo}/git/blobs/{blobSha}");
        var doc = await SendJsonAsync(req, ct);
        var encoded = doc.RootElement.GetProperty("content").GetString() ?? string.Empty;
        var encoding = doc.RootElement.GetProperty("encoding").GetString();
        if (encoding != "base64")
            throw new InvalidOperationException($"Unexpected GitHub blob encoding: {encoding}");
        return Convert.FromBase64String(encoded.Replace("\n", string.Empty));
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
