namespace CHAP2.Chorus.Api.Configuration;

/// <summary>
/// Bound to the "GitSync" config section. Drives the GitHub-API mirror
/// of /var/data chorus JSONs to a remote branch.
///
/// Default Enabled=false so a dev / offline-docker run never tries to
/// touch GitHub -- only render.yaml flips it on.
/// </summary>
public sealed class GitSyncOptions
{
    public bool Enabled { get; set; } = false;

    /// <summary>HH:mm UTC time-of-day for the daily mirror (e.g. "03:00").</summary>
    public string ScheduleUtc { get; set; } = "03:00";

    /// <summary>Absolute path on disk where chorus JSONs live (flat: {id}.json).</summary>
    public string DataDirectory { get; set; } = "data/chorus";

    /// <summary>Absolute path inside the image for fallback seeding when Enabled=false.</summary>
    public string ImageSeedDirectory { get; set; } = "/app/data/chorus";

    /// <summary>GitHub repo owner (e.g. "loganventer").</summary>
    public string Owner { get; set; } = "loganventer";

    /// <summary>GitHub repo name (e.g. "CHAP2").</summary>
    public string Repo { get; set; } = "CHAP2";

    /// <summary>Branch to mirror against.</summary>
    public string Branch { get; set; } = "main";

    /// <summary>
    /// Path within the repo where chorus JSONs live (e.g. "data/chorus").
    /// Local files at /var/data/{id}.json mirror to {RemotePathPrefix}/{id}.json.
    /// </summary>
    public string RemotePathPrefix { get; set; } = "data/chorus";

    public string AuthorName { get; set; } = "CHAP2 API";
    public string AuthorEmail { get; set; } = "chap2-api@noreply.local";

    /// <summary>
    /// GitHub PAT with contents:write on the repo. Bound from an env
    /// var (typically GITHUB_TOKEN). Sent as a Bearer header on every
    /// API call -- never logged.
    /// </summary>
    public string? GitHubToken { get; set; }
}
