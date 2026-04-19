namespace CHAP2.Application.Interfaces;

/// <summary>
/// Resource-access abstraction over the `git` CLI. Runs a single git
/// subcommand in a given working directory and returns stdout/stderr/exit.
/// Implementations must not throw on non-zero exit codes — callers decide
/// how to treat them.
/// </summary>
public interface IGitCommandRunner
{
    Task<GitCommandResult> RunAsync(string workingDirectory, params string[] args);
}

public sealed record GitCommandResult(int ExitCode, string StdOut, string StdErr);
