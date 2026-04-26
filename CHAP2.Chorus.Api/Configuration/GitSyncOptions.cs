namespace CHAP2.Chorus.Api.Configuration;

/// <summary>
/// Bound to the "GitSync" config section. Default Enabled=false so a
/// dev container / local run never tries to push -- only render.yaml
/// (or an explicit local override) flips it on.
/// </summary>
public sealed class GitSyncOptions
{
    public bool Enabled { get; set; } = false;

    /// <summary>HH:mm UTC time-of-day for the daily sync (e.g. "03:00").</summary>
    public string ScheduleUtc { get; set; } = "03:00";

    /// <summary>
    /// Absolute path on disk where the git working tree (the cloned
    /// repo) lives. The actual chorus JSONs live one level deeper at
    /// <c>{DataDirectory}/{SparseCheckoutPath}</c>; ChorusResource
    /// points there.
    /// </summary>
    public string DataDirectory { get; set; } = "data/chorus";

    /// <summary>
    /// Subset of the repo we actually need on disk -- the rest is
    /// excluded via sparse checkout so the working tree stays small.
    /// Empty string means "everything" (sparse disabled).
    /// </summary>
    public string SparseCheckoutPath { get; set; } = "data/chorus";

    /// <summary>Absolute path inside the image for fallback seeding when Enabled=false.</summary>
    public string ImageSeedDirectory { get; set; } = "/app/data/chorus";

    public string RemoteUrl { get; set; } = "https://github.com/loganventer/CHAP2.git";
    public string Branch { get; set; } = "main";
    public string AuthorName { get; set; } = "CHAP2 API";
    public string AuthorEmail { get; set; } = "chap2-api@noreply.local";

    /// <summary>
    /// GitHub PAT with contents:write on the repo. Bound from an env
    /// var (typically GITHUB_TOKEN). Injected into the HTTPS remote URL
    /// only at clone/push time -- never logged.
    /// </summary>
    public string? GitHubToken { get; set; }
}
