using System.Diagnostics;
using CHAP2.Application.Interfaces;

namespace CHAP2.Infrastructure.Git;

/// <summary>
/// Resource-access implementation of <see cref="IGitCommandRunner"/>.
/// Wraps Process.Start for the `git` CLI. Auth (SSH key, credential
/// helper, PAT) is inherited from the host git configuration.
/// </summary>
public sealed class GitCommandRunner : IGitCommandRunner
{
    public async Task<GitCommandResult> RunAsync(string workingDirectory, params string[] args)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = new Process { StartInfo = psi };
        proc.Start();
        var stdout = await proc.StandardOutput.ReadToEndAsync();
        var stderr = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();
        return new GitCommandResult(proc.ExitCode, stdout, stderr);
    }
}
