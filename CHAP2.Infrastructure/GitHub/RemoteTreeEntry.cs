namespace CHAP2.Infrastructure.GitHub;

/// <summary>One file in a GitHub tree response (path within the repo + git blob sha).</summary>
internal sealed record RemoteTreeEntry(string Path, string Sha);
