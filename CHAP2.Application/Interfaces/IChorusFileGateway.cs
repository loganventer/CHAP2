namespace CHAP2.Application.Interfaces;

/// <summary>
/// Read-only access to the local on-disk JSON for a single chorus.
/// Lets Application-tier handlers (e.g. the per-edit GitHub push handler)
/// pull the exact bytes the daily mirror would push, without hard-coding
/// disk paths.
/// </summary>
public interface IChorusFileGateway
{
    /// <summary>The on-disk file name for a chorus, e.g. "{id}.json".</summary>
    string GetFileName(Guid chorusId);

    /// <summary>Returns the file's bytes, or null if the file is not on disk.</summary>
    Task<byte[]?> ReadAsync(Guid chorusId, CancellationToken cancellationToken = default);
}
